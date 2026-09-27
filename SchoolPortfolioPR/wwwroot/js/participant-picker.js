(() => {
    const normalize = value => value.normalize('NFKC').toLocaleLowerCase('th').trim().replace(/\s+/g, ' ');
    const collator = new Intl.Collator('th', { numeric: true, sensitivity: 'base' });
    document.querySelectorAll('[data-participant-picker]').forEach(picker => {
        const source = picker.querySelector('[data-participant-source]');
        if (!source || picker.dataset.ready) return;
        const teacher = picker.dataset.kind === 'teacher';
        const search = picker.querySelector('[data-participant-search]');
        const group = picker.querySelector('[data-filter-group]');
        const level = picker.querySelector('[data-filter-level]');
        const room = picker.querySelector('[data-filter-room]');
        const results = picker.querySelector('[data-participant-results]');
        const chips = picker.querySelector('[data-participant-chips]');
        const status = picker.querySelector('[data-participant-status]');
        const more = picker.querySelector('[data-show-more]');
        const entries = Array.from(source.options, option => ({ option, id: option.value, ...option.dataset,
            search: normalize(`${option.dataset.name} ${option.dataset.code}`) }));
        let limit = 50;
        let filtered = [];
        function options(select, items, allLabel) {
            select.replaceChildren(new Option(allLabel, ''));
            [...new Map(items).entries()].sort((a,b)=>collator.compare(a[1], b[1])).forEach(([value,label])=>select.add(new Option(label,value)));
        }
        if (teacher) options(group, entries.map(e=>[e.group,e.groupName]), 'ทุกกลุ่มสาระ');
        else {
            options(level, entries.map(e=>[e.level,e.level]), 'ทุกระดับชั้น');
            options(room, entries.map(e=>[e.room,e.room]), 'ทุกห้อง');
        }
        function metadata(entry) { return `${entry.code} · ${teacher ? entry.groupName : `${entry.level} / ${entry.room}`}`; }
        function picked() {
            const selected = entries.filter(e=>e.option.selected);
            picker.querySelector('[data-picked-count]').textContent = `เลือก ${selected.length}`;
            picker.querySelector('[data-clear-picked]').disabled = selected.length === 0;
            const fragment = document.createDocumentFragment();
            if (!selected.length) { const empty = document.createElement('span'); empty.className='participant-empty-picked'; empty.textContent='ยังไม่ได้เลือกรายชื่อ'; fragment.append(empty); }
            selected.forEach(entry=>{
                const chip=document.createElement('span');chip.className='participant-chip';
                const name=document.createElement('span');name.textContent=`${entry.name} (${entry.code})`;
                const remove=document.createElement('button');remove.type='button';remove.textContent='×';remove.setAttribute('aria-label',`นำ ${entry.name} ${entry.code} ออกจากรายการ`);
                remove.addEventListener('click',()=>{entry.option.selected=false;source.dispatchEvent(new Event('change',{bubbles:true}));search.focus();});
                chip.append(name,remove);fragment.append(chip);
            });
            chips.replaceChildren(fragment);
        }
        function render() {
            const tokens=normalize(search.value).split(' ').filter(Boolean);
            filtered=entries.filter(e=>(!teacher || !group.value || e.group===group.value) && (teacher || (!level.value || e.level===level.value) && (!room.value || e.room===room.value)) && tokens.every(t=>e.search.includes(t)));
            const fragment=document.createDocumentFragment();
            for (const entry of filtered.slice(0,limit)) {
                const label=document.createElement('label');label.className='participant-result';
                const input=document.createElement('input');input.type='checkbox';input.value=entry.id;input.checked=entry.option.selected;
                input.addEventListener('change',()=>{entry.option.selected=input.checked;picked();status.textContent=`พบ ${filtered.length} คน · เลือก ${source.selectedOptions.length} คน`;});
                const text=document.createElement('span');const name=document.createElement('strong');name.textContent=entry.name;const meta=document.createElement('small');meta.textContent=metadata(entry);text.append(name,meta);label.append(input,text);fragment.append(label);
            }
            if (!filtered.length) { const empty=document.createElement('p');empty.className='participant-empty';empty.textContent='ไม่พบรายชื่อที่ตรงกับตัวกรอง ลองเปลี่ยนคำค้นหรือกลุ่มที่เลือก';fragment.append(empty); }
            results.replaceChildren(fragment);
            more.hidden=filtered.length<=limit;
            more.textContent=`แสดงเพิ่ม (อีก ${Math.max(0,filtered.length-limit)} คน)`;
            status.textContent=`พบ ${filtered.length} คน · เลือก ${source.selectedOptions.length} คน`;
            picker.querySelector('[data-pick-visible]').disabled=filtered.length===0;
            picked();
        }
        function refresh() { limit=50;render(); }
        search.addEventListener('input',refresh);
        search.addEventListener('keydown',event=>{if(event.key==='Enter')event.preventDefault();});
        group?.addEventListener('change',refresh);
        level?.addEventListener('change',()=>{options(room,entries.filter(e=>!level.value||e.level===level.value).map(e=>[e.room,e.room]),'ทุกห้อง');refresh();});
        room?.addEventListener('change',refresh);
        source.addEventListener('change',render);
        more.addEventListener('click',()=>{limit+=50;render();});
        picker.querySelector('[data-pick-visible]').addEventListener('click',()=>{filtered.forEach(e=>e.option.selected=true);source.dispatchEvent(new Event('change',{bubbles:true}));});
        picker.querySelector('[data-clear-picked]').addEventListener('click',()=>{entries.forEach(e=>e.option.selected=false);source.dispatchEvent(new Event('change',{bubbles:true}));});
        function resetFilters(){search.value='';if(group)group.value='';if(level){level.value='';options(room,entries.map(e=>[e.room,e.room]),'ทุกห้อง');}refresh();}
        picker.querySelector('[data-reset-filters]').addEventListener('click',resetFilters);
        source.form?.addEventListener('reset',()=>setTimeout(resetFilters,0));
        render();
        source.hidden=true;
        picker.querySelector('.participant-native-label').hidden=true;
        picker.querySelector('[data-participant-controls]').hidden=false;
        picker.dataset.ready='true';
    });
})();
