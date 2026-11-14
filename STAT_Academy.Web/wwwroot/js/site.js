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
