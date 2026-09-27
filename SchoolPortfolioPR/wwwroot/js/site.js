const menuButton = document.querySelector('[data-menu]');
const sidebar = document.querySelector('.sidebar');
const scrim = document.querySelector('[data-menu-close]');
function closeMenu() { sidebar?.classList.remove('open'); menuButton?.setAttribute('aria-expanded','false'); if(scrim) scrim.hidden=true; }
menuButton?.addEventListener('click',()=>{const open=sidebar.classList.toggle('open');menuButton.setAttribute('aria-expanded',String(open));scrim.hidden=!open;if(open)sidebar.querySelector('a')?.focus()});
scrim?.addEventListener('click',()=>{closeMenu();menuButton?.focus()});
document.addEventListener('keydown',e=>{if(e.key==='Escape'&&sidebar?.classList.contains('open')){closeMenu();menuButton?.focus()}});
document.querySelectorAll('[data-nav]').forEach(a=>{
 const target=new URL(a.href);const current=location.pathname.toLowerCase();const expected=target.pathname.toLowerCase();
 const routeMatch=current===expected||current.startsWith(expected+'/')||(expected==='/activities'&&current.startsWith('/activity/'));
 const queryMatch=!target.searchParams.has('kind')||target.searchParams.get('kind')===(new URLSearchParams(location.search).get('kind')||'Department');
 if(routeMatch&&queryMatch){a.classList.add('active');a.setAttribute('aria-current','page');const group=a.closest('details');if(group)group.open=true;}
 a.addEventListener('click',closeMenu);
});
document.querySelectorAll('.toast').forEach(el=>new bootstrap.Toast(el).show());
let deleteForm;document.querySelectorAll('form[data-confirm]').forEach(form=>form.addEventListener('submit',event=>{event.preventDefault();deleteForm=form;bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmModal')).show()}));
document.getElementById('confirmDelete')?.addEventListener('click',()=>deleteForm?.submit());
document.querySelectorAll('[data-sortable]').forEach(list=>{let dragged;list.querySelectorAll('.drag-item').forEach(item=>{item.draggable=true;item.addEventListener('dragstart',()=>{dragged=item;item.classList.add('dragging')});item.addEventListener('dragend',()=>item.classList.remove('dragging'));item.addEventListener('dragover',e=>{e.preventDefault();if(dragged&&dragged!==item){const middle=item.getBoundingClientRect().top+item.offsetHeight/2;list.insertBefore(dragged,e.clientY<middle?item:item.nextSibling)}});item.querySelector('[data-up]')?.addEventListener('click',()=>{if(item.previousElementSibling)list.insertBefore(item,item.previousElementSibling)});item.querySelector('[data-down]')?.addEventListener('click',()=>{if(item.nextElementSibling)list.insertBefore(item.nextElementSibling,item)})})});
const pendingUrls=new Set();
document.querySelectorAll('input[type=file]').forEach((input,inputIndex)=>{
 let urls=[];
 input.addEventListener('change',()=>{
  document.dispatchEvent(new Event('media:close'));
  urls.forEach(url=>{URL.revokeObjectURL(url);pendingUrls.delete(url)});urls=[];
  let preview=input.parentElement.querySelector('.preview-list');if(!preview){preview=document.createElement('div');preview.className='preview-list';input.after(preview)}preview.replaceChildren();
  for(const file of input.files){
   const image=['image/jpeg','image/png','image/webp'].includes(file.type),pdf=file.type==='application/pdf';
   const tile=document.createElement(image||pdf?'a':'div');tile.className='upload-preview';
   if(image||pdf){const url=URL.createObjectURL(file);urls.push(url);pendingUrls.add(url);tile.href=url;tile.previewFile=file;tile.dataset.mediaPreview='';tile.dataset.mediaType=image?'image':'pdf';tile.dataset.mediaTitle=file.name;tile.dataset.mediaGroup='upload-'+inputIndex;tile.dataset.mediaDownload=url;}
   if(image){const img=document.createElement('img');img.src=tile.href;img.alt=file.name;tile.append(img)}else{const badge=document.createElement('span');badge.className='upload-file-badge';badge.textContent=pdf?'PDF':'ไฟล์';tile.append(badge)}
   const label=document.createElement('span');label.textContent=file.name;tile.append(label);preview.append(tile);
  }
 });
});
window.addEventListener('pagehide',()=>pendingUrls.forEach(url=>URL.revokeObjectURL(url)));
document.querySelector('[data-print]')?.addEventListener('click',()=>window.print());
