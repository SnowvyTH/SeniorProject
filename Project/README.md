# Project - ASP.NET Core MVC

โปรเจค ASP.NET Core MVC สำหรับการจัดการฐานข้อมูลผู้ป่วยและแบบประเมิน

## โครงสร้างโปรเจค

### Models/Entity/
ไฟล์ Entity สำหรับตารางฐานข้อมูล:
- `Role.cs` - ตารางบทบาทผู้ใช้
- `Title.cs` - ตารางคำนำหน้าชื่อ
- `Treatment.cs` - ตารางการรักษา
- `Form.cs` - ตารางแบบฟอร์มประเมิน
- `User.cs` - ตารางผู้ใช้งานระบบ
- `Log.cs` - ตารางบันทึกการใช้งาน
- `Patient.cs` - ตารางข้อมูลผู้ป่วย
- `Province.cs` - ตารางจังหวัด
- `Amphur.cs` - ตารางอำเภอ
- `District.cs` - ตารางตำบล
- `Question.cs` - ตารางคำถาม
- `Option.cs` - ตารางตัวเลือกคำตอบ
- `Evaluation.cs` - ตารางการประเมิน
- `EvaluationDetail.cs` - ตารางรายละเอียดการประเมิน
- `Interpreter.cs` - ตารางการแปลผลคะแนน

### Data/
- `ApplicationDbContext.cs` - DbContext สำหรับ Entity Framework

## การติดตั้งและการใช้งาน

### 1. ข้อกำหนดระบบ
- .NET 9.0 หรือใหม่กว่า
- SQL Server Express หรือ SQL Server
- Visual Studio 2022 หรือ VS Code

### 2. การตั้งค่าฐานข้อมูล
โปรเจคนี้ได้กำหนดให้ไม่ใช้ Migration ดังนั้นต้องสร้างฐานข้อมูลด้วยตนเอง

Connection String ในไฟล์ `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=db_project;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

### 3. การสร้างฐานข้อมูล
รัน SQL Script ที่กำหนดให้ในฐานข้อมูล SQL Server เพื่อสร้างตารางต่างๆ

### 4. การรันโปรเจค
```bash
dotnet restore
dotnet build
dotnet run
```

## คุณสมบัติหลัก
- ระบบจัดการผู้ใช้และบทบาท
- ระบบจัดการข้อมูลผู้ป่วย
- ระบบแบบฟอร์มประเมิน
- ระบบจัดการที่อยู่ (จังหวัด อำเภอ ตำบล)
- ระบบบันทึกการใช้งาน

## โครงสร้างฐานข้อมูล
โปรเจคนี้มีตารางฐานข้อมูลหลัก 15 ตาราง พร้อมความสัมพันธ์แบบ Foreign Key ตามที่กำหนด

## Entity Framework
ใช้ Entity Framework Core 9.0 สำหรับการจัดการฐานข้อมูล โดยไม่ใช้ Migration ตามที่ระบุ
