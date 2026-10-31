using System.IO.Compression;
using System.Text;
using HDC.API.Infraestructura;
using HDC.BLL;
using HDC.BLL.Comun;
using HDC.BLL.Servicios;
using HDC.DAL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var connectionString = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection");
var claveJwt = config["Jwt:Clave"] ?? throw new InvalidOperationException("Falta Jwt:Clave");
if (Encoding.UTF8.GetByteCount(claveJwt) < 32)
    throw new InvalidOperationException("Jwt:Clave debe tener al menos 32 caracteres");

builder.Services.AgregarCapas(connectionString, config["Georef:Url"] ?? "https://apis.datos.gob.ar/georef/api/");

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ctx =>
    {
        var errores = ctx.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => string.IsNullOrEmpty(e.Key) ? "general" : char.ToLowerInvariant(e.Key[0]) + e.Key[1..],
                e => e.Value!.Errors.Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Valor inválido" : x.ErrorMessage).ToArray());
        return new BadRequestObjectResult(new { mensaje = "Revisá los datos ingresados", errores });
    });

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Emisor"],
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audiencia"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "sub",
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
    });

builder.Services.AddAuthorization(o =>
{
    foreach (var permiso in Permisos.Todos)
        o.AddPolicy(permiso, p => p.RequireClaim(Permisos.Claim, permiso));

    // Políticas compuestas: alcanza con tener alguno de los permisos
    o.AddPolicy(Politicas.CargarEmisiones, p => p.RequireClaim(Permisos.Claim, Permisos.GestionarEmisiones, Permisos.Colaborar));
    o.AddPolicy(Politicas.Colaboraciones, p => p.RequireClaim(Permisos.Claim, Permisos.GestionarColaboraciones, Permisos.Colaborar));
});

// Límite de pedidos por IP para la recuperación de contraseña (ver Infraestructura/LimitesPedidos.cs)
builder.Services.AgregarLimitesPedidos();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(config.GetSection("Cors:Origenes").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Compresión de respuestas: el JSON (listados, dashboard, reportes) es texto muy repetitivo
// y se reduce entre 70 % y 90 %. Brotli si el navegador lo soporta; si no, gzip.
// Nivel "Fastest": casi toda la ganancia de tamaño con muy poco uso de CPU.
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true; // la respuesta lleva solo datos del propio usuario, sin secretos que puedan filtrarse por BREACH
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Huella de Carbono API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, [] }
    });
});

var app = builder.Build();

await InicializarDatosAsync(app);

// Toma la IP real del usuario del encabezado que agrega nginx (necesario para el límite de pedidos por IP)
app.UseForwardedHeaders();

// Va primero para que comprima todas las respuestas, incluidas las de error
app.UseResponseCompression();
app.UseMiddleware<ManejoErroresMiddleware>();

if (app.Environment.IsDevelopment() || config.GetValue<bool>("Swagger:Habilitado"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/salud", () => Results.Ok(new { estado = "ok", fecha = DateTime.Now }));
app.MapControllers();

app.Run();

static async Task InicializarDatosAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Inicializacion");
    await InicializadorBaseDatos.InicializarAsync(app.Configuration.GetConnectionString("DefaultConnection")!, logger);

    using (var scope = app.Services.CreateScope())
        await scope.ServiceProvider.GetRequiredService<SemillaServicio>().CrearAdministradorAsync();

    // La geografía se descarga en segundo plano para no demorar el arranque de la API
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<SemillaServicio>().SincronizarGeorefAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo cargar la geografía desde georef. Reintentá desde Administración > Catálogos.");
        }

        // Datos de prueba (solo si DatosPrueba:Habilitado = true). Van después de la geografía
        // porque los domicilios de prueba necesitan una ciudad.
        try
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<DatosPruebaServicio>().CrearAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudieron crear los datos de prueba.");
        }
    });
}
