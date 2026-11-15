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
        var campo = input.closest(".portada-campo");
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

// Avisos de resultado (toast): los de éxito se ocultan solos; los de error quedan hasta cerrarlos.
(function () {
    "use strict";

    if (!window.bootstrap || !window.bootstrap.Toast) return;

    document.querySelectorAll(".aviso-toast").forEach(function (el) {
        var toast = window.bootstrap.Toast.getOrCreateInstance(el);
        toast.show();
        el.addEventListener("hidden.bs.toast", function () { el.remove(); });
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
