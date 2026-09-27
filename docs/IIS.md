# ติดตั้งบน Windows Server IIS x64

1. ติดตั้ง IIS และ ASP.NET Core **.NET 8 Hosting Bundle x64** จาก Microsoft จากนั้น restart IIS ถ้าเคยติดตั้ง bundle ก่อน IIS ให้ repair bundle
2. Build/publish จากเครื่องพัฒนา:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Publish-IIS.ps1
```

3. สร้าง App Pool `SchoolPortfolioPR`, .NET CLR = **No Managed Code**, Enable 32-bit Applications = **False**, Load User Profile = **True**. สร้างเว็บไซต์โดยให้ physical path ชี้ไปยังโฟลเดอร์ publish; ให้ `IIS AppPool\SchoolPortfolioPR` มีสิทธิ์ Read & Execute
4. ตั้ง environment variables ใน IIS Configuration Editor / `system.webServer/aspNetCore` ของไซต์ โดยเก็บใน configuration ของเซิร์ฟเวอร์ ไม่ใส่รหัสผ่านลง repository:

| ตัวแปร | ค่า |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | connection string ของ Sar ตามสภาพแวดล้อม |
| `AllowedHosts` | โดเมนของเว็บไซต์จริง |
| `PLAYWRIGHT_BROWSERS_PATH` | เช่น `C:\ProgramData\SchoolPortfolioPR\browsers` |

5. ใช้บัญชี deploy ที่มีสิทธิ์ DDL เพื่อ apply migration **ครั้งละหนึ่ง process** ก่อนเปิดเว็บไซต์ สร้าง SQL สำหรับตรวจสอบได้ด้วย:

```powershell
dotnet tool restore
dotnet ef migrations script --project SchoolPortfolioPR --idempotent --output artifacts/migrations.sql
```

สำรอง Sar ก่อนอัปเกรด อ่าน SQL ก่อนใช้งาน ตารางใหม่อยู่ใน schema `portfolio`. ใช้ `dotnet SchoolPortfolioPR.dll --initialize` จากโฟลเดอร์ publish โดยตั้ง `ConnectionStrings__DefaultConnection`, `SeedAdmin__Email`, `SeedAdmin__Password` ใน environment ของ process สำหรับการสร้างเริ่มต้น ลบตัวแปร SeedAdmin หลังสร้างบัญชีแล้ว ไม่ใช้ `EnsureCreated` และไม่ลบฐานข้อมูล

6. บัญชี SQL สำหรับ process เว็บควรมี SELECT/INSERT/UPDATE/DELETE เฉพาะ schema `portfolio`; บัญชี migration แยกต่างหาก Connection ที่ให้มาถูกเก็บไว้ใน User Secrets สำหรับเครื่องพัฒนาเท่านั้น และจะไม่ถูกคัดลอกไป publish
7. ติดตั้ง Chromium ไปยังเส้นทางที่ App Pool อ่าน/เรียกใช้ได้:

```powershell
$env:PLAYWRIGHT_BROWSERS_PATH = 'C:\ProgramData\SchoolPortfolioPR\browsers'
powershell -ExecutionPolicy Bypass -File .\playwright.ps1 install chromium
```

ตั้ง `PLAYWRIGHT_BROWSERS_PATH` ค่าเดียวกันให้ IIS App Pool และ recycle pool ติดตั้งฟอนต์ภาษาไทย เช่น Tahoma บนเซิร์ฟเวอร์ ตรวจการสร้าง PDF ก่อนเปิดใช้จริง

8. ผูก HTTPS certificate กับโดเมนจริง Production เปิด HSTS/HTTPS redirect และ secure cookies แล้ว กรณีอยู่หลัง reverse proxy ต้องกำหนด trusted proxies และ forwarded headers ให้ตรง topology ก่อนเปิดใช้งาน
9. เก็บ ASP.NET Data Protection keys ใน user profile ที่ถาวร/มี ACL จำกัด เมื่อใช้หลาย instance ต้องกำหนด key store ร่วมและ certificate สำหรับปกป้อง keys ก่อนขยายระบบ ไม่ใช้ profile ชั่วคราว
10. `web.config` ตั้ง upload limit 55 MiB แอปจำกัด batch 50 MiB และไฟล์ละ 10 MiB ให้สิทธิ์เขียนเฉพาะตำแหน่ง logs/temp ที่จำเป็น ไฟล์ของผู้ใช้อยู่ใน SQL Server ทั้งหมด

## ตรวจหลัง deploy

- หน้าแรกและ login โหลดผ่าน HTTPS; anonymous เข้า Dashboard ถูกส่งไป login
- ผู้ดูแลสร้างกิจกรรม อัปโหลดภาพ/PDF ตรวจภาพปกและ preview
- ทดสอบ Teacher → DepartmentAdmin → Approver → PRAdmin ตามฝ่าย
- รายงาน PDF/DOCX/XLSX และวารสารภาษาไทยเปิดได้ครบ
- Flipbook, QR และลิงก์ดาวน์โหลดเปิดผ่านโดเมนจริง
- ตรวจ AuditLog, backup SQL (รวม varbinary), restore และพื้นที่ฐานข้อมูล

เก็บ stdout logging ปิดไว้ตามค่าเริ่มต้น หากต้องแก้ปัญหา 500.30 ให้เปิดชั่วคราวใน web.config และสร้างโฟลเดอร์ logs ที่ App Pool เขียนได้ จากนั้นปิดเมื่อแก้ไขเสร็จ ห้ามเปิด EF sensitive data logging ใน production
