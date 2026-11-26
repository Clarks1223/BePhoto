using BePhoto.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BePhoto.Api.Servicios
{
    // EnsureCreated solo crea las tablas cuando la base está vacía: no agrega las que se
    // añaden después. Esto crea las tablas nuevas si faltan, sin tocar las que ya existen
    // (ni sus datos), así nadie tiene que borrar su base "bephoto". Puede correr en cada inicio.
    public static class EsquemaExtra
    {
        public static async Task AsegurarAsync(AppDbContext db)
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS codigos_restablecimiento (
                    "Id" uuid NOT NULL PRIMARY KEY,
                    "UsuarioId" uuid NOT NULL REFERENCES usuarios ("Id") ON DELETE CASCADE,
                    "CodigoHash" character varying(100) NOT NULL,
                    "Expira" timestamp with time zone NOT NULL,
                    "Intentos" integer NOT NULL,
                    "Usado" boolean NOT NULL,
                    "FechaCreacion" timestamp with time zone NOT NULL
                );
                CREATE INDEX IF NOT EXISTS "IX_codigos_restablecimiento_UsuarioId_FechaCreacion"
                    ON codigos_restablecimiento ("UsuarioId", "FechaCreacion");
                """);
        }
    }
}
