(() => {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const input = document.getElementById('mediaFiles');
    const dropzone = document.getElementById('mediaDropzone');
    const browse = document.getElementById('mediaBrowse');

    async function upload(files) {
        if (!files?.length) return;
        const data = new FormData();
        [...files].forEach(file => data.append('files', file));
        dropzone?.classList.add('is-uploading');
        try {
            const response = await fetch('/admin/media/index/upload', {
                method: 'POST', body: data, headers: token ? { 'RequestVerificationToken': token } : {}
            });
            if (!response.ok) throw new Error((await response.json().catch(() => null))?.message || 'Upload fehlgeschlagen.');
            location.reload();
        } catch (error) { alert(error.message); }
        finally { dropzone?.classList.remove('is-uploading'); }
    }

    browse?.addEventListener('click', () => input.click());
    input?.addEventListener('change', () => upload(input.files));
    ['dragenter', 'dragover'].forEach(name => dropzone?.addEventListener(name, event => {
        event.preventDefault(); dropzone.classList.add('is-dragging');
    }));
    ['dragleave', 'drop'].forEach(name => dropzone?.addEventListener(name, event => {
        event.preventDefault(); dropzone.classList.remove('is-dragging');
    }));
    dropzone?.addEventListener('drop', event => upload(event.dataTransfer.files));

    document.querySelectorAll('.media-select').forEach(button => button.addEventListener('click', () => {
        const card = button.closest('.media-card');
        window.parent.postMessage({ type: 'milu-media-selected', media: {
            id: Number(card.dataset.id), url: card.dataset.url, title: card.dataset.title,
            altText: card.dataset.alt, contentType: card.dataset.type
        }}, window.location.origin);
    }));

    document.getElementById('deleteMedia')?.addEventListener('click', async event => {
        if (!confirm('Dieses Medium dauerhaft löschen?')) return;
        const response = await fetch(`/admin/media/index/delete/id/${event.currentTarget.dataset.id}`, {
            method: 'POST', headers: token ? { 'RequestVerificationToken': token } : {}
        });
        if (response.ok) location.href = '/admin/media/index/index';
        else alert((await response.json().catch(() => null))?.message || 'Löschen fehlgeschlagen.');
    });
})();
