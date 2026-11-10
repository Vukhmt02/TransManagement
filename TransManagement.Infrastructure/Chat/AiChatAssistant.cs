using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransManagement.Application.Chat;
using TransManagement.Application.Common.Models;
using TransManagement.Application.Transport;
using TransManagement.Domain.Entities;
using TransManagement.Domain.Enums;
using TransManagement.Infrastructure.Identity;
using TransManagement.Infrastructure.Persistence;

namespace TransManagement.Infrastructure.Chat;

public sealed class AiChatAssistant(
    ApplicationDbContext dbContext,
    ITransportService transportService,
    IOptions<AiAssistantOptions> options,
    ILogger<AiChatAssistant> logger) : IAiChatAssistant
{
    public static readonly Guid BotUserId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
    private const string BotEmail = "ai-assistant@transflow.local";

    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly Error DisabledError = new("Chat.AiDisabled", "AI assistant is disabled.");
    private static readonly Error SkipReplyError = new("Chat.AiSkipped", "Conversation is already assigned or closed.");

    private readonly AiAssistantOptions _options = options.Value;

    public async Task<Result<ChatMessageDto>> GenerateCustomerReplyAsync(
        Guid customerUserId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return Result<ChatMessageDto>.Failure(DisabledError);

        var conversation = await dbContext.Conversations
            .Include(item => item.Messages)
            .SingleOrDefaultAsync(
                item => item.Id == conversationId && item.CustomerUserId == customerUserId,
                cancellationToken);

        if (conversation is null ||
            conversation.Status != ConversationStatus.Open ||
            conversation.AssignedAdminUserId.HasValue)
        {
            return Result<ChatMessageDto>.Failure(SkipReplyError);
        }

        var latestCustomerMessage = conversation.Messages
            .Where(item => item.SenderUserId == customerUserId && item.MessageType == ChatMessageType.Text)
            .OrderByDescending(item => item.SentAtUtc)
            .FirstOrDefault();

        if (latestCustomerMessage is null)
            return Result<ChatMessageDto>.Failure(SkipReplyError);

        await EnsureBotUserExistsAsync(cancellationToken);

        var orders = await transportService.GetCustomerOrdersAsync(customerUserId, cancellationToken);
        var customerResult = await transportService.GetCustomerByUserIdAsync(customerUserId, cancellationToken);
        var customerName = customerResult.IsSuccess ? customerResult.Value.Name : "Quý khách";

        var replyText = await TryGenerateExternalLlmReplyAsync(
                            customerName,
                            conversation,
                            orders,
                            cancellationToken)
                        ?? await BuildDomainSmartReplyAsync(
                            customerUserId,
                            customerName,
                            conversation.Subject,
                            latestCustomerMessage.Content,
                            orders,
                            cancellationToken);

        if (replyText.Length > 2000)
            replyText = replyText[..1997] + "...";

        var aiMessage = new ChatMessage(
            conversationId,
            BotUserId,
            replyText,
            DateTime.UtcNow,
            ChatMessageType.AiAssistant);

        dbContext.ChatMessages.Add(aiMessage);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ChatMessageDto>.Success(new ChatMessageDto(
            aiMessage.Id,
            aiMessage.ConversationId,
            aiMessage.SenderUserId,
            _options.BotName,
            aiMessage.Content,
            aiMessage.SentAtUtc,
            aiMessage.ReadAtUtc,
            aiMessage.MessageType));
    }

    private async Task EnsureBotUserExistsAsync(CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == BotUserId, cancellationToken);
        if (exists) return;

        var botUser = new ApplicationUser
        {
            Id = BotUserId,
            UserName = BotEmail,
            NormalizedUserName = BotEmail.ToUpperInvariant(),
            Email = BotEmail,
            NormalizedEmail = BotEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            FullName = _options.BotName,
            IsActive = false,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

        dbContext.Users.Add(botUser);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(botUser).State = EntityState.Detached;
        }
    }

    private async Task<string?> TryGenerateExternalLlmReplyAsync(
        string customerName,
        Conversation conversation,
        IReadOnlyList<TransportOrderDto> orders,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return null;

        try
        {
            var systemInstruction = BuildSystemPrompt(customerName, conversation.Subject, orders);
            var history = conversation.Messages
                .OrderBy(item => item.SentAtUtc)
                .TakeLast(10)
                .Select(item => new
                {
                    role = item.MessageType == ChatMessageType.AiAssistant ? "model" : "user",
                    parts = new[] { new { text = item.Content } }
                })
                .ToArray();

            var endpoint =
                $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

            var payload = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents = history,
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 600
                }
            };

            using var response = await SharedHttpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Gemini API returned status {StatusCode}. Falling back to built-in domain assistant.", response.StatusCode);
                return null;
            }

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            if (document.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var textElement))
            {
                var text = textElement.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "External AI call failed. Falling back to built-in domain assistant.");
        }

        return null;
    }

    private static string BuildSystemPrompt(
        string customerName,
        string subject,
        IReadOnlyList<TransportOrderDto> orders)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là Trợ lý AI TransFlow của hệ thống Quản lý Vận tải TransManagement.");
        sb.AppendLine($"Khách hàng đang trò chuyện: {customerName}. Chủ đề hỗ trợ: {subject}.");
        sb.AppendLine("Quy tắc trả lời:");
        sb.AppendLine("- Trả lời bằng tiếng Việt lịch sự, ngắn gọn, rõ ràng, chuyên nghiệp.");
        sb.AppendLine("- Dựa trực tiếp vào dữ liệu đơn hàng thực tế của khách hàng bên dưới để giải đáp.");
        sb.AppendLine("- Nếu khách muốn gặp nhân viên/quản trị viên, hãy thông báo yêu cầu đã được chuyển tới bộ phận điều phối viên và quản trị viên sẽ tiếp nhận sớm nhất.");
        sb.AppendLine();
        sb.AppendLine($"Danh sách đơn hàng hiện tại của khách ({orders.Count} đơn):");

        if (orders.Count == 0)
        {
            sb.AppendLine("- Khách hàng hiện chưa có đơn vận chuyển nào.");
        }
        else
        {
            foreach (var order in orders.Take(10))
            {
                sb.AppendLine(
                    $"- Mã: {order.Code} | Trạng thái: {FormatOrderStatus(order.Status)} | Hàng: {order.GoodsDescription} ({order.WeightKg:N0} kg) | Lấy tại: {order.PickupAddress} -> Giao tới: {order.DeliveryAddress}");
            }
        }

        return sb.ToString();
    }

    private async Task<string> BuildDomainSmartReplyAsync(
        Guid customerUserId,
        string customerName,
        string subject,
        string userMessage,
        IReadOnlyList<TransportOrderDto> orders,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeText($"{subject} {userMessage}");
        var messageOnlyNormalized = NormalizeText(userMessage);

        // 1. Check if customer asks for human support / admin handoff
        if (ContainsAny(messageOnlyNormalized, "gap nhan vien", "nhan vien ho tro", "gap admin", "quan tri vien", "nguoi that", "tu van vien", "chuyen nhan vien"))
        {
            return $"Dạ chào {customerName}, tôi đã ghi nhận yêu cầu gặp nhân viên hỗ trợ trực tiếp của bạn.\n\n" +
                   "Hệ thống đã gửi thông báo ưu tiên tới **Bộ phận Điều phối & Quản trị TransFlow**. Ngay khi Quản trị viên bấm **Tiếp nhận**, cuộc trò chuyện sẽ chuyển sang chế độ hỗ trợ trực tiếp bởi nhân viên.\n\n" +
                   "Trong lúc chờ đợi, bạn có thể để lại mã đơn hàng hoặc nội dung chi tiết cần xử lý gấp tại đây nhé!";
        }

        // 2. Check if customer mentions a specific order code (e.g. ORD-...) or asks to cancel a specific order
        var matchedOrder = FindMentionedOrder(userMessage, orders) ?? FindMentionedOrder(subject, orders);
        var wantsCancel = ContainsAny(messageOnlyNormalized, "huy don", "huy van don", "cancel");

        if (wantsCancel && matchedOrder is not null)
        {
            if (matchedOrder.Status is TransportOrderStatus.WaitingForAssignment or TransportOrderStatus.Draft)
            {
                var cancelResult = await transportService.CancelCustomerOrderAsync(customerUserId, matchedOrder.Id, cancellationToken);
                if (cancelResult.IsSuccess)
                {
                    return $"✅ Tôi đã hỗ trợ **hủy thành công** đơn hàng **{matchedOrder.Code}** ({matchedOrder.GoodsDescription}) theo yêu cầu của bạn.\n\n" +
                           $"- Điểm lấy: {matchedOrder.PickupAddress}\n" +
                           $"- Điểm giao: {matchedOrder.DeliveryAddress}\n" +
                           "- Trạng thái mới: **Đã hủy (Cancelled)**\n\n" +
                           "Nếu bạn cần tạo lại đơn vận chuyển mới với thông tin khác, bạn có thể vào mục **Tạo đơn hàng** hoặc nhắn tôi tư vấn thêm nhé!";
                }
            }

            return $"Đơn hàng **{matchedOrder.Code}** hiện đang ở trạng thái **{FormatOrderStatus(matchedOrder.Status)}** nên không thể tự động hủy qua hệ thống (chỉ các đơn ở trạng thái *Chờ điều phối* mới có thể hủy trực tiếp).\n\n" +
                   "Tôi đã lưu ý yêu cầu này để Quản trị viên điều phối kiểm tra và hỗ trợ phương án xử lý chuyến hàng cho bạn.";
        }

        if (matchedOrder is not null)
        {
            return FormatSingleOrderDetail(customerName, matchedOrder);
        }

        // 3. Check if customer asks about cancellation rules in general
        if (wantsCancel)
        {
            var cancellableOrders = orders
                .Where(item => item.Status is TransportOrderStatus.WaitingForAssignment or TransportOrderStatus.Draft)
                .ToList();

            if (cancellableOrders.Count == 0)
            {
                return "Về quy định hủy đơn tại **TransFlow**:\n" +
                       "- Bạn có thể hủy trực tiếp các đơn đang ở trạng thái **Chờ điều phối (WaitingForAssignment)**.\n" +
                       "- Hiện tại bạn không có đơn nào đang ở trạng thái chờ điều phối. Nếu muốn hủy đơn đã phân xe, vui lòng nhắn **\"Gặp nhân viên hỗ trợ\"** kèm mã đơn để Điều phối viên can thiệp.";
            }

            var codes = string.Join(", ", cancellableOrders.Select(item => $"**{item.Code}**"));
            return $"Bạn hiện có {cancellableOrders.Count} đơn hàng đủ điều kiện hủy ngay ({codes}).\n\n" +
                   "Bạn có thể:\n" +
                   "1. Nhắn trực tiếp cú pháp: **\"Hủy đơn [Mã đơn]\"** (ví dụ: `Hủy đơn " + cancellableOrders[0].Code + "`) để tôi hủy giúp bạn ngay tại đây.\n" +
                   "2. Hoặc bấm nút **Hủy đơn** trong bảng Danh sách đơn hàng của bạn.";
        }

        // 4. Check if customer asks about pricing / vehicle capacity / quote
        if (ContainsAny(normalized, "gia", "cuoc", "chi phi", "bao gia", "tai trong", "loai xe", "xe tai", "bao nhieu tien", "khoi luong", "tan", "kg"))
        {
            return BuildPricingAndVehicleAdvice(userMessage);
        }

        // 5. Check if customer asks about their orders / tracking / delivery status
        if (ContainsAny(normalized, "don hang", "van don", "kiem tra", "tra cuu", "trang thai", "dang o dau", "bao gio giao", "khi nao giao", "lich giao", "ord"))
        {
            return FormatOrderSummaryList(customerName, orders);
        }

        // 6. Check if customer asks how to create/book an order
        if (ContainsAny(normalized, "tao don", "dat don", "gui hang", "dat xe", "huong dan"))
        {
            return "Để tạo đơn vận chuyển mới trên **TransFlow**, bạn thực hiện 3 bước rất nhanh:\n" +
                   "1. Chuyển sang tab **Đặt đơn vận chuyển** trên Cổng Khách hàng.\n" +
                   "2. Nhập **Địa chỉ lấy hàng**, **Địa chỉ giao hàng**, **Mô tả hàng hóa**, **Khối lượng (kg)**, số kiện và thời gian mong muốn.\n" +
                   "3. Bấm **Gửi yêu cầu vận chuyển** — hệ thống sẽ cấp mã đơn (`ORD-...`) và bộ phận Điều phối sẽ ghép xe + tài xế phù hợp.\n\n" +
                   "Bạn cũng có thể cho tôi biết khối lượng hàng (kg/tấn) để tôi tư vấn loại xe và cước phí dự kiến trước nhé!";
        }

        // 7. Default helpful greeting with live order snapshot
        var summaryLine = orders.Count == 0
            ? "Hiện tài khoản của bạn chưa có đơn vận chuyển nào."
            : $"Hiện bạn đang có **{orders.Count} đơn hàng** trên hệ thống (gần nhất là đơn **{orders[0].Code}** - *{FormatOrderStatus(orders[0].Status)}*).";

        return $"Xin chào {customerName}! Tôi là **{_options.BotName}** ✦\n" +
               $"{summaryLine}\n\n" +
               "Tôi có thể hỗ trợ bạn ngay các việc sau:\n" +
               "• **Tra cứu đơn hàng:** Nhập mã đơn (VD: `ORD-...`) hoặc nhắn *\"Kiểm tra đơn hàng của tôi\"*.\n" +
               "• **Báo giá & chọn xe:** Nhắn khối lượng hàng (VD: *\"Báo giá hàng 1200kg\"*).\n" +
               "• **Hủy đơn chờ điều phối:** Nhắn *\"Hủy đơn [Mã đơn]\"*.\n" +
               "• **Gặp Quản trị viên:** Nhắn *\"Gặp nhân viên hỗ trợ\"* bất cứ lúc nào.";
    }

    private static TransportOrderDto? FindMentionedOrder(
        string text,
        IReadOnlyList<TransportOrderDto> orders)
    {
        if (string.IsNullOrWhiteSpace(text) || orders.Count == 0)
            return null;

        foreach (var order in orders)
        {
            if (text.Contains(order.Code, StringComparison.OrdinalIgnoreCase))
                return order;
        }

        var match = Regex.Match(text, @"ORD[-_A-Z0-9]+", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var token = match.Value.Trim();
            return orders.FirstOrDefault(order =>
                order.Code.Equals(token, StringComparison.OrdinalIgnoreCase) ||
                order.Code.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private static string FormatSingleOrderDetail(string customerName, TransportOrderDto order)
    {
        var pickupTime = order.ExpectedPickupAtUtc.HasValue
            ? order.ExpectedPickupAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
            : "Đang cập nhật";
        var deliveryTime = order.ExpectedDeliveryAtUtc.HasValue
            ? order.ExpectedDeliveryAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
            : "Đang cập nhật";
        var price = order.EstimatedPrice.HasValue
            ? $"{order.EstimatedPrice.Value:N0} VNĐ"
            : "Đang chờ báo giá điều phối";

        return $"Chào {customerName}, đây là thông tin chi tiết đơn hàng **{order.Code}** của bạn:\n\n" +
               $"• **Trạng thái hiện tại:** {FormatOrderStatus(order.Status)}\n" +
               $"• **Hàng hóa:** {order.GoodsDescription} ({order.WeightKg:N0} kg · {order.PackageCount} kiện)\n" +
               $"• **Điểm lấy hàng:** {order.PickupAddress}\n" +
               $"• **Điểm giao hàng:** {order.DeliveryAddress}\n" +
               $"• **Dự kiến lấy / giao:** {pickupTime} → {deliveryTime}\n" +
               $"• **Cước phí dự kiến:** {price}\n\n" +
               (order.Status == TransportOrderStatus.WaitingForAssignment
                   ? "💡 Đơn hàng này đang chờ điều phối phân xe. Nếu cần thay đổi hoặc hủy đơn, bạn có thể nhắn `Hủy đơn " + order.Code + "`."
                   : order.Status == TransportOrderStatus.InTransit
                       ? "🚚 Đơn hàng đang trên đường vận chuyển! Bạn có thể mở tab **Theo dõi GPS** để xem vị trí xe trực tiếp trên bản đồ."
                       : "Nếu cần hỗ trợ thêm về đơn hàng này, bạn cứ nhắn cho tôi nhé!");
    }

    private static string FormatOrderSummaryList(
        string customerName,
        IReadOnlyList<TransportOrderDto> orders)
    {
        if (orders.Count == 0)
        {
            return $"Chào {customerName}, hiện tại tài khoản của bạn chưa có đơn vận chuyển nào.\n\n" +
                   "Bạn có thể vào mục **Đặt đơn vận chuyển** để tạo yêu cầu mới, hoặc nhắn khối lượng hàng hóa để tôi tư vấn loại xe và báo giá nhé!";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Chào {customerName}, bạn hiện có **{orders.Count} đơn vận chuyển** trên hệ thống:");
        sb.AppendLine();

        foreach (var order in orders.Take(5))
        {
            sb.AppendLine(
                $"• **{order.Code}** — **{FormatOrderStatus(order.Status)}**\n" +
                $"  Hàng: {order.GoodsDescription} ({order.WeightKg:N0} kg) | {order.PickupAddress} → {order.DeliveryAddress}");
        }

        if (orders.Count > 5)
            sb.AppendLine($"\n*(Và {orders.Count - 5} đơn hàng khác)*");

        sb.AppendLine();
        sb.Append("Bạn muốn xem chi tiết đơn nào, hãy nhắn mã đơn (ví dụ: `").Append(orders[0].Code).Append("`) cho tôi nhé!");
        return sb.ToString();
    }

    private static string BuildPricingAndVehicleAdvice(string userMessage)
    {
        var weightKg = TryExtractWeightKg(userMessage);
        if (weightKg.HasValue && weightKg.Value > 0)
        {
            var kg = weightKg.Value;
            var (vehicleType, basePrice, perKgRate) = kg switch
            {
                <= 1_500m => ("Xe tải nhẹ 1.5 tấn (Thùng kín/Bạt)", 450_000m, 1_200m),
                <= 3_500m => ("Xe tải trung 3.5 tấn", 850_000m, 950m),
                <= 8_000m => ("Xe tải lớn 8 tấn", 1_600_000m, 750m),
                _ => ("Xe tải nặng 15 tấn / Đầu kéo Container", 3_200_000m, 550m)
            };

            var estimatedCost = basePrice + (kg * perKgRate);
            return $"Với khối lượng hàng khoảng **{kg:N0} kg**, **TransFlow AI** gợi ý phương án vận chuyển như sau:\n\n" +
                   $"• **Loại xe phù hợp:** {vehicleType}\n" +
                   $"• **Cước phí ước tính:** khoảng **{estimatedCost:N0} VNĐ** *(tùy cự ly tuyến đường thực tế)*\n" +
                   "• **Dịch vụ bao gồm:** Giám sát hành trình GPS thời gian thực & biên bản giao nhận điện tử (POD).\n\n" +
                   "Bạn có thể tạo đơn ngay ở mục **Đặt đơn vận chuyển** để Điều phối viên xác nhận báo giá chính xác theo điểm lấy/giao nhé!";
        }

        return "Bảng tư vấn tải trọng xe & cước phí tham khảo tại **TransFlow**:\n\n" +
               "• **Hàng <= 1.500 kg:** Xe tải nhẹ 1.5T — Cước mở chuyến từ **450.000đ**\n" +
               "• **Hàng 1.500 – 3.500 kg:** Xe tải trung 3.5T — Cước mở chuyến từ **850.000đ**\n" +
               "• **Hàng 3.500 – 8.000 kg:** Xe tải lớn 8T — Cước mở chuyến từ **1.600.000đ**\n" +
               "• **Hàng > 8.000 kg:** Xe tải nặng / Container — Báo giá theo hợp đồng tuyến\n\n" +
               "💡 *Mẹo:* Bạn có thể nhắn khối lượng cụ thể (ví dụ: **\"Báo giá hàng 1200kg\"** hoặc **\"Hàng 2.5 tấn\"**) để tôi tính cước dự kiến ngay cho bạn!";
    }

    private static decimal? TryExtractWeightKg(string text)
    {
        var tonMatch = Regex.Match(text, @"(\d+(?:[.,]\d+)?)\s*(?:tấn|tan)\b", RegexOptions.IgnoreCase);
        if (tonMatch.Success && TryParseDecimal(tonMatch.Groups[1].Value, out var tons))
            return tons * 1000m;

        var kgMatch = Regex.Match(text, @"(\d+(?:[.,]\d+)?)\s*(?:kg|kí|ký|kilogram)\b", RegexOptions.IgnoreCase);
        if (kgMatch.Success && TryParseDecimal(kgMatch.Groups[1].Value, out var kgs))
            return kgs;

        return null;
    }

    private static bool TryParseDecimal(string raw, out decimal value)
    {
        var normalized = raw.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatOrderStatus(TransportOrderStatus status) => status switch
    {
        TransportOrderStatus.Draft => "Bản nháp (Draft)",
        TransportOrderStatus.WaitingForAssignment => "Chờ điều phối (WaitingForAssignment)",
        TransportOrderStatus.Assigned => "Đã phân xe & tài xế (Assigned)",
        TransportOrderStatus.PickingUp => "Tài xế đang lấy hàng (PickingUp)",
        TransportOrderStatus.InTransit => "Đang vận chuyển (InTransit)",
        TransportOrderStatus.Delivered => "Đã giao hàng (Delivered)",
        TransportOrderStatus.Completed => "Hoàn tất (Completed)",
        TransportOrderStatus.DeliveryFailed => "Giao hàng thất bại (DeliveryFailed)",
        TransportOrderStatus.Cancelled => "Đã hủy (Cancelled)",
        _ => status.ToString()
    };

    private static string NormalizeText(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var formD = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch == 'đ' ? 'd' : ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool ContainsAny(string normalizedInput, params string[] keywords) =>
        keywords.Any(normalizedInput.Contains);
}
