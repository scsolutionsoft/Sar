# ภาพประกอบข้อมูลตัวอย่าง

ภาพสต็อกประกอบกิจกรรมสมมติ ใช้ทดสอบระบบเท่านั้น ไม่ใช่ภาพกิจกรรมจริงของโรงเรียน และไม่ได้แสดงถึงการรับรองจากบุคคลหรือเจ้าของภาพ

ตรวจสอบแหล่งที่มาเมื่อ 26 กันยายน 2026 ใช้งานตาม [Pexels License](https://www.pexels.com/license/) โดยภาพ 265076 มีป้าย CC0 บนหน้าต้นทางด้วย

| กิจกรรม | ภาพและแหล่งที่มา | เครดิต |
| --- | --- | --- |
| DEMO-ACT-001 วิทยาศาสตร์ | [ห้องปฏิบัติการ 4033019](https://www.pexels.com/photo/a-man-using-a-microscope-in-a-laboratory-4033019/) | Edward Jenner / Pexels |
| DEMO-ACT-002 คณิตศาสตร์ | [เครื่องคิดเลขและสมุด 36488453](https://www.pexels.com/photo/calculator-and-notebook-on-study-desk-36488453/) | Jahra Tasfia Reza / Pexels |
| DEMO-ACT-003 ภาษาไทย | [หนังสือเพื่อการเรียนรู้ 18620043](https://www.pexels.com/photo/books-stacked-on-library-shelves-18620043/) | aboodi vesakaran / Pexels |
| DEMO-ACT-004 ภาษาอังกฤษ | [หนังสือและกระดาน ABC 265076](https://www.pexels.com/photo/abc-books-chalk-chalkboard-265076/) | Pixabay / Pexels (CC0) |
| DEMO-ACT-005 กีฬาสี | [สนามบาสเกตบอล 15305007](https://www.pexels.com/photo/ball-on-basketball-court-15305007/) | Dhally Romy / Pexels |
| DEMO-ACT-006 จิตอาสา | [ดูแลสวน 27176068](https://www.pexels.com/photo/person-plucking-weeds-from-soil-27176068/) | Helena Lopes / Pexels |
| DEMO-ACT-007 สื่อดิจิทัล | [คอมพิวเตอร์ในห้องเรียน 20432916](https://www.pexels.com/photo/laptop-in-a-classroom-20432916/) | Adam Sondel / Pexels |
| DEMO-ACT-008 โครงงาน | [คอมพิวเตอร์และเอกสาร 6683440](https://www.pexels.com/photo/a-laptop-on-the-table-with-paper-and-pencil-on-the-side-6683440/) | Andy Barbour / Pexels |
| DEMO-ACT-009 ห้องสมุด | [ชั้นหนังสือ 18620043](https://www.pexels.com/photo/books-stacked-on-library-shelves-18620043/) | aboodi vesakaran / Pexels |
| DEMO-ACT-010 ปลูกต้นไม้ | [ต้นกล้าในมือ 12969371](https://www.pexels.com/photo/close-up-of-man-holding-a-seedling-in-soil-in-his-hands-12969371/) | Atlantic Ambience / Pexels |

## การจัดเก็บและนำเข้า

ภาพ 10 ไฟล์ (9 ภาพต้นฉบับ ภาพชั้นหนังสือใช้ประกอบ 2 กิจกรรม) ดาวน์โหลดเป็น JPEG กว้าง 1280 พิกเซลจาก URL รูปแบบ `https://images.pexels.com/photos/{id}/pexels-photo-{id}.jpeg?auto=compress&cs=tinysrgb&w=1280&fm=jpg`

ไบต์ของภาพและเอกสารเก็บใน `portfolio.FileObjects.FileData` ชนิด SQL Server `varbinary(max)` พร้อม MIME, ขนาดไฟล์ และ SHA-256 ใช้ `SqlFileStorage` ชุดเดียวกับการอัปโหลดปกติ ภาพแสดงผ่าน `/files/{FileId}` โดยตรวจสิทธิ์ของกิจกรรม ไม่ hotlink ภาพภายนอก และไม่สร้างโฟลเดอร์ uploads สำหรับไฟล์เหล่านี้

ตัวนำเข้าจะเพิ่มเฉพาะภาพที่ยังไม่มีในกิจกรรมตัวอย่าง เก็บภาพปกเดิมหากมีอยู่แล้ว และรักษาสถานะการเผยแพร่ของกิจกรรม

```powershell
dotnet run --project SchoolPortfolioPR.Seed -- --images --verify-ui
dotnet run --project SchoolPortfolioPR.Seed -- --verify-images --verify-ui
```

คำสั่งแรกต้องมีข้อมูลตัวอย่าง 10 กิจกรรมอยู่แล้ว และอ่านการเชื่อมต่อฐานข้อมูลจาก User Secrets หรือ environment เหมือนโปรเจคหลัก คำสั่งที่สองตรวจสอบอย่างเดียว ไม่ดาวน์โหลดหรือลงข้อมูลซ้ำ

การตรวจสอบอ่านภาพและเอกสารตัวอย่างกลับจาก SQL เทียบขนาด ลายเซ็นไฟล์ และ SHA-256 ตรวจชนิดคอลัมน์จริง แล้วทดสอบภาพปก ป๊อปอัปภาพทั้ง 10 กิจกรรม ป๊อปอัป PDF ไบต์ที่ส่งผ่าน HTTP และสิทธิ์เข้าถึงภาพส่วนตัว โดยบล็อกคำขอภายนอกระหว่างทดสอบหน้าเว็บ

ผลทดสอบวันที่ 26 กันยายน 2026: เพิ่มภาพ 10 ไฟล์ รวม 1,362,497 ไบต์ลง Sar สำเร็จ ตรวจภาพ 10 ไฟล์และเอกสาร 7 ไฟล์ผ่านทุกข้อ ตรวจหน้าเว็บทั้ง 10 กิจกรรมผ่าน ไม่มี JavaScript error และรันนำเข้าซ้ำเพิ่ม 0 ไฟล์ ภาพหน้าจออยู่ที่ `artifacts/ui/sample-image-popup.png` และ `artifacts/ui/sample-activity-with-images.png`
