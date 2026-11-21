namespace STAT_Academy.Web.Models
{
    /// <summary>Datos de la página de error. Nunca incluye detalles técnicos de la excepción.</summary>
    public class ErrorViewModel
    {
        public int Codigo { get; set; } = 500;

        public string Titulo { get; set; } = "Algo salió mal";

        public string Mensaje { get; set; } = "Ocurrió un error inesperado. Intente de nuevo en unos minutos.";

        /// <summary>Identificador para que el usuario pueda reportar el error a soporte.</summary>
        public string? RequestId { get; set; }

        /// <summary>Muestra el enlace "Ingresar" (p. ej. en acceso denegado sin sesión).</summary>
        public bool MostrarIngresar { get; set; }

        /// <summary>URL a la que volver después de iniciar sesión.</summary>
        public string? ReturnUrl { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        /// <summary>Textos amigables según el código de estado HTTP.</summary>
        public static ErrorViewModel ParaCodigo(int codigo, string? requestId)
        {
            ErrorViewModel modelo = codigo switch
            {
                400 => new() { Codigo = 400, Titulo = "Solicitud no válida", Mensaje = "No pudimos procesar la solicitud. Revise los datos e intente de nuevo." },
                401 => new() { Codigo = 401, Titulo = "Inicie sesión para continuar", Mensaje = "Necesita iniciar sesión para ver esta página.", MostrarIngresar = true },
                403 => new() { Codigo = 403, Titulo = "Acceso denegado", Mensaje = "No tiene permiso para ver esta página o realizar esta acción. Si cree que es un error, comuníquese con un administrador." },
                404 => new() { Codigo = 404, Titulo = "Página no encontrada", Mensaje = "La página o el recurso que busca no existe, cambió de dirección o ya no está disponible." },
                405 => new() { Codigo = 405, Titulo = "Acción no permitida", Mensaje = "Esta acción no se puede realizar de esta forma. Vuelva a la página anterior e intente de nuevo." },
                408 => new() { Codigo = 408, Titulo = "La solicitud tardó demasiado", Mensaje = "El servidor dejó de esperar la solicitud. Intente de nuevo." },
                413 => new() { Codigo = 413, Titulo = "Archivo demasiado grande", Mensaje = "El archivo o los datos enviados superan el tamaño máximo permitido." },
                429 => new() { Codigo = 429, Titulo = "Demasiadas solicitudes", Mensaje = "Hizo demasiadas solicitudes en poco tiempo. Espere un momento e intente de nuevo." },
                502 or 503 or 504 => new() { Codigo = codigo, Titulo = "Servicio no disponible", Mensaje = "El servicio no está disponible en este momento. Intente de nuevo en unos minutos." },
                >= 500 => new() { Codigo = codigo, Titulo = "Algo salió mal", Mensaje = "Ocurrió un error inesperado en el sistema. Intente de nuevo en unos minutos; si el problema continúa, comuníquese con soporte e indique el código del error." },
                _ => new() { Codigo = codigo, Titulo = "No se pudo completar la solicitud", Mensaje = "Ocurrió un problema al procesar la solicitud. Intente de nuevo." }
            };

            modelo.RequestId = requestId;
            return modelo;
        }
    }
}
