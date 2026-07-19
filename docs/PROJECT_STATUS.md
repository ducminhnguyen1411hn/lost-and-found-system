# Trạng thái dự án — cập nhật 2026-07-20

> File này trả lời đúng 2 câu: **đang ở đâu** và **làm gì tiếp**.
> Bài học chi tiết của từng feature nằm ở `docs/features/*.md` — đọc cái đó trước khi sửa vào vùng tương ứng.
> Yêu cầu gốc: `docs/specs/REQUIREMENTS_2DEV.md`.

## 1. Bảng trạng thái FR

| FR | Trạng thái | Ghi chú |
|---|---|---|
| **FR-AUTH** 01–05 | ✅ Xong | Identity + 4 role + seed + `AdminUsersController` (quản lý user/role) |
| **FR-FOUND** 01–05 | ✅ Xong | + Sửa/Xoá, nhiều ảnh (Cloudinary) |
| **FR-TAG** 01–03 | ✅ Xong | `TagService.Normalize` dùng chung |
| **FR-TAG-04** autocomplete | ✅ Xong | `TagsController.Suggest` (public) + `TagService.SuggestTagsAsync`, dùng ở form đăng |
| **FR-LOG** 01–02 | ✅ Xong | `AuditService`, ghi trong cùng transaction |
| **FR-CLAIM** 01–05 | ✅ Xong | + huỷ-chấp-nhận, thread nhắn tin, contact optional, mốc giờ bàn giao |
| **FR-CLAIM-06** tranh chấp | ❌ | Staff phân xử — **thread đã là nguyên liệu sẵn** |
| **FR-TL** 01–02 | ✅ Xong | timeline trên trang item, lọc `IsPublic`, không rò tên |
| **FR-TL-03** | 🟡 Một nửa | `Returned` chốt timeline ✅; lời cảm ơn chờ FR-THANK |
| **FR-NOTI-01** | ✅ Xong | ghi `Notification` vào DB |
| **FR-NOTI-03** | ✅ Xong | chuông: đếm chưa đọc / list / mark-read |
| **FR-NOTI-02/04** realtime | ❌ | **`Hubs/` vẫn trống** — chưa có SignalR. Seam đã cắm sẵn `// TODO (FR-NOTI)` trong `NotificationService` |
| **FR-HOLD** 01–03 | ✅ Xong | Custodial intake: tab "Chờ tiếp nhận" (`PendingDropoff`→`Open`, nhập nơi cất) + tab "Đã tiếp nhận" (kho: lọc trạng thái, sửa nơi cất). `HoldingController`/`HoldingService`, nav Staff/Admin |
| **FR-MATCH** 01–05 | ❌ | bảng `LostAlert` có, **0 code dùng tới**. Giá trị lõi lớn nhất còn lại — xem §2 |
| **FR-CAM** 01–04 | ❌ | bảng có, **0 code dùng tới** |
| **FR-THANK** 01–03 | ❌ **đã quyết bỏ** | bảng để không cũng vô hại; kéo theo FR-TL-03 khép luôn |
| **FR-ADMIN** 01–06 | ✅ Xong (đã merge từ `main`) | CRUD Category/Location/Tag · Unclaimed→Disposed · dashboard · xem AuditLog · `AdminUsersController` (user/role). **Mình bổ sung**: màn "Quản lý bài đăng" (list mọi post + gỡ cascade + đăng hộ) và nút "Quét đồ quá hạn" (Open→Unclaimed) |

**Vòng đời đồ nhặt giờ khép kín:** đăng → (Staff tiếp nhận nếu Custodial) → `Open` → claim/return **hoặc** quá 30 ngày không ai nhận → `Unclaimed` (sweep `UnclaimedSweepService` + `BackgroundService` chạy mỗi 24h, hoặc nút "Quét ngay") → admin `Disposed`.

**Ngoài spec (tự thêm):** bảng "Đồ bị mất" (`FR-LOST`) · bảng gộp `/Items` (found+lost, filter sidebar) · form đăng gộp · `/Items/Mine` "Bài đăng của tôi" · admin "Quản lý bài đăng" (gỡ cascade + đăng hộ) · chặn đăng bài theo cờ `IsPostingBlocked` (mọi đường Create) · chọn lại ảnh bìa ở màn Sửa · Open→Unclaimed sweep + cron.

## 2. Làm gì tiếp — theo thứ tự đề xuất

1. **FR-MATCH** ← *cái to còn lại*. Khớp lost↔found + tự động báo. ⚠️ **Cần brainstorm chốt thiết kế trước**: spec dùng `LostAlert` (đăng ký theo dõi thụ động), nhưng ta **đã có bảng "Đồ bị mất" chủ động**. Phải quyết match với cái nào, đừng làm trùng.
2. **FR-NOTI-02/04 (SignalR)** — chuông hiện chỉ cập nhật khi load lại trang. Hạ tầng DB đã xong, chỉ cần Hub + push. "Giá trị lõi realtime" của đồ án.
3. **FR-CAM**, **FR-CLAIM-06** — thứ yếu.

> **FR-THANK đã quyết bỏ**; FR-TL-03 theo đó khép (`Returned` chốt timeline là hết).

> **Phân công (cập nhật):** FR-ADMIN vốn của đồng đội, **giờ đã merge vào nhánh này và xong**; mình đã bổ sung màn quản lý bài đăng + sweep Unclaimed. Trước khi sửa sâu vào khu admin, **phối hợp với đồng đội** tránh giẫm chân + tránh conflict lần nữa.

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
- **`main` có `db/schema.sql` dính trailing whitespace (~449 dòng), nhánh feature thì sạch (0)** → merge/rebase qua nhau sẽ **conflict NGUYÊN file schema** dù thay đổi thật rất nhỏ và không đè lên nhau (git không match nổi dòng nào vì khác khoảng trắng cuối dòng). Resolve về **bản sạch (không trailing-ws)** + ghép phần bên kia thêm vào, để các commit sau auto-merge thay vì conflict lại y hệt. Kiểm chứng bằng `diff --ignore-all-space` — delta thật thường chỉ vài bảng/cột. Fix tận gốc = strip trailing-ws trên `main`.

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
