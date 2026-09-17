using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    public class AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager) : Controller
    {
        // ─────────────────────────── LOGIN ───────────────────────────

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Obtener el usuario y sus roles para redirigir correctamente
                var user = await userManager.FindByEmailAsync(model.Email);
                var roles = await userManager.GetRolesAsync(user!);

                // Roles de personal interno → Panel Administrativo
                var staffRoles = new[] { "GerenteGeneral", "GerenteRegional", "GerenteSucursal", "Vendedor", "Comercial", "Admin" };

                if (roles.Any(r => staffRoles.Contains(r)))
                    return RedirectToAction("Dashboard", "Admin");

                // Rol Cliente → Tienda web
                if (roles.Contains("Cliente") || roles.Contains("User"))
                    return RedirectToAction("Index", "Tienda");

                // Fallback: si no tiene rol asignado aún, enviar a productos
                return RedirectToAction("Index", "Products");
            }

            if (result.IsLockedOut)
                ModelState.AddModelError(string.Empty, "Cuenta bloqueada temporalmente. Intenta más tarde.");
            else
                ModelState.AddModelError(string.Empty, "Credenciales inválidas. Verifica tu correo y contraseña.");

            return View(model);
        }

        // ─────────────────────────── REGISTER ───────────────────────────

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                NombreCompleto = model.FullName,
                FullName = model.FullName,
                // Generar un código de cliente único automáticamente
                CodigoCliente = "CLI-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()
            };

            var result = await userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Por defecto, los usuarios registrados son Clientes
                await userManager.AddToRoleAsync(user, "Cliente");
                await signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Tienda");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ─────────────────────────── LOGOUT ───────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // ─────────────────────────── ACCESS DENIED ───────────────────────────

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}

