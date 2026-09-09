# EcommerceApp - Proyecto Final Desarrollo de Sistemas 1

Aplicación web de comercio electrónico desarrollada con **ASP.NET Core MVC (.NET 10 LTS)**, **Entity Framework Core (PostgreSQL)** y **Supabase**.

## 🚀 Tecnologías Utilizadas

- **Framework**: .NET 10 (C# 14 / ASP.NET Core MVC)
- **Base de Datos**: PostgreSQL en la nube alojada en [Supabase](https://supabase.com)
- **ORM**: Entity Framework Core 10 con Npgsql PostgreSQL Provider
- **Autenticación y Seguridad**: ASP.NET Core Identity con Roles (`Admin`, `User`) y Hashing seguro de contraseñas.
- **Frontend**: Razor Views, Bootstrap 5, Vanilla CSS con diseño responsivo.

## 🛠️ Requisitos Previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Cuenta y proyecto en Supabase con PostgreSQL activo.

## ⚙️ Configuración y Ejecución

1. Clonar el repositorio:
   ```bash
   git clone https://github.com/Angel-gg/proyecto-final-desarrollo-de-sistemas-1.git
   cd proyecto-final-desarrollo-de-sistemas-1/EcommerceApp
   ```

2. Configurar la cadena de conexión en `appsettings.json` con las credenciales de Supabase:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=aws-0-us-west-2.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.tu-usuario;Password=tu-password"
     }
   }
   ```

3. Aplicar migraciones a la base de datos (si es la primera vez):
   ```bash
   dotnet ef database update
   ```

4. Ejecutar el proyecto:
   ```bash
   dotnet run
   ```

5. Abrir en el navegador: `http://localhost:5134`

## 👥 Roles y Permisos

- **User**: Puede registrarse, iniciar sesión, ver catálogo de productos y detalles de productos.
- **Admin**: Acceso completo al CRUD de productos (Crear, Editar, Eliminar productos).
