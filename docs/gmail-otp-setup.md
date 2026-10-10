# Cấu hình OTP đăng ký qua Gmail

Hệ thống dùng Gmail SMTP và yêu cầu **App Password**, không dùng mật khẩu đăng nhập Gmail thông thường.

## 1. Tạo Gmail App Password

1. Bật xác minh hai bước cho tài khoản Google.
2. Mở phần `Security > App passwords` của tài khoản Google.
3. Tạo App Password mới cho `TransFlow`.
4. Sao chép chuỗi 16 ký tự được Google cung cấp.

## 2. Lưu cấu hình bằng .NET User Secrets

Chạy trong thư mục gốc dự án:

```powershell
dotnet user-secrets set "Email:Username" "your-account@gmail.com" --project TransManagement.API
dotnet user-secrets set "Email:SenderEmail" "your-account@gmail.com" --project TransManagement.API
dotnet user-secrets set "Email:AppPassword" "your-16-character-app-password" --project TransManagement.API
```

Không ghi App Password thật vào `appsettings.json` hoặc đưa lên GitHub.

## 3. Cập nhật database

```powershell
dotnet ef database update --project TransManagement.Infrastructure --startup-project TransManagement.API
```

## Luồng đăng ký

1. Người dùng nhập thông tin đăng ký.
2. Frontend gọi `POST /api/Auth/register/request-otp`.
3. Gmail gửi mã OTP 6 số, hiệu lực 5 phút.
4. Người dùng nhập OTP.
5. Frontend gọi `POST /api/Auth/register` với thông tin đăng ký và OTP.
6. Backend xác thực OTP rồi mới tạo tài khoản khách hàng.

Hệ thống giới hạn gửi lại sau 60 giây và khóa mã sau 5 lần nhập sai.
