using System.Globalization;
using System.Text;
using BePhoto.Api.Configuracion;
using BePhoto.Api.Data;
using BePhoto.Api.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Los números de la app llegan con punto decimal (-0.18065). Si el servidor usara
// la cultura de Windows en español (coma decimal), esas coordenadas se leerían mal.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IConfiguration config = builder.Configuration;

// ---------- Configuración (las claves viven en user-secrets, no en el repositorio) ----------

var jwt = new JwtOpciones
{
    Key = config["Jwt:Key"] ?? string.Empty,
    Issuer = config["Jwt:Issuer"] ?? "BePhoto",
    Audience = config["Jwt:Audience"] ?? "BePhoto.App",
    DiasValidez = config.GetValue("Jwt:DiasValidez", 30)
};

// También acepta los nombres de variable CLOUD_NAME, CLOUD_API_KEY y CLOUD_API_SECRET.
var cloudinary = new CloudinaryOpciones
{
    CloudName = config["Cloudinary:CloudName"] ?? config["CLOUD_NAME"] ?? string.Empty,
    ApiKey = config["Cloudinary:ApiKey"] ?? config["CLOUD_API_KEY"] ?? string.Empty,
    ApiSecret = config["Cloudinary:ApiSecret"] ?? config["CLOUD_API_SECRET"] ?? string.Empty,
    Carpeta = config["Cloudinary:Carpeta"] ?? "BePhoto"
};

string cadenaConexion = config.GetConnectionString("Default") ?? string.Empty;

var faltantes = new List<string>();
if (jwt.Key.Length < 32) faltantes.Add("Jwt:Key (mínimo 32 caracteres)");
if (cloudinary.CloudName.Length == 0) faltantes.Add("Cloudinary:CloudName");
if (cloudinary.ApiKey.Length == 0) faltantes.Add("Cloudinary:ApiKey");
if (cloudinary.ApiSecret.Length == 0) faltantes.Add("Cloudinary:ApiSecret");
if (cadenaConexion.Length == 0) faltantes.Add("ConnectionStrings:Default");

if (faltantes.Count > 0)
{
    throw new InvalidOperationException(
        "Falta configurar: " + string.Join(", ", faltantes) +
        ". Se guardan con \"dotnet user-secrets set\" (mira BackEnd/README.md).");
}

builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton(cloudinary);

// ---------- Servicios ----------

builder.Services.AddDbContext<AppDbContext>(opciones => opciones.UseNpgsql(cadenaConexion));
builder.Services.AddSingleton<TokenServicio>();
builder.Services.AddSingleton<IAlmacenFotos, CloudinaryAlmacenFotos>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(opciones =>
    {
        // Los errores de validación se responden en español y con un solo mensaje
        // en "detail", igual que los demás errores de la API.
        opciones.InvalidModelStateResponseFactory = contexto =>
        {
            string mensaje = contexto.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Los datos enviados no son válidos.";

            return new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Datos no válidos",
                Status = StatusCodes.Status400BadRequest,
                Detail = mensaje
            });
        };
    });

builder.Services.AddOpenApi();

// ---------- Aplicación ----------

WebApplication app = builder.Build();

// Crea la base de datos y las tablas la primera vez. Si cambias las entidades más
// adelante, borra la base "bephoto" en PostgreSQL para que se vuelva a crear.
using (IServiceScope alcance = app.Services.CreateScope())
{
    AppDbContext db = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi(); // descripción de la API en /openapi/v1.json

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { servicio = "BePhoto API", estado = "ok" }));
app.MapControllers();

app.Run();
