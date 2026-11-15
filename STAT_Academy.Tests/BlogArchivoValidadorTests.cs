using System.Text;
using Microsoft.AspNetCore.Http;
using STAT_Academy.Api.Services;

namespace STAT_Academy.Tests;

public class BlogArchivoValidadorTests
{
    private static IFormFile Archivo(string nombre, byte[] contenido) =>
        new FormFile(new MemoryStream(contenido), 0, contenido.Length, "archivos", nombre);

    // Cabecera mínima de un PNG (firma de 8 bytes) seguida de datos.
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13];

    [Fact]
    public async Task Acepta_una_imagen_con_firma_valida()
    {
        var (archivo, error) = await BlogArchivoValidador.Validar(Archivo("foto.png", Png));

        Assert.Null(error);
        Assert.NotNull(archivo);
        Assert.True(archivo!.EsImagen);
    }

    [Fact]
    public async Task Rechaza_una_extension_no_permitida()
    {
        var (archivo, error) = await BlogArchivoValidador.Validar(Archivo("script.exe", [0x4D, 0x5A, 0, 0]));

        Assert.Null(archivo);
        Assert.Contains("tipo no permitido", error);
    }

    [Fact]
    public async Task Rechaza_un_archivo_que_no_coincide_con_su_extension()
    {
        // Texto con extensión .png: la firma no es la de una imagen.
        var (archivo, error) = await BlogArchivoValidador.Validar(Archivo("falsa.png", Encoding.UTF8.GetBytes("<script>alert(1)</script>")));

        Assert.Null(archivo);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Rechaza_un_archivo_vacio()
    {
        var (archivo, error) = await BlogArchivoValidador.Validar(Archivo("vacio.pdf", []));

        Assert.Null(archivo);
        Assert.Contains("vacío", error);
    }
}
