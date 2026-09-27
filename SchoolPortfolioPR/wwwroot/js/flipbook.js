import * as pdfjs from '/lib/pdfjs/pdf.mjs';
pdfjs.GlobalWorkerOptions.workerSrc='/lib/pdfjs/pdf.worker.mjs';
const root=document.getElementById('flipbook');
if(root){
 const status=document.getElementById('viewerStatus');
 try{
 const pdf=await pdfjs.getDocument({url:root.dataset.pdf,isEvalSupported:false}).promise;
 const canvas=document.getElementById('pdfCanvas'),ctx=canvas.getContext('2d'),input=document.getElementById('pageNumber'),toc=document.getElementById('toc');
 const firstPage=await pdf.getPage(1);
 let current=1,scale=Math.min(1.5,(root.querySelector('.flip-stage').clientWidth-50)/firstPage.getViewport({scale:1}).width),rendering=false,pending=false;
 document.getElementById('pageCount').textContent=pdf.numPages;input.max=pdf.numPages;
 async function render(){if(rendering){pending=true;return}rendering=true;try{const page=await pdf.getPage(current);const viewport=page.getViewport({scale});const ratio=window.devicePixelRatio||1;canvas.height=Math.floor(viewport.height*ratio);canvas.width=Math.floor(viewport.width*ratio);canvas.style.width=viewport.width+'px';canvas.style.height=viewport.height+'px';canvas.style.maxWidth='none';await page.render({canvasContext:ctx,viewport,transform:ratio===1?null:[ratio,0,0,ratio,0,0]}).promise;input.value=current;toc.value=current;canvas.classList.remove('turning');void canvas.offsetWidth;canvas.classList.add('turning');status.textContent=`หน้า ${current} จาก ${pdf.numPages}`;document.getElementById('previous').disabled=current===1;document.getElementById('next').disabled=current===pdf.numPages;}finally{rendering=false;if(pending){pending=false;await render()}}}
 const go=n=>{current=Math.min(pdf.numPages,Math.max(1,Number(n)||1));render().catch(e=>status.textContent='ไม่สามารถแสดงหน้านี้ได้')};
 document.getElementById('previous').onclick=()=>go(current-1);document.getElementById('next').onclick=()=>go(current+1);input.onchange=()=>go(input.value);toc.onchange=()=>go(toc.value);
 document.getElementById('zoomIn').onclick=()=>{scale=Math.min(3,scale+.2);render()};document.getElementById('zoomOut').onclick=()=>{scale=Math.max(.4,scale-.2);render()};
 document.getElementById('fullscreen').onclick=async()=>{if(document.fullscreenElement)await document.exitFullscreen();else await root.requestFullscreen()};
 const share=document.getElementById('share');if(share)share.onclick=async()=>{try{if(navigator.share)await navigator.share({title:document.title,url:location.href});else{await navigator.clipboard.writeText(location.href);status.textContent='คัดลอกลิงก์แล้ว'}}catch{status.textContent='ลิงก์วารสาร: '+location.href}};
 root.tabIndex=0;root.addEventListener('keydown',e=>{if(e.target.tagName==='INPUT'||e.target.tagName==='SELECT')return;if(e.key==='ArrowRight')go(current+1);if(e.key==='ArrowLeft')go(current-1)});
 let touchX;canvas.addEventListener('touchstart',e=>touchX=e.changedTouches[0].screenX,{passive:true});canvas.addEventListener('touchend',e=>{const dx=e.changedTouches[0].screenX-touchX;if(Math.abs(dx)>60)go(current+(dx<0?1:-1))},{passive:true});
 await render();
 const outline=await pdf.getOutline();
 async function addOutline(items,depth=0){for(const item of items){const destination=typeof item.dest==='string'?await pdf.getDestination(item.dest):item.dest;if(destination){const pageNumber=typeof destination[0]==='number'?destination[0]+1:await pdf.getPageIndex(destination[0])+1;const option=document.createElement('option');option.value=pageNumber;option.textContent=`${'　'.repeat(depth)}${pageNumber}. ${item.title}`;toc.append(option)}if(item.items?.length)await addOutline(item.items,depth+1)}}
 if(outline?.length)await addOutline(outline);else for(let i=1;i<=pdf.numPages;i++){const option=document.createElement('option');option.value=i;option.textContent=`หน้า ${i}`;toc.append(option)}toc.value=current;
 }catch{status.textContent='ไม่สามารถโหลดวารสารได้ กรุณาดาวน์โหลด PDF จากปุ่มด้านบน';}
}
