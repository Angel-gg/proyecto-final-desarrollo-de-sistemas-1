using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Models;

namespace EcommerceApp.Data
{
    // Constructor primario: reemplaza el constructor clásico + base(options)
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        // ── Entidades legacy ──
        public DbSet<Product> Products { get; set; }

        // ── Entidades principales del ERP ──
        public DbSet<Componente> Componentes { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboComponente> ComboComponentes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Product (legacy) ──
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            // ── Componente ──
            modelBuilder.Entity<Componente>()
                .Property(c => c.Precio)
                .HasPrecision(18, 2);

            // ── Combo ──
            modelBuilder.Entity<Combo>()
                .Property(c => c.PrecioVenta)
                .HasPrecision(18, 2);

            // ── ComboComponente — clave compuesta ──
            modelBuilder.Entity<ComboComponente>()
                .HasKey(cc => new { cc.ComboId, cc.ComponenteId });

            modelBuilder.Entity<ComboComponente>()
                .HasOne(cc => cc.Combo)
                .WithMany(c => c.ComboComponentes)
                .HasForeignKey(cc => cc.ComboId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ComboComponente>()
                .HasOne(cc => cc.Componente)
                .WithMany(c => c.ComboComponentes)
                .HasForeignKey(cc => cc.ComponenteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
