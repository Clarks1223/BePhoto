using BePhoto.Api.Data;
using BePhoto.Api.Entidades;
using Microsoft.EntityFrameworkCore;

namespace BePhoto.Api.Servicios
{
    // Al iniciar la API, lleva a la carpeta del correo de su dueño las fotos que se
    // subieron antes con la carpeta del Id del usuario, y guarda su nueva URL.
    // Solo toca las que faltan, así que puede correr en cada inicio sin repetir trabajo.
    public static class MigracionCarpetas
    {
        public static async Task EjecutarAsync(IServiceProvider servicios, CancellationToken ct = default)
        {
            using IServiceScope alcance = servicios.CreateScope();
            AppDbContext db = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
            IAlmacenFotos fotos = alcance.ServiceProvider.GetRequiredService<IAlmacenFotos>();
            ILogger log = alcance.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(MigracionCarpetas));

            List<Recuerdo> recuerdos = await db.Recuerdos
                .Include(r => r.Usuario)
                .ToListAsync(ct);

            List<Recuerdo> pendientes = recuerdos
                .Where(r => r.Usuario is not null && !fotos.EstaEnCarpeta(r.FotoPublicId, r.Usuario.Correo))
                .ToList();
            if (pendientes.Count == 0)
                return;

            log.LogInformation("Moviendo {Cantidad} fotos a la carpeta del correo de su usuario", pendientes.Count);

            int movidas = 0;
            foreach (Recuerdo recuerdo in pendientes)
            {
                try
                {
                    FotoSubida nueva = await fotos.MoverACarpetaAsync(recuerdo.FotoPublicId, recuerdo.Usuario!.Correo, ct);
                    recuerdo.FotoPublicId = nueva.PublicId;
                    recuerdo.FotoUrl = nueva.Url;
                    // Se guarda una por una: si algo falla a la mitad, lo ya movido queda registrado.
                    await db.SaveChangesAsync(ct);
                    movidas++;
                }
                catch (Exception ex)
                {
                    // Esa foto sigue funcionando con su nombre anterior; se reintenta en el próximo inicio.
                    log.LogWarning(ex, "No se pudo mover la foto {PublicId}", recuerdo.FotoPublicId);
                }
            }

            log.LogInformation("Fotos movidas: {Movidas} de {Total}", movidas, pendientes.Count);
        }
    }
}
