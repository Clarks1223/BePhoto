using System.Security.Claims;

namespace BePhoto.Api.Servicios
{
    public static class ClaimsExtensiones
    {
        // Id del usuario que hace la petición, leído de su token. Guid.Empty si no hay.
        public static Guid UsuarioId(this ClaimsPrincipal usuario)
        {
            string? valor = usuario.FindFirst("sub")?.Value
                            ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(valor, out Guid id) ? id : Guid.Empty;
        }
    }
}
