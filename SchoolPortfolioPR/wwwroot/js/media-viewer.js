const root = document.getElementById('mediaViewer');
if (root) {
    const ui = Object.fromEntries(['Title','Type','Position','Download','Fullscreen','Previous','Next','PdfControls','PagePrevious','PageNext','Page','PageCount','ZoomOut','ZoomIn','Zoom','Fit','Stage','Surface','Loading','Thumbnails','Status'].map(key => [key, document.getElementById('media' + key)]));
    const modal = bootstrap.Modal.getOrCreateInstance(root);
    let items = [], index = 0, opener = null, pdf = null, image = null, pdfModule = null;
    let loadingTask = null, renderTask = null, generation = 0, renderVersion = 0, zoom = 1, pageNumber = 1, opened = false, resizeTimer;
    const allowedUrl = value => {
        try { const url = new URL(value, location.href); return url.origin === location.origin && ['http:', 'https:', 'blob:'].includes(url.protocol) ? url.href : null; }
        catch { return null; }
    };
    const downloadUrl = item => {
        if (item.download) return allowedUrl(item.download);
        const url = new URL(item.url); if (url.pathname.startsWith('/files/')) url.searchParams.set('download', 'true'); return url.href;
    };
    function busy(value) { ui.Loading.hidden = !value; ui.Stage.setAttribute('aria-busy', String(value)); }
    function controls() {
        ui.Previous.disabled = index === 0; ui.Next.disabled = index === items.length - 1;
        ui.PagePrevious.disabled = !pdf || pageNumber <= 1; ui.PageNext.disabled = !pdf || pageNumber >= pdf.numPages;
        ui.Page.disabled = !pdf; ui.Page.value = pageNumber; ui.Page.max = pdf?.numPages ?? 1;
        ui.PageCount.textContent = pdf?.numPages ?? '…'; ui.Zoom.textContent = Math.round(zoom * 100) + '%';
        ui.ZoomOut.disabled = zoom <= .5 || (!pdf && !image); ui.ZoomIn.disabled = zoom >= 3 || (!pdf && !image);
        ui.Fit.disabled = !pdf && !image;
    }
    function release() {
        renderVersion++;
        renderTask?.cancel(); renderTask = null;
        if (loadingTask) loadingTask.destroy().catch(() => {});
        else if (pdf) pdf.destroy().catch(() => {});
        loadingTask = null; pdf = null; image = null; ui.Surface.replaceChildren();
    }
    function fail(message, token) {
        if (token !== generation || !opened) return;
        busy(false); ui.Surface.replaceChildren();
        const box = document.createElement('div'); box.className = 'media-error';
        const heading = document.createElement('strong'); heading.textContent = 'ไม่สามารถแสดงตัวอย่างได้';
        const detail = document.createElement('p'); detail.textContent = message;
        box.append(heading, detail); ui.Surface.append(box); ui.Status.textContent = message; controls();
    }
    function thumbnails() {
        ui.Thumbnails.replaceChildren(); ui.Thumbnails.hidden = items.length < 2;
        items.forEach((item, i) => {
            const button = document.createElement('button'); button.type = 'button'; button.className = 'media-thumb';
            button.setAttribute('aria-label', `ดูไฟล์ ${i + 1}: ${item.title}`); button.setAttribute('aria-pressed', String(i === index));
            if (item.type === 'image') { const img = document.createElement('img'); img.src = item.url; img.alt = ''; img.loading = 'lazy'; button.append(img); }
            else { const badge = document.createElement('span'); badge.textContent = 'PDF'; button.append(badge); }
            const label = document.createElement('span'); label.textContent = String(i + 1); button.append(label);
            button.addEventListener('click', () => select(i)); ui.Thumbnails.append(button);
        });
    }
    async function render() {
        const token = generation, version = ++renderVersion;
        if (!opened || (!pdf && !image)) return;
        renderTask?.cancel(); renderTask = null; controls();
        const width = Math.max(100, ui.Stage.clientWidth - 48), height = Math.max(100, ui.Stage.clientHeight - 48);
        if (image) {
            const fit = Math.min(width / image.naturalWidth, height / image.naturalHeight, 1);
            image.style.width = Math.round(image.naturalWidth * fit * zoom) + 'px'; image.style.height = 'auto';
            ui.Surface.replaceChildren(image); busy(false); ui.Status.textContent = `รูปภาพ ${index + 1} จาก ${items.length}`; return;
        }
        const document = pdf;
        try {
            busy(true);
            const page = await document.getPage(pageNumber);
            if (token !== generation || version !== renderVersion || !opened) return;
            const base = page.getViewport({scale: 1}); const fit = Math.min(width / base.width, height / base.height);
            const viewport = page.getViewport({scale: fit * zoom}); const ratio = Math.min(devicePixelRatio || 1, 2);
            const canvas = window.document.createElement('canvas'); canvas.setAttribute('role', 'img'); canvas.setAttribute('aria-label', `${items[index].title} หน้า ${pageNumber}`);
            canvas.width = Math.ceil(viewport.width * ratio); canvas.height = Math.ceil(viewport.height * ratio);
            canvas.style.width = viewport.width + 'px'; canvas.style.height = viewport.height + 'px';
            renderTask = page.render({canvasContext: canvas.getContext('2d'), viewport, transform: ratio === 1 ? null : [ratio, 0, 0, ratio, 0, 0]});
            await renderTask.promise;
            if (token !== generation || version !== renderVersion || !opened) return;
            ui.Surface.replaceChildren(canvas); renderTask = null; busy(false);
            ui.Status.textContent = `PDF หน้า ${pageNumber} จาก ${document.numPages}`; controls();
        } catch (error) {
            if (error.name === 'RenderingCancelledException' || token !== generation || version !== renderVersion) return;
            fail('กรุณาลองเปิดใหม่ หรือดาวน์โหลดไฟล์จากปุ่มด้านบน', token);
        }
    }
    async function select(next) {
        if (next < 0 || next >= items.length) return;
        const token = ++generation; release(); index = next; zoom = 1; pageNumber = 1;
        const item = items[index]; ui.Title.textContent = item.title; ui.Type.textContent = item.type === 'pdf' ? 'PDF' : 'รูปภาพ';
        root.dataset.mediaKind = item.type; ui.Position.textContent = `${index + 1} / ${items.length} ไฟล์`;
        ui.Download.href = downloadUrl(item); ui.Download.download = item.title;
        ui.PdfControls.hidden = item.type !== 'pdf'; ui.Status.textContent = 'กำลังโหลด ' + item.title;
        ui.Stage.scrollTo(0, 0); busy(true); controls(); thumbnails();
        if (item.type === 'image') {
            const img = new Image(); img.alt = item.title;
            img.onload = () => { if (token !== generation || !opened) return; image = img; render(); };
            img.onerror = () => fail('ไฟล์อาจถูกลบ หรือคุณไม่มีสิทธิ์เข้าถึงไฟล์นี้', token); img.src = item.url;
        } else {
            try {
                pdfModule ??= await import('/lib/pdfjs/pdf.mjs');
                if (token !== generation || !opened) return;
                pdfModule.GlobalWorkerOptions.workerSrc = '/lib/pdfjs/pdf.worker.mjs';
                const source = item.file instanceof File ? {data: new Uint8Array(await item.file.arrayBuffer())} : {url: item.url};
                if (token !== generation || !opened) return;
                loadingTask = pdfModule.getDocument({...source, isEvalSupported: false}); const task = loadingTask;
                const loaded = await task.promise;
                if (token !== generation || !opened) { await loaded.destroy(); return; }
                pdf = loaded; await render();
            } catch { fail('ไฟล์อาจถูกลบ ไม่มีสิทธิ์เข้าถึง หรือไม่ใช่ PDF ที่รองรับ กรุณาลองดาวน์โหลดไฟล์', token); }
        }
    }
    function open(collection, selected, trigger) {
        items = collection.filter(item => allowedUrl(item.url)); if (!items.length) return;
        index = Math.max(0, items.findIndex(item => item.url === selected.url)); opener = trigger; opened = true;
        if (root.classList.contains('show')) select(index); else modal.show();
    }
    const itemFrom = link => ({url: allowedUrl(link.href), title: link.dataset.mediaTitle || link.textContent.trim() || 'ไฟล์แนบ', type: link.dataset.mediaType || 'image', download: link.dataset.mediaDownload, file: link.previewFile});
    document.addEventListener('click', event => {
        const link = event.target.closest('a[data-media-preview]');
        if (!link || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || event.button !== 0) return;
        const selected = itemFrom(link); if (!selected.url) return; event.preventDefault();
        const group = link.dataset.mediaGroup;
        const links = group ? [...document.querySelectorAll('a[data-media-preview]')].filter(x => x.dataset.mediaGroup === group) : [link];
        const unique = new Map(); links.map(itemFrom).filter(x => x.url).forEach(item => { if (!unique.has(item.url)) unique.set(item.url, item); });
        open([...unique.values()], selected, link);
    });
    document.addEventListener('submit', event => {
        const button = event.submitter, form = event.target;
        if (!button?.hasAttribute('data-preview-report') || form.method.toLowerCase() !== 'get') return;
        const url = new URL(button.getAttribute('formaction') || form.action, location.href);
        if (url.origin !== location.origin) return;
        event.preventDefault(); url.search = '';
        new FormData(form).forEach((value, name) => { if (typeof value === 'string') url.searchParams.append(name, value); });
        if (button.name) url.searchParams.set(button.name, button.value);
        const item = {url: url.href, download: url.href, title: 'รายงานผลงานโรงเรียน.pdf', type: 'pdf'}; open([item], item, button);
    });
    root.addEventListener('shown.bs.modal', () => { opened = true; select(index); });
    root.addEventListener('hide.bs.modal', () => {
        opened = false; generation++; release(); busy(false);
        if (document.fullscreenElement === root) document.exitFullscreen().catch(() => {});
    });
    root.addEventListener('hidden.bs.modal', () => { ui.Thumbnails.replaceChildren(); if (opener?.isConnected) opener.focus({preventScroll: true}); });
    ui.Previous.addEventListener('click', () => select(index - 1)); ui.Next.addEventListener('click', () => select(index + 1));
    const changePage = value => { if (!pdf) return; pageNumber = Math.max(1, Math.min(pdf.numPages, Math.trunc(Number(value)) || 1)); ui.Stage.scrollTo(0, 0); render(); };
    ui.PagePrevious.addEventListener('click', () => changePage(pageNumber - 1)); ui.PageNext.addEventListener('click', () => changePage(pageNumber + 1)); ui.Page.addEventListener('change', () => changePage(ui.Page.value));
    ui.ZoomOut.addEventListener('click', () => { zoom = Math.max(.5, +(zoom - .25).toFixed(2)); render(); });
    ui.ZoomIn.addEventListener('click', () => { zoom = Math.min(3, +(zoom + .25).toFixed(2)); render(); });
    ui.Fit.addEventListener('click', () => { zoom = 1; ui.Stage.scrollTo(0, 0); render(); });
    ui.Fullscreen.hidden = !document.fullscreenEnabled;
    ui.Fullscreen.addEventListener('click', async () => { try { if (document.fullscreenElement) await document.exitFullscreen(); else await root.requestFullscreen(); } catch { ui.Status.textContent = 'เบราว์เซอร์นี้ไม่รองรับการเปิดเต็มจอ'; } });
    document.addEventListener('fullscreenchange', () => { ui.Fullscreen.setAttribute('aria-label', document.fullscreenElement ? 'ออกจากเต็มจอ' : 'เปิดเต็มจอ'); if (opened) render(); });
    root.addEventListener('keydown', event => {
        if (['INPUT', 'SELECT', 'TEXTAREA'].includes(event.target.tagName) || !['ArrowLeft', 'ArrowRight'].includes(event.key)) return;
        event.preventDefault(); const delta = event.key === 'ArrowLeft' ? -1 : 1; if (pdf) changePage(pageNumber + delta); else select(index + delta);
    });
    new ResizeObserver(() => { clearTimeout(resizeTimer); resizeTimer = setTimeout(() => { if (opened) render(); }, 180); }).observe(ui.Stage);
    document.addEventListener('media:close', () => modal.hide());
}
