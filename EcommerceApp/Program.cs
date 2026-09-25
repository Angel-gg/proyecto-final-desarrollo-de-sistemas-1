using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

// Compatibilidad de DateTime con PostgreSQL en Npgsql 6+
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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

// ─── Encabezados de seguridad y permisos ─────────────────────────────────────
// Permite el uso del micrófono (Web Speech API) en producción
app.Use(async (context, next) =>
{
    context.Response.Headers["Permissions-Policy"] = "microphone=*, camera=()";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ─── Sembrar todos los roles del sistema ERP ───────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EcommerceApp.Data.ApplicationDbContext>();

    // 1. Auto-aplicar migraciones de Entity Framework en la base de datos (PostgreSQL Railway)
    Console.WriteLine("[BD] Verificando y aplicando migraciones pendientes...");
    await db.Database.MigrateAsync();
    Console.WriteLine("[BD] Migraciones aplicadas con éxito.");

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

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


    // ─── 1. Sembrar Sucursales (Upsert) ──────────────────────────────────
    var sucursales = new List<EcommerceApp.Models.Sucursal>
    {
        new() { Nombre="Sucursal Centro", Direccion="Plaza Principal 14 de Septiembre", Ciudad="Cochabamba", Telefono="44556677", Email="centro@techparts.com.bo", Activa=true },
        new() { Nombre="Sucursal Norte", Direccion="Av. América y Libertador", Ciudad="Cochabamba", Telefono="44556678", Email="norte@techparts.com.bo", Activa=true },
        new() { Nombre="Sucursal Sur", Direccion="Av. Panamericana esq. 6 de Agosto", Ciudad="Cochabamba", Telefono="44556679", Email="sur@techparts.com.bo", Activa=true },
        new() { Nombre="Sucursal Quillacollo", Direccion="Plaza Bolívar", Ciudad="Cochabamba", Telefono="44556680", Email="quillacollo@techparts.com.bo", Activa=true },
        new() { Nombre="Sucursal Sacaba", Direccion="Plaza Principal de Sacaba", Ciudad="Cochabamba", Telefono="44556681", Email="sacaba@techparts.com.bo", Activa=true }
    };
    foreach (var suc in sucursales)
    {
        var dbSuc = db.Sucursales.FirstOrDefault(s => s.Nombre == suc.Nombre);
        if (dbSuc == null) db.Sucursales.Add(suc);
        else { dbSuc.Direccion = suc.Direccion; dbSuc.Telefono = suc.Telefono; }
    }
    await db.SaveChangesAsync();
    Console.WriteLine("[SEED] Sucursales listas.");

    // ─── 2. Sembrar Proveedores (Upsert) ─────────────────────────────────
    var proveedores = new List<EcommerceApp.Models.Proveedor>
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
    foreach (var prov in proveedores)
    {
        var dbProv = db.Proveedores.FirstOrDefault(p => p.Nombre == prov.Nombre);
        if (dbProv == null) db.Proveedores.Add(prov);
        else { dbProv.Contacto = prov.Contacto; dbProv.Telefono = prov.Telefono; }
    }
    await db.SaveChangesAsync();
    Console.WriteLine("[SEED] Proveedores listos.");

    // ─── 3. Sembrar Componentes (Upsert) ─────────────────────────────────
    var componentes = new List<EcommerceApp.Models.Componente>
    {
        new() { Nombre="NVIDIA RTX 5080 Ti", Tipo="Tarjetas de Video", Marca="NVIDIA", Descripcion="GPU de última generación con 24 GB GDDR7, ray tracing y DLSS 4.0 para gaming 4K extremo.", Especificaciones="24 GB GDDR7 · PCIe 5.0 · 450W TDP · DLSS 4.0 · Ray Tracing Gen 4", Precio=19500.00m, Stock=15, ImageUrl="https://pngimg.com/uploads/video_card/video_card_PNG155.png" },
        new() { Nombre="Intel Core Ultra 9 285K", Tipo="Procesadores", Marca="Intel", Descripcion="Procesador de 24 núcleos (8P+16E) a 5.7 GHz boost, socket LGA1851.", Especificaciones="24 núcleos · 5.7 GHz boost · LGA1851 · DDR5-6400 · 125W TDP", Precio=6500.00m, Stock=28, ImageUrl="https://pngimg.com/uploads/processor/processor_PNG40.png" },
        new() { Nombre="Samsung 990 Pro 2TB NVMe", Tipo="Almacenamiento", Marca="Samsung", Descripcion="SSD PCIe 5.0 con velocidades de lectura hasta 14,800 MB/s. Ideal para workstations.", Especificaciones="2 TB · PCIe 5.0 · 14,800 MB/s lectura · M.2 2280", Precio=1600.00m, Stock=52, ImageUrl="https://pngimg.com/uploads/hard_disc/hard_disc_PNG8436.png" },
        new() { Nombre="ASUS ROG Maximus Z890 Hero", Tipo="Tarjetas Madre", Marca="ASUS", Descripcion="Tarjeta madre ATX LGA1851, DDR5 hasta 8000 MHz OC, 5x M.2 PCIe 5.0, Wi-Fi 7.", Especificaciones="LGA1851 · DDR5-8000 OC · 5x M.2 · Wi-Fi 7 · Thunderbolt 4", Precio=4500.00m, Stock=10, ImageUrl="https://pngimg.com/uploads/motherboard/motherboard_PNG39.png" },
        new() { Nombre="G.SKILL Trident Z5 RGB 64GB DDR5", Tipo="Memoria RAM", Marca="G.Skill", Descripcion="Kit de memoria DDR5-6400 CL32, 2x32 GB, con iluminación RGB y XMP 3.0.", Especificaciones="64 GB (2x32) · DDR5-6400 · CL32 · XMP 3.0 · ARGB", Precio=2400.00m, Stock=34, ImageUrl="https://pngimg.com/uploads/ram/ram_PNG46.png" },
        new() { Nombre="Corsair HX1500i Platinum", Tipo="Fuentes de Poder", Marca="Corsair", Descripcion="Fuente modular 1500W 80+ Platinum con iCUE, protección completa y cableado sleeved.", Especificaciones="1500W · 80+ Platinum · Full Modular · ATX 3.0 · PCIe 5.0", Precio=2500.00m, Stock=20, ImageUrl="https://pngimg.com/uploads/computer_case/computer_case_PNG27.png" },
        new() { Nombre="LG UltraGear 27GS95QE OLED 240Hz", Tipo="Monitores", Marca="LG", Descripcion="Monitor OLED QHD 27\" a 240Hz con 0.03ms GTG, HDR True Black 400, HDMI 2.1.", Especificaciones="27\" OLED QHD · 240Hz · 0.03ms · HDR True Black 400 · HDMI 2.1", Precio=8500.00m, Stock=8, ImageUrl="https://pngimg.com/uploads/monitor/monitor_PNG88.png" },
        new() { Nombre="Logitech G Pro X Superlight 2", Tipo="Periféricos", Marca="Logitech", Descripcion="Mouse gaming inalámbrico 32,000 DPI, 60h de batería, 60g con sensor HERO 2.", Especificaciones="32,000 DPI · 60h batería · 60g · USB-C · LIGHTSPEED", Precio=1200.00m, Stock=45, ImageUrl="https://pngimg.com/uploads/mouse_pc/mouse_pc_PNG10207.png" },
        new() { Nombre="Cooler Master MasterLiquid 360 ATMOS", Tipo="Refrigeración", Marca="Cooler Master", Descripcion="Refrigeración líquida AIO 360mm, 3x ventiladores ARGB 120mm, soporte LGA1851 y AM5.", Especificaciones="360mm AIO · 3x120mm ARGB · LGA1851 / AM5 · 2800 RPM", Precio=1800.00m, Stock=22, ImageUrl="https://pngimg.com/uploads/computer_fan/computer_fan_PNG82.png" },
        new() { Nombre="Razer BlackWidow V4 Pro", Tipo="Periféricos", Marca="Razer", Descripcion="Teclado mecánico inalámbrico, switches Razer Yellow, Chroma RGB y dial multimedia.", Especificaciones="Razer Yellow · Wireless · Chroma RGB · Dial multimedia · Macro", Precio=1500.00m, Stock=30, ImageUrl="https://pngimg.com/uploads/keyboard/keyboard_PNG5856.png" },
        
        // Nuevos Productos AMD y Accesorios
        new() { Nombre="AMD Ryzen 9 9950X", Tipo="Procesadores", Marca="AMD", Descripcion="16 núcleos y 32 hilos con arquitectura Zen 5, socket AM5, PCIe 5.0.", Especificaciones="16 núcleos · 5.7 GHz boost · AM5 · 170W TDP", Precio=6200.00m, Stock=15, ImageUrl="https://pngimg.com/uploads/processor/processor_PNG40.png" },
        new() { Nombre="ASUS ROG Crosshair X670E Hero", Tipo="Tarjetas Madre", Marca="ASUS", Descripcion="Placa madre entusiasta AM5 para Ryzen serie 7000/9000, 2x PCIe 5.0 x16, Wi-Fi 6E.", Especificaciones="AM5 · X670E · DDR5-6400+ · Wi-Fi 6E · Dual USB4", Precio=4200.00m, Stock=12, ImageUrl="https://pngimg.com/uploads/motherboard/motherboard_PNG39.png" },
        new() { Nombre="AMD Radeon RX 7900 XTX", Tipo="Tarjetas de Video", Marca="AMD", Descripcion="GPU tope de gama RDNA 3, 24GB GDDR6, DisplayPort 2.1, ideal para 4K.", Especificaciones="24 GB GDDR6 · RDNA 3 · DP 2.1 · 355W TDP", Precio=9800.00m, Stock=8, ImageUrl="https://pngimg.com/uploads/video_card/video_card_PNG155.png" },
        new() { Nombre="Corsair MP700 PRO 2TB Gen5", Tipo="Almacenamiento", Marca="Corsair", Descripcion="SSD NVMe M.2 PCIe Gen5 x4 con enfriador activo, velocidades hasta 12,400 MB/s.", Especificaciones="2 TB · PCIe 5.0 · 12,400 MB/s · Activo", Precio=2100.00m, Stock=25, ImageUrl="https://pngimg.com/uploads/hard_disc/hard_disc_PNG8436.png" },
        new() { Nombre="Lian Li O11 Dynamic EVO", Tipo="Cases", Marca="Lian Li", Descripcion="Chasis Mid-Tower ATX de doble cámara, paneles de cristal templado reversibles.", Especificaciones="ATX Mid-Tower · Cristal Templado · Dual Chamber", Precio=1450.00m, Stock=18, ImageUrl="https://pngimg.com/uploads/computer_case/computer_case_PNG27.png" }
    };
    
    foreach (var comp in componentes)
    {
        var dbComp = db.Componentes.FirstOrDefault(c => c.Nombre == comp.Nombre);
        if (dbComp == null) {
            db.Componentes.Add(comp);
        } else {
            dbComp.Precio = comp.Precio;
            dbComp.ImageUrl = comp.ImageUrl;
            dbComp.Descripcion = comp.Descripcion;
            dbComp.Especificaciones = comp.Especificaciones;
        }
    }
    await db.SaveChangesAsync();
    Console.WriteLine("[SEED] Componentes actualizados.");

    // ─── 4. Sembrar Combos (Upsert) ──────────────────────────────────────
    var dbComps = db.Componentes.ToList();
    var gpu  = dbComps.FirstOrDefault(c => c.Nombre.Contains("RTX 5080 Ti"));
    var cpu  = dbComps.FirstOrDefault(c => c.Nombre.Contains("Core Ultra 9"));
    var ram  = dbComps.FirstOrDefault(c => c.Nombre.Contains("Trident"));
    var ssd  = dbComps.FirstOrDefault(c => c.Nombre.Contains("990 Pro"));
    var mb   = dbComps.FirstOrDefault(c => c.Nombre.Contains("Maximus"));
    var psu  = dbComps.FirstOrDefault(c => c.Nombre.Contains("HX1500"));
    var cool = dbComps.FirstOrDefault(c => c.Nombre.Contains("MasterLiquid"));
    var mon  = dbComps.FirstOrDefault(c => c.Nombre.Contains("UltraGear"));
    var mouse= dbComps.FirstOrDefault(c => c.Nombre.Contains("Superlight"));
    var kb   = dbComps.FirstOrDefault(c => c.Nombre.Contains("BlackWidow"));

    var combos = new List<EcommerceApp.Models.Combo>();
    
    if (gpu != null && cpu != null && ram != null && ssd != null && mb != null && psu != null && cool != null)
    {
        combos.Add(new EcommerceApp.Models.Combo {
            Nombre = "🎮 Gaming PC Ultimate", Descripcion = "La máquina definitiva para gaming 4K. RTX 5080 Ti + Core Ultra 9 285K + 64GB DDR5. Sin compromiso.",
            PrecioVenta = 36800.00m, Stock = 5, Activo = true, ImageUrl = "https://pngimg.com/uploads/computer_case/computer_case_PNG27.png",
            ComboComponentes = new List<EcommerceApp.Models.ComboComponente> {
                new() { ComponenteId = gpu.Id, Cantidad = 1 }, new() { ComponenteId = cpu.Id, Cantidad = 1 },
                new() { ComponenteId = ram.Id, Cantidad = 1 }, new() { ComponenteId = ssd.Id, Cantidad = 1 },
                new() { ComponenteId = mb.Id,  Cantidad = 1 }, new() { ComponenteId = psu.Id, Cantidad = 1 },
                new() { ComponenteId = cool.Id,Cantidad = 1 }
            }
        });
    }

    if (mon != null && mouse != null && kb != null)
    {
        combos.Add(new EcommerceApp.Models.Combo {
            Nombre = "🖥️ Setup Gaming Completo", Descripcion = "Combo periféricos premium: monitor OLED 240Hz + mouse Superlight 2 + teclado BlackWidow V4.",
            PrecioVenta = 10500.00m, Stock = 12, Activo = true, ImageUrl = "https://pngimg.com/uploads/monitor/monitor_PNG88.png",
            ComboComponentes = new List<EcommerceApp.Models.ComboComponente> {
                new() { ComponenteId = mon.Id,  Cantidad = 1 },
                new() { ComponenteId = mouse.Id,Cantidad = 1 },
                new() { ComponenteId = kb.Id,   Cantidad = 1 }
            }
        });
    }

    if (cpu != null && mb != null && ram != null && ssd != null)
    {
        combos.Add(new EcommerceApp.Models.Combo {
            Nombre = "⚡ Starter Build DDR5", Descripcion = "Base perfecta para armar tu primera PC DDR5: CPU + Motherboard + RAM + SSD NVMe. Upgradeable.",
            PrecioVenta = 14250.00m, Stock = 8, Activo = true, ImageUrl = "https://pngimg.com/uploads/motherboard/motherboard_PNG39.png",
            ComboComponentes = new List<EcommerceApp.Models.ComboComponente> {
                new() { ComponenteId = cpu.Id, Cantidad = 1 }, new() { ComponenteId = mb.Id,  Cantidad = 1 },
                new() { ComponenteId = ram.Id, Cantidad = 1 }, new() { ComponenteId = ssd.Id, Cantidad = 1 }
            }
        });
    }

    foreach (var combo in combos)
    {
        var dbCombo = db.Combos.FirstOrDefault(c => c.Nombre == combo.Nombre);
        if (dbCombo == null) {
            db.Combos.Add(combo);
        } else {
            dbCombo.PrecioVenta = combo.PrecioVenta;
            dbCombo.ImageUrl = combo.ImageUrl;
            dbCombo.Descripcion = combo.Descripcion;
        }
    }
    await db.SaveChangesAsync();
    Console.WriteLine("[SEED] Combos actualizados.");

    // ─── 5. Sembrar productos legacy (Products) ──────────────────────────
    var sampleProducts = componentes.Select(c => new EcommerceApp.Models.Product {
        Name = c.Nombre, Description = c.Descripcion, Price = c.Precio, Stock = c.Stock, Category = c.Tipo, ImageUrl = c.ImageUrl
    }).ToList();

    foreach (var prod in sampleProducts)
    {
        var dbProd = db.Products.FirstOrDefault(p => p.Name == prod.Name);
        if (dbProd == null) {
            db.Products.Add(prod);
        } else {
            dbProd.Price = prod.Price;
            dbProd.ImageUrl = prod.ImageUrl;
            dbProd.Description = prod.Description;
        }
    }
    await db.SaveChangesAsync();
    Console.WriteLine("[SEED] Legacy Products actualizados.");
}
catch (Exception ex)
{
    Console.WriteLine($"[AVISO BD] Error en seeding: {ex.Message}");
}

// ─── Sembrar Pedidos de Demo ───────────────────────────────────────────────
try
{
    using var scope2 = app.Services.CreateScope();
    var db2 = scope2.ServiceProvider.GetRequiredService<EcommerceApp.Data.ApplicationDbContext>();
    var userMgr2 = scope2.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!db2.Pedidos.Any())
    {
        // Asegurarnos que existen componentes
        var comps = await db2.Componentes.Take(5).ToListAsync();
        var combos2 = await db2.Combos.Take(2).ToListAsync();

        if (comps.Count >= 3)
        {
            var clienteUser  = await userMgr2.FindByEmailAsync("cliente@techparts.com");
            var vendedorUser = await userMgr2.FindByEmailAsync("vendedor@techparts.com");
            var gerenteUser  = await userMgr2.FindByEmailAsync("gerente.general@techparts.com");

            string clienteId  = clienteUser?.Id  ?? "demo-cliente";
            string vendedorId = vendedorUser?.Id ?? "demo-vendedor";
            string gerenteId  = gerenteUser?.Id  ?? "demo-gerente";

            // Helper local para crear numero de pedido
            static string NumPedido(int seq, DateTime fecha) =>
                $"TPC-{fecha:yyyyMMdd}-{seq:D4}";

            var pedidosDemo = new List<EcommerceApp.Models.Pedido>();

            // ── 10 pedidos distribuidos en los últimos 3 meses ──
            var datos = new[]
            {
                (UserId: clienteId,  Fecha: DateTime.Now.AddDays(-89), Metodo: EcommerceApp.Models.MetodoPago.Tarjeta,       Estado: EcommerceApp.Models.EstadoPedido.Completado),
                (UserId: clienteId,  Fecha: DateTime.Now.AddDays(-75), Metodo: EcommerceApp.Models.MetodoPago.Efectivo,       Estado: EcommerceApp.Models.EstadoPedido.Completado),
                (UserId: clienteId,  Fecha: DateTime.Now.AddDays(-60), Metodo: EcommerceApp.Models.MetodoPago.Transferencia,  Estado: EcommerceApp.Models.EstadoPedido.Devuelto),
                (UserId: vendedorId, Fecha: DateTime.Now.AddDays(-45), Metodo: EcommerceApp.Models.MetodoPago.Tarjeta,        Estado: EcommerceApp.Models.EstadoPedido.Completado),
                (UserId: vendedorId, Fecha: DateTime.Now.AddDays(-30), Metodo: EcommerceApp.Models.MetodoPago.Tarjeta,        Estado: EcommerceApp.Models.EstadoPedido.Completado),
                (UserId: gerenteId,  Fecha: DateTime.Now.AddDays(-20), Metodo: EcommerceApp.Models.MetodoPago.Efectivo,       Estado: EcommerceApp.Models.EstadoPedido.Completado),
                (UserId: clienteId,  Fecha: DateTime.Now.AddDays(-15), Metodo: EcommerceApp.Models.MetodoPago.Transferencia,  Estado: EcommerceApp.Models.EstadoPedido.Completado),
                (UserId: clienteId,  Fecha: DateTime.Now.AddDays(-10), Metodo: EcommerceApp.Models.MetodoPago.Tarjeta,        Estado: EcommerceApp.Models.EstadoPedido.Completado),
                (UserId: vendedorId, Fecha: DateTime.Now.AddDays(-5),  Metodo: EcommerceApp.Models.MetodoPago.Efectivo,       Estado: EcommerceApp.Models.EstadoPedido.Devuelto),
                (UserId: clienteId,  Fecha: DateTime.Now.AddDays(-1),  Metodo: EcommerceApp.Models.MetodoPago.Tarjeta,        Estado: EcommerceApp.Models.EstadoPedido.Completado),
            };

            int seq = 1;
            foreach (var d in datos)
            {
                var c1 = comps[seq % comps.Count];
                var c2 = comps[(seq + 1) % comps.Count];
                int qty1 = (seq % 3) + 1;
                int qty2 = 1;

                decimal sub = c1.Precio * qty1 + c2.Precio * qty2;
                decimal iva = Math.Round(sub * 0.16m, 2);
                decimal tot = sub + iva;
                decimal com = Math.Round(sub * 0.05m, 2);

                var pedido = new EcommerceApp.Models.Pedido
                {
                    NumeroPedido = NumPedido(seq, d.Fecha),
                    UserId       = d.UserId,
                    FechaPedido  = d.Fecha,
                    Subtotal     = sub,
                    Impuesto     = iva,
                    Total        = tot,
                    Comision     = com,
                    MetodoPago   = d.Metodo,
                    Estado       = d.Estado,
                    Items = new List<EcommerceApp.Models.PedidoItem>
                    {
                        new() { ProductoNombre = c1.Nombre, Tipo = "componente", ProductoId = c1.Id,
                                PrecioUnitario = c1.Precio, Cantidad = qty1,
                                Subtotal = c1.Precio * qty1, ImageUrl = c1.ImageUrl },
                        new() { ProductoNombre = c2.Nombre, Tipo = "componente", ProductoId = c2.Id,
                                PrecioUnitario = c2.Precio, Cantidad = qty2,
                                Subtotal = c2.Precio * qty2, ImageUrl = c2.ImageUrl },
                    }
                };
                pedidosDemo.Add(pedido);
                seq++;
            }

            db2.Pedidos.AddRange(pedidosDemo);
            await db2.SaveChangesAsync();
            Console.WriteLine($"[SEED] {pedidosDemo.Count} pedidos de demo insertados.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[AVISO SEED PEDIDOS] {ex.Message}");
}

app.Run();

