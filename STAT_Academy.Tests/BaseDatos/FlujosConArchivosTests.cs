using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using STAT_Academy.Api.Configuration;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Services;
using STAT_Academy.Api.Storage;
using STAT_Academy.DTOs.Blogs;
using STAT_Academy.DTOs.Factura;

namespace STAT_Academy.Tests.BaseDatos;

/// <summary>
/// Compras, facturas y pagos, blog, y aula y entregas: servicios que además de la base
/// usan Blob Storage (simulado en memoria) y transacciones. El correo no está configurado,
/// así que los avisos fallan al instante y los servicios solo lo registran.
/// </summary>
public sealed class FlujosConArchivosTests : IDisposable
{
    private readonly BaseDatosPrueba _bd = new();
    private readonly BlobStorageSimulado _blob = new();
    private readonly List<ApplicationDbContext> _contextos = [];

    public void Dispose()
    {
        foreach (var c in _contextos)
            c.Dispose();
        _bd.Dispose();
    }

    private ApplicationDbContext Contexto()
    {
        var c = _bd.NuevoContexto();
        _contextos.Add(c);
        return c;
    }

    private static CorreoService Correo() =>
        new(new ConfigurationBuilder().Build(), NullLogger<CorreoService>.Instance);

    private FacturaService Facturas(ApplicationDbContext c) =>
        new(c, _blob, Correo(), new AuditoriaService(c),
            Options.Create(new PagosOptions { SinpeTelefono = "8888-8888", SinpeNombre = "STAT Academy" }),
            NullLogger<FacturaService>.Instance);

    private CompraService Compras(ApplicationDbContext c)
    {
        var auditoria = new AuditoriaService(c);
        return new CompraService(c, auditoria,
            new StockAlertaService(c, Correo(), auditoria, NullLogger<StockAlertaService>.Instance),
            Facturas(c));
    }

    // ==================================================================
    // Compras
    // ==================================================================

    private (UsuarioModel cliente, ProductoModel producto) SembrarCarrito(int stock, int cantidad, int minimo = 1)
    {
        var cliente = BaseDatosPrueba.Usuario("cliente@correo.cr");
        var proveedor = new ProveedorModel { nombre = "Proveedor", estado = true };
        _bd.Sembrar(cliente, proveedor);
        var producto = BaseDatosPrueba.Producto("Termómetro", proveedor.id, stock, minimo);
        var carrito = new CarritoModel { fk_Usuario = cliente.id };
        _bd.Sembrar(producto, carrito);
        _bd.Sembrar(new CarritoDetalleModel { fk_Carrito = carrito.id, fk_Producto = producto.id, cantidad = cantidad });
        return (cliente, producto);
    }

    [Fact]
    public async Task Comprar_crea_la_factura_descuenta_stock_y_vacia_el_carrito()
    {
        var (cliente, producto) = SembrarCarrito(stock: 5, cantidad: 2);

        var (compra, error) = await Compras(Contexto()).Comprar(cliente.id, "cliente@correo.cr");

        Assert.Null(error);
        Assert.Equal(2000m, compra!.total);
        Assert.False(compra.pagada);
        Assert.Equal("Termómetro", Assert.Single(compra.lineas).nombre);

        var c = Contexto();
        Assert.Equal(3, (await c.Producto.SingleAsync()).stock);
        Assert.Equal(0, await c.CarritoDetalle.CountAsync());
        var factura = await c.Factura.Include(f => f.Detalles).SingleAsync();
        Assert.Equal(2000m, factura.total);
        Assert.Equal(2, Assert.Single(factura.Detalles).cantidad);
        Assert.Equal(1, await c.Auditoria.CountAsync(a => a.entidad == "FACTURA" && a.accion == "CREATE"));
    }

    [Fact]
    public async Task Comprar_sin_stock_suficiente_no_deja_cambios()
    {
        var (cliente, _) = SembrarCarrito(stock: 1, cantidad: 3);

        var (compra, error) = await Compras(Contexto()).Comprar(cliente.id, "cliente@correo.cr");

        Assert.Null(compra);
        Assert.Contains("Solo quedan 1 unidades", error);

        var c = Contexto();
        Assert.Equal(1, (await c.Producto.SingleAsync()).stock);
        Assert.Equal(1, await c.CarritoDetalle.CountAsync());
        Assert.Equal(0, await c.Factura.CountAsync());
    }

    [Fact]
    public async Task Comprar_con_el_carrito_vacio_da_error()
    {
        var cliente = BaseDatosPrueba.Usuario("cliente@correo.cr");
        _bd.Sembrar(cliente);

        var (compra, error) = await Compras(Contexto()).Comprar(cliente.id, "cliente@correo.cr");

        Assert.Null(compra);
        Assert.Equal("Tu carrito está vacío.", error);
    }

    [Fact]
    public async Task Comprar_un_curso_de_pago_reutiliza_la_factura_pendiente()
    {
        var cliente = BaseDatosPrueba.Usuario("cliente@correo.cr");
        var tutor = BaseDatosPrueba.Usuario("tutor@correo.cr", tipo: 2);
        _bd.Sembrar(cliente, tutor);
        var curso = BaseDatosPrueba.Curso("Bioestadística", tutor.id);
        curso.precio = 25000m;
        curso.fechaInicio = CursoModel.HoyCostaRica.AddDays(7);
        curso.fechaFin = CursoModel.HoyCostaRica.AddDays(60);
        _bd.Sembrar(curso);

        var primera = await Compras(Contexto()).ComprarCurso(cliente.id, "cliente@correo.cr", curso.id);
        var segunda = await Compras(Contexto()).ComprarCurso(cliente.id, "cliente@correo.cr", curso.id);

        Assert.Null(primera.error);
        Assert.False(primera.existente);
        Assert.True(segunda.existente);
        Assert.Equal(primera.facturaId, segunda.facturaId);
        Assert.Equal(1, await Contexto().Factura.CountAsync());
    }

    // ==================================================================
    // Facturas y pagos
    // ==================================================================

    private async Task<(UsuarioModel cliente, int facturaId)> SembrarFacturaPendiente()
    {
        var (cliente, _) = SembrarCarrito(stock: 5, cantidad: 1);
        var (compra, _) = await Compras(Contexto()).Comprar(cliente.id, "cliente@correo.cr");
        return (cliente, compra!.facturaId);
    }

    private Task<(DTOs.Factura.PagoComprobanteResponse? pago, string? error, bool noExiste)> EnviarComprobante(
        int facturaId, int usuarioId, IFormFile? archivo, string metodo = PedidoPagoModel.MetodoSinpe) =>
        Facturas(Contexto()).EnviarComprobante(facturaId, usuarioId, "cliente@correo.cr", metodo, " REF-123 ", archivo, default);

    [Fact]
    public async Task Comprobante_de_pago_queda_en_verificacion_y_en_el_contenedor_privado()
    {
        var (cliente, facturaId) = await SembrarFacturaPendiente();

        var (pago, error, _) = await EnviarComprobante(facturaId, cliente.id, BlobStorageSimulado.Archivo("sinpe.png", BlobStorageSimulado.Png));

        Assert.Null(error);
        Assert.NotNull(pago);
        var guardado = await Contexto().PedidoPago.SingleAsync();
        Assert.Equal(PedidoPagoModel.Pendiente, guardado.estado);
        Assert.Equal("REF-123", guardado.referencia);
        Assert.Equal(ContenedorBlob.Documentos, _blob.Archivos[guardado.comprobanteBlob].contenedor);

        var detalle = await Facturas(Contexto()).Detalle(facturaId, cliente.id, esAdmin: false);
        Assert.Equal(EstadoFactura.EnVerificacion, detalle!.estado);
        Assert.False(detalle.puedeEnviarComprobante);
        Assert.Equal(1, await Facturas(Contexto()).PendientesDeRevision());

        var (_, repetido, _) = await EnviarComprobante(facturaId, cliente.id, BlobStorageSimulado.Archivo("otro.png", BlobStorageSimulado.Png));
        Assert.Contains("en verificación", repetido);
    }

    [Fact]
    public async Task Comprobante_invalido_o_de_otra_persona_se_rechaza_sin_subir_nada()
    {
        var (cliente, facturaId) = await SembrarFacturaPendiente();
        var otro = BaseDatosPrueba.Usuario("otro@correo.cr");
        _bd.Sembrar(otro);

        var ajena = await EnviarComprobante(facturaId, otro.id, BlobStorageSimulado.Archivo("sinpe.png", BlobStorageSimulado.Png));
        Assert.True(ajena.noExiste);

        var sinArchivo = await EnviarComprobante(facturaId, cliente.id, null);
        Assert.Equal("Adjunta el comprobante de pago.", sinArchivo.error);

        var disfrazado = await EnviarComprobante(facturaId, cliente.id, BlobStorageSimulado.Archivo("sinpe.png", "<html>"u8.ToArray()));
        Assert.Contains("no corresponde", disfrazado.error);

        var metodo = await EnviarComprobante(facturaId, cliente.id, BlobStorageSimulado.Archivo("sinpe.png", BlobStorageSimulado.Png), "EFECTIVO");
        Assert.Contains("SINPE", metodo.error);

        Assert.Empty(_blob.Archivos);
        Assert.Equal(0, await Contexto().PedidoPago.CountAsync());
    }

    [Fact]
    public async Task Si_falla_Blob_Storage_no_se_registra_el_comprobante()
    {
        var (cliente, facturaId) = await SembrarFacturaPendiente();
        _blob.FallarSubidas = true;

        var (pago, error, _) = await EnviarComprobante(facturaId, cliente.id, BlobStorageSimulado.Archivo("sinpe.pdf", BlobStorageSimulado.Pdf));

        Assert.Null(pago);
        Assert.Contains("No se pudo guardar", error);
        Assert.Equal(0, await Contexto().PedidoPago.CountAsync());
    }

    [Fact]
    public async Task Aprobar_un_pago_marca_la_factura_pagada_y_matricula_los_cursos()
    {
        var cliente = BaseDatosPrueba.Usuario("cliente@correo.cr");
        var tutor = BaseDatosPrueba.Usuario("tutor@correo.cr", tipo: 2);
        var admin = BaseDatosPrueba.Usuario("admin@correo.cr", tipo: 1);
        _bd.Sembrar(cliente, tutor, admin, new MetodoPagoModel { nombre = "SINPE Móvil", estado = true });
        var curso = BaseDatosPrueba.Curso("Bioestadística", tutor.id);
        curso.precio = 25000m;
        curso.fechaInicio = CursoModel.HoyCostaRica.AddDays(7);
        _bd.Sembrar(curso);

        var (facturaId, _, _) = await Compras(Contexto()).ComprarCurso(cliente.id, "cliente@correo.cr", curso.id);
        var (pago, _, _) = await EnviarComprobante(facturaId!.Value, cliente.id, BlobStorageSimulado.Archivo("sinpe.png", BlobStorageSimulado.Png));

        var (ok, mensaje) = await Facturas(Contexto()).Aprobar(pago!.id, admin.id, "admin@correo.cr");

        Assert.True(ok, mensaje);
        var c = Contexto();
        var factura = await c.Factura.SingleAsync();
        Assert.True(factura.pagada);
        Assert.NotNull(factura.fk_Pago);
        Assert.Equal(PedidoPagoModel.Aprobado, (await c.PedidoPago.SingleAsync()).estado);
        var matricula = await c.EstudianteCurso.SingleAsync();
        Assert.Equal((curso.id, cliente.id, "Activo"), (matricula.CursoId, matricula.EstudianteId, matricula.Estado));

        // Un segundo intento no vuelve a aprobar ni crea otro pago.
        var (otraVez, _) = await Facturas(Contexto()).Aprobar(pago.id, admin.id, "admin@correo.cr");
        Assert.False(otraVez);
        Assert.Equal(1, await Contexto().Pago.CountAsync());
    }

    [Fact]
    public async Task Rechazar_exige_motivo_y_permite_enviar_otro_comprobante()
    {
        var (cliente, facturaId) = await SembrarFacturaPendiente();
        var admin = BaseDatosPrueba.Usuario("admin@correo.cr", tipo: 1);
        _bd.Sembrar(admin);
        var (pago, _, _) = await EnviarComprobante(facturaId, cliente.id, BlobStorageSimulado.Archivo("sinpe.png", BlobStorageSimulado.Png));

        Assert.False((await Facturas(Contexto()).Rechazar(pago!.id, admin.id, "admin@correo.cr", "  ")).ok);
        Assert.True((await Facturas(Contexto()).Rechazar(pago.id, admin.id, "admin@correo.cr", "El monto no coincide")).ok);

        var detalle = await Facturas(Contexto()).Detalle(facturaId, cliente.id, esAdmin: false);
        Assert.Equal(EstadoFactura.Rechazada, detalle!.estado);
        Assert.True(detalle.puedeEnviarComprobante);
        Assert.Equal("El monto no coincide", (await Contexto().PedidoPago.SingleAsync()).observacion);
    }

    [Fact]
    public async Task Solo_el_dueno_o_un_admin_ven_la_factura_y_el_comprobante()
    {
        var (cliente, facturaId) = await SembrarFacturaPendiente();
        var otro = BaseDatosPrueba.Usuario("otro@correo.cr");
        _bd.Sembrar(otro);
        var (pago, _, _) = await EnviarComprobante(facturaId, cliente.id, BlobStorageSimulado.Archivo("sinpe.pdf", BlobStorageSimulado.Pdf));

        var facturas = Facturas(Contexto());
        Assert.Null(await facturas.Detalle(facturaId, otro.id, esAdmin: false));
        Assert.NotNull(await facturas.Detalle(facturaId, otro.id, esAdmin: true));
        Assert.Null(await facturas.AbrirComprobante(pago!.id, otro.id, esAdmin: false, default));

        var archivo = await facturas.AbrirComprobante(pago.id, cliente.id, esAdmin: false, default);
        Assert.EndsWith(".pdf", archivo!.Value.nombre);
        Assert.Equal(1, (await facturas.MisFacturasPaginadas(cliente.id, null, 1, 10)).total);
        Assert.Empty(await facturas.MisFacturas(otro.id));
    }

    // ==================================================================
    // Blog
    // ==================================================================

    private BlogService Blog(ApplicationDbContext c) =>
        new(c, new AuditoriaService(c), _blob, NullLogger<BlogService>.Instance,
            new BlogContenidoService(Options.Create(new BlobStorageOptions { AccountUrl = "https://pruebas.blob.core.windows.net" })));

    private UsuarioModel SembrarAutor()
    {
        var autor = BaseDatosPrueba.Usuario("autor@correo.cr", tipo: 2, nombre: "Autora");
        _bd.Sembrar(autor);
        return autor;
    }

    [Fact]
    public async Task Crear_entrada_sanea_el_html_y_conserva_solo_los_colores()
    {
        var autor = SembrarAutor();

        var (blog, error) = await Blog(Contexto()).CrearBlogAsync(new CreateBlogRequest
        {
            titulo = "  Novedades ",
            contenido = "<p style=\"color: red; position: fixed\" onclick=\"x()\">Hola</p><script>alert(1)</script>"
        }, autor.id, "autor@correo.cr");

        Assert.Null(error);
        Assert.Equal("Novedades", blog!.titulo);
        Assert.Equal("Autora", blog.autor);
        Assert.Contains("color: rgba(255, 0, 0, 1)", blog.contenido); // el sanitizador normaliza el color
        Assert.DoesNotContain("position", blog.contenido);
        Assert.DoesNotContain("onclick", blog.contenido);
        Assert.DoesNotContain("<script", blog.contenido);
    }

    [Fact]
    public async Task Entrada_vacia_no_se_crea()
    {
        var autor = SembrarAutor();

        var (blog, error) = await Blog(Contexto()).CrearBlogAsync(new CreateBlogRequest { titulo = "Vacía", contenido = "<p><script>x</script></p>" }, autor.id, "autor@correo.cr");

        Assert.Null(blog);
        Assert.Equal("El contenido no puede estar vacío.", error);
        Assert.Equal(0, await Contexto().EntradaBlog.CountAsync());
    }

    [Fact]
    public async Task Entrada_desactivada_solo_la_ve_un_admin()
    {
        var autor = SembrarAutor();
        var (blog, _) = await Blog(Contexto()).CrearBlogAsync(new CreateBlogRequest { titulo = "Temporal", contenido = "<p>Texto</p>" }, autor.id, "autor@correo.cr");

        await Blog(Contexto()).DesactivarBlogAsync(blog!.id, "autor@correo.cr");

        var servicio = Blog(Contexto());
        Assert.Null(await servicio.GetBlogByIdAsync(blog.id, incluirInactivos: false));
        Assert.Empty(await servicio.GetBlogsAsync(incluirInactivos: false));
        Assert.False((await servicio.GetBlogByIdAsync(blog.id, incluirInactivos: true))!.estado);
        Assert.Equal(autor.id, await servicio.ObtenerAutorIdAsync(blog.id));
    }

    [Fact]
    public async Task Adjuntos_la_primera_imagen_es_la_principal_y_al_quitarla_pasa_la_siguiente()
    {
        var autor = SembrarAutor();
        var (blog, _) = await Blog(Contexto()).CrearBlogAsync(new CreateBlogRequest { titulo = "Con fotos", contenido = "<p>Texto</p>" }, autor.id, "autor@correo.cr");

        var (conArchivos, errores) = await Blog(Contexto()).AgregarArchivos(blog!.id,
        [
            BlobStorageSimulado.Archivo("guia.pdf", BlobStorageSimulado.Pdf),
            BlobStorageSimulado.Archivo("uno.png", BlobStorageSimulado.Png),
            BlobStorageSimulado.Archivo("dos.png", BlobStorageSimulado.Png)
        ], "autor@correo.cr");

        Assert.Empty(errores);
        Assert.Equal(3, conArchivos!.archivos.Count);
        var principal = Assert.Single(conArchivos.archivos, a => a.es_principal);
        Assert.Equal("uno.png", principal.nombre);
        Assert.Equal(principal.url, conArchivos.imagen_principal);

        // El PDF va al contenedor privado y se descarga por la API.
        var pdf = conArchivos.archivos.Single(a => !a.es_imagen);
        var descarga = await Blog(Contexto()).AbrirDocumento(blog.id, pdf.id, incluirInactivos: false, default);
        Assert.Equal("guia.pdf", descarga!.Value.nombre);

        Assert.True(await Blog(Contexto()).QuitarArchivo(blog.id, principal.id, "autor@correo.cr"));
        var despues = await Blog(Contexto()).GetBlogByIdAsync(blog.id, incluirInactivos: false);
        Assert.Equal("dos.png", Assert.Single(despues!.archivos, a => a.es_principal).nombre);
        Assert.Equal(2, _blob.Archivos.Count);
    }

    [Fact]
    public async Task Un_archivo_invalido_impide_subir_los_demas()
    {
        var autor = SembrarAutor();
        var (blog, _) = await Blog(Contexto()).CrearBlogAsync(new CreateBlogRequest { titulo = "Entrada", contenido = "<p>Texto</p>" }, autor.id, "autor@correo.cr");

        var (resultado, errores) = await Blog(Contexto()).AgregarArchivos(blog!.id,
        [
            BlobStorageSimulado.Archivo("bien.png", BlobStorageSimulado.Png),
            BlobStorageSimulado.Archivo("script.exe", [0x4D, 0x5A])
        ], "autor@correo.cr");

        Assert.Null(resultado);
        Assert.Single(errores);
        Assert.Empty(_blob.Archivos);
        Assert.Equal(0, await Contexto().EntradaBlogArchivo.CountAsync());
    }

    // ==================================================================
    // Aula y entregas
    // ==================================================================

    private AulaService Aula(ApplicationDbContext c) =>
        new(c, new AuditoriaService(c), _blob,
            new NotaFinalService(c, new AuditoriaService(c), NullLogger<NotaFinalService>.Instance),
            Correo(), NullLogger<AulaService>.Instance);

    private (UsuarioModel estudiante, TareaModel tarea) SembrarTarea()
    {
        // Tutor con el correo vacío: el aviso de entrega no intenta enviarse.
        var tutor = BaseDatosPrueba.Usuario("tutor@correo.cr", tipo: 2);
        var estudiante = BaseDatosPrueba.Usuario("estudiante@correo.cr");
        _bd.Sembrar(tutor, estudiante);
        var curso = BaseDatosPrueba.Curso("Estadística", tutor.id);
        _bd.Sembrar(curso);
        var tarea = new TareaModel
        {
            CursoId = curso.id, Titulo = "Tarea 1", Estado = true,
            Fecha_Inicio = DateTime.UtcNow.AddDays(-1), Fecha_Limite = DateTime.UtcNow.AddDays(7)
        };
        _bd.Sembrar(tarea, new EstudianteCursoModel
        {
            CursoId = curso.id, EstudianteId = estudiante.id, Estado = "Activo", Fecha_Matricula = DateTime.UtcNow
        });
        using (var c = _bd.NuevoContexto())
            c.Usuario.Where(u => u.id == tutor.id).ExecuteUpdate(s => s.SetProperty(u => u.email, ""));
        return (estudiante, tarea);
    }

    private Task<(DTOs.Aula.AulaTareaResponse? tarea, string? error)> Entregar(
        int tareaId, int estudianteId, IFormFile[] archivos, string? enlace = null, bool quitarEnlace = false) =>
        Aula(Contexto()).Entregar(tareaId, estudianteId, "estudiante@correo.cr", archivos, enlace, quitarEnlace, default);

    [Fact]
    public async Task Entregar_un_archivo_marca_la_tarea_entregada()
    {
        var (estudiante, tarea) = SembrarTarea();

        var (respuesta, error) = await Entregar(tarea.ID, estudiante.id, [BlobStorageSimulado.Archivo("respuesta.pdf", BlobStorageSimulado.Pdf)]);

        Assert.Null(error);
        Assert.Equal(EntregaTareaModel.EstadoEntregada, respuesta!.estado);
        Assert.Equal("respuesta.pdf", Assert.Single(respuesta.archivos).nombre);
        Assert.Equal(ContenedorBlob.Documentos, Assert.Single(_blob.Archivos).Value.contenedor);
        Assert.Equal(1, await Contexto().Auditoria.CountAsync(a => a.entidad == "ENTREGA_TAREA" && a.accion == "CREATE"));

        var curso = await Aula(Contexto()).CursoAsync(tarea.CursoId!.Value, estudiante.id);
        Assert.Equal(EntregaTareaModel.EstadoEntregada, Assert.Single(curso!.tareas).estado);
    }

    [Fact]
    public async Task Entrega_sin_archivo_ni_enlace_o_con_enlace_invalido_se_rechaza()
    {
        var (estudiante, tarea) = SembrarTarea();

        Assert.Equal("Adjunte al menos un archivo o escriba un enlace.", (await Entregar(tarea.ID, estudiante.id, [])).error);
        Assert.Contains("dirección web completa", (await Entregar(tarea.ID, estudiante.id, [], enlace: "javascript:alert(1)")).error);
        Assert.Equal(0, await Contexto().EntregaTarea.CountAsync());
    }

    [Fact]
    public async Task Quitar_el_unico_archivo_deja_la_tarea_sin_entregar()
    {
        var (estudiante, tarea) = SembrarTarea();
        var (respuesta, _) = await Entregar(tarea.ID, estudiante.id, [BlobStorageSimulado.Archivo("respuesta.pdf", BlobStorageSimulado.Pdf)]);
        var archivoId = respuesta!.archivos[0].id;

        var ajeno = await Aula(Contexto()).QuitarArchivo(archivoId, estudianteId: 999, "otro@correo.cr");
        Assert.False(ajeno.exitoso);

        var (exitoso, mensaje, _) = await Aula(Contexto()).QuitarArchivo(archivoId, estudiante.id, "estudiante@correo.cr");

        Assert.True(exitoso);
        Assert.Contains("sin entregar", mensaje);
        Assert.Empty(_blob.Archivos);
        Assert.Null((await Contexto().EntregaTarea.SingleAsync()).fecha_Entrega);
    }

    [Fact]
    public async Task Una_entrega_calificada_ya_no_se_modifica()
    {
        var (estudiante, tarea) = SembrarTarea();
        await Entregar(tarea.ID, estudiante.id, [], enlace: "https://docs.example.com/mi-trabajo");
        using (var c = _bd.NuevoContexto())
            await c.EntregaTarea.ExecuteUpdateAsync(s => s.SetProperty(e => e.calificacion, 90m));

        var (_, error) = await Entregar(tarea.ID, estudiante.id, [BlobStorageSimulado.Archivo("tarde.pdf", BlobStorageSimulado.Pdf)]);

        Assert.Contains("ya fue calificada", error);
        Assert.Empty(_blob.Archivos);
    }

    [Fact]
    public async Task El_estudiante_solo_descarga_archivos_de_su_entrega()
    {
        var (estudiante, tarea) = SembrarTarea();
        var (respuesta, _) = await Entregar(tarea.ID, estudiante.id, [BlobStorageSimulado.Archivo("respuesta.pdf", BlobStorageSimulado.Pdf)]);
        var archivoId = respuesta!.archivos[0].id;

        var aula = Aula(Contexto());
        Assert.Equal(tarea.CursoId, await aula.CursoDeArchivoPropioAsync(archivoId, estudiante.id));
        Assert.Null(await aula.CursoDeArchivoPropioAsync(archivoId, estudianteId: 999));
        Assert.Equal("respuesta.pdf", (await aula.AbrirArchivo(archivoId, default))!.Value.nombre);
    }
}
