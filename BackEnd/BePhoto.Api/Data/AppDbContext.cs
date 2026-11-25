using BePhoto.Api.Entidades;
using Microsoft.EntityFrameworkCore;

namespace BePhoto.Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> opciones) : DbContext(opciones)
    {
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Recuerdo> Recuerdos => Set<Recuerdo>();

        protected override void OnModelCreating(ModelBuilder modelo)
        {
            modelo.Entity<Usuario>(e =>
            {
                e.ToTable("usuarios");
                e.HasKey(u => u.Id);
                e.Property(u => u.Nombre).HasMaxLength(50).IsRequired();
                e.Property(u => u.Apellido).HasMaxLength(50).IsRequired();
                e.Property(u => u.Correo).HasMaxLength(100).IsRequired();
                e.Property(u => u.ContrasenaHash).HasMaxLength(100).IsRequired();
                // Un correo no se puede repetir (HU-BP-0001).
                e.HasIndex(u => u.Correo).IsUnique();
            });

            modelo.Entity<Recuerdo>(e =>
            {
                e.ToTable("recuerdos");
                e.HasKey(r => r.Id);
                e.Property(r => r.Titulo).HasMaxLength(60).IsRequired();
                e.Property(r => r.Descripcion).HasMaxLength(300).IsRequired();
                e.Property(r => r.FotoUrl).HasMaxLength(500).IsRequired();
                e.Property(r => r.FotoPublicId).HasMaxLength(300).IsRequired();

                e.HasOne(r => r.Usuario)
                    .WithMany(u => u.Recuerdos)
                    .HasForeignKey(r => r.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(r => new { r.UsuarioId, r.Fecha });
            });
        }
    }
}
