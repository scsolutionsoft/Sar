document.querySelectorAll('form[data-generating]').forEach(form=>form.addEventListener('submit',()=>{
 if(!form.checkValidity())return;
 const button=form.querySelector('button');button.disabled=true;button.textContent='กำลังสร้าง PDF…';form.querySelector('.generate-status').hidden=false;
}));
