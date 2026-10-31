using HDC.BLL.Comun;
using HDC.BLL.Servicios;
using HDC.DAL;
using HDC.DAL.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HDC.BLL;

public static class DependencyInjection
{
    public static IServiceCollection AgregarCapas(this IServiceCollection services, string connectionString, string urlGeoref)
    {
        // DAL
        // PostgreSQL vía Npgsql. EnableRetryOnFailure reintenta ante cortes momentáneos de conexión.
        services.AddDbContext<HdcDbContext>(o => o.UseNpgsql(connectionString, pg => pg.EnableRetryOnFailure(5)));
        services.AddScoped<UsuarioRepositorio>();
        services.AddScoped<RolRepositorio>();
        services.AddScoped<PermisoRepositorio>();
        services.AddScoped<AlcanceRepositorio>();
        services.AddScoped<UnidadMedidaRepositorio>();
        services.AddScoped<TipoEmisionRepositorio>();
        services.AddScoped<FuenteEmisionRepositorio>();
        services.AddScoped<GeoRepositorio>();
        services.AddScoped<DomicilioRepositorio>();
        services.AddScoped<RegistroRepositorio>();
        services.AddScoped<ColaboracionRepositorio>();
        services.AddScoped<ReporteRepositorio>();
        services.AddScoped<RecuperacionRepositorio>();
        services.AddScoped<ConvocatoriaRepositorio>();

        // Cache de catálogos y geografía: Singleton para que la compartan todos los requests
        services.AddMemoryCache();
        services.AddSingleton<CacheCatalogos>();

        // BLL
        services.AddScoped<TokenServicio>();
        services.AddScoped<AuthServicio>();
        services.AddScoped<PerfilServicio>();
        services.AddScoped<UsuarioServicio>();
        services.AddScoped<RolServicio>();
        services.AddScoped<PermisoServicio>();
        services.AddScoped<CatalogoServicio>();
        services.AddScoped<FuenteEmisionServicio>();
        services.AddScoped<DomicilioServicio>();
        services.AddScoped<RegistroServicio>();
        services.AddScoped<ColaboracionServicio>();
        services.AddScoped<EstadisticaServicio>();
        services.AddScoped<SemillaServicio>();
        services.AddScoped<DatosPruebaServicio>();
        services.AddScoped<RecuperacionServicio>();
        services.AddScoped<ConvocatoriaServicio>();

        // Emails: la cola es Singleton (compartida) y el envío corre en segundo plano (ver EmailServicio.cs)
        services.AddSingleton<ColaEmails>();
        services.AddHostedService<EnvioEmailsServicio>();

        services.AddHttpClient(SemillaServicio.ClienteGeoref, c =>
        {
            c.BaseAddress = new Uri(urlGeoref.TrimEnd('/') + "/");
            c.Timeout = TimeSpan.FromMinutes(2);
        });

        return services;
    }
}
