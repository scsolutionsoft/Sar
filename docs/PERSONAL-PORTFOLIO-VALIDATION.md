# ผลทดสอบคลังเอกสารและแฟ้มศึกษาต่อ — 26 กันยายน 2026

- Migration `20260926152934_AddPersonalDocumentsAndAdmissions` ใช้กับฐานข้อมูล Sar แล้ว
- Build solution ผ่าน ไม่มี warnings/errors; unit tests ผ่าน 38 กรณี
- EF ตรวจแล้วไม่มี model changes ที่ขาด migration; ชุด UiChecks เดิมผ่านทั้ง popup ภาพ/PDF รายงาน เกียรติบัตร มือถือ และ fallback ไฟล์หาย
- สร้างบัญชีทดสอบ 9 บัญชี ครบ 8 บทบาท รวม Student สองคน รหัสผ่านสุ่มแยกกันและจัดเก็บด้วย Identity hash บัญชีเดิมไม่ถูกรีเซ็ต
- ทดสอบล็อกอินทั้ง 9 บัญชี บทบาท สิทธิ์จัดการผู้ใช้และข่าว การอ่าน PDF ของตน และการปฏิเสธไฟล์/แฟ้มของคนอื่น รวมบัญชี SuperAdmin ผ่าน
- Browser workflow ผ่าน: สร้างรายงานเป็น PDF, อัปโหลด PDF, บันทึกรายงานจากหน้ารายงานลงคลังส่วนตัว, สร้างและแก้ไขแฟ้ม, เพิ่มภาพ/ผลงาน, เรียงลำดับ, รวมหลักฐานโดยไม่ซ้ำ, ตรวจจำนวนหน้าเกินกำหนด, ยกเลิก PDF เก่าเมื่อแก้เนื้อหา และ soft delete
- ทดสอบปลอม OwnerId, POST ข้ามเจ้าของโดยมี CSRF token, ขาด CSRF token, anonymous access และ Audit ที่ไม่คัดลอกเนื้อหาส่วนตัว ผ่าน
- แฟ้มตัวอย่างนักเรียน 10 หน้า รวมหลักฐาน PDF สองรายการ ตรวจจำนวนหน้าและ SHA-256 เทียบ binary ใน SQL ผ่าน
- Flipbook เปลี่ยนหน้า เปิด popup PDF ปิดด้วย Escape แสดง infographic และมือถือ 390px ผ่าน ไม่มี JavaScript page errors
- ข้อมูลและไฟล์ชั่วคราวของชุดทดสอบลบเฉพาะรายการที่สร้างในรอบทดสอบแล้ว
- Publish Release / win-x64 ไปที่ `artifacts/publish` สำเร็จ ไม่รวมไฟล์ credentials และยังไม่ได้ติดตั้งบน IIS

บัญชี: `artifacts/private/TEST-ACCOUNTS.md`  
PDF ตัวอย่าง: `artifacts/private/student-sample-portfolio.pdf`  
ภาพหน้าจอ: `artifacts/ui/admission-studio.png`, `admission-flipbook.png`, `admission-infographic.png`, `admission-mobile.png`  
คู่มือ: [PERSONAL-PORTFOLIO.md](PERSONAL-PORTFOLIO.md)
