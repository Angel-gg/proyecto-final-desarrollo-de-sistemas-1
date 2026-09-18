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

    // ─── Sembrar Componentes si la tabla está vacía ──────────────────────
    if (!db.Componentes.Any())
    {
        var componentes = new List<EcommerceApp.Models.Componente>
        {
            new() { Nombre="NVIDIA RTX 5080 Ti", Tipo="Tarjetas de Video", Marca="NVIDIA",
                Descripcion="GPU de última generación con 24 GB GDDR7, ray tracing y DLSS 4.0 para gaming 4K extremo.",
                Especificaciones="24 GB GDDR7 · PCIe 5.0 · 450W TDP · DLSS 4.0 · Ray Tracing Gen 4",
                Precio=42999.00m, Stock=15,
                ImageUrl="https://images.unsplash.com/photo-1587202372634-32705e3bf49c?w=600&q=80&auto=format&fit=crop" },
            new() { Nombre="Intel Core Ultra 9 285K", Tipo="Procesadores", Marca="Intel",
                Descripcion="Procesador de 24 núcleos (8P+16E) a 5.7 GHz boost, socket LGA1851, compatible con DDR5-6400.",
                Especificaciones="24 núcleos · 5.7 GHz boost · LGA1851 · DDR5-6400 · 125W TDP",
                Precio=12499.00m, Stock=28,
                ImageUrl="https://images.unsplash.com/photo-1591799264318-7e6ef8ddb7ea?w=600&q=80&auto=format&fit=crop" },
            new() { Nombre="Samsung 990 Pro 2TB NVMe", Tipo="Almacenamiento", Marca="Samsung",
                Descripcion="SSD PCIe 5.0 con velocidades de lectura hasta 14,800 MB/s. Ideal para workstations.",
                Especificaciones="2 TB · PCIe 5.0 · 14,800 MB/s lectura · M.2 2280",
                Precio=4299.00m, Stock=52,
                ImageUrl="https://images.unsplash.com/photo-1544986581-efac024faf62?w=600&q=80&auto=format&fit=crop" },
            new() { Nombre="ASUS ROG Maximus Z890 Hero", Tipo="Tarjetas Madre", Marca="ASUS",
                Descripcion="Tarjeta madre ATX LGA1851, DDR5 hasta 8000 MHz OC, 5x M.2 PCIe 5.0, Wi-Fi 7.",
                Especificaciones="LGA1851 · DDR5-8000 OC · 5x M.2 · Wi-Fi 7 · Thunderbolt 4",
                Precio=18799.00m, Stock=10,
                ImageUrl="https://images.unsplash.com/photo-1518770660439-4636190af475?w=600&q=80&auto=format&fit=crop" },
            new() { Nombre="G.SKILL Trident Z5 RGB 64GB DDR5", Tipo="Memoria RAM", Marca="G.Skill",
                Descripcion="Kit de memoria DDR5-6400 CL32, 2x32 GB, con iluminación RGB y XMP 3.0.",
                Especificaciones="64 GB (2x32) · DDR5-6400 · CL32 · XMP 3.0 · ARGB",
                Precio=5899.00m, Stock=34,
                ImageUrl="https://images.unsplash.com/photo-1562408590-e32931084e23?w=600&q=80&auto=format&fit=crop" },
            new() { Nombre="Corsair HX1500i Platinum", Tipo="Fuentes de Poder", Marca="Corsair",
                Descripcion="Fuente modular 1500W 80+ Platinum con iCUE, protección completa y cableado sleeved.",
                Especificaciones="1500W · 80+ Platinum · Full Modular · ATX 3.0 · PCIe 5.0",
                Precio=7199.00m, Stock=20,
                ImageUrl="https://images.unsplash.com/photo-1617791160536-598cf32026fb?w=600&q=80&auto=format&fit=crop" },
            new() { Nombre="LG UltraGear 27GS95QE OLED 240Hz", Tipo="Monitores", Marca="LG",
                Descripcion="Monitor OLED QHD 27\" a 240Hz con 0.03ms GTG, HDR True Black 400, HDMI 2.1.",
                Especificaciones="27\" OLED QHD · 240Hz · 0.03ms · HDR True Black 400 · HDMI 2.1",
                Precio=19499.00m, Stock=8,
                ImageUrl="https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=600&q=80&auto=format&fit=crop" },
            new() { Nombre="Logitech G Pro X Superlight 2", Tipo="Periféricos", Marca="Logitech",
                Descripcion="Mouse gaming inalámbrico 32,000 DPI, 60h de batería, 60g con sensor HERO 2.",
                Especificaciones="32,000 DPI · 60h batería · 60g · USB-C · LIGHTSPEED",
                Precio=2299.00m, Stock=45,
                ImageUrl="https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=600&q=80&auto=format&fit=crop" },
            new() { Nombre="Cooler Master MasterLiquid 360 ATMOS", Tipo="Refrigeración", Marca="Cooler Master",
                Descripcion="Refrigeración líquida AIO 360mm, 3x ventiladores ARGB 120mm, soporte LGA1851 y AM5.",
                Especificaciones="360mm AIO · 3x120mm ARGB · LGA1851 / AM5 · 2800 RPM",
                Precio=5499.00m, Stock=22,
                ImageUrl="https://images.unsplash.com/photo-1563396983906-b3795482a59a?w=600&q=80&auto=format&fit=crop" },
            new() { Nombre="Razer BlackWidow V4 Pro", Tipo="Periféricos", Marca="Razer",
                Descripcion="Teclado mecánico inalámbrico, switches Razer Yellow, Chroma RGB y dial multimedia.",
                Especificaciones="Razer Yellow · Wireless · Chroma RGB · Dial multimedia · Macro",
                Precio=3799.00m, Stock=30,
                ImageUrl="https://images.unsplash.com/photo-1541140532154-b024d705b90a?w=600&q=80&auto=format&fit=crop" },
        };
        db.Componentes.AddRange(componentes);
        await db.SaveChangesAsync();
        Console.WriteLine($"[SEED] {componentes.Count} componentes insertados.");

        // ─── Sembrar Combos ──────────────────────────────────────────────
        var gpu  = componentes.First(c => c.Nombre.Contains("RTX"));
        var cpu  = componentes.First(c => c.Nombre.Contains("Core Ultra"));
        var ram  = componentes.First(c => c.Nombre.Contains("Trident"));
        var ssd  = componentes.First(c => c.Nombre.Contains("990 Pro"));
        var mb   = componentes.First(c => c.Nombre.Contains("Maximus"));
        var psu  = componentes.First(c => c.Nombre.Contains("HX1500"));
        var cool = componentes.First(c => c.Nombre.Contains("MasterLiquid"));
        var mon  = componentes.First(c => c.Nombre.Contains("UltraGear"));
        var mouse= componentes.First(c => c.Nombre.Contains("Superlight"));
        var kb   = componentes.First(c => c.Nombre.Contains("BlackWidow"));

        var combos = new List<EcommerceApp.Models.Combo>
        {
            new() {
                Nombre = "🎮 Gaming PC Ultimate",
                Descripcion = "La máquina definitiva para gaming 4K. RTX 5080 Ti + Core Ultra 9 285K + 64GB DDR5. Sin compromiso.",
                PrecioVenta = 78999.00m, Stock = 5, Activo = true,
                ImageUrl = "https://images.unsplash.com/photo-1593640495253-23196b27a87f?w=600&q=80&auto=format&fit=crop",
                ComboComponentes = new List<EcommerceApp.Models.ComboComponente> {
                    new() { ComponenteId = gpu.Id, Cantidad = 1 },
                    new() { ComponenteId = cpu.Id, Cantidad = 1 },
                    new() { ComponenteId = ram.Id, Cantidad = 1 },
                    new() { ComponenteId = ssd.Id, Cantidad = 1 },
                    new() { ComponenteId = mb.Id,  Cantidad = 1 },
                    new() { ComponenteId = psu.Id, Cantidad = 1 },
                    new() { ComponenteId = cool.Id,Cantidad = 1 },
                }
            },
            new() {
                Nombre = "🖥️ Setup Gaming Completo",
                Descripcion = "Combo periféricos premium: monitor OLED 240Hz + mouse Superlight 2 + teclado BlackWidow V4.",
                PrecioVenta = 23999.00m, Stock = 12, Activo = true,
                ImageUrl = "https://images.unsplash.com/photo-1612287230202-1ff1d85d1bdf?w=600&q=80&auto=format&fit=crop",
                ComboComponentes = new List<EcommerceApp.Models.ComboComponente> {
                    new() { ComponenteId = mon.Id,  Cantidad = 1 },
                    new() { ComponenteId = mouse.Id,Cantidad = 1 },
                    new() { ComponenteId = kb.Id,   Cantidad = 1 },
                }
            },
            new() {
                Nombre = "⚡ Starter Build DDR5",
                Descripcion = "Base perfecta para armar tu primera PC DDR5: CPU + Motherboard + RAM + SSD NVMe. Upgradeable.",
                PrecioVenta = 34999.00m, Stock = 8, Activo = true,
                ImageUrl = "https://images.unsplash.com/photo-1547082299-de196ea013d6?w=600&q=80&auto=format&fit=crop",
                ComboComponentes = new List<EcommerceApp.Models.ComboComponente> {
                    new() { ComponenteId = cpu.Id, Cantidad = 1 },
                    new() { ComponenteId = mb.Id,  Cantidad = 1 },
                    new() { ComponenteId = ram.Id, Cantidad = 1 },
                    new() { ComponenteId = ssd.Id, Cantidad = 1 },
                }
            },
        };

        db.Combos.AddRange(combos);
        await db.SaveChangesAsync();
        Console.WriteLine($"[SEED] {combos.Count} combos insertados.");
    }

    // ─── Sembrar productos legacy de muestra si la tabla está vacía ──────
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

