using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using STAT_Academy.Api.Data;
using STAT_Academy.Api.Models;
using STAT_Academy.Api.Services;
using STAT_Academy.DTOs.Carrito;
using STAT_Academy.DTOs.Producto;
using STAT_Academy.DTOs.Proveedor;
using STAT_Academy.DTOs.Tutor;
using STAT_Academy.DTOs.Usuarios;

namespace STAT_Academy.Tests.BaseDatos;

/// <summary>
/// Servicios de la API contra una base SQLite en memoria: consultas traducidas a SQL,
/// reglas de negocio y registros de auditoría.
/// </summary>
public sealed class ServiciosConBaseDatosTests : IDisposable
{
    private readonly BaseDatosPrueba _bd = new();
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

    private UsuarioService Usuarios(ApplicationDbContext c) =>
        new(c, new AuditoriaService(c), BaseDatosPrueba.Validador());

    private static TutorService Tutor(ApplicationDbContext c) =>
        new(c, new AuditoriaService(c), null!, new NotaFinalService(c, new AuditoriaService(c), NullLogger<NotaFinalService>.Instance),
            Correo(), NullLogger<TutorService>.Instance);

    private static CorreoService Correo() =>
        new(new ConfigurationBuilder().Build(), NullLogger<CorreoService>.Instance);

    // ------------------------------------------------------------------
    // Proveedores
    // ------------------------------------------------------------------

    [Fact]
    public async Task Proveedor_creado_queda_activo_y_en_auditoria()
    {
        var c = Contexto();
        var servicio = new ProveedorService(c, new AuditoriaService(c));

        var creado = await servicio.CrearAsync(new ProveedorCreateRequest { nombre = "Distribuidora", email = "ventas@dist.cr" });

        var lista = await new ProveedorService(Contexto(), new AuditoriaService(Contexto())).GetAllAsync();
        Assert.Single(lista);
        Assert.True(lista[0].estado);
        Assert.Equal(creado.id, lista[0].id);
        Assert.Single(await Contexto().Auditoria.Where(a => a.entidad == "PROVEEDOR" && a.accion == "CREATE").ToListAsync());
    }

    [Fact]
    public async Task Proveedor_con_productos_activos_no_se_puede_desactivar()
    {
        var proveedor = new ProveedorModel { nombre = "Con productos", estado = true };
        _bd.Sembrar(proveedor);
        _bd.Sembrar(BaseDatosPrueba.Producto("Guantes", proveedor.id));

        var c = Contexto();
        var servicio = new ProveedorService(c, new AuditoriaService(c));

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.DesactivarAsync(proveedor.id));
        Assert.True((await Contexto().Proveedor.SingleAsync()).estado);
    }

    [Fact]
    public async Task Proveedor_inexistente_devuelve_null()
    {
        var c = Contexto();
        var servicio = new ProveedorService(c, new AuditoriaService(c));

        Assert.Null(await servicio.GetByIdAsync(999));
        Assert.Null(await servicio.EditarAsync(999, new ProveedorCreateRequest { nombre = "X" }));
        Assert.Null(await servicio.DesactivarAsync(999));
    }

    // ------------------------------------------------------------------
    // Carrito
    // ------------------------------------------------------------------

    private (int usuarioId, ProductoModel producto) SembrarTienda(int stock = 5)
    {
        var usuario = BaseDatosPrueba.Usuario("cliente@correo.cr");
        var proveedor = new ProveedorModel { nombre = "Proveedor", estado = true };
        _bd.Sembrar(usuario, proveedor);
        var producto = BaseDatosPrueba.Producto("Mascarillas", proveedor.id, stock);
        _bd.Sembrar(producto);
        return (usuario.id, producto);
    }

    [Fact]
    public async Task Carrito_se_crea_vacio_la_primera_vez()
    {
        var (usuarioId, _) = SembrarTienda();

        var carrito = await new CarritoService(Contexto()).GetCarritoAsync(usuarioId);

        Assert.Empty(carrito.items);
        Assert.Equal(1, await Contexto().Carrito.CountAsync(x => x.fk_Usuario == usuarioId));
    }

    [Fact]
    public async Task Agregar_dos_veces_el_mismo_producto_suma_la_cantidad()
    {
        var (usuarioId, producto) = SembrarTienda(stock: 5);
        var servicio = new CarritoService(Contexto());

        await servicio.AgregarItemAsync(new AgregarAlCarritoRequest { usuarioId = usuarioId, productoId = producto.id, cantidad = 2 });
        var carrito = await servicio.AgregarItemAsync(new AgregarAlCarritoRequest { usuarioId = usuarioId, productoId = producto.id, cantidad = 1 });

        var item = Assert.Single(carrito.items);
        Assert.Equal(3, item.cantidad);
        Assert.Equal(3000m, carrito.total);
    }

    [Fact]
    public async Task No_se_puede_agregar_mas_que_el_stock()
    {
        var (usuarioId, producto) = SembrarTienda(stock: 2);
        var servicio = new CarritoService(Contexto());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.AgregarItemAsync(new AgregarAlCarritoRequest { usuarioId = usuarioId, productoId = producto.id, cantidad = 3 }));

        Assert.Contains("2 unidades", error.Message);
        Assert.Equal(0, await Contexto().CarritoDetalle.CountAsync());
    }

    [Fact]
    public async Task Solo_se_modifica_el_detalle_del_carrito_propio()
    {
        var (usuarioId, producto) = SembrarTienda();
        var otro = BaseDatosPrueba.Usuario("otro@correo.cr");
        _bd.Sembrar(otro);

        var servicio = new CarritoService(Contexto());
        var carrito = await servicio.AgregarItemAsync(new AgregarAlCarritoRequest { usuarioId = usuarioId, productoId = producto.id, cantidad = 1 });
        var detalleId = carrito.items[0].detalleId;

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ActualizarCantidadAsync(detalleId, otro.id, 2));
        await servicio.EliminarItemAsync(detalleId, otro.id);

        Assert.Equal(1, await Contexto().CarritoDetalle.CountAsync());
        Assert.Empty((await servicio.EliminarItemAsync(detalleId, usuarioId)).items);
    }

    // ------------------------------------------------------------------
    // Productos
    // ------------------------------------------------------------------

    private ProductoService Productos(ApplicationDbContext c)
    {
        var auditoria = new AuditoriaService(c);
        return new ProductoService(c, auditoria, null!,
            new StockAlertaService(c, Correo(), auditoria, NullLogger<StockAlertaService>.Instance),
            NullLogger<ProductoService>.Instance);
    }

    [Fact]
    public async Task Producto_con_proveedor_inexistente_no_se_crea()
    {
        var (producto, error) = await Productos(Contexto()).CrearAsync(new ProductoCreateRequest
        {
            nombre = "Jeringas", categoria = "Insumos", descripcion = "Caja", precio_base = 500, stock = 3, min_stock = 1, fk_proveedor = 42
        }, "admin@stat.cr");

        Assert.Null(producto);
        Assert.Equal("El proveedor seleccionado no existe.", error);
        Assert.Equal(0, await Contexto().Producto.CountAsync());
    }

    [Fact]
    public async Task Clientes_solo_ven_productos_activos_y_stock_bajo_filtra_por_minimo()
    {
        var proveedor = new ProveedorModel { nombre = "P", estado = true };
        _bd.Sembrar(proveedor);
        _bd.Sembrar(
            BaseDatosPrueba.Producto("Activo con stock", proveedor.id, stock: 10, minimo: 2),
            BaseDatosPrueba.Producto("Activo en el mínimo", proveedor.id, stock: 2, minimo: 2),
            BaseDatosPrueba.Producto("Inactivo agotado", proveedor.id, stock: 0, minimo: 2, activo: false));

        var servicio = Productos(Contexto());

        Assert.Equal(2, (await servicio.GetAllAsync(incluirInactivos: false)).Count);
        Assert.Equal(3, (await servicio.GetAllAsync(incluirInactivos: true)).Count);
        Assert.Equal("Activo en el mínimo", Assert.Single(await servicio.StockBajoAsync()).nombre);
    }

    // ------------------------------------------------------------------
    // Usuarios y perfil
    // ------------------------------------------------------------------

    private static RegisterUsuarioRequest Registro(bool acepta = true, string version = "2026-11-16") => new()
    {
        email = "registro@correo.cr", nombre = "Persona Registro", password = "Clave123!",
        aceptaTerminos = acepta, versionTerminos = version
    };

    [Fact]
    public async Task Registro_guarda_version_y_fecha_de_aceptacion_de_terminos()
    {
        var antes = DateTime.UtcNow.AddSeconds(-1);

        var usuario = await Usuarios(Contexto()).RegisterUsuarioAsync(Registro());

        var aceptacion = await Contexto().UsuarioTerminos.SingleAsync();
        Assert.Equal(usuario.id, aceptacion.fk_Usuario);
        Assert.Equal("2026-11-16", aceptacion.version_Terminos);
        Assert.InRange(aceptacion.fecha_Aceptacion, antes, DateTime.UtcNow.AddSeconds(1));
    }

    [Theory]
    [InlineData(false, "2026-11-16")]
    [InlineData(true, "")]
    public async Task Registro_sin_aceptar_terminos_falla_y_no_crea_la_cuenta(bool acepta, string version)
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Usuarios(Contexto()).RegisterUsuarioAsync(Registro(acepta, version)));

        Assert.Equal(RegisterUsuarioRequest.MensajeTerminos, error.Message);
        Assert.False(await Contexto().Usuario.AnyAsync());
        Assert.False(await Contexto().UsuarioTerminos.AnyAsync());
    }

    [Fact]
    public void Validacion_del_registro_exige_aceptar_los_terminos()
    {
        static List<ValidationResult> Errores(RegisterUsuarioRequest r)
        {
            var errores = new List<ValidationResult>();
            Validator.TryValidateObject(r, new ValidationContext(r), errores, validateAllProperties: true);
            return errores;
        }

        Assert.Contains(Errores(Registro(acepta: false)), e => e.ErrorMessage == RegisterUsuarioRequest.MensajeTerminos
                                      && e.MemberNames.Contains(nameof(RegisterUsuarioRequest.aceptaTerminos)));
        Assert.Empty(Errores(Registro()));
    }

    [Fact]
    public async Task Crear_usuario_con_correo_repetido_falla()
    {
        _bd.Sembrar(BaseDatosPrueba.Usuario("repetido@correo.cr"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Usuarios(Contexto()).CreateUsuarioAsync(new CreateUsuarioRequest
        {
            email = "repetido@correo.cr", nombre = "Otro", password = "Clave123!", fk_Tipo_Usuario = 3
        }, "admin@stat.cr"));
    }

    [Fact]
    public async Task Usuario_creado_trae_fecha_de_registro_desde_auditoria()
    {
        var usuario = await Usuarios(Contexto()).CreateUsuarioAsync(new CreateUsuarioRequest
        {
            email = "nuevo@correo.cr", nombre = "  Persona Nueva ", password = "Clave123!", fk_Tipo_Usuario = 2
        }, "admin@stat.cr");

        Assert.Equal("Persona Nueva", usuario.nombre);
        Assert.Equal("Tutor", usuario.tipo);
        Assert.NotNull(usuario.fecha_Registro);
        Assert.NotEqual("Clave123!", (await Contexto().Usuario.SingleAsync()).password);
    }

    [Fact]
    public async Task Buscar_filtra_pagina_y_resume_por_tipo()
    {
        _bd.Sembrar(
            BaseDatosPrueba.Usuario("ana@correo.cr", tipo: 3, nombre: "Ana"),
            BaseDatosPrueba.Usuario("beto@correo.cr", tipo: 3, nombre: "Beto", activo: false),
            BaseDatosPrueba.Usuario("carla@correo.cr", tipo: 2, nombre: "Carla"),
            BaseDatosPrueba.Usuario("admin@correo.cr", tipo: 1, nombre: "Admin"));

        var servicio = Usuarios(Contexto());

        var clientes = await servicio.BuscarAsync(null, tipo: 3, estado: null, orden: "nombre_desc", pagina: 1, tamano: 1);
        Assert.Equal(2, clientes.total);
        Assert.Equal(2, clientes.totalPaginas);
        Assert.Equal("Beto", Assert.Single(clientes.items).nombre);

        var porTexto = await servicio.BuscarAsync("carla", null, null, null, 1, 10);
        Assert.Equal("Carla", Assert.Single(porTexto.items).nombre);

        Assert.Equal(4, clientes.resumen.total);
        Assert.Equal(2, clientes.resumen.clientes);
        Assert.Equal(1, clientes.resumen.tutores);
        Assert.Equal(1, clientes.resumen.admins);
        Assert.Equal(1, clientes.resumen.inactivos);
    }

    [Fact]
    public async Task Un_admin_no_puede_desactivarse_a_si_mismo()
    {
        var admin = BaseDatosPrueba.Usuario("admin@correo.cr", tipo: 1);
        _bd.Sembrar(admin);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Usuarios(Contexto()).CambiarEstadoAsync(admin.id, activo: false, actorId: admin.id, actorEmail: "admin@correo.cr"));

        Assert.True((await Contexto().Usuario.SingleAsync()).estado);
    }

    [Fact]
    public async Task Desactivar_otro_usuario_queda_guardado_y_auditado()
    {
        var admin = BaseDatosPrueba.Usuario("admin@correo.cr", tipo: 1);
        var cliente = BaseDatosPrueba.Usuario("cliente@correo.cr");
        _bd.Sembrar(admin, cliente);

        var respuesta = await Usuarios(Contexto()).CambiarEstadoAsync(cliente.id, false, admin.id, "admin@correo.cr");

        Assert.False(respuesta!.estado);
        Assert.NotNull(respuesta.fecha_Edicion);
        Assert.Equal(1, await Contexto().Auditoria.CountAsync(a => a.accion == "DISABLE" && a.entidad_id == cliente.id));
    }

    [Fact]
    public async Task Perfil_cambia_el_nombre_propio_y_rechaza_nombres_invalidos()
    {
        var cliente = BaseDatosPrueba.Usuario("cliente@correo.cr", nombre: "Nombre Viejo");
        _bd.Sembrar(cliente);
        var servicio = Usuarios(Contexto());

        var perfil = await servicio.ActualizarNombrePropioAsync(cliente.id, "  Nombre   Nuevo ", "cliente@correo.cr");

        Assert.Equal("Nombre Nuevo", perfil!.nombre);
        Assert.Equal("cliente@correo.cr", perfil.email);
        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.ActualizarNombrePropioAsync(cliente.id, " ", "cliente@correo.cr"));
    }

    [Fact]
    public async Task Perfil_de_cuenta_desactivada_no_se_edita()
    {
        var inactivo = BaseDatosPrueba.Usuario("inactivo@correo.cr", activo: false);
        _bd.Sembrar(inactivo);

        Assert.Null(await Usuarios(Contexto()).ActualizarNombrePropioAsync(inactivo.id, "Otro Nombre", "inactivo@correo.cr"));
    }

    // ------------------------------------------------------------------
    // Tutor
    // ------------------------------------------------------------------

    private (UsuarioModel tutor, CursoModel curso, UsuarioModel estudiante) SembrarCurso()
    {
        var tutor = BaseDatosPrueba.Usuario("tutor@correo.cr", tipo: 2);
        var estudiante = BaseDatosPrueba.Usuario("estudiante@correo.cr");
        _bd.Sembrar(tutor, estudiante);
        var curso = BaseDatosPrueba.Curso("Estadística", tutor.id);
        _bd.Sembrar(curso);
        _bd.Sembrar(new EstudianteCursoModel
        {
            CursoId = curso.id, EstudianteId = estudiante.id, Estado = "Activo", Fecha_Matricula = DateTime.UtcNow
        });
        return (tutor, curso, estudiante);
    }

    [Fact]
    public async Task Tutor_ve_sus_cursos_con_cifras_y_permisos_por_curso()
    {
        var (tutor, curso, _) = SembrarCurso();
        var servicio = Tutor(Contexto());

        await servicio.CrearTareaAsync(curso.id, new TutorTareaRequest
        {
            titulo = "Tarea 1", fechaInicio = DateTime.UtcNow, fechaLimite = DateTime.UtcNow.AddDays(7)
        }, tutor.id, "tutor@correo.cr");

        var cursos = await Tutor(Contexto()).CursosAsignadosAsync(tutor.id);
        var resumen = Assert.Single(cursos);
        Assert.Equal(1, resumen.estudiantes);
        Assert.Equal(1, resumen.tareas);
        Assert.Empty(await Tutor(Contexto()).CursosAsignadosAsync(tutorId: 999));

        Assert.Equal((true, tutor.id), await servicio.TutorDelCursoAsync(curso.id));
        Assert.Equal((false, 0), await servicio.TutorDelCursoAsync(999));
        Assert.True(await servicio.PuedeVerComoEstudianteAsync(curso.id, (await Contexto().EstudianteCurso.SingleAsync()).EstudianteId));
    }

    [Fact]
    public async Task Calificar_una_entrega_cuenta_y_no_se_califica_lo_no_entregado()
    {
        var (tutor, curso, estudiante) = SembrarCurso();
        var tarea = new TareaModel { CursoId = curso.id, Titulo = "Tarea", Estado = true, AutorId = tutor.id };
        _bd.Sembrar(tarea);
        var entregada = new EntregaTareaModel
        {
            fk_Tarea = tarea.ID, fk_Estudiante = estudiante.id, fecha_Entrega = DateTime.UtcNow, estado = EntregaTareaModel.EstadoEntregada
        };
        // Sin estudiante asociado: no hay a quién avisar por correo.
        var sinEntregar = new EntregaTareaModel { fk_Tarea = tarea.ID, fk_Estudiante = 999 };
        _bd.Sembrar(entregada, sinEntregar);

        var servicio = Tutor(Contexto());
        Assert.False((await servicio.CalificarAsync(sinEntregar.id, new TutorCalificarRequest { calificacion = 80 }, "tutor@correo.cr")).exitoso);

        // Sin el usuario del estudiante no hay a quién avisar: la prueba no intenta enviar correo.
        using (var c = _bd.NuevoContexto())
        {
            await c.Usuario.Where(u => u.id == estudiante.id).ExecuteDeleteAsync();
        }

        var (exitoso, _) = await servicio.CalificarAsync(entregada.id, new TutorCalificarRequest { calificacion = 87.456m, feedback = " Bien " }, "tutor@correo.cr");

        Assert.True(exitoso);
        var guardada = await Contexto().EntregaTarea.SingleAsync(e => e.id == entregada.id);
        Assert.Equal(87.46m, guardada.calificacion);
        Assert.Equal("Bien", guardada.feedback);
        Assert.Equal(EntregaTareaModel.EstadoCalificada, guardada.estado);
        Assert.Equal(1, await Contexto().Auditoria.CountAsync(a => a.entidad == "ENTREGA_TAREA" && a.accion == "GRADE"));
    }
}
