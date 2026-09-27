# SchoolPortfolioPR

ระบบ ASP.NET Core MVC / .NET 8 สำหรับข่าว กิจกรรม รางวัล แฟ้มผลงาน และวารสารโรงเรียน UI ภาษาไทย แสดงวันที่ พ.ศ. ช่องกรอกวันที่และเดือนวารสารรับปี ค.ศ.

เพิ่มคลัง PDF ส่วนตัวและแฟ้มสะสมผลงานยื่นเรียนต่อ พร้อมบัญชีทดสอบครบ 8 บทบาท (รวม Student) ดู [คู่มือแฟ้มผลงานและบัญชีทดสอบ](docs/PERSONAL-PORTFOLIO.md)

หน้าแรกและคิวงานแยกตามบทบาท พร้อมขอบเขตสิทธิ์และวิธีตรวจทุกบัญชี: [พื้นที่ทำงานแต่ละบทบาท](docs/ROLE-WORKSPACES.md)

แฟ้มยื่นเรียนต่อใช้ได้เฉพาะ Student ทั้งเมนู หน้าใช้งาน และไฟล์แฟ้ม เมนูแบ่งกรอบหมวดหมู่พร้อมไอคอนสีและรองรับมือถือ

เลือกผู้ร่วมกิจกรรมและผู้รับรางวัลด้วยตัวกรองกลุ่มสาระ / ระดับชั้น / ห้อง พร้อมค้นชื่อทันที: [คู่มือชุดเลือกรายชื่อ](docs/PARTICIPANT-PICKER.md)

เพิ่มกระดานประชาสัมพันธ์หน้าแรก: เลือกภาพ กรอบเล็ก/กลาง/ใหญ่ ลำดับ และวันเริ่ม–สิ้นสุดได้ พร้อมวารสารตัวอย่างที่ `/publications/2569/9` ดู [คู่มือกระดานข่าวและวารสาร](docs/PUBLIC-RELATIONS.md)

## เริ่มใช้งานบนเครื่องนี้

ตั้งค่าการเชื่อมต่อฐานข้อมูล Sar ที่ผู้ใช้ระบุไว้ใน .NET User Secrets แล้ว ไม่เก็บรหัสผ่านใน Git และไม่ใส่ใน appsettings ที่ publish ออกไป ตารางทั้งหมดอยู่ใน schema `portfolio` รวมถึง Identity และประวัติ migrations

```powershell
dotnet restore
dotnet build
dotnet tool restore
dotnet run --project SchoolPortfolioPR -- --check-db
dotnet run --project SchoolPortfolioPR -- --initialize
dotnet run --project SchoolPortfolioPR
```

เปิด http://localhost:5093 บัญชีเริ่มต้น `admin@school.local` รหัสผ่านสุ่มอยู่ใน User Secrets ของผู้ใช้ Windows คนนี้ ดูได้ในเทอร์มินัลส่วนตัวด้วย:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Get-LocalAdmin.ps1
```

เปลี่ยนชื่อโรงเรียนจากเมนู **ข้อมูลโรงเรียน** แล้วเพิ่มฝ่าย กลุ่มสาระ ครู นักเรียน ปีการศึกษา และผู้ใช้จากเมนูผู้ดูแล ก่อนเริ่มบันทึกข้อมูลจริง บัญชี Teacher และ DepartmentAdmin ต้องกำหนดฝ่าย

เครื่องใหม่ใช้ `scripts/Set-LocalSecrets.ps1` เพื่อรับ connection string และรหัสผ่านแบบซ่อนตัวอักษร หรือกำหนด `ConnectionStrings__DefaultConnection`, `SeedAdmin__Email`, `SeedAdmin__Password` ผ่าน environment variables. User Secrets แยกจาก repository แต่ไม่ได้เข้ารหัสไฟล์

`--initialize` ใช้ migration และสร้างบทบาท/ข้อมูลพื้นฐาน หากอีเมลนั้นมีบัญชีอยู่แล้ว จะไม่เปลี่ยนรหัสผ่านหรือยกระดับสิทธิ์ซ้ำ การรันเว็บปกติไม่แก้ schema อัตโนมัติ

## โมดูลและการทำงาน

| โมดูล | เส้นทาง | ความสามารถ |
|---|---|---|
| เว็บไซต์สาธารณะ | `/` | ข่าว กิจกรรมแนะนำ รางวัล วารสาร ค้นหา/แบ่งหน้า |
| ภาพรวม | `/Dashboard` | 9 ตัวชี้วัด กราฟเดือน ฝ่าย ประเภท และระดับรางวัล ตามสิทธิ์ผู้ใช้ |
| กิจกรรม | `/Activities` | ร่าง/แก้ไข ผู้ร่วม แท็ก โครงการ ข่าว SEO รูปปก จัดลำดับภาพ เอกสาร |
| รางวัล | `/Awards` | การแข่งขัน ระดับ อันดับ ผู้จัด ครู/นักเรียน บทบาทครู เกียรติบัตร |
| แฟ้มผลงาน | `/Portfolios` | กิจกรรม โครงการ รางวัล เกียรติบัตร กรองบุคคล/ปี/วันที่/ฝ่าย/กลุ่มสาระ |
| วารสาร | `/Publications` | เลือกข้อมูลเผยแพร่ตามเดือน จัดลำดับ สร้าง PDF 11 หมวดหลัก |
| Flipbook | `/publications/2569/09` | PDF.js เปลี่ยนหน้า ปัดหน้าบนมือถือ คีย์ลูกศร ซูม เต็มจอ สารบัญ ดาวน์โหลด แชร์ QR |
| รายงาน | `/Reports` | 12 ประเภทรายงาน ตัวกรองร่วม ส่งออก PDF/DOCX/XLSX และพิมพ์ |
| ข้อมูลหลัก | `/Masters` | โรงเรียน ฝ่าย กลุ่มสาระ ครู นักเรียน ปีการศึกษา ประเภทกิจกรรม แท็ก |
| บัญชี | `/Users` | สร้างผู้ใช้ แก้ไขฝ่าย/บทบาท ตั้งรหัสผ่านใหม่ โดย SuperAdmin |
| Audit | `/Audit` | ก่อน/หลัง ผู้ใช้ การกระทำ Entity/ID IP เวลา UTC |

ไฟล์อยู่ใน SQL Server เป็น `varbinary(max)` พร้อม SHA-256, MIME, ขนาด, ผู้ส่ง และวันเวลา เข้าถึงผ่าน controller ที่ตรวจสิทธิ์ตามกิจกรรม/รางวัล/วารสาร ไม่เผยแพร่ไฟล์ดิบใน wwwroot. `IFileStorage` เป็นจุดเปลี่ยนไปใช้ FILESTREAM ในอนาคต

รองรับ JPG/JPEG/PNG/WEBP/PDF/DOCX/XLSX ตรวจ extension, MIME, magic bytes และโครงสร้าง Office ZIP จำกัด 10 MB ต่อไฟล์ 20 ไฟล์/50 MB ต่อคำขอ เอกสาร Office ดาวน์โหลด ส่วนรูป/PDF เปิด preview ได้ การตรวจชนิดไฟล์ไม่ใช่ระบบตรวจไวรัส

ลำดับงาน: Draft → Submitted → Reviewed → Approved → Published → Archived. ส่งกลับ Rejected ได้จาก Submitted/Reviewed แล้วแก้ไขและส่งใหม่ ข้ามขั้นตอนไม่ได้

- Teacher: บันทึก/ส่งตรวจรายการของตนในฝ่าย แก้ไขเฉพาะ Draft/Rejected
- DepartmentAdmin: ตรวจและจัดการฉบับร่างในฝ่ายที่ได้รับมอบหมาย
- Approver: อนุมัติรายการที่ผ่านตรวจ
- PRAdmin: เผยแพร่รายการที่อนุมัติ จัดทำวารสาร
- Administrator / SuperAdmin: จัดการข้อมูลทุกฝ่าย; SuperAdmin จัดการบัญชีผู้ใช้
- Viewer: อ่านข้อมูลสาธารณะที่เผยแพร่ แฟ้มผลงานและรายงานที่เข้าถึงได้

การแก้ไขวารสารทำให้ PDF รุ่นเดิมถูกซ่อนและยกเลิกเผยแพร่ ต้องสร้าง PDF ใหม่ก่อนเผยแพร่ เนื้อหา PDF เป็นสำเนา ณ เวลาสร้าง หากแก้ไข/ถอนข่าวภายหลัง ให้แก้ไขและสร้างวารสารใหม่ด้วย วารสารนำเข้าเฉพาะข้อมูลที่ตั้งเป็นสาธารณะแล้ว สารบัญตัวอ่านใช้ PDF outline ที่ฝังจากหัวข้อในเอกสาร; ลำดับรายการใช้ภายในหมวดอัตโนมัติ

## โครงสร้างซอร์ส

```text
SchoolPortfolioPR.sln
SchoolPortfolioPR/
  Controllers/             MVC, authorization, form validation
  Models/Domain.cs          Normalized entities and join tables
  ViewModels/Forms.cs       Form models, filters, Thai date/status display
  Data/ApplicationDbContext.cs  Keys, indexes, filters, transactions, audit
  Data/DbInitializer.cs     Explicit migration and role/admin initialization
  Migrations/              EF SQL Server migration and snapshot
  Services/                Access, SQL storage, reports, exports, publications
  Views/                   Razor pages and reusable partials
  wwwroot/                 Local Bootstrap/Materia/PDF.js, CSS, JavaScript
  appsettings*.json         Non-secret configuration
  web.config               IIS ASP.NET Core Module and upload limits
SchoolPortfolioPR.Tests/   Isolated tests without database access
SchoolPortfolioPR.Smoke/   SQL transaction rollback + Playwright browser smoke
scripts/                  Secret setup and IIS publish helpers
docs/IIS.md                Deployment instructions
```

Identity ใช้ MVC AccountController และ UserManager/SignInManager โดยตรง ไม่มีหน้าลงทะเบียนสาธารณะ และไม่ใช้ scaffolding ของ Identity Razor Pages. EF DbContext ทำหน้าที่ unit of work จึงไม่มี repository ที่เพียงห่อเมธอด EF ซ้ำ

## ทดสอบ

```powershell
dotnet test
powershell -ExecutionPolicy Bypass -File SchoolPortfolioPR/bin/Debug/net8.0/playwright.ps1 install chromium
# เปิดเว็บอีกเทอร์มินัลก่อนรัน smoke
dotnet run --project SchoolPortfolioPR.Smoke
```

Smoke test อ่าน User Secrets ทดสอบ SQL ใน transaction แล้ว rollback รวมข้อมูลไฟล์และ Audit ที่สร้างชั่วคราว จากนั้นตรวจหน้าเว็บจริงและดาวน์โหลดรายงานด้วยบัญชีเริ่มต้น ทดสอบแบบฟอร์มกิจกรรม/อัปโหลด/สถานะ/วารสารด้วยข้อมูลชั่วคราวที่ commit เพื่อให้ process เว็บอ่านได้ และลบเฉพาะ ID ของรอบทดสอบใน finally ภาพหน้าจอและผลส่งออกเก็บใน `artifacts/` ที่ไม่รวม Git (Identity login อาจอัปเดตข้อมูลการล็อกอินตามปกติ) ใช้ `-- --browser-only` เพื่อทดสอบเฉพาะส่วนเบราว์เซอร์

PDF ใช้ Chromium ผ่าน Playwright เพื่อรองรับการจัดรูปภาษาไทย ติดตั้ง browser บนเครื่องรันเว็บด้วย ฟอนต์ Tahoma บน Windows หรือฟอนต์ไทยที่ติดตั้งไว้บนระบบ

## เอกสารอ้างอิง

- [Microsoft: User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-8.0)
- [Microsoft: Applying migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying)
- [ติดตั้ง IIS](docs/IIS.md)
- [ธีมและป๊อปอัปดูรูป/PDF](docs/UI-PREVIEW.md)
- [ตั้งค่าชื่อและโลโก้โปรแกรม](docs/APPLICATION-BRANDING.md)
- [คลังภาพกิจกรรมและลิงก์คลาวด์](docs/ACTIVITY-GALLERY.md)
- [Slug อัตโนมัติและตัวช่วยใช้งานตามบทบาท](docs/HELP-WIZARD.md)

ระบบผ่านการทดสอบตามคำสั่งข้างต้นเมื่อส่งมอบ การติดตั้ง production ยังต้องกำหนดโดเมน/HTTPS, ตัวตน IIS, บัญชี SQL สำหรับแอป, เส้นทาง browser, backup และสิทธิ์ไฟล์ให้เหมาะกับเซิร์ฟเวอร์จริง

