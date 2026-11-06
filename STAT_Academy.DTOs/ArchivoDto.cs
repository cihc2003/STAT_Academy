namespace STAT_Academy.DTOs
{
    public record ArchivoDto(
        int Id,
        string? NombreArchivo,
        string? TipoArchivo,
        bool EsPrincipal,
        string Url);
}