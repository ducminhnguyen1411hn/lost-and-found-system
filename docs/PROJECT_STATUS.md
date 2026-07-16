# Trạng thái dự án — cập nhật 2026-07-17

> File này trả lời đúng 2 câu: **đang ở đâu** và **làm gì tiếp**.
> Bài học chi tiết của từng feature nằm ở `docs/features/*.md` — đọc cái đó trước khi sửa vào vùng tương ứng.
> Yêu cầu gốc: `docs/specs/REQUIREMENTS_2DEV.md`.

## 1. Bảng trạng thái FR

| FR | Trạng thái | Ghi chú |
|---|---|---|
| **FR-AUTH** 01–05 | ✅ Xong | Identity + 4 role + seed + `AdminUsersController` (quản lý user/role) |
| **FR-FOUND** 01–05 | ✅ Xong | + Sửa/Xoá, nhiều ảnh (Cloudinary) |
| **FR-TAG** 01–03 | ✅ Xong | `TagService.Normalize` dùng chung |
| **FR-TAG-04** autocomplete | ❌ | nice-to-have |
| **FR-LOG** 01–02 | ✅ Xong | `AuditService`, ghi trong cùng transaction |
| **FR-CLAIM** 01–05 | ✅ Xong | + huỷ-chấp-nhận, thread nhắn tin, contact optional, mốc giờ bàn giao |
| **FR-CLAIM-06** tranh chấp | ❌ | Staff phân xử — **thread đã là nguyên liệu sẵn** |
| **FR-TL** 01–02 | ✅ Xong | timeline trên trang item, lọc `IsPublic`, không rò tên |
| **FR-TL-03** | 🟡 Một nửa | `Returned` chốt timeline ✅; lời cảm ơn chờ FR-THANK |
| **FR-NOTI-01** | ✅ Xong | ghi `Notification` vào DB |
| **FR-NOTI-03** | ✅ Xong | chuông: đếm chưa đọc / list / mark-read |
| **FR-NOTI-02/04** realtime | ❌ | **`Hubs/` vẫn trống** — chưa có SignalR. Seam đã cắm sẵn `// TODO (FR-NOTI)` trong `NotificationService` |
| **FR-HOLD** 01–03 | ❌ | **Lỗ hổng lớn nhất** — xem §2 |
| **FR-MATCH** 01–05 | ❌ | bảng `LostAlert` có, **0 code dùng tới** |
| **FR-CAM** 01–04 | ❌ | bảng có, **0 code dùng tới** |
| **FR-THANK** 01–03 | ❌ | bảng có, **0 code dùng tới** |
| **FR-ADMIN** 01–06 | 🚧 **Đồng đội đang làm — ĐỪNG ĐỤNG** | Đã pull về một lần nhưng nhiều lỗi → **đang được làm lại**, nên hiện **không có trong nhánh này** (`Views/Admin/` không tồn tại, không có `AdminController`). Gồm: CRUD Category/Location/Tag, Unclaimed→Disposed, dashboard, trang xem AuditLog. Chỉ `AdminUsersController` (quản lý user/role = FR-AUTH-05) là đang có. |

**Ngoài spec (tự thêm):** bảng "Đồ bị mất" (`FR-LOST`) · bảng gộp `/Items` (found+lost, filter sidebar) · form đăng gộp · `/Items/Mine` "Bài đăng của tôi".

## 2. Làm gì tiếp — theo thứ tự đề xuất

1. **FR-HOLD** ← *nên làm trước*. Chọn "Ký gửi cho Staff" → đồ vào `PendingDropoff` và **kẹt vĩnh viễn**: không có màn Staff xác nhận nhận đồ để đẩy sang `Open`. Đây là **luồng chết** đang tồn tại trong app, không phải feature thiếu.
2. **FR-NOTI-02/04 (SignalR)** — chuông hiện chỉ cập nhật khi load lại trang. Hạ tầng DB đã xong, chỉ cần Hub + push. Là "giá trị lõi realtime" của đồ án.
3. **FR-MATCH** — giá trị lõi còn lại. ⚠️ Cần chốt thiết kế trước: spec dùng `LostAlert` (đăng ký theo dõi thụ động), nhưng ta **đã có bảng "Đồ bị mất" chủ động**. Phải quyết match với cái nào, đừng làm trùng.
4. **FR-THANK** → chỗ hiện đã sẵn sàng (`Returned` giờ khách xem được).
5. **FR-CAM**, **FR-CLAIM-06**.

> **Phân công:** **FR-ADMIN là của đồng đội** (đang làm lại sau khi bản đầu nhiều lỗi) — **không tự làm, không sửa chồng lên**. Phần còn lại ở trên là của mình.

## 3. Việc còn nợ (đã biết, cố ý chưa làm)

- **Chưa ai click-through 2 tài khoản** cho FR-CLAIM — xem kịch bản ở §5.
- `FoundItemService.SearchAsync` / `LostItemService.SearchAsync` + `Views/Shared/_Pager.cshtml` giờ **là code chết** (bảng đã chuyển sang `ItemBoardService`; `FoundItems/Index` chỉ còn redirect).
- Chưa có **unique index DB** cho luật "1 claim đang hiệu lực / user / item" — mới chặn ở tầng code.
- Bảng gộp **chưa có control "Sắp xếp"** (đang cứng: mới nhất trước).
- `PrivateMarks` của dữ liệu seed vẫn là câu rỗng ("có đặc điểm riêng…") — form đã sửa lời nhắc, nhưng **seed cũ không đổi**.

## 4. Ràng buộc & bẫy toàn dự án (đọc trước khi code)

- **Bootstrap ở đây là v5.1.0** — `text-bg-*` (5.2+), `bg-*-subtle` (5.3+), `object-fit-*` (5.3+) **KHÔNG tồn tại** và **fail im lặng**: không lỗi build, không cảnh báo, chỉ là element vô hình (badge thành chữ trắng trên nền trắng). **Grep vào `wwwroot/lib/bootstrap/dist/css/bootstrap.min.css` trước khi dùng bất kỳ class nào.**
- **`.ratio > *` ép MỌI con trực tiếp phủ 100%×100%** → overlay (badge…) phải đặt **ngoài** `.ratio`, nếu không nó che kín ảnh.
- **Hai phía KHÔNG chung thang trạng thái:** `FoundItemStatus.Open = 1` nhưng `LostItemStatus.Open = 0`. Không bao giờ so status mà chưa check `Kind`.
- **Toàn bộ thời gian lưu UTC** (đã audit: 0 `DateTime.Now`, 0 `GETDATE`). Cột **không có DB default** (`FoundAt`, `LostAt`, `HandledAt`, `*ConfirmedAt`) thì **DB không ép được** — code phải tự dùng `AppTime.ToUtc`/`DateTime.UtcNow`. View luôn render qua `AppTime.ToLocal`.
- **Category là cây 2 tầng, đồ gán ở LÁ** → lọc theo cha phải gồm cả con (`c.Id == cat || c.ParentId == cat`), không thì cha luôn ra 0.
- **Đừng để 1 biến bool trả lời 2 câu hỏi.** "Được mở trang?" ≠ "Được đọc field ẩn?" — gộp lại là lỗ bảo mật chờ nổ (đã dính với `canSee` ở `FoundItemService`).
- **Đừng model-bind quyền sở hữu.** Thứ như `ownerUserId` phải là **tham số service**, controller lấy từ user đăng nhập — nếu nằm trong VM bị bind thì `?OwnerUserId=<người khác>` là đọc được dữ liệu ẩn của họ.
- **Razor gộp cả view thành 1 method** → biến pattern (`is DateTime at`) ở 2 khối khác nhau vẫn đụng scope (CS0128).
- **Verify bằng curl:** Razor **HTML-encode output của `@expression`** thành entity (`ặ` → `&#x1EB7;`) còn chữ literal thì để nguyên → grep chữ tiếng Việt sẽ trượt. **Grep tên class**, đừng grep chuỗi hiển thị.
- **`MSB3027`/`MSB3021` khi build** = app đang chạy giữ khoá `bin\...\LostAndFound.exe`, **không phải lỗi code** (compile đã xong, chết ở bước copy). Tắt app, hoặc `dotnet build -o <thư-mục-tạm>` để kiểm tra compile mà không tranh khoá.

## 5. Chạy & test

```bash
dotnet run --project LostAndFound/LostAndFound.csproj      # http://localhost:5082
```
DB: SQL Server `localhost,1433` (Windows auth), override qua user-secrets. Lệnh apply schema + scaffold: xem `docs/superpowers/scaffold-command.md` *(file local, không commit)*.

| Vai | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@lostandfound.local` | `Admin#12345` |
| Staff | `staff@lostandfound.local` | `Staff#12345` |
| Member | `member@lostandfound.local` | `Member#12345` |
| Demo user01–23 | `user01@lostandfound.local`… | `Demo#12345` |

**Click-through 2 tài khoản** = tự mở trình duyệt, đăng nhập **2 tài khoản khác nhau** (1 người nhặt, 1 người nhận) rồi bấm qua từng bước. Phải 2 người vì service **chặn tự nhận đồ của chính mình**, và `Returned` chỉ đạt khi **hai bên khác nhau** cùng xác nhận. Dùng **1 cửa sổ thường + 1 cửa sổ ẩn danh** (cùng trình duyệt thì login sau đá văng login trước).

Luồng cần thử: gửi claim → holder hỏi trong thread → holder chấp nhận (các claim khác **tự động bị từ chối**) → 2 bên xác nhận bàn giao → `Returned`. Kiểm tra: **một bên xác nhận thì CHƯA phải `Returned`**; khách vãng lai không thấy verification/contact/PrivateMarks.
