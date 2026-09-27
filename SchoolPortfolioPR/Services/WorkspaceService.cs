using Microsoft.EntityFrameworkCore;
using SchoolPortfolioPR.Data;
using SchoolPortfolioPR.ViewModels;
namespace SchoolPortfolioPR.Services;
public class WorkspaceService(ApplicationDbContext db, AccessService access)
{
    public async Task<WorkspaceView> BuildAsync()
    {
        var role = Roles.All.FirstOrDefault(access.User.IsInRole) ?? "Viewer";
        var model = new WorkspaceView { Role = Roles.Labels[role], Description = role switch {
            "SuperAdmin"=>"ดูแลบัญชี สิทธิ์ ข้อมูลพื้นฐาน และทุกขั้นตอนของผลงานโรงเรียน",
            "Administrator"=>"จัดการข้อมูลโรงเรียน ตรวจสอบ อนุมัติ และเผยแพร่ผลงาน",
            "PRAdmin"=>"เผยแพร่เรื่องที่อนุมัติแล้ว จัดข่าวตามช่วงเวลา และจัดทำวารสาร",
            "Approver"=>"ตรวจพิจารณารายการที่ฝ่ายตรวจแล้ว เพื่ออนุมัติหรือส่งกลับแก้ไข",
            "DepartmentAdmin"=>"ตรวจสอบรายการของฝ่าย และส่งต่อให้ผู้อนุมัติ",
            "Teacher"=>"สร้างกิจกรรมและรางวัลของคุณ แนบหลักฐาน และส่งตรวจตามขั้นตอน",
            "Student"=>"จัดเก็บหลักฐานและสร้างแฟ้มศึกษาต่อของตนเอง พร้อมส่งออก PDF",
            _=>"อ่านข่าว ผลงาน และวารสารที่เผยแพร่ พร้อมเก็บเอกสารในคลังส่วนตัว" },
            Documents = await db.PersonalDocuments.CountAsync(x=>x.OwnerId==access.UserId),
            Portfolios = access.IsStudent ? await db.AdmissionPortfolios.CountAsync(x=>x.OwnerId==access.UserId) : 0 };
        if(access.IsEditor){model.Pending=await (await access.PendingActivitiesAsync()).CountAsync()+await (await access.PendingAwardsAsync()).CountAsync();model.Links.Add(new("งานที่รอดำเนินการ",$"{model.Pending} รายการที่ดำเนินการได้ตามสิทธิ์","/Workflows"));}
        if(await access.CanAuthorAsync()){model.Links.Add(new("เพิ่มกิจกรรม","บันทึกเรื่องราวและแนบภาพหรือเอกสาร","/Activities/Edit"));model.Links.Add(new("เพิ่มรางวัล","บันทึกความสำเร็จและเกียรติบัตร","/Awards/Edit"));}
        if(access.IsPublisher){model.Links.Add(new("จัดข่าวประชาสัมพันธ์","กำหนดวันแสดงและขนาดกรอบหน้าแรก","/Announcements"));model.Links.Add(new("จัดทำวารสาร","รวบรวมเรื่องที่เผยแพร่เป็น Flipbook","/Publications"));}
        if(access.IsAdmin){model.Links.Add(new("ข้อมูลพื้นฐาน","จัดการฝ่าย ครู นักเรียน และปีการศึกษา","/Masters"));model.Links.Add(new("ประวัติการใช้งาน","ติดตามการเปลี่ยนแปลงในระบบ","/Audit"));}
        if(access.IsAdmin)model.Links.Add(new("ตั้งค่าโปรแกรม","เปลี่ยนชื่อและโลโก้ที่แสดงในระบบ","/Branding"));
        if(access.User.IsInRole("SuperAdmin"))model.Links.Add(new("ผู้ใช้และสิทธิ์","สร้างบัญชี กำหนดบทบาทและฝ่าย","/Users"));
        if(access.IsStudent)model.Links.Add(new("สร้างแฟ้มยื่นเรียนต่อ","เลือกธีม จัดผลงาน และส่งออก PDF","/Admissions"));
        model.Links.Add(new("คลัง PDF ของฉัน",$"เอกสารส่วนตัว {model.Documents} รายการ","/Documents"));
        model.Links.Add(new("คลังภาพกิจกรรม","ชมภาพพร้อมคำบรรยายและอัลบั้มจากคลาวด์","/Gallery"));
        if(!access.IsEditor&&!access.IsStudent)model.Links.Add(new("อ่านวารสารโรงเรียน","ข่าวและผลงานที่เปิดเผยแพร่","/Publications"));
        model.Links.Add(new(access.IsStudent?"รายงานกิจกรรมของฉัน":"รายงานและผลงาน",access.IsStudent?"รายงานกิจกรรมที่เผยแพร่และมีชื่อคุณเข้าร่วม":"สร้างรายงานจากข้อมูลที่คุณมีสิทธิ์ดู","/Reports"));
        return model;
    }
}
