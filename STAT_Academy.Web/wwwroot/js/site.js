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

// Ver la contraseña de forma temporal (botón de ojo, parcial _BotonVerContrasena).
// - Mantener presionado (mouse o dedo): se ve mientras se mantiene y se oculta al soltar.
// - Toque corto, clic, Enter o Espacio: se ve unos segundos y vuelve a ocultarse sola.
(function () {
    "use strict";

    var VISIBLE_MS = 4000;     // tiempo visible tras un toque corto o con teclado
    var TOQUE_CORTO_MS = 350;  // por debajo de esto, soltar no la oculta de inmediato

    function iniciar(boton) {
        var campo = boton.closest(".campo-contrasena");
        var input = campo && campo.querySelector("input");
        if (!input) return;

        var temporizador = null;
        var inicioPresion = 0;
        var presionando = false;

        function mostrar() {
            input.type = "text";
            boton.setAttribute("aria-pressed", "true");
            boton.setAttribute("aria-label", "Ocultar contraseña");
            campo.classList.add("is-visible");
        }

        function ocultar() {
            clearTimeout(temporizador);
            temporizador = null;
            input.type = "password";
            boton.setAttribute("aria-pressed", "false");
            boton.setAttribute("aria-label", "Mostrar contraseña temporalmente");
            campo.classList.remove("is-visible");
        }

        function mostrarPorUnosSegundos() {
            mostrar();
            clearTimeout(temporizador);
            temporizador = setTimeout(ocultar, VISIBLE_MS);
        }

        boton.addEventListener("pointerdown", function (e) {
            if (e.button !== 0) return;
            e.preventDefault(); // el foco se queda en el campo
            presionando = true;
            inicioPresion = Date.now();
            clearTimeout(temporizador);
            if (boton.setPointerCapture) boton.setPointerCapture(e.pointerId);
            mostrar();
        });

        function soltar() {
            if (!presionando) return;
            presionando = false;
            if (Date.now() - inicioPresion < TOQUE_CORTO_MS) {
                mostrarPorUnosSegundos();
            } else {
                ocultar();
            }
        }

        boton.addEventListener("pointerup", soltar);
        boton.addEventListener("pointercancel", soltar);
        boton.addEventListener("lostpointercapture", soltar);

        // Mantener presionado en móvil no debe abrir el menú contextual.
        boton.addEventListener("contextmenu", function (e) { e.preventDefault(); });

        // Teclado (Enter/Espacio llegan como clic con detail 0). Los clics del puntero ya se atendieron arriba.
        boton.addEventListener("click", function (e) {
            if (e.detail !== 0) return;
            if (input.type === "text") {
                ocultar();
            } else {
                mostrarPorUnosSegundos();
            }
        });

        // Nunca enviar ni dejar la página con la contraseña a la vista.
        if (input.form) input.form.addEventListener("submit", ocultar);
        window.addEventListener("pagehide", ocultar);
    }

    document.querySelectorAll("[data-ver-contrasena]").forEach(iniciar);
})();
