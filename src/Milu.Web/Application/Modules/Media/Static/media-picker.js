(() => {
    let callback;
    function open(options = {}) {
        callback = options.onSelect;
        let element = document.getElementById('miluMediaPicker');
        if (!element) {
            element = document.createElement('div');
            element.id = 'miluMediaPicker';
            element.className = 'modal fade';
            element.innerHTML = `<div class="modal-dialog modal-xl modal-dialog-scrollable"><div class="modal-content">
                <div class="modal-header"><h2 class="modal-title fs-5">Medium auswählen</h2><button class="btn-close" data-bs-dismiss="modal"></button></div>
                <div class="modal-body p-0"><iframe title="Medienbibliothek" src="/admin/media/index/index?picker=true" style="width:100%;height:70vh;border:0"></iframe></div>
            </div></div>`;
            document.body.appendChild(element);
        }
        bootstrap.Modal.getOrCreateInstance(element).show();
    }
    window.addEventListener('message', event => {
        if (event.origin !== window.location.origin || event.data?.type !== 'milu-media-selected') return;
        callback?.(event.data.media);
        bootstrap.Modal.getInstance(document.getElementById('miluMediaPicker'))?.hide();
    });
    document.addEventListener('click', event => {
        const trigger = event.target.closest('[data-media-picker]');
        if (!trigger) return;
        const target = document.querySelector(trigger.dataset.target);
        open({ onSelect: media => {
            if (target) { target.value = media.id; target.dispatchEvent(new Event('change', { bubbles: true })); }
            const preview = document.querySelector(trigger.dataset.preview);
            if (preview) { preview.src = media.url; preview.alt = media.altText || media.title; preview.hidden = false; }
        }});
    });
    window.MiluMediaPicker = { open };
})();
