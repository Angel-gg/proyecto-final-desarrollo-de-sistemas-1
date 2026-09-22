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

        // ── Módulo de Pedidos y Ventas ──
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoItem> PedidoItems { get; set; }
        public DbSet<CarritoAbandonado> CarritosAbandonados { get; set; }

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

            // ── Pedido ──
            modelBuilder.Entity<Pedido>()
                .Property(p => p.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Pedido>()
                .Property(p => p.Impuesto).HasPrecision(18, 2);
            modelBuilder.Entity<Pedido>()
                .Property(p => p.Total).HasPrecision(18, 2);
            modelBuilder.Entity<Pedido>()
                .Property(p => p.Comision).HasPrecision(18, 2);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Usuario)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasMany(p => p.Items)
                .WithOne(i => i.Pedido)
                .HasForeignKey(i => i.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Pedido>()
                .HasIndex(p => p.NumeroPedido)
                .IsUnique();

            // ── PedidoItem ──
            modelBuilder.Entity<PedidoItem>()
                .Property(p => p.PrecioUnitario).HasPrecision(18, 2);
            modelBuilder.Entity<PedidoItem>()
                .Property(p => p.Subtotal).HasPrecision(18, 2);

            // ── CarritoAbandonado ──
            modelBuilder.Entity<CarritoAbandonado>()
                .Property(c => c.TotalEstimado).HasPrecision(18, 2);

            modelBuilder.Entity<CarritoAbandonado>()
                .HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
