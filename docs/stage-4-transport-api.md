# Giai đoạn 4 - API quản lý vận tải

API chạy tại `http://localhost:5091`, Swagger tại
`http://localhost:5091/swagger/index.html`.

## Nhóm chức năng

- `Customers`: tra cứu, tạo, cập nhật và khóa/mở khách hàng.
- `Drivers`: tra cứu, tạo, cập nhật và đổi trạng thái tài xế.
- `Vehicles`: tra cứu, tạo, cập nhật và đổi trạng thái phương tiện.
- `TransportOrders`: tạo/cập nhật đơn và quản lý vòng đời đơn vận chuyển.
- `Shipments`: phân công đơn cho tài xế/phương tiện, bắt đầu, hoàn thành hoặc hủy chuyến.

Các endpoint đọc yêu cầu đăng nhập. Endpoint thay đổi dữ liệu yêu cầu vai trò
`Admin` hoặc `Dispatcher`; thao tác bắt đầu/hoàn thành chuyến cũng cho phép vai
trò `Driver`.

## Tạo tài khoản quản trị khi phát triển

Đặt thông tin quản trị bằng User Secrets rồi khởi động lại API:

```powershell
dotnet user-secrets set --project TransManagement.API "DevelopmentAdmin:Email" "admin@transmanagement.local"
dotnet user-secrets set --project TransManagement.API "DevelopmentAdmin:FullName" "Local Administrator"
dotnet user-secrets set --project TransManagement.API "DevelopmentAdmin:Password" "Thay-bang-mat-khau-manh-123!"
```

Tài khoản chỉ được khởi tạo trong môi trường `Development` khi
`Database:SeedDevelopmentData` đang bật. Không lưu mật khẩu thật trong
`appsettings.json`.

## Luồng thử trên Swagger

1. Đăng nhập bằng `POST /api/Auth/login`.
2. Sao chép `accessToken`, bấm **Authorize** và nhập token.
3. Tạo hoặc chọn khách hàng, tài xế và xe đang ở trạng thái `Available`.
4. Tạo đơn bằng `POST /api/TransportOrders`. Đơn mới tự chuyển sang
   `WaitingForAssignment`.
5. Tạo chuyến bằng `POST /api/Shipments`, truyền danh sách `orderIds`. Hệ thống
   kiểm tra tải trọng, thể tích, hạn giấy phép và trạng thái tài nguyên.
6. Gọi `POST /api/Shipments/{id}/start` để bắt đầu chuyến.
7. Gọi `POST /api/Shipments/{id}/complete` để hoàn thành. Đơn chuyển sang
   `Completed`, tài xế và xe tự trở lại `Available`.

Nếu hủy một chuyến chưa bắt đầu, đơn quay lại `WaitingForAssignment` để có thể
phân công sang chuyến khác.

## Trạng thái chính

- Đơn: `Draft` → `WaitingForAssignment` → `Assigned` → `PickingUp` →
  `InTransit` → `Delivered` → `Completed`.
- Chuyến: `Planned` → `Assigned` → `Started` → `Completed`.
- Tài xế: `Available`, `Assigned`, `OnLeave`, `Inactive`.
- Xe: `Available`, `Assigned`, `InTransit`, `Maintenance`, `Inactive`.

Thay đổi ở giai đoạn này không thêm cột hoặc bảng mới, do đó không cần migration
mới. Các bảng nghiệp vụ đã được tạo trong migration ban đầu.
