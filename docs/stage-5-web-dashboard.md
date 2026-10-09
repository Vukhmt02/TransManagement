# Giai đoạn 5 - Web dashboard

Frontend nằm trong `TransManagement.Web`, sử dụng React, TypeScript và Vite.

## Chức năng

- Đăng nhập, đăng ký và đăng xuất.
- Access token tự động được làm mới một lần bằng refresh token khi API trả 401.
- Đăng ký tạo đồng thời tài khoản Identity và hồ sơ khách hàng liên kết.
- Dashboard tổng quan sử dụng dữ liệu thật từ PostgreSQL.
- Tìm kiếm khách hàng, tài xế, xe, đơn và chuyến.
- Tạo, cập nhật, khóa/mở khách hàng.
- Tạo, cập nhật và đổi trạng thái tài xế, phương tiện.
- Tạo, cập nhật và hủy đơn chưa phân công.
- Tạo chuyến từ các đơn chờ phân công.
- Bắt đầu, hoàn thành và hủy chuyến.
- Modal xác nhận, thông báo kết quả và giao diện responsive.

API điều hành chỉ cho phép `Admin` và `Dispatcher`. Tài khoản đăng ký công khai
nhận vai trò `Customer` và không được tải dữ liệu quản trị.

## Chạy dự án

```powershell
dotnet run --project TransManagement.API --launch-profile http
```

Trong terminal khác:

```powershell
cd TransManagement.Web
npm.cmd install
npm.cmd run dev
```

Mở `http://localhost:5173`.
