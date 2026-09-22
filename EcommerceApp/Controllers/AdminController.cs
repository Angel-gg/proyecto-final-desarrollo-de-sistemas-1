using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "GerenteGeneral,GerenteRegional,GerenteSucursal,Vendedor,Comercial,Admin")]
    public class AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext db) : Controller
    {
        public async Task<IActionResult> Dashboard()
        {
            var user = await userManager.GetUserAsync(User);
            var roles = await userManager.GetRolesAsync(user!);

            ViewBag.NombreUsuario = user?.NombreCompleto ?? user?.FullName ?? user?.Email;
            ViewBag.Rol = roles.FirstOrDefault() ?? "Sin rol";
            ViewBag.SucursalId = user?.SucursalId;
            ViewBag.RegionId = user?.RegionId;

            var hoy = DateTime.Now.Date;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

            // KPIs básicos
            var totalInventario = await db.Componentes.SumAsync(c => c.Stock);
            var ventasMes = await db.Pedidos.Where(p => p.FechaPedido >= inicioMes && p.Estado == EstadoPedido.Completado).SumAsync(p => p.Total);
            var clientes = await userManager.GetUsersInRoleAsync("Cliente");
            var sucursales = await db.Sucursales.CountAsync(s => s.Activa);

            ViewBag.TotalInventario = totalInventario;
            ViewBag.VentasMes = ventasMes;
            ViewBag.TotalClientes = clientes.Count;
            ViewBag.TotalSucursales = sucursales;

            // Alertas
            var stockCritico = await db.Componentes.Where(c => c.Stock <= 10).OrderBy(c => c.Stock).Take(5).ToListAsync();
            var carritosActivos = await db.CarritosAbandonados.CountAsync(c => !c.Convertido);
            
            ViewBag.StockCritico = stockCritico;
            ViewBag.CarritosActivos = carritosActivos;

            // Últimas 5 ventas
            var ultimasVentas = await db.Pedidos
                .Include(p => p.Usuario)
                .OrderByDescending(p => p.FechaPedido)
                .Take(5)
                .ToListAsync();
            ViewBag.UltimasVentas = ultimasVentas;

            // Datos para Gráfico Líneas (6 meses)
            var ventasMeses = new List<decimal>();
            var labelsMeses = new List<string>();
            for (int i = 5; i >= 0; i--)
            {
                var m = hoy.AddMonths(-i);
                var start = new DateTime(m.Year, m.Month, 1);
                var end = start.AddMonths(1).AddDays(-1);
                var sum = await db.Pedidos.Where(p => p.FechaPedido >= start && p.FechaPedido <= end && p.Estado == EstadoPedido.Completado).SumAsync(p => p.Total);
                ventasMeses.Add(sum);
                labelsMeses.Add(start.ToString("MMM yyyy"));
            }
            ViewBag.LabelsVentas = labelsMeses;
            ViewBag.DatosVentas = ventasMeses;

            // Datos para Gráfico Barras (Top 5 Productos)
            var topProductos = await db.PedidoItems
                .GroupBy(i => i.ProductoNombre)
                .Select(g => new { Nombre = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .OrderByDescending(g => g.Cantidad)
                .Take(5)
                .ToListAsync();
            ViewBag.LabelsTop = topProductos.Select(p => p.Nombre).ToList();
            ViewBag.DatosTop = topProductos.Select(p => p.Cantidad).ToList();

            // Datos para Dona (Métodos de pago)
            var metodos = await db.Pedidos
                .Where(p => p.Estado == EstadoPedido.Completado)
                .GroupBy(p => p.MetodoPago)
                .Select(g => new { Metodo = g.Key, Total = g.Count() })
                .ToListAsync();
            ViewBag.LabelsMetodos = metodos.Select(m => m.Metodo.ToString()).ToList();
            ViewBag.DatosMetodos = metodos.Select(m => m.Total).ToList();

            return View();
        }
    }
}
