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

            var hoy = DateTime.UtcNow.Date;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);

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
                var start = new DateTime(m.Year, m.Month, 1, 0, 0, 0, DateTimeKind.Utc);
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

        [HttpGet]
        public async Task<IActionResult> SincronizarBD()
        {
            try
            {
                await db.Database.MigrateAsync();

                // 1. Sucursales
                var sucursales = new List<Sucursal>
                {
                    new() { Nombre="Sucursal Centro", Direccion="Plaza Principal 14 de Septiembre", Ciudad="Cochabamba", Telefono="44556677", Email="centro@techparts.com.bo", Activa=true },
                    new() { Nombre="Sucursal Norte", Direccion="Av. América y Libertador", Ciudad="Cochabamba", Telefono="44556678", Email="norte@techparts.com.bo", Activa=true },
                    new() { Nombre="Sucursal Sur", Direccion="Av. Panamericana esq. 6 de Agosto", Ciudad="Cochabamba", Telefono="44556679", Email="sur@techparts.com.bo", Activa=true },
                    new() { Nombre="Sucursal Quillacollo", Direccion="Plaza Bolívar", Ciudad="Cochabamba", Telefono="44556680", Email="quillacollo@techparts.com.bo", Activa=true },
                    new() { Nombre="Sucursal Sacaba", Direccion="Plaza Principal de Sacaba", Ciudad="Cochabamba", Telefono="44556681", Email="sacaba@techparts.com.bo", Activa=true }
                };
                foreach (var s in sucursales)
                {
                    var exists = await db.Sucursales.FirstOrDefaultAsync(x => x.Nombre == s.Nombre);
                    if (exists == null) db.Sucursales.Add(s);
                    else { exists.Direccion = s.Direccion; exists.Telefono = s.Telefono; }
                }

                // 2. Proveedores
                var proveedores = new List<Proveedor>
                {
                    new() { Nombre="NVIDIA Latin America", Contacto="Carlos Mendoza", Telefono="800123456", Email="ventas@nvidia.la", Activo=true },
                    new() { Nombre="AMD Bolivia", Contacto="Ana Suárez", Telefono="800654321", Email="distribucion@amd.bo", Activo=true },
                    new() { Nombre="Intel Andina", Contacto="Luis Fernandez", Telefono="800111222", Email="sales@intel.com.bo", Activo=true },
                    new() { Nombre="ASUS ROG Latam", Contacto="Pedro Gómez", Telefono="800333444", Email="rog@asus.la", Activo=true },
                    new() { Nombre="Gigabyte AORUS", Contacto="Sofía Ríos", Telefono="800555666", Email="aorus@gigabyte.com", Activo=true },
                    new() { Nombre="MSI Gaming", Contacto="Jorge Vargas", Telefono="800777888", Email="latam@msi.com", Activo=true },
                    new() { Nombre="Corsair Distribución", Contacto="Miguel Rojas", Telefono="800999000", Email="b2b@corsair.com", Activo=true },
                    new() { Nombre="Kingston Technology", Contacto="Laura Pineda", Telefono="800222333", Email="ventas@kingston.la", Activo=true },
                    new() { Nombre="Samsung Electronics", Contacto="Roberto Díaz", Telefono="800444555", Email="ssd@samsung.com", Activo=true },
                    new() { Nombre="Logitech G", Contacto="Carla Ruiz", Telefono="800666777", Email="gaming@logitech.bo", Activo=true }
                };
                foreach (var p in proveedores)
                {
                    var exists = await db.Proveedores.FirstOrDefaultAsync(x => x.Nombre == p.Nombre);
                    if (exists == null) db.Proveedores.Add(p);
                    else { exists.Contacto = p.Contacto; exists.Telefono = p.Telefono; }
                }

                await db.SaveChangesAsync();
                TempData["Exito"] = "¡Base de datos migrada y sincronizada con éxito (Sucursales y Proveedores listos)!";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                return Content($"Error al sincronizar BD: {ex.Message} \n\nDetalles: {ex.InnerException?.Message ?? ex.StackTrace}");
            }
        }
    }
}
