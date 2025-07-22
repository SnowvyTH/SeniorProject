# Project - ASP.NET Core MVC

โปรเจค ASP.NET Core MVC สำหรับการจัดการฐานข้อมูลผู้ป่วยและแบบประเมิน พร้อมระบบ Login ที่ใช้การเข้ารหัส SHA-256

## คุณสมบัติหลัก

### ✅ **ระบบการเข้าสู่ระบบ (Authentication)**
- หน้า Login ที่ Views/Home/Index
- การเข้ารหัสรหัสผ่านด้วย SHA-256
- Cookie Authentication
- การจำการเข้าสู่ระบบ (Remember Me)
- ระบบ Logout
- การบันทึก Log การเข้าสู่ระบบ

### 📁 **โครงสร้างโปรเจค**

```
d:\Project\Project\
├── Models/
│   ├── Entity/             # Entity Models (15 files)
│   └── LoginViewModel.cs   # ViewModel สำหรับ Login
├── Data/
│   └── ApplicationDbContext.cs  # DbContext
├── Services/
│   ├── PasswordService.cs  # บริการเข้ารหัส SHA-256
│   └── AuthService.cs      # บริการการตรวจสอบสิทธิ์
├── Controllers/
│   ├── HomeController.cs   # Controller หลักพร้อม Login/Dashboard
│   └── SeedController.cs   # Controller สำหรับสร้างข้อมูลตัวอย่าง
├── Views/
│   └── Home/
│       ├── Index.cshtml    # หน้า Login
│       └── Dashboard.cshtml # หน้าหลักหลังเข้าสู่ระบบ
└── wwwroot/css/
    └── site.css            # CSS ที่ปรับปรุงแล้ว
```

### 🔐 **ระบบความปลอดภัย**
- รหัสผ่านเข้ารหัสด้วย SHA-256
- Cookie Authentication พร้อม Secure settings
- Anti-forgery token protection
- Session timeout management

### 👥 **ผู้ใช้ทดสอบ (Test Users)**

หลังจากสร้างฐานข้อมูลแล้ว ให้เรียก URL: `/Seed/CreateSampleData` เพื่อสร้างข้อมูลตัวอย่าง

**ผู้ใช้ทดสอบ:**
- **Admin:** username: `admin`, password: `admin123`
- **Doctor:** username: `doctor1`, password: `doctor123`

## การติดตั้งและการใช้งาน

### 1. ข้อกำหนดระบบ
- .NET 9.0 หรือใหม่กว่า
- SQL Server Express หรือ SQL Server
- Visual Studio 2022 หรือ VS Code

### 2. การตั้งค่าฐานข้อมูล
โปรเจคนี้ได้กำหนดให้ไม่ใช้ Migration ดังนั้นต้องสร้างฐานข้อมูลด้วยตนเอง

**Connection String ในไฟล์ `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=db_project;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

### 3. ขั้นตอนการติดตั้ง

1. **สร้างฐานข้อมูล:** รัน SQL Script ที่กำหนดให้ในฐานข้อมูล SQL Server
2. **Restore packages:** `dotnet restore`
3. **Build โปรเจค:** `dotnet build`
4. **สร้างข้อมูลตัวอย่าง:** เข้า `/Seed/CreateSampleData`
5. **รันโปรเจค:** `dotnet run`

### 4. การใช้งาน
1. เปิดเบราว์เซอร์ไปที่ `https://localhost:5001` หรือ `http://localhost:5000`
2. จะเจอหน้า Login
3. ใช้ผู้ใช้ทดสอบที่กำหนดไว้เพื่อเข้าสู่ระบบ
4. หลังเข้าสู่ระบบจะไปที่หน้า Dashboard

## เทคโนโลยีที่ใช้

- **ASP.NET Core 9.0 MVC**
- **Entity Framework Core 9.0**
- **SQL Server**
- **Bootstrap 5**
- **FontAwesome 6**
- **jQuery**
- **Cookie Authentication**
- **SHA-256 Password Hashing**

## โครงสร้างฐานข้อมูล
โปรเจคนี้มีตารางฐานข้อมูลหลัก 15 ตาราง:
- Role, Title, Treatment, Form
- User, Log, Patient
- Province, Amphur, District  
- Question, Option, Evaluation, EvaluationDetail, Interpreter

พร้อมความสัมพันธ์แบบ Foreign Key ตามที่กำหนด

## การพัฒนาต่อ
- เพิ่มหน้าจัดการผู้ใช้
- เพิ่มหน้าจัดการผู้ป่วย
- เพิ่มระบบแบบฟอร์มประเมิน
- เพิ่มระบบรายงาน
- เพิ่มระบบสิทธิ์การเข้าถึง (Authorization)
