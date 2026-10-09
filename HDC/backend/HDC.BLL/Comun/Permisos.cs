namespace HDC.BLL.Comun;

/// <summary>Permisos que el código verifica. Deben existir en la tabla PERMISOS (ver 02_datos_iniciales.sql).</summary>
public static class Permisos
{
    public const string AdminPanel = "ADMIN_PANEL";
    public const string GestionarFuentes = "GESTIONAR_FUENTES";
    public const string GestionarUsuarios = "GESTIONAR_USUARIOS";
    public const string GestionarRoles = "GESTIONAR_ROLES";
    public const string GestionarCatalogos = "GESTIONAR_CATALOGOS";
    public const string ReportesGlobales = "REPORTES_GLOBALES";
    public const string VerDashboard = "VER_DASHBOARD";
    public const string GestionarDomicilios = "GESTIONAR_DOMICILIOS";
    public const string GestionarEmisiones = "GESTIONAR_EMISIONES";
    public const string GestionarColaboraciones = "GESTIONAR_COLABORACIONES";
    public const string Colaborar = "COLABORAR";
    public const string GenerarReportes = "GENERAR_REPORTES";

    public static readonly string[] Todos =
    [
        AdminPanel, GestionarFuentes, GestionarUsuarios, GestionarRoles, GestionarCatalogos, ReportesGlobales,
        VerDashboard, GestionarDomicilios, GestionarEmisiones, GestionarColaboraciones, Colaborar, GenerarReportes
    ];

    /// <summary>Nombre del claim del JWT que contiene cada permiso.</summary>
    public const string Claim = "permiso";
}

/// <summary>Roles creados por el script inicial (no se pueden eliminar).</summary>
public static class RolesSistema
{
    public const long Administrador = 1;
    public const long Individuo = 2;
    public const long Organizacion = 3;
}
