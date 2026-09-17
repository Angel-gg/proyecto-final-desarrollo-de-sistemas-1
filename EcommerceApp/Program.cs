using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

// Habilitar sesiones (para el carrito de compras del E-Commerce)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// ─── Sembrar todos los roles del sistema ERP ───────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var db = scope.ServiceProvider.GetRequiredService<EcommerceApp.Data.ApplicationDbContext>();

    // Roles del nuevo sistema ERP + roles legacy para compatibilidad
    string[] roles =
    {
        "GerenteGeneral",
        "GerenteRegional",
        "GerenteSucursal",
        "Vendedor",
        "Comercial",
        "Cliente",
        "Admin",
        "User"
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // ─── Sembrar usuarios de demostración ────────────────────────────────
    // Contraseña para todos los usuarios de demo: TechParts2026!
    const string demoPassword = "TechParts2026!";

    var demoUsers = new (string Email, string NombreCompleto, string Rol, int? SucursalId, int? RegionId)[]
    {
        // Gerente General — acceso total al sistema ERP
        ("gerente.general@techparts.com", "Carlos Mendoza Rivera", "GerenteGeneral", null, null),
        // Gerente Regional — supervisa múltiples sucursales de una región
        ("gerente.regional@techparts.com", "María Fernández López", "GerenteRegional", null, 1),
        // Gerente de Sucursal — gestiona solo su tienda física
        ("gerente.sucursal@techparts.com", "Roberto Jiménez Cruz", "GerenteSucursal", 1, null),
        // Vendedor — POS y atención al cliente en tienda
        ("vendedor@techparts.com", "Laura Sánchez Morales", "Vendedor", 1, null),
        // Comercial — reabastece stock, gestiona compras a proveedores
        ("comercial@techparts.com", "Diego Reyes Castillo", "Comercial", null, null),
        // Cliente — acceso a la tienda web
        ("cliente@techparts.com", "Ana García Pérez", "Cliente", null, null),
    };

    foreach (var (email, nombre, rol, sucId, regId) in demoUsers)
    {
        if (await userManager.FindByEmailAsync(email) == null)
        {
            var demoUser = new ApplicationUser
            {
                UserName        = email,
                Email           = email,
                EmailConfirmed  = true,
                NombreCompleto  = nombre,
                FullName        = nombre,
                SucursalId      = sucId,
                RegionId        = regId,
                CodigoCliente   = rol == "Cliente" ? "CLI-DEMO01" : null
            };

            var createResult = await userManager.CreateAsync(demoUser, demoPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(demoUser, rol);
                Console.WriteLine($"[SEED] Usuario '{email}' creado con rol '{rol}'.");
            }
            else
            {
                Console.WriteLine($"[SEED] Error al crear '{email}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }
    }

    // ─── Sembrar productos de muestra si la tabla está vacía ─────────────
    if (!db.Products.Any())
    {
        var sampleProducts = new List<EcommerceApp.Models.Product>
        {
            new() {
                Name        = "NVIDIA RTX 5080 Ti",
                Description = "GPU de última generación con 24 GB GDDR7, ray tracing de 4ta generación y DLSS 4.0 para gaming 4K extremo.",
                Price       = 42999.00m,
                Stock       = 15,
                Category    = "Tarjetas de Video",
                ImageUrl    = "https://images.unsplash.com/photo-1587202372634-32705e3bf49c?w=600&q=80&auto=format&fit=crop"
            },
            new() {
                Name        = "Intel Core Ultra 9 285K",
                Description = "Procesador de 24 núcleos (8P+16E) a 5.7 GHz boost, socket LGA1851, compatible con DDR5-6400.",
                Price       = 12499.00m,
                Stock       = 28,
                Category    = "Procesadores",
                ImageUrl    = "https://images.unsplash.com/photo-1591799264318-7e6ef8ddb7ea?w=600&q=80&auto=format&fit=crop"
            },
            new() {
                Name        = "Samsung 990 Pro 2TB NVMe",
                Description = "SSD PCIe 5.0 con velocidades de lectura hasta 14,800 MB/s. Ideal para workstations de diseño y edición.",
                Price       = 4299.00m,
                Stock       = 52,
                Category    = "Almacenamiento",
                ImageUrl    = "https://images.unsplash.com/photo-1544986581-efac024faf62?w=600&q=80&auto=format&fit=crop"
            },
            new() {
                Name        = "ASUS ROG Maximus Z890 Hero",
                Description = "Tarjeta madre ATX para LGA1851, soporte DDR5 hasta 8000 MHz OC, 5x M.2 PCIe 5.0, Wi-Fi 7.",
                Price       = 18799.00m,
                Stock       = 10,
                Category    = "Tarjetas Madre",
                ImageUrl    = "https://images.unsplash.com/photo-1518770660439-4636190af475?w=600&q=80&auto=format&fit=crop"
            },
            new() {
                Name        = "G.SKILL Trident Z5 RGB 64GB DDR5",
                Description = "Kit de memoria RAM DDR5-6400 CL32, 2x32 GB, con iluminación RGB y XMP 3.0 listo para overclocking.",
                Price       = 5899.00m,
                Stock       = 34,
                Category    = "Memoria RAM",
                ImageUrl    = "https://images.unsplash.com/photo-1562408590-e32931084e23?w=600&q=80&auto=format&fit=crop"
            },
            new() {
                Name        = "Corsair HX1500i Platinum",
                Description = "Fuente de poder modular 1500W 80+ Platinum con iCUE, protección completa y cableado sleeved premium.",
                Price       = 7199.00m,
                Stock       = 20,
                Category    = "Fuentes de Poder",
                ImageUrl    = "https://images.unsplash.com/photo-1617791160536-598cf32026fb?w=600&q=80&auto=format&fit=crop"
            },
            new() {
                Name        = "LG UltraGear 27GS95QE OLED 240Hz",
                Description = "Monitor OLED QHD 27\" a 240Hz con 0.03ms GTG, DisplayHDR True Black 400, HDMI 2.1 y USB-C.",
                Price       = 19499.00m,
                Stock       = 8,
                Category    = "Monitores",
                ImageUrl    = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=600&q=80&auto=format&fit=crop"
            },
            new() {
                Name        = "Logitech G Pro X Superlight 2",
                Description = "Mouse gaming inalámbrico 32,000 DPI, 60 horas de batería, peso de solo 60g con sensor HERO 2.",
                Price       = 2299.00m,
                Stock       = 45,
                Category    = "Periféricos",
                ImageUrl    = "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=600&q=80&auto=format&fit=crop"
            },
            new() {
                Name        = "Cooler Master MasterLiquid 360 ATMOS",
                Description = "Refrigeración líquida AIO 360mm, 3x ventiladores ARGB 120mm, soporte LGA1851 y AM5.",
                Price       = 5499.00m,
                Stock       = 22,
                Category    = "Refrigeración",
                ImageUrl    = "https://images.unsplash.com/photo-1563396983906-b3795482a59a?w=600&q=80&auto=format&fit=crop"
            },
            new() {
                Name        = "Razer BlackWidow V4 Pro",
                Description = "Teclado mecánico inalámbrico TKL, switches Razer Yellow, iluminación Chroma RGB y dial multimedia.",
                Price       = 3799.00m,
                Stock       = 30,
                Category    = "Periféricos",
                ImageUrl    = "https://images.unsplash.com/photo-1541140532154-b024d705b90a?w=600&q=80&auto=format&fit=crop"
            }
        };

        db.Products.AddRange(sampleProducts);
        await db.SaveChangesAsync();
        Console.WriteLine($"[SEED] Se insertaron {sampleProducts.Count} productos de muestra con imágenes.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[AVISO BD] Error en seeding: {ex.Message}");
}

app.Run();

