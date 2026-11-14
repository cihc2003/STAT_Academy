/*
 * Editor de entradas del blog (Quill 2).
 *
 * Uso: <div data-editor-blog data-campo="Contenido" data-subir-url="/Blog/SubirImagenContenido">HTML inicial</div>
 *      + un <input type="hidden" name="Contenido"> dentro del mismo <form>.
 *
 * - Barra de formato en español: títulos, negrita, cursiva, subrayado, tachado, color de texto,
 *   resaltado, listas, sangría, alineación, cita, enlace e imagen.
 * - Las imágenes se suben al servidor y se insertan donde está el cursor.
 * - Al hacer clic en una imagen aparece un panel para elegir tamaño y posición.
 * - El HTML final lo limpia la API (lista blanca); aquí solo se arma.
 */
(function () {
    'use strict';

    if (typeof Quill === 'undefined') {
        console.error('No se pudo cargar el editor (Quill).');
        return;
    }

    // ------------------------------------------------------------------
    // Imagen con clases de tamaño/posición (img-sm|md|lg + img-izquierda|centro|derecha)
    // ------------------------------------------------------------------
    const ImagenBase = Quill.import('formats/image');
    const TAMANOS = ['img-sm', 'img-md', 'img-lg'];
    const POSICIONES = ['img-izquierda', 'img-centro', 'img-derecha'];

    class ImagenBlog extends ImagenBase {
        static formats(nodo) {
            const formatos = super.formats(nodo) || {};
            const clase = nodo.getAttribute('class');
            if (clase) formatos.class = clase;
            return formatos;
        }

        format(nombre, valor) {
            if (nombre === 'class') {
                if (valor) this.domNode.setAttribute('class', valor);
                else this.domNode.removeAttribute('class');
            } else {
                super.format(nombre, valor);
            }
        }
    }
    ImagenBlog.blotName = 'image';
    ImagenBlog.tagName = 'IMG';
    Quill.register(ImagenBlog, true);

    // ------------------------------------------------------------------

    const BARRA = [
        [{ header: [2, 3, false] }],
        ['bold', 'italic', 'underline', 'strike'],
        [{ color: [] }, { background: [] }],
        [{ list: 'ordered' }, { list: 'bullet' }, { indent: '-1' }, { indent: '+1' }],
        [{ align: [] }],
        ['blockquote', 'link', 'image'],
        ['clean']
    ];

    const TITULOS = {
        'ql-header': 'Tipo de texto (título o párrafo)',
        'ql-bold': 'Negrita (Ctrl+B)',
        'ql-italic': 'Cursiva (Ctrl+I)',
        'ql-underline': 'Subrayado (Ctrl+U)',
        'ql-strike': 'Tachado',
        'ql-color': 'Color del texto',
        'ql-background': 'Color de resaltado',
        'ql-list[value=ordered]': 'Lista numerada',
        'ql-list[value=bullet]': 'Lista con viñetas',
        'ql-indent[value=-1]': 'Quitar sangría',
        'ql-indent[value=+1]': 'Aumentar sangría',
        'ql-align': 'Alineación',
        'ql-blockquote': 'Cita',
        'ql-link': 'Insertar enlace',
        'ql-image': 'Insertar imagen en esta posición',
        'ql-clean': 'Quitar formato'
    };

    const MAX_BYTES = 5 * 1024 * 1024;
    const TIPOS = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];

    document.querySelectorAll('[data-editor-blog]').forEach(iniciar);

    function iniciar(contenedor) {
        const form = contenedor.closest('form');
        const campo = form.querySelector(`input[type=hidden][name="${contenedor.dataset.campo}"]`);
        const urlSubida = contenedor.dataset.subirUrl;
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const estado = contenedor.parentElement.querySelector('[data-editor-estado]');

        const quill = new Quill(contenedor, {
            theme: 'snow',
            placeholder: 'Escribe aquí tu entrada… Usa la barra de arriba para dar formato e insertar imágenes.',
            modules: {
                toolbar: { container: BARRA, handlers: { image: () => elegirImagen(quill) } },
                history: { delay: 1000, maxStack: 200, userOnly: true }
            }
        });

        // Contenido inicial: llega como JSON (no se inyecta en la página). Quill lo interpreta con
        // DOMParser, que no ejecuta scripts, y conserva solo los formatos que conoce.
        const inicial = contenedor.parentElement.querySelector('script[data-editor-inicial]');
        let htmlInicial = '';
        try { htmlInicial = JSON.parse(inicial?.textContent || '""'); } catch { htmlInicial = ''; }
        if (htmlInicial.trim()) {
            quill.clipboard.dangerouslyPasteHTML(0, htmlInicial, 'silent');
            quill.history.clear();
        }

        traducirBarra(contenedor.previousElementSibling);
        const panel = crearPanelImagen(quill, contenedor);

        // Contador de palabras y tiempo de lectura
        const actualizarEstado = () => {
            if (!estado) return;
            const palabras = quill.getText().trim().split(/\s+/).filter(Boolean).length;
            const minutos = Math.max(1, Math.round(palabras / 200));
            estado.textContent = `${palabras} ${palabras === 1 ? 'palabra' : 'palabras'} · ~${minutos} min de lectura`;
        };
        quill.on('text-change', actualizarEstado);
        actualizarEstado();

        // Al enviar, el HTML del editor va al campo oculto.
        form.addEventListener('submit', (e) => {
            const html = quill.getSemanticHTML().replace(/&nbsp;/g, ' ');
            const vacio = quill.getText().trim().length === 0 && !quill.root.querySelector('img');
            campo.value = vacio ? '' : html;

            if (vacio) {
                e.preventDefault();
                mostrarError(contenedor, 'El contenido no puede estar vacío.');
                quill.focus();
            }
        });

        // ---------------- Subida de imágenes ----------------

        function elegirImagen(q) {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = TIPOS.join(',');
            input.addEventListener('change', () => {
                const archivo = input.files && input.files[0];
                if (archivo) subirImagen(q, archivo);
            });
            input.click();
        }

        async function subirImagen(q, archivo) {
            if (!TIPOS.includes(archivo.type)) {
                return mostrarError(contenedor, 'Solo se pueden insertar imágenes JPG, PNG, GIF o WEBP.');
            }
            if (archivo.size > MAX_BYTES) {
                return mostrarError(contenedor, 'La imagen supera el máximo de 5 MB.');
            }

            const rango = q.getSelection(true) || { index: q.getLength(), length: 0 };
            const aviso = mostrarAviso(contenedor, `Subiendo «${archivo.name}»…`);

            try {
                const datos = new FormData();
                datos.append('imagen', archivo);

                const respuesta = await fetch(urlSubida, {
                    method: 'POST',
                    body: datos,
                    headers: token ? { 'RequestVerificationToken': token } : {},
                    credentials: 'same-origin'
                });
                const json = await respuesta.json().catch(() => ({}));

                if (!respuesta.ok || !json.url) {
                    throw new Error(json.message || 'No se pudo subir la imagen.');
                }

                // Se inserta en la posición del cursor, mediana y centrada por defecto.
                q.insertEmbed(rango.index, 'image', json.url, 'user');
                q.formatText(rango.index, 1, 'class', 'img-md img-centro', 'user');
                q.setSelection(rango.index + 1, 0, 'silent');
                aviso.remove();
            } catch (err) {
                aviso.remove();
                mostrarError(contenedor, err.message);
            }
        }

        // Pegar o arrastrar imágenes también las sube (no se guardan como base64).
        quill.root.addEventListener('paste', (e) => {
            const archivo = [...(e.clipboardData?.files || [])].find(f => f.type.startsWith('image/'));
            if (archivo) { e.preventDefault(); subirImagen(quill, archivo); }
        }, true);
        quill.root.addEventListener('drop', (e) => {
            const archivo = [...(e.dataTransfer?.files || [])].find(f => f.type.startsWith('image/'));
            if (archivo) { e.preventDefault(); subirImagen(quill, archivo); }
        }, true);

        return { quill, panel };
    }

    // ------------------------------------------------------------------
    // Panel flotante para la imagen seleccionada: tamaño y posición
    // ------------------------------------------------------------------
    function crearPanelImagen(quill, contenedor) {
        const panel = document.createElement('div');
        panel.className = 'editor-panel-imagen';
        panel.setAttribute('role', 'toolbar');
        panel.setAttribute('aria-label', 'Opciones de la imagen');
        panel.hidden = true;
        panel.innerHTML = `
            <span class="editor-panel-imagen__grupo" aria-label="Tamaño">
                <button type="button" data-tam="img-sm">Pequeña</button>
                <button type="button" data-tam="img-md">Mediana</button>
                <button type="button" data-tam="img-lg">Completa</button>
            </span>
            <span class="editor-panel-imagen__grupo" aria-label="Posición">
                <button type="button" data-pos="img-izquierda" title="A la izquierda, con el texto alrededor">⇤ Izquierda</button>
                <button type="button" data-pos="img-centro" title="Centrada, sola en su línea">Centro</button>
                <button type="button" data-pos="img-derecha" title="A la derecha, con el texto alrededor">Derecha ⇥</button>
            </span>
            <button type="button" data-quitar class="editor-panel-imagen__quitar" title="Quitar imagen">Quitar</button>`;
        contenedor.parentElement.style.position = 'relative';
        contenedor.parentElement.appendChild(panel);

        let actual = null;

        const marcar = () => {
            if (!actual) return;
            const clases = actual.classList;
            panel.querySelectorAll('[data-tam]').forEach(b => b.classList.toggle('activo', clases.contains(b.dataset.tam)));
            panel.querySelectorAll('[data-pos]').forEach(b => b.classList.toggle('activo', clases.contains(b.dataset.pos)));
        };

        const ubicar = () => {
            if (!actual) return;
            const base = contenedor.parentElement.getBoundingClientRect();
            const r = actual.getBoundingClientRect();
            panel.style.top = `${Math.max(0, r.top - base.top - panel.offsetHeight - 8)}px`;
            panel.style.left = `${Math.max(0, Math.min(r.left - base.left, base.width - panel.offsetWidth))}px`;
        };

        const aplicar = (tam, pos) => {
            const blot = Quill.find(actual);
            if (!blot) return;
            const clases = actual.className.split(/\s+/).filter(Boolean);
            const t = tam || clases.find(c => TAMANOS.includes(c)) || 'img-md';
            const p = pos || clases.find(c => POSICIONES.includes(c)) || 'img-centro';
            quill.formatText(quill.getIndex(blot), 1, 'class', `${t} ${p}`, 'user');
            marcar();
            requestAnimationFrame(ubicar);
        };

        quill.root.addEventListener('click', (e) => {
            if (e.target.tagName === 'IMG') {
                actual = e.target;
                panel.hidden = false;
                marcar();
                ubicar();
            } else {
                cerrar();
            }
        });

        panel.addEventListener('click', (e) => {
            const b = e.target.closest('button');
            if (!b || !actual) return;
            if (b.dataset.tam) aplicar(b.dataset.tam, null);
            if (b.dataset.pos) aplicar(null, b.dataset.pos);
            if ('quitar' in b.dataset) {
                const blot = Quill.find(actual);
                if (blot) quill.deleteText(quill.getIndex(blot), 1, 'user');
                cerrar();
            }
        });

        document.addEventListener('keydown', (e) => { if (e.key === 'Escape') cerrar(); });
        quill.on('text-change', () => { if (actual && !quill.root.contains(actual)) cerrar(); else ubicar(); });
        window.addEventListener('resize', ubicar);

        function cerrar() { panel.hidden = true; actual = null; }

        return panel;
    }

    // ------------------------------------------------------------------

    function traducirBarra(barra) {
        if (!barra) return;
        Object.entries(TITULOS).forEach(([selector, titulo]) => {
            // "ql-indent[value=-1]" -> ".ql-indent[value="-1"]": el valor va entre comillas,
            // si no "-1" y "+1" no son CSS válido y querySelectorAll lanza una excepción.
            const [clase, atributo] = selector.split('[');
            const filtro = atributo ? `[${atributo.replace(/^(\w+)=(.*)\]$/, '$1="$2"')}]` : '';
            const css = '.' + clase + filtro;
            barra.querySelectorAll(`button${css}, span${css}`).forEach(el => {
                el.setAttribute('title', titulo);
                el.setAttribute('aria-label', titulo);
            });
        });
    }

    function mostrarAviso(contenedor, texto) {
        const div = document.createElement('div');
        div.className = 'alert alert-info py-2 my-2 editor-aviso';
        div.setAttribute('role', 'status');
        div.textContent = texto;
        contenedor.parentElement.prepend(div);
        return div;
    }

    function mostrarError(contenedor, texto) {
        const div = document.createElement('div');
        div.className = 'alert alert-danger py-2 my-2 editor-aviso';
        div.setAttribute('role', 'alert');
        div.textContent = texto;
        contenedor.parentElement.prepend(div);
        setTimeout(() => div.remove(), 6000);
    }
})();
