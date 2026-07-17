(() => {
    const presets = {
        admin: {
            height: 520,
            menubar: 'edit insert format table tools',
            plugins: 'advlist autolink charmap code fullscreen image link lists media preview searchreplace table visualblocks',
            toolbar: 'undo redo | styles | bold italic underline | alignleft aligncenter alignright | bullist numlist | link milumedia table | blockquote code fullscreen',
            block_formats: 'Absatz=p; Überschrift 2=h2; Überschrift 3=h3; Überschrift 4=h4; Vorformatiert=pre'
        },
        frontend: {
            height: 260,
            menubar: false,
            plugins: 'autolink link lists',
            toolbar: 'bold italic | bullist numlist | link',
            block_formats: 'Absatz=p; Überschrift=h3'
        }
    };

    function mediaButton(editor) {
        editor.ui.registry.addButton('milumedia', {
            text: 'Medien', tooltip: 'Medium aus der Bibliothek einfügen',
            onAction: () => window.MiluMediaPicker?.open({ onSelect: media => {
                const id = String(media.id);
                const url = editor.dom.encode(media.url);
                const label = editor.dom.encode(media.altText || media.title || 'Medium');
                if (media.contentType?.startsWith('image/'))
                    editor.insertContent(`<img src="${url}" alt="${label}" data-milu-media-id="${id}">`);
                else if (media.contentType?.startsWith('video/'))
                    editor.insertContent(`<video controls preload="metadata" data-milu-media-id="${id}"><source src="${url}" type="${editor.dom.encode(media.contentType)}"></video><p></p>`);
                else
                    editor.insertContent(`<a href="${url}" data-milu-media-id="${id}">${label}</a>`);
            }})
        });
    }

    function create(target, options = {}) {
        const element = typeof target === 'string' ? document.querySelector(target) : target;
        if (!element) return Promise.resolve(null);
        const preset = options.preset || element.dataset.editorPreset || 'admin';
        if (preset === 'bbcode') return Promise.resolve(createBbCode(element));
        if (!window.tinymce) return Promise.reject(new Error('TinyMCE wurde nicht geladen.'));
        return tinymce.init({
            target: element, license_key: 'gpl', language: 'de',
            language_url: '/lib/tinymce-package/langs/de.js', promotion: false, branding: false,
            convert_urls: false, relative_urls: false, remove_script_host: false,
            media_live_embeds: true,
            extended_valid_elements: 'video[controls|preload|poster|width|height|data-milu-media-id],source[src|type]',
            content_css: '/css/site.css', ...presets[preset], ...options,
            setup(editor) { mediaButton(editor); options.setup?.(editor); }
        });
    }

    function createBbCode(textarea) {
        const bar = document.createElement('div');
        bar.className = 'btn-toolbar gap-1 mb-2';
        [['B', 'b'], ['I', 'i'], ['Zitat', 'quote'], ['Code', 'code'], ['Link', 'url']].forEach(([label, tag]) => {
            const button = document.createElement('button');
            button.type = 'button'; button.className = 'btn btn-sm btn-outline-secondary'; button.textContent = label;
            button.addEventListener('click', () => wrapSelection(textarea, `[${tag}]`, `[/${tag}]`));
            bar.appendChild(button);
        });
        const media = document.createElement('button');
        media.type = 'button'; media.className = 'btn btn-sm btn-outline-primary'; media.textContent = 'Medien';
        media.addEventListener('click', () => window.MiluMediaPicker?.open({
            onSelect: item => wrapSelection(textarea, `[media id="${item.id}"]`, '')
        }));
        bar.appendChild(media); textarea.before(bar); return { element: textarea, preset: 'bbcode' };
    }

    function wrapSelection(textarea, before, after) {
        const start = textarea.selectionStart, end = textarea.selectionEnd;
        textarea.setRangeText(before + textarea.value.slice(start, end) + after, start, end, 'select');
        textarea.focus();
    }

    window.MiluEditor = { create, presets };
    document.addEventListener('submit', event => {
        if (event.target.matches('[data-rich-text-form]')) window.tinymce?.triggerSave();
    }, true);
    document.addEventListener('DOMContentLoaded', () =>
        document.querySelectorAll('[data-editor-preset]').forEach(element => create(element).catch(console.error)));
})();
