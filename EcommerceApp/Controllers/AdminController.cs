using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    /// <summary>
    /// Panel administrativo para todos los roles de personal interno.
    /// Cada acción filtra los datos según el rol del usuario autenticado.
    /// </summary>
    [Authorize(Roles = "GerenteGeneral,GerenteRegional,GerenteSucursal,Vendedor,Comercial,Admin")]
    public class AdminController(UserManager<ApplicationUser> userManager) : Controller
    {
        // Dashboard principal — punto de entrada para todo el personal
        public async Task<IActionResult> Dashboard()
        {
            var user = await userManager.GetUserAsync(User);
            var roles = await userManager.GetRolesAsync(user!);

            ViewBag.NombreUsuario = user?.NombreCompleto ?? user?.FullName ?? user?.Email;
            ViewBag.Rol = roles.FirstOrDefault() ?? "Sin rol";
            ViewBag.SucursalId = user?.SucursalId;
            ViewBag.RegionId = user?.RegionId;

            return View();
        }
    }
}
