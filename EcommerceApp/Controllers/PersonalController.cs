using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "GerenteGeneral,Admin")]
    public class PersonalController(UserManager<ApplicationUser> userManager) : Controller
    {
        private static readonly string[] RolesDisponibles = { "GerenteGeneral", "GerenteRegional", "GerenteSucursal", "Vendedor", "Comercial", "Cliente", "Admin" };

        public async Task<IActionResult> Index(string? search, string? rol)
        {
            var usersQuery = userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                usersQuery = usersQuery.Where(u => u.Email!.ToLower().Contains(s) || (u.NombreCompleto != null && u.NombreCompleto.ToLower().Contains(s)) || (u.FullName != null && u.FullName.ToLower().Contains(s)));
            }

            var users = await usersQuery.OrderByDescending(u => u.CreatedAt).ToListAsync();
            
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var u in users)
            {
                userRoles[u.Id] = await userManager.GetRolesAsync(u);
            }

            if (!string.IsNullOrEmpty(rol) && rol != "Todos")
            {
                users = users.Where(u => userRoles[u.Id].Contains(rol)).ToList();
            }

            ViewBag.RolesDisponibles = RolesDisponibles;
            ViewBag.UserRoles = userRoles;
            ViewBag.Search = search;
            ViewBag.RolActual = rol;

            return View(users);
        }

        public async Task<IActionResult> EditarRol(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await userManager.GetRolesAsync(user);

            ViewBag.RolesDisponibles = RolesDisponibles;
            ViewBag.CurrentRoles = currentRoles;

            return View(user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarRoles(string id, List<string> selectedRoles)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            selectedRoles ??= new List<string>();
            var currentRoles = await userManager.GetRolesAsync(user);
            
            // Check if removing the last GerenteGeneral
            if (currentRoles.Contains("GerenteGeneral") && !selectedRoles.Contains("GerenteGeneral"))
            {
                var gerentes = await userManager.GetUsersInRoleAsync("GerenteGeneral");
                if (gerentes.Count <= 1)
                {
                    TempData["Error"] = "No puedes quitar el rol de GerenteGeneral al único usuario que lo tiene.";
                    return RedirectToAction(nameof(EditarRol), new { id = user.Id });
                }
            }

            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();

            if (rolesToRemove.Any())
                await userManager.RemoveFromRolesAsync(user, rolesToRemove);

            if (rolesToAdd.Any())
                await userManager.AddToRolesAsync(user, rolesToAdd);

            TempData["Exito"] = $"Roles actualizados para {user.Email}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> BuscarUsuario(string q)
        {
            if (string.IsNullOrEmpty(q)) return Json(new List<object>());

            var s = q.ToLower();
            var users = await userManager.Users
                .Where(u => u.Email!.ToLower().Contains(s) || (u.NombreCompleto != null && u.NombreCompleto.ToLower().Contains(s)))
                .Take(10)
                .Select(u => new { u.Id, u.Email, u.NombreCompleto })
                .ToListAsync();

            return Json(users);
        }
    }
}
