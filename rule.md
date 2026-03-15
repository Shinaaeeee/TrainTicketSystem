# TrainTicketSystem - Agent Rules

> Bộ quy tắc dành cho AI Agent khi làm việc với dự án hệ thống đặt vé tàu.

---

## 📋 Tổng Quan Dự Án

| Thuộc tính         | Giá trị                                 |
| ------------------ | --------------------------------------- |
| **Tên dự án**      | TrainTicketSystem                       |
| **Loại**           | ASP.NET Core Razor Pages Web App        |
| **Framework**      | .NET 8.0                                |
| **Database**       | SQL Server (EF Core 8.0)                |
| **ORM**            | Entity Framework Core (Database-First)  |
| **Authentication** | Session-based (custom)                  |
| **Ngôn ngữ**       | C#                                      |
| **Môn học**        | PRN222 - FPT University SE8             |

---

## 🏗️ Cấu Trúc Dự Án

```plaintext
TrainTicketSystem/
├── TrainTicketSystem.sln          # Solution file
├── rule.md                        # File này
├── .agent/                        # AI Agent config
│   ├── ARCHITECTURE.md
│   ├── agents/                    # Specialist agents
│   ├── skills/                    # Domain skills
│   ├── workflows/                 # Slash commands
│   └── rules/                    # Global rules
└── TrainTicketSystem/             # Main project
    ├── Program.cs                 # Entry point
    ├── appsettings.json           # Config + Connection String
    ├── TrainTicketSystem.csproj   # .NET 8.0 project
    ├── Models/                    # EF Core entities + DbContext
    │   ├── TrainTicketDbContext.cs
    │   ├── User.cs
    │   ├── Booking.cs
    │   ├── BookingDetail.cs
    │   ├── BookingViewModel.cs
    │   ├── Payment.cs
    │   ├── Route.cs
    │   ├── Schedule.cs
    │   ├── Seat.cs
    │   ├── SeatType.cs
    │   └── Train.cs
    ├── Pages/                     # Razor Pages (UI + Code-behind)
    │   ├── Index.cshtml
    │   ├── Login.cshtml
    │   ├── Register.cshtml
    │   ├── Admin/
    │   ├── Routes/
    │   ├── Schedules/
    │   ├── Seats/
    │   ├── Trains/
    │   └── Shared/                # _Layout, _ViewImports
    ├── Service/
    │   └── UserSession.cs         # In-memory user session
    └── wwwroot/                   # Static files (CSS, JS, images)
```

---

## 🗄️ Database Schema (TrainTicketDB)

### Entity Relationship

```
User (1) ──── (N) Booking (1) ──── (N) BookingDetail
                     │                       │
                     │                       └──── (1) Seat
                     │
                     ├──── (1) Schedule
                     │          │
                     │          ├──── (1) Train
                     │          └──── (1) Route
                     │
                     └──── (N) Payment

Train (1) ──── (N) Seat (N) ──── (1) SeatType
```

### Bảng chính

| Bảng            | Mô tả                          | Khóa chính    |
| --------------- | ------------------------------- | ------------- |
| `Users`         | Người dùng (Customer/Admin)     | `UserId`      |
| `Train`         | Thông tin tàu                   | `TrainId`     |
| `Route`         | Tuyến đường (ga đi - ga đến)   | `RouteId`     |
| `Schedule`      | Lịch chạy tàu                  | `ScheduleId`  |
| `Seat`          | Ghế ngồi trên tàu              | `SeatId`      |
| `SeatType`      | Loại ghế (VIP, thường...)       | `SeatTypeId`  |
| `Booking`       | Đơn đặt vé                     | `BookingId`   |
| `BookingDetail` | Chi tiết vé (ghế nào, giá nào) | `Id`          |
| `Payment`       | Thanh toán                      | `PaymentId`   |

### Quan hệ Foreign Key

| Bảng            | FK Column      | Tham chiếu        |
| --------------- | -------------- | ------------------ |
| `Booking`       | `UserId`       | `Users.UserId`     |
| `Booking`       | `ScheduleId`   | `Schedule.ScheduleId` |
| `BookingDetail` | `BookingId`    | `Booking.BookingId` |
| `BookingDetail` | `SeatId`       | `Seat.SeatId`      |
| `Payment`       | `BookingId`    | `Booking.BookingId` |
| `Schedule`      | `TrainId`      | `Train.TrainId`    |
| `Schedule`      | `RouteId`      | `Route.RouteId`    |
| `Seat`          | `TrainId`      | `Train.TrainId`    |
| `Seat`          | `SeatTypeId`   | `SeatType.SeatTypeId` |

---

## ⚙️ Quy Tắc Code

### 1. Kiến Trúc (Architecture)

- **Pattern**: Razor Pages (Page Model pattern) — KHÔNG dùng MVC Controller.
- **ORM**: Entity Framework Core — Database-First (scaffold từ DB).
- **Session**: Dùng `HttpContext.Session` + `UserSession` service.
- **DI**: Đăng ký service trong `Program.cs` qua `builder.Services`.

### 2. Naming Convention

| Loại              | Quy tắc                    | Ví dụ                      |
| ----------------- | --------------------------- | --------------------------- |
| **Class**         | PascalCase                  | `BookingDetail`, `SeatType` |
| **Property**      | PascalCase                  | `TrainName`, `SeatNumber`   |
| **Method**        | PascalCase                  | `OnGetAsync`, `OnPostAsync` |
| **Private field** | _camelCase                  | `_context`, `_session`      |
| **Page Handler**  | OnGet/OnPost + Async suffix | `OnGetAsync()`, `OnPostAsync()` |
| **File .cshtml**  | PascalCase                  | `CreateBooking.cshtml`      |
| **Namespace**     | TrainTicketSystem.{Folder}  | `TrainTicketSystem.Models`  |

### 3. Razor Pages Rules

```csharp
// ✅ ĐÚNG: Inject DbContext qua constructor
public class IndexModel : PageModel
{
    private readonly TrainTicketDbContext _context;
    public IndexModel(TrainTicketDbContext context) => _context = context;
}

// ❌ SAI: New trực tiếp DbContext
var db = new TrainTicketDbContext();
```

- Mỗi page gồm 2 file: `PageName.cshtml` + `PageName.cshtml.cs`
- Dùng `[BindProperty]` cho form binding
- Dùng `TempData` cho thông báo flash message
- Dùng `RedirectToPage()` để navigate giữa các page

### 4. Entity Framework Core Rules

```csharp
// ✅ ĐÚNG: Async query
var bookings = await _context.Bookings
    .Include(b => b.User)
    .Include(b => b.Schedule)
    .ToListAsync();

// ❌ SAI: Sync query trong async context
var bookings = _context.Bookings.ToList();
```

- **LUÔN** dùng `async/await` cho database operations
- **LUÔN** dùng `.Include()` khi cần navigation property
- **KHÔNG** sửa file `TrainTicketDbContext.cs` trực tiếp (generated code)
- Dùng `SaveChangesAsync()` thay vì `SaveChanges()`

### 5. Authentication & Authorization

```csharp
// Check login trong PageModel
var userId = HttpContext.Session.GetInt32("UserId");
if (userId == null) return RedirectToPage("/Login");

// Check admin role
var role = HttpContext.Session.GetString("Role");
if (role != "Admin") return RedirectToPage("/Index");
```

- **Session keys**: `"UserId"`, `"Username"`, `"Role"`, `"FullName"`
- Admin pages nằm trong folder `Pages/Admin/`
- Customer pages nằm ngoài folder Admin

### 6. Validation Rules (User Model)

| Field      | Rule                                       |
| ---------- | ------------------------------------------ |
| `Username` | Required                                   |
| `Password` | Min 6 chars, uppercase + lowercase + digit |
| `FullName` | Required                                   |
| `Phone`    | Bắt đầu `03`, đúng 10 chữ số              |
| `Email`    | Phải là `@gmail.com`                       |

---

## 🚫 Những Điều KHÔNG Được Làm

1. **KHÔNG** dùng MVC Controller — dự án dùng Razor Pages
2. **KHÔNG** thay đổi connection string trong `appsettings.json` (mỗi dev có config riêng)
3. **KHÔNG** hardcode connection string trong code
4. **KHÔNG** thêm package NuGet mà không hỏi trước
5. **KHÔNG** thay đổi schema database mà không có migration plan
6. **KHÔNG** dùng `ViewBag`/`ViewData` — dùng `PageModel` properties thay thế
7. **KHÔNG** bỏ qua async/await cho database operations
8. **KHÔNG** commit file `bin/`, `obj/`, `.vs/` lên git

---

## ✅ Checklist Khi Tạo Feature Mới

- [ ] Tạo Razor Page (.cshtml + .cshtml.cs) trong đúng folder
- [ ] Inject `TrainTicketDbContext` qua constructor
- [ ] Kiểm tra authentication/authorization nếu cần
- [ ] Dùng `[BindProperty]` cho form fields
- [ ] Validate input data (DataAnnotation hoặc manual)
- [ ] Dùng async/await cho tất cả DB operations
- [ ] Include navigation properties khi query
- [ ] Thêm error handling (try-catch cho DB operations)
- [ ] Update `_Layout.cshtml` nếu cần thêm menu item
- [ ] Test trên cả role Customer và Admin

---

## 🔧 Cách Chạy Dự Án

```powershell
# 1. Đảm bảo SQL Server đang chạy
# 2. Kiểm tra connection string trong appsettings.json
# 3. Chạy dự án
cd TrainTicketSystem
dotnet run

# Hoặc dùng Visual Studio: F5 / Ctrl+F5
```

### Connection String Format

```
Data Source=localhost\SQLEXPRESS;Initial Catalog=TrainTicketDB;User ID=sa;Password=123;Encrypt=false;TrustServerCertificate=true;
```

> ⚠️ **Lưu ý**: Connection string có thể khác nhau giữa các máy dev. Không commit thay đổi connection string.

---

## 🌐 Tech Stack Summary

| Layer          | Technology                          |
| -------------- | ----------------------------------- |
| **Frontend**   | Razor Pages + HTML/CSS/JS           |
| **Backend**    | ASP.NET Core 8.0                    |
| **ORM**        | Entity Framework Core 8.0           |
| **Database**   | SQL Server (SQLEXPRESS)             |
| **Auth**       | Session-based (HttpContext.Session)  |
| **DI**         | Built-in ASP.NET Core DI            |
| **Packages**   | Microsoft.Data.SqlClient, EF Core   |

---

## 📝 Ghi Chú Cho Agent

1. **Ngôn ngữ giao tiếp**: Ưu tiên tiếng Việt khi trả lời user
2. **Code comments**: Giữ nguyên tiếng Anh
3. **Khi tạo page mới**: Luôn follow pattern của các page hiện có
4. **Khi query DB**: Tham khảo `TrainTicketDbContext.cs` cho entity relationships
5. **Khi cần thêm tính năng**: Hỏi user trước về scope và yêu cầu cụ thể
6. **Đây là bài tập môn học**: Code cần rõ ràng, dễ hiểu, có comment giải thích
