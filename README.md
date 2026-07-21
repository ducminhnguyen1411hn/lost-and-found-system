# LostAndFound

Website tìm đồ thất lạc cho trường học: người nhặt được đăng món đồ, người mất đi tìm, gửi yêu cầu
nhận lại và hai bên xác nhận bàn giao. Viết bằng **ASP.NET Core MVC (.NET 8)** + **SQL Server**.

---

## Yêu cầu máy

- **.NET 8 SDK** — kiểm tra: `dotnet --list-sdks` (cần có dòng `8.x`).
- **SQL Server** bất kỳ: LocalDB, SQL Server Express, hoặc một SQL Server bạn đã có sẵn.
- **Một tool quản lý SQL Server**: SSMS, Azure Data Studio, hoặc DBeaver.

---

## Chạy project — 3 bước

### Bước 1 — Trỏ chuỗi kết nối tới SQL Server của bạn

Mở **`LostAndFound/appsettings.json`**, sửa `ConnectionStrings:DefaultConnection` cho trỏ đúng SQL Server
của bạn (giữ nguyên `Database=LostAndFound`):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=<MÁY_CHỦ_CỦA_BẠN>;Database=LostAndFound;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Vài ví dụ cho `Server=`:

| Loại SQL Server | Chuỗi mẫu |
|---|---|
| LocalDB | `Server=(localdb)\MSSQLLocalDB;Database=LostAndFound;Trusted_Connection=True;TrustServerCertificate=True` |
| SQL Server trên máy (Windows auth) | `Server=localhost;Database=LostAndFound;Trusted_Connection=True;TrustServerCertificate=True` |
| SQL Server + tài khoản SQL (user/pass) | `Server=localhost,1433;Database=LostAndFound;User Id=sa;Password=MẬT_KHẨU;TrustServerCertificate=True` |

> Không cần tạo sẵn database — bước 2 sẽ tự tạo. Chỉ cần `Server=` trỏ đúng, phần `Database=LostAndFound`
> để nguyên.

### Bước 2 — Chạy `schema.sql` + `seed-data.sql` bằng tool DB

Mở tool quản lý SQL (**SSMS / Azure Data Studio / DBeaver**), kết nối tới server ở bước 1, rồi mở và
**Execute** lần lượt 2 file:

1. `LostAndFound/db/schema.sql` — tự tạo database `LostAndFound` + toàn bộ bảng (chạy lại nhiều lần vô hại).
2. `LostAndFound/db/seed-data.sql` — nạp dữ liệu mẫu (tài khoản demo, món đồ…). Chạy **sau** `schema.sql`.

### Bước 3 — Build và chạy

```bash
dotnet run --project LostAndFound/LostAndFound.csproj
```

Mở trình duyệt: **http://localhost:5082** (hoặc `https://localhost:7257`).

Lần chạy đầu, app tự tạo **4 vai trò** và các **tài khoản mẫu** + một ít **dữ liệu demo**. Chạy lại nhiều
lần vô hại (đã có thì thôi).

---

## Tài khoản đăng nhập sẵn

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@lostandfound.local` | `Admin#12345` |
| Nhân viên | `staff@lostandfound.local` | `Staff#12345` |
| Thành viên | `member@lostandfound.local` | `Member#12345` |

> Nếu có chạy `seed-data.sql`, còn có thêm các tài khoản demo `user01@lostandfound.local` … với mật khẩu `Demo#12345`.
> Đổi mật khẩu admin trước khi dùng thật.

Bạn cũng có thể tự đăng ký tài khoản mới ở `/Identity/Account/Register` (mặc định là vai trò Thành viên).

---

## Upload ảnh (không bắt buộc)

App upload ảnh món đồ lên **Cloudinary**. Nếu **không** cấu hình Cloudinary, app vẫn chạy bình thường và
**tự lưu ảnh vào `LostAndFound/wwwroot/uploads/`** trên máy chủ — chỉ khác chỗ lưu, không lỗi gì.

Muốn dùng Cloudinary thật, thêm khoá vào **`appsettings.json`** (mục `Cloudinary`) hoặc tạo file
`LostAndFound/appsettings.Development.json` (không bị commit):

```json
"Cloudinary": {
  "CloudName": "<cloud-name-của-bạn>",
  "ApiKey": "<api-key>",
  "ApiSecret": "<api-secret>"
}
```

---

## Ghi chú kỹ thuật

- **DB-First**: `LostAndFound/db/schema.sql` là nguồn chân lý của cơ sở dữ liệu. Đổi cấu trúc bảng thì sửa
  ở đó rồi chạy lại. Không dùng EF migrations.
- URL & cổng chạy nằm ở `LostAndFound/Properties/launchSettings.json`.
- Chạy HTTPS lần đầu cần tin cậy chứng chỉ dev một lần: `dotnet dev-certs https --trust`.

---

## Xử lý sự cố nhanh

| Triệu chứng | Cách xử lý |
|---|---|
| `Cannot open database "LostAndFound"` | Chưa chạy `schema.sql`, hoặc `Server=` trỏ sai. Chạy lại bước 2. |
| `A network-related... error / login failed` | Sai server/tài khoản trong chuỗi kết nối. Kiểm tra lại `Server=`, `User Id`, `Password`. |
| Build báo thiếu targeting pack `net8.0` | Máy chỉ có SDK mới hơn. Cài thêm **.NET 8 SDK**. |
| `Address already in use` (cổng 5082/7257) | Đang có tiến trình chiếm cổng. Đóng nó, hoặc đổi cổng trong `launchSettings.json`. |
