// Pantalla de carga global.
// Se muestra al navegar a otra página del sitio o al enviar un formulario, porque muchas
// páginas esperan respuestas de la API. Aparece tras un breve retraso para no parpadear
// en las páginas rápidas. Para excluir un enlace o formulario: atributo data-sin-carga.
(function () {
    "use strict";

    var RETRASO_MS = 250;
    var MAXIMO_MS = 20000; // por si la página no cambia (p. ej. una descarga)
    var pantalla = document.getElementById("pantalla-carga");
    if (!pantalla) return;

    var temporizador = null;
    var limite = null;

    function mostrar() {
        ocultar();
        temporizador = setTimeout(function () {
            pantalla.hidden = false;
            document.body.setAttribute("aria-busy", "true");
        }, RETRASO_MS);
        limite = setTimeout(ocultar, MAXIMO_MS);
    }

    function ocultar() {
        clearTimeout(temporizador);
        clearTimeout(limite);
        pantalla.hidden = true;
        document.body.removeAttribute("aria-busy");
    }

    function esMismoSitio(url) {
        try {
            return new URL(url, location.href).origin === location.origin;
        } catch (e) {
            return false;
        }
    }

    document.addEventListener("click", function (e) {
        if (e.defaultPrevented || e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;

        var enlace = e.target.closest("a[href]");
        if (!enlace || enlace.hasAttribute("data-sin-carga") || enlace.hasAttribute("download")) return;
        if (enlace.hasAttribute("data-bs-toggle")) return;
        if (enlace.target && enlace.target !== "_self") return;

        var href = enlace.getAttribute("href");
        if (!href || href.charAt(0) === "#" || /^(mailto|tel|javascript):/i.test(href)) return;
        if (!esMismoSitio(enlace.href)) return;

        // Solo cambia el ancla dentro de la misma página: no hay navegación.
        var destino = new URL(enlace.href);
        if (destino.pathname === location.pathname && destino.search === location.search && destino.hash) return;

        mostrar();
    });

    // Fase de burbuja: los confirm() en línea y la validación del cliente ya cancelaron el envío si correspondía.
    document.addEventListener("submit", function (e) {
        var formulario = e.target;
        if (e.defaultPrevented || formulario.hasAttribute("data-sin-carga")) return;
        if (formulario.target && formulario.target !== "_self") return;

        mostrar();
    });

    // Al volver con el botón "Atrás" (caché del navegador) la pantalla no debe quedar visible.
    window.addEventListener("pageshow", ocultar);
})();

// Mostrar u ocultar la contraseña (botón de ojo, parcial _BotonVerContrasena).
// Cada clic, toque, Enter o Espacio alterna entre visible y oculta.
(function () {
    "use strict";

    function iniciar(boton) {
        var campo = boton.closest(".campo-contrasena");
        var input = campo && campo.querySelector("input");
        if (!input) return;

        function poner(visible) {
            input.type = visible ? "text" : "password";
            boton.setAttribute("aria-pressed", visible ? "true" : "false");
            boton.setAttribute("aria-label", visible ? "Ocultar contraseña" : "Mostrar contraseña");
            campo.classList.toggle("is-visible", visible);
        }

        // Con el mouse, el foco se queda en el campo para seguir escribiendo.
        boton.addEventListener("mousedown", function (e) { e.preventDefault(); });

        boton.addEventListener("click", function () {
            poner(input.type === "password");
        });

        // Nunca enviar ni dejar la página con la contraseña a la vista.
        if (input.form) input.form.addEventListener("submit", function () { poner(false); });
        window.addEventListener("pagehide", function () { poner(false); });
    }

    document.querySelectorAll("[data-ver-contrasena]").forEach(iniciar);
})();

// Confirmación antes de enviar formularios con data-confirmar (p. ej. desactivar un usuario).
// Usa la ventana #modal-confirmar (Views/Shared/_ConfirmarModal.cshtml); sin ella, confirm() nativo.
(function () {
    "use strict";

    var modalEl = document.getElementById("modal-confirmar");
    var pendiente = null;

    function abrir(formulario) {
        var texto = formulario.getAttribute("data-confirmar") || "¿Desea continuar?";

        if (!modalEl || !window.bootstrap) {
            if (window.confirm(texto)) enviar(formulario);
            return;
        }

        var aceptar = modalEl.querySelector("[data-confirmar-aceptar]");
        modalEl.querySelector("[data-confirmar-texto]").textContent = texto;
        aceptar.textContent = formulario.getAttribute("data-confirmar-boton") || "Confirmar";
        aceptar.classList.toggle("btn-danger", formulario.hasAttribute("data-confirmar-peligro"));
        aceptar.classList.toggle("btn-primary", !formulario.hasAttribute("data-confirmar-peligro"));

        pendiente = formulario;
        window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }

    function enviar(formulario) {
        formulario.setAttribute("data-confirmado", "");
        formulario.requestSubmit();
    }

    // Fase de captura: se cancela el envío antes que la pantalla de carga lo vea.
    document.addEventListener("submit", function (e) {
        var formulario = e.target;
        if (!formulario.hasAttribute || !formulario.hasAttribute("data-confirmar")) return;

        if (formulario.hasAttribute("data-confirmado")) {
            formulario.removeAttribute("data-confirmado");
            return;
        }

        e.preventDefault();
        abrir(formulario);
    }, true);

    if (modalEl) {
        modalEl.querySelector("[data-confirmar-aceptar]").addEventListener("click", function () {
            var formulario = pendiente;
            pendiente = null;
            window.bootstrap.Modal.getOrCreateInstance(modalEl).hide();
            if (formulario) enviar(formulario);
        });

        modalEl.addEventListener("hidden.bs.modal", function () {
            pendiente = null;
        });
    }
})();

// Menú de Administración en tabletas y teléfonos (barra horizontal): deja visible la sección actual.
(function () {
    "use strict";

    var menu = document.querySelector(".admin-sidebar");
    var activo = menu && menu.querySelector(".admin-sidebar__link--activo");
    if (!activo || menu.scrollWidth <= menu.clientWidth) return;

    menu.scrollLeft = activo.offsetLeft - (menu.clientWidth - activo.offsetWidth) / 2;
})();

// Formulario de curso (Admin): vista previa de la portada elegida antes de guardar.
(function () {
    "use strict";

    document.querySelectorAll("[data-portada-input]").forEach(function (input) {
        var campo = input.closest(".portada-campo") || input.closest("[data-portada-grupo]");
        var vista = campo && campo.querySelector("[data-portada-vista]");
        if (!vista || !window.URL) return;

        var original = vista.innerHTML;
        var urlActual = null;

        input.addEventListener("change", function () {
            if (urlActual) {
                URL.revokeObjectURL(urlActual);
                urlActual = null;
            }

            var archivo = input.files && input.files[0];
            if (!archivo || archivo.type.indexOf("image/") !== 0) {
                vista.innerHTML = original;
                return;
            }

            urlActual = URL.createObjectURL(archivo);
            var img = document.createElement("img");
            img.src = urlActual;
            img.alt = "Vista previa de la nueva portada";
            vista.innerHTML = "";
            vista.appendChild(img);
        });
    });
})();

// Notificaciones flotantes (Shared/_Notificaciones): se vuelven a anunciar al cargar (los lectores
// de pantalla no leen una región que ya tenía texto), las de éxito e información se ocultan a
// los 6 s salvo que el mouse o el foco estén encima, y todas se pueden cerrar.
(function () {
    "use strict";

    var DURACION_MS = 6000;

    function cerrar(el) {
        el.hidden = true;
        el.remove();
    }

    document.querySelectorAll("[data-notificacion]").forEach(function (el) {
        var texto = el.querySelector("[data-notificacion-texto]");
        if (texto) {
            var contenido = texto.textContent;
            texto.textContent = "";
            setTimeout(function () { texto.textContent = contenido; }, 150);
        }

        var boton = el.querySelector("[data-notificacion-cerrar]");
        if (boton) boton.addEventListener("click", function () { cerrar(el); });

        el.addEventListener("keydown", function (e) {
            if (e.key === "Escape") cerrar(el);
        });

        if (el.getAttribute("data-autocerrar") !== "true") return;

        var temporizador = null;
        function programar() { temporizador = setTimeout(function () { cerrar(el); }, DURACION_MS); }
        function pausar() { clearTimeout(temporizador); }

        el.addEventListener("mouseenter", pausar);
        el.addEventListener("focusin", pausar);
        el.addEventListener("mouseleave", programar);
        el.addEventListener("focusout", function (e) {
            if (!el.contains(e.relatedTarget)) programar();
        });
        programar();
    });
})();

// Filtros del panel: los selectores con data-autoenviar aplican el filtro al cambiar.
(function () {
    "use strict";

    document.addEventListener("change", function (e) {
        var campo = e.target;
        if (!campo.matches || !campo.matches("select[data-autoenviar]") || !campo.form) return;
        campo.form.requestSubmit();
    });
})();

// Filtros del panel: los campos vacíos no se envían, así la dirección queda limpia (?estado=true en vez de ?search=&estado=true).
(function () {
    "use strict";

    document.addEventListener("submit", function (e) {
        var form = e.target;
        if (!form.classList || !form.classList.contains("panel-filtros")) return;
        form.querySelectorAll("input[name], select[name]").forEach(function (campo) {
            if (campo.value === "") campo.disabled = true;
        });
    });

    // Al volver con el botón Atrás, el formulario puede quedar con campos deshabilitados.
    window.addEventListener("pageshow", function () {
        document.querySelectorAll(".panel-filtros [disabled]").forEach(function (campo) { campo.disabled = false; });
    });
})();

// Calificaciones del tutor: la ventana "Ajustar nota final" se llena con los datos del botón que la abre.
(function () {
    "use strict";

    var modal = document.getElementById("modal-nota-final");
    if (!modal) return;

    modal.addEventListener("show.bs.modal", function (e) {
        var boton = e.relatedTarget;
        if (!boton || !boton.hasAttribute("data-nota-final")) return;

        var campo = function (nombre) { return modal.querySelector("[data-campo='" + nombre + "']"); };
        var ajustada = boton.getAttribute("data-ajustada") || "";

        campo("estudiante").value = boton.getAttribute("data-estudiante-id") || "";
        campo("nombre").textContent = boton.getAttribute("data-nombre") || "";
        campo("calculada").textContent = boton.getAttribute("data-calculada") || "sin datos";
        modal.querySelector("[name='Nota']").value = ajustada;
        modal.querySelector("[name='Comentario']").value = boton.getAttribute("data-comentario") || "";
        // "Usar la calculada" solo tiene sentido si ya hay un ajuste guardado.
        campo("quitar").hidden = ajustada === "";
    });

    modal.addEventListener("shown.bs.modal", function () {
        modal.querySelector("[name='Nota']").focus();
    });
})();

// Entrega de tareas: avisa antes de enviar si se eligieron más archivos de los permitidos.
(function () {
    "use strict";

    document.querySelectorAll("[data-entrega-form] input[type='file'][data-max-archivos]").forEach(function (input) {
        var maximo = parseInt(input.getAttribute("data-max-archivos"), 10) || 5;
        var error = input.parentElement.querySelector("[data-entrega-error]");

        input.addEventListener("change", function () {
            var excede = input.files && input.files.length > maximo;
            input.setCustomValidity(excede ? "Puede adjuntar como máximo " + maximo + " archivo(s) más." : "");
            input.classList.toggle("is-invalid", excede);
            if (error) error.textContent = excede ? input.validationMessage : "";
        });
    });
})();

// ---------------------------------------------------------------------------
// Comportamientos que antes estaban en scripts y atributos on* dentro de las vistas.
// Viven aquí para que la política CSP pueda prohibir el JavaScript inline.
// ---------------------------------------------------------------------------
(function () {
    // Formularios de pago/compra: el botón se desactiva al enviar para no duplicar la operación.
    document.querySelectorAll("form[data-un-solo-envio]").forEach(function (form) {
        form.addEventListener("submit", function (e) {
            var boton = form.querySelector("button[type=submit]") || form.querySelector("button");
            if (!boton) return;
            // Se espera a que terminen los demás manejadores: si la validación del cliente
            // canceló el envío, el botón sigue disponible para corregir y reintentar.
            setTimeout(function () {
                if (e.defaultPrevented) return;
                // Estado "cargando": el botón queda inactivo y lo anuncia (aria-busy muestra el indicador).
                boton.setAttribute("aria-busy", "true");
                boton.setAttribute("aria-disabled", "true");
                boton.disabled = true;
            }, 0);
        });
    });

    // Filtros que se aplican al cambiar la opción (sin botón "Filtrar").
    document.querySelectorAll("select[data-enviar-al-cambiar]").forEach(function (select) {
        select.addEventListener("change", function () {
            if (select.form) select.form.requestSubmit();
        });
    });

    document.querySelectorAll("[data-imprimir]").forEach(function (boton) {
        boton.addEventListener("click", function () { window.print(); });
    });

    // Blog: copiar el enlace de la entrada.
    document.querySelectorAll("[data-copiar-enlace]").forEach(function (boton) {
        boton.addEventListener("click", function () {
            navigator.clipboard.writeText(window.location.href).then(function () {
                var etiqueta = boton.querySelector("[data-copiar-texto]") || boton;
                var original = etiqueta.textContent;
                etiqueta.textContent = "Enlace copiado";
                setTimeout(function () { etiqueta.textContent = original; }, 2000);
            });
        });
    });

    // Página de error: "Volver atrás" solo si hay una página anterior en el historial.
    var volver = document.querySelector("[data-volver]");
    if (volver && window.history.length > 1) {
        volver.hidden = false;
        volver.addEventListener("click", function () { window.history.back(); });
    }
})();

// Vista previa de los archivos elegidos: avisa antes de enviar si alguno no se aceptará.
// La validación definitiva la hace la API (extensión, tamaño y contenido real).
(function () {
    const imagenes = ['jpg', 'jpeg', 'png', 'gif', 'webp'];
    const documentos = ['pdf', 'doc', 'docx', 'xlsx', 'pptx', 'txt', 'csv'];
    const MB = 1024 * 1024;

    document.querySelectorAll('[data-blog-archivos]').forEach(function (input) {
        const lista = input.parentElement.querySelector('[data-blog-seleccion]');
        if (!lista) return;

        input.addEventListener('change', function () {
            lista.replaceChildren();
            const archivos = Array.from(input.files);
            lista.hidden = archivos.length === 0;

            if (archivos.length > 10) {
                const aviso = document.createElement('li');
                aviso.className = 'text-danger';
                aviso.textContent = 'Puede subir como máximo 10 archivos a la vez.';
                lista.appendChild(aviso);
            }

            archivos.forEach(function (archivo) {
                const extension = (archivo.name.split('.').pop() || '').toLowerCase();
                const esImagen = imagenes.includes(extension);
                const permitido = esImagen || documentos.includes(extension);
                const maximo = esImagen ? 5 * MB : 10 * MB;

                const item = document.createElement('li');
                let detalle = (archivo.size / MB).toFixed(1) + ' MB';

                if (!permitido) {
                    item.className = 'text-danger';
                    detalle += ' · tipo no permitido';
                } else if (archivo.size > maximo) {
                    item.className = 'text-danger';
                    detalle += ' · supera ' + (maximo / MB) + ' MB';
                } else {
                    detalle += esImagen ? ' · imagen' : ' · documento';
                }

                item.textContent = archivo.name + ' (' + detalle + ')';
                lista.appendChild(item);
            });
        });
    });
})();

// Barras de progreso: data-progreso="45" → ancho 45 % (la CSP no permite style= en el HTML;
// asignar el estilo desde JavaScript sí está permitido).
(function () {
    document.querySelectorAll('[data-progreso]').forEach(function (barra) {
        const valor = Number(barra.dataset.progreso);
        barra.style.width = (Number.isFinite(valor) ? Math.min(Math.max(valor, 0), 100) : 0) + '%';
    });
})();

// Al volver con "Atrás" (caché del navegador), los botones de un solo envío recuperan su estado.
window.addEventListener("pageshow", function (e) {
    if (!e.persisted) return;
    document.querySelectorAll("form[data-un-solo-envio] [aria-busy='true']").forEach(function (boton) {
        boton.removeAttribute("aria-busy");
        boton.removeAttribute("aria-disabled");
        boton.disabled = false;
    });
});

// Portada: las cifras cuentan desde 0 hasta su valor una sola vez (solo la parte visual;
// el número real está en un texto oculto para lectores de pantalla). Sin animación si el
// sistema pide reducir el movimiento.
(function () {
    "use strict";

    var cifras = document.querySelectorAll("[data-contar]");
    if (!cifras.length || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    var DURACION = 1100;

    cifras.forEach(function (el) {
        var final = parseInt(el.getAttribute("data-contar"), 10);
        if (!isFinite(final) || final <= 0) return;

        var inicio = null;
        el.textContent = "0";

        function paso(t) {
            if (inicio === null) inicio = t;
            var avance = Math.min((t - inicio) / DURACION, 1);
            var suavizado = 1 - Math.pow(1 - avance, 3);
            el.textContent = String(Math.round(final * suavizado));
            if (avance < 1) requestAnimationFrame(paso);
        }

        setTimeout(function () { requestAnimationFrame(paso); }, 350);
    });
})();

// Pulso de la portada: botón para pausarlo o reanudarlo (se recuerda en este navegador)
// y pausa automática cuando el hero no está a la vista, para no gastar batería en
// teléfonos y equipos poco potentes.
(function () {
    "use strict";

    var hero = document.querySelector("[data-pulso]");
    if (!hero) return;

    var boton = hero.querySelector("[data-pulso-control]");
    var texto = hero.querySelector("[data-pulso-texto]");
    var CLAVE = "stat-pulso-pausado";

    function aplicar(pausado) {
        hero.classList.toggle("inicio-hero--pausado", pausado);
        if (!boton) return;
        boton.setAttribute("aria-pressed", pausado ? "true" : "false");
        if (texto) texto.textContent = pausado ? "Reanudar animación" : "Pausar animación";
    }

    var guardado = false;
    try { guardado = localStorage.getItem(CLAVE) === "1"; } catch (e) { /* sin almacenamiento */ }
    aplicar(guardado);

    if (boton) {
        boton.addEventListener("click", function () {
            var pausado = !hero.classList.contains("inicio-hero--pausado");
            aplicar(pausado);
            try { localStorage.setItem(CLAVE, pausado ? "1" : "0"); } catch (e) { /* sin almacenamiento */ }
        });
    }

    if ("IntersectionObserver" in window) {
        new IntersectionObserver(function (entradas) {
            hero.classList.toggle("inicio-hero--fuera", !entradas[0].isIntersecting);
        }).observe(hero);
    }
})();

// Casilla obligatoria (aceptar términos): <input type="checkbox" data-habilita-envio> deja el botón
// de envío de su formulario deshabilitado mientras no esté marcada. Un [data-ayuda-envio] del mismo
// formulario explica por qué. El servidor también lo valida; sin JavaScript el botón queda activo.
(function () {
    "use strict";

    var casillas = document.querySelectorAll("input[type=checkbox][data-habilita-envio]");
    if (!casillas.length) return;

    function sincronizar(casilla) {
        var form = casilla.form;
        if (!form) return;
        var boton = form.querySelector("button[type=submit]");
        var ayuda = form.querySelector("[data-ayuda-envio]");
        var marcadas = Array.prototype.every.call(
            form.querySelectorAll("input[type=checkbox][data-habilita-envio]"),
            function (c) { return c.checked; });

        if (boton && boton.getAttribute("aria-busy") !== "true") boton.disabled = !marcadas;
        if (ayuda) ayuda.hidden = marcadas;
    }

    casillas.forEach(function (casilla) {
        sincronizar(casilla);
        casilla.addEventListener("change", function () { sincronizar(casilla); });
    });

    // Al volver con "Atrás" el navegador puede restaurar la casilla marcada o desmarcada.
    window.addEventListener("pageshow", function () {
        casillas.forEach(sincronizar);
    });
})();
