(() => {
 const form=document.getElementById('announcementForm'),preview=document.getElementById('announcementPreview');
 if(!form||!preview)return;
 let imageUrl;
 const value=id=>document.getElementById(id)?.value||'';
 function update(){
  const selected=form.querySelector('input[name=SourceImageId]:checked');
  const upload=document.getElementById('Upload').files[0];
  const remove=document.getElementById('RemoveImage')?.checked;
  if(imageUrl){URL.revokeObjectURL(imageUrl);imageUrl=null;}
  const src=remove?'':upload?(imageUrl=URL.createObjectURL(upload)):selected?.value?'/files/'+selected.value:preview.dataset.currentImage;
  const img=preview.querySelector('img');img.hidden=!src;if(src)img.src=src;else img.removeAttribute('src');
  const tones=['indigo','teal','amber','rose'];
  preview.className='pr-tile pr-preview pr-tone-'+(tones[Number(value('Tone'))]||'indigo');
  const size=form.querySelector('input[name=Size]:checked')?.value||'Medium';
  preview.classList.add('pr-preview-'+size.toLowerCase());
  preview.querySelector('[data-preview-title]').textContent=value('Title')||'หัวข้อข่าวประชาสัมพันธ์';
  preview.querySelector('[data-preview-summary]').textContent=value('Summary')||'ข้อความสั้นที่ชวนให้ติดตามรายละเอียด';
  preview.querySelector('[data-preview-category]').textContent=value('Category')||'ประชาสัมพันธ์';
  const end=value('EndDate');preview.querySelector('[data-preview-date]').textContent=end?'แสดงถึง '+new Date(end+'T00:00:00+07:00').toLocaleDateString('th-TH',{timeZone:'Asia/Bangkok',day:'numeric',month:'short',year:'numeric'}):'เลือกวันที่สิ้นสุด';
  document.getElementById('previewSizeLabel').textContent='ขนาดกรอบ: '+({Small:'เล็ก',Medium:'กลาง',Large:'ใหญ่'}[size]||size);
  document.getElementById('EndDate').min=value('StartDate');
 }
 form.addEventListener('input',update);form.addEventListener('change',update);update();
 window.addEventListener('pagehide',()=>{if(imageUrl)URL.revokeObjectURL(imageUrl);});
})();
