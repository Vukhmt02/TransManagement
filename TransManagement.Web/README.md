# TransManagement Web

Giao diện đăng nhập và đăng ký React + TypeScript cho TransManagement API.

Tài khoản đăng ký công khai được backend tự động gán vai trò `Customer` và được
đăng nhập ngay sau khi đăng ký thành công.

Sau khi đăng nhập bằng tài khoản `Admin` hoặc `Dispatcher`, ứng dụng hiển thị
dashboard quản lý sử dụng dữ liệu thật từ API: tổng quan, đơn vận chuyển, chuyến
xe, khách hàng, tài xế và phương tiện. Các vai trò khác được đưa đến màn hình
thông báo quyền phù hợp để tránh hiển thị dữ liệu điều hành.

## Chạy ứng dụng

Mở hai cửa sổ PowerShell tại thư mục gốc dự án.

Backend:

```powershell
dotnet run --project TransManagement.API --launch-profile http
```

Frontend:

```powershell
cd TransManagement.Web
npm.cmd install
npm.cmd run dev
```

Truy cập `http://localhost:5173`. Vite chuyển tiếp các request `/api` đến
`http://localhost:5091`, vì vậy không cần cấu hình CORS khi phát triển.

## Bảo mật phiên đăng nhập

Access token và refresh token được lưu trong `sessionStorage`, tự mất khi phiên
trình duyệt kết thúc. Tùy chọn ghi nhớ chỉ lưu địa chỉ email, không lưu mật khẩu.
Trong bản production nên chuyển refresh token sang cookie `HttpOnly`, `Secure`
do backend phát hành.
