# Sistema visual de STAT Academy

Referencia de la capa visual de `STAT_Academy.Web`. Todo color, fuente, espacio, radio, sombra y
duración sale de las variables de la sección 1 de `wwwroot/css/site.css`. No hay gradientes,
emojis ni estilos en línea (la CSP no los permite).

## Archivos

| Archivo | Contenido |
|---|---|
| `wwwroot/css/site.css` | 1 Tokens · 2 Base · 3 Estructura (cabecera, pie, panel) · 4 Componentes · 5 Vistas públicas · 6 Panel interno |
| `wwwroot/css/blog.css` | Blog: listado, artículo, editor Quill y adjuntos |
| `wwwroot/img/iconos.svg` | Única familia de íconos (símbolos de 24 × 24, trazo 1,75) |
| `Helpers/Iconos.cs` | `@Html.Icono("nombre", "clase")` |
| `Helpers/AvisoTagHelper.cs` | `<aviso tipo="…">` (notificación en la página) |
| `Views/Shared/_Notificaciones.cshtml` | Notificaciones flotantes (TempData) |

## Color

Paleta oficial de STAT Medical: `#AE040F`, `#1C1C1C`, `#112945`, `#BEBEBE`, `#EFEFEF`. El resto son
tonos derivados para estados (hover, activo, tinte) y textos secundarios con contraste AA.

| Rol | Token | Valor | Uso |
|---|---|---|---|
| Marca / acción | `--rojo-600` (`--color-accion`) | `#AE040F` | Botón principal, enlaces de acción, sección activa. 7,4:1 sobre blanco |
| Acción hover / activo | `--rojo-700` / `--rojo-800` | `#8E030C` / `#6F020A` | |
| Tinte de acción | `--rojo-50` | `#FBEAEB` | Fondo de íconos de error de página |
| Superficie oscura / títulos | `--marino-900` | `#112945` | Títulos, cabecera del panel, pie, agenda. 14:1 |
| Marino derivados | `--marino-50 … 800` | `#EDF1F6 … #1A3558` | Selección, hover sobre oscuro, foco |
| Texto | `--tinta` | `#1C1C1C` | Texto de lectura |
| Texto secundario | `--gris-700` | `#4D4D4D` | 8,5:1 |
| Texto tenue | `--gris-600` | `#666666` | 5,7:1 (mínimo para texto pequeño) |
| Borde de controles | `--gris-500` | `#8C8C8C` | 3,4:1 (WCAG 1.4.11) |
| Borde fuerte | `--gris-300` | `#BEBEBE` | Marca |
| Bordes | `--gris-200` | `#DEDEDE` | |
| Superficie hundida | `--gris-100` | `#EFEFEF` | Marca |
| Fondo alterno | `--gris-50` | `#F6F6F6` | Secciones y panel |

Estados (texto · fondo · borde), todos con texto ≥ 4,5:1 sobre su fondo:
éxito `#17663A · #E8F3EC · #9CCBAE`, advertencia `#7A4D00 · #FDF3DC · #E3C06D`,
error `#A3121C · #FCECEC · #EBA5A8`, información `#1F4E8C · #EAF1FA · #A9C3E6`.

Reglas: una acción roja por bloque; el rojo nunca va como texto sobre marino (2:1); sobre fondos
oscuros el foco es blanco.

## Tipografía

**Instrument Sans Variable** (peso 400–700, ancho 75–100 %), con todos los caracteres del español.
Se carga desde jsDelivr (`@fontsource-variable/instrument-sans@5.1.0`, ya permitido en la CSP para
estilos; se agregó a `font-src`). Para alojarla en `wwwroot` basta copiar los `.woff2` y cambiar el
`<link>` del layout.

- Títulos: peso 600, ancho 86 %, interlineado 1,12, tracking −0,015 em.
- Portada y cifras: ancho 76 %, tracking −0,03 em (`--texto-display`, 40–76 px fluido).
- Texto: 16 px, interlineado 1,6, líneas de máximo 68 caracteres (`--medida`).
- Escala: 12 · 14 · 16 · 18 · 20 · 25 · 26–31 · 30–39 · 40–76.

Una sola familia: la jerarquía sale del ancho variable (condensado en títulos), no de una segunda fuente.

## Espaciado, radios, sombras y movimiento

- Espacios en base 4: `--e-1` (4 px) … `--e-24` (96 px); entre secciones `--seccion` (48–96 px).
- Ancho: contenedor fluido hasta 1680 px con márgenes `--margen` (16–48 px). Lectura: 68 ch.
- Radios: 4 (etiquetas), 8 (controles y botones), 12 (tarjetas), 20 (bloques destacados), total (chips).
- Sombras: `--sombra-1` (reposo), `--sombra-2` (hover / elevado), `--sombra-3` (menús, modales, notificaciones).
- Movimiento: 120 / 200 / 320 ms; con `prefers-reduced-motion` se desactiva.
- Objetivo táctil mínimo: 44 px (`--objetivo-tactil`).

## Íconos

Una familia (geometría de Lucide, licencia ISC) en un sprite propio. Tamaños: `icono--sm` 16,
base 20, `icono--lg` 24, `icono--xl` 32. Son decorativos por defecto (`aria-hidden`); si un ícono
es lo único que comunica algo, se pasa `etiqueta`.

## Componentes

| Componente | Clases | Estados |
|---|---|---|
| Botón | `btn` + `btn-primary` (rojo), `btn-oscuro`, `btn-secundario` (contorno), `btn-outline-danger`, `btn-fantasma`, `btn-claro` y `btn-contorno-claro` (sobre oscuro); tamaños `btn-sm`, `btn-lg`; `btn-icono` | hover, activo, foco, deshabilitado, cargando (`aria-busy`, lo pone `data-un-solo-envio`) |
| Campo | `form-control`, `form-select`, `form-check` | hover, foco, deshabilitado, solo lectura, error (borde + mensaje con ícono, asociado por el validador) |
| Campo con prefijo | `input-group` + `input-group-text` (₡, @…) | El campo y el prefijo comparten fila y altura (44 px) |
| Selector de archivos | `form-control` en `input type="file"` + `form-text` con formatos y límite | Crece en alto: si el texto del navegador no cabe, pasa a la línea siguiente (nunca se recorta) |
| Campo + botón | `campo-con-boton` (p. ej. archivos + «Subir») | El botón baja de línea si no cabe |
| Tarjeta de catálogo | `tarjeta-item` (`_CursoTarjeta`, `_ProductoTarjeta`, `_BlogTarjeta`) | hover, foco (toda la tarjeta es un enlace), apagada (agotado / oculta) |
| Estado | `estado estado--exito/advertencia/error/info/neutro` | Punto + texto, nunca solo color |
| Chip | `chip`, `chip--activo` | Filtros aplicados y categorías |
| Aviso en página | `<aviso tipo="exito/error/advertencia/info" titulo="…">` | `role="alert"` en errores, `status` en el resto |
| Notificación flotante | `TempData["Success" / "Error" / "Advertencia" / "Info"]` | Éxito e info se ocultan a los 6 s (pausa con mouse o foco); error y advertencia quedan; Esc o botón para cerrar |
| Estado vacío | `vacio` + `vacio__icono` | Qué es, por qué está vacío y qué hacer |
| Carga | `pantalla-carga` (navegación), `aria-busy` en botones | |
| Confirmación | `_ConfirmarModal` + `data-confirmar` | |
| Tabla del panel | `table-card panel-tabla` + `data-label` | Se convierte en tarjetas por debajo de 820 px; entre 821 y 1279 px se ajusta al ancho (acciones apiladas) |
| Existencias | `panel-existencias` (cantidad, «/ mín.» y estado) | Una línea desde 1280 px; el estado baja de línea solo si falta espacio |
| Métricas del panel | `panel-metricas` + `panel-metrica`; `panel-metricas--cuatro` para cuatro cifras | 2 × 2 en teléfono y tableta, una fila en escritorio |
| Botón de alta (panel) | `btn btn-primary` + `@Html.Icono("mas")` + «Nuevo/Nueva + elemento», dentro de `panel-encabezado__acciones` | |

## Accesibilidad (WCAG 2.2 AA)

Contraste verificado en las vistas públicas con un script de auditoría (sin fallos), foco visible
uniforme (`:focus-visible`, 2 px), enlace "Saltar al contenido", un `h1` por página y niveles de
encabezado sin saltos, etiquetas asociadas en todos los campos, objetivos táctiles de 44 px,
textos alternativos, regiones vivas para resultados y notificaciones, y `prefers-reduced-motion`.

## Logotipo

El logotipo de la plataforma es la **estrella roja** recortada del logo oficial de STATMEDICAL
(sin redibujar). Todas las rutas están en `Helpers/Marca.cs`; las vistas lo insertan con
`@Html.Logo("clase", tamaño)`, que fija ancho y alto y usa `object-fit: contain` (sin deformar).

| Archivo (`wwwroot/img/marca`) | Uso |
|---|---|
| `statmedical-estrella.png` (343 × 343, transparente) | Logo en cabecera, pie, landing, cuenta y menús de Admin y Tutor |
| `statmedical-logo.png` (horizontal, transparente) | Logo oficial completo, reservado para impresos o documentos |
| `/favicon.ico`, `favicon-32.png`, `icono-192.png`, `apple-touch-icon.png` | Favicon e íconos del navegador y del teléfono |

Sobre fondos oscuros (pie, menús laterales, panel de las páginas de cuenta) la estrella va sobre
una placa blanca para conservar el contraste del rojo.
