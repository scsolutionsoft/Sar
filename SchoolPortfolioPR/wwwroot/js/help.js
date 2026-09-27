(() => {
    const root = document.querySelector('[data-help-key]');
    if (!root) return;
    const key = topic => root.dataset.helpKey + ':' + topic;
    const read = topic => { try { return JSON.parse(localStorage.getItem(key(topic))) || {}; } catch { return {}; } };
    const write = (topic, value) => { try { localStorage.setItem(key(topic), JSON.stringify(value)); } catch { /* The guide also works without browser storage. */ } };
    if (root.matches('.help-wizard')) {
        const topic = root.dataset.helpTopic, step = Number(root.dataset.helpStep), count = Number(root.dataset.helpCount);
        const saved = read(topic);
        write(topic, { step, done: saved.done === true });
        if (saved.done === true) root.querySelector('[data-help-status]').textContent = 'อ่านหัวข้อนี้ครบแล้ว';
        root.querySelector('[data-help-complete]')?.addEventListener('click', () => write(topic, { step: count, done: true }));
        root.querySelector('[data-help-restart]')?.addEventListener('click', () => write(topic, { step: 1, done: false }));
    } else {
        root.querySelectorAll('[data-help-topic]').forEach(card => {
            const saved = read(card.dataset.helpTopic), count = Number(card.dataset.helpCount);
            const step = Number.isInteger(saved.step) && saved.step >= 1 && saved.step <= count ? saved.step : 1;
            const link = card.querySelector('[data-help-resume]');
            if (saved.done === true) {
                card.querySelector('[data-help-status]').textContent = '✓ อ่านครบแล้ว · ' + count + ' ขั้นตอน';
                link.textContent = 'อ่านอีกครั้ง';
            } else if (step > 1) {
                card.querySelector('[data-help-status]').textContent = 'อ่านถึงขั้นตอน ' + step + ' จาก ' + count;
                link.textContent = 'อ่านต่อ';
                link.href = '/Help/Wizard?' + new URLSearchParams({ topic: card.dataset.helpTopic, step });
            }
        });
    }
})();
