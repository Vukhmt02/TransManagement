# Giai đoạn 6 — Cổng khách hàng, tài xế và theo dõi chuyến

## Chức năng đã triển khai

- Khách hàng xem, tạo và hủy đơn của chính mình.
- Tài xế xem các chuyến được phân công, bắt đầu và hoàn thành chuyến.
- Quy trình điểm dừng theo thứ tự: `Pending -> Arrived -> Completed/Skipped`.
- Điểm giao hàng yêu cầu ảnh (URL) hoặc xác nhận/chữ ký người nhận.
- Tài xế gửi vị trí GPS từ trình duyệt; hệ thống lưu tối đa lịch sử để tra cứu.
- SignalR phát sự kiện `locationUpdated` theo nhóm `shipment-{id:N}`.
- Liên kết một hồ sơ tài xế với một tài khoản đăng nhập.
- OpenStreetMap mở vị trí mới nhất của chuyến.

## Chuẩn bị tài khoản tài xế

Đăng nhập Swagger bằng tài khoản Admin, sau đó:

1. `GET /api/Users` để lấy `userId`.
2. `PUT /api/Users/{userId}/roles` với nội dung `{ "roles": ["Driver"] }`.
3. `GET /api/Drivers` để lấy `driverId`.
4. `PUT /api/Drivers/{driverId}/user` với nội dung `{ "userId": "..." }`.
5. Đăng nhập lại tài khoản tài xế vì đổi vai trò sẽ thu hồi phiên cũ.

Tài khoản đăng ký từ giao diện mặc định mang vai trò `Customer` và tự động có hồ sơ khách hàng.

## Cập nhật cơ sở dữ liệu PostgreSQL

```powershell
dotnet ef database update --project TransManagement.Infrastructure --startup-project TransManagement.API
```

Migration của giai đoạn này là `AddDriverPortalTrackingAndProof`. Migration tạo liên kết `drivers.UserId`, bảng `shipment_locations` và `delivery_proofs`.

## Chạy dự án

Terminal 1:

```powershell
dotnet run --project TransManagement.API --launch-profile http
```

Terminal 2:

```powershell
cd TransManagement.Web
npm.cmd run dev
```

Mở `http://localhost:5173`. GPS trên trình duyệt cần quyền truy cập vị trí; trong môi trường phát triển, `localhost` được trình duyệt coi là ngữ cảnh an toàn.
