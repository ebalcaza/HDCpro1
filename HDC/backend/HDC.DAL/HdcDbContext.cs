using HDC.Entity.Entities;
using Microsoft.EntityFrameworkCore;

namespace HDC.DAL;

/// <summary>
/// Mapeo de las entidades al esquema definido en Scripts/01_esquema.sql.
/// La base se crea con ese script (no con migraciones) para que el SQL sea la fuente de verdad.
/// </summary>
public class HdcDbContext(DbContextOptions<HdcDbContext> options) : DbContext(options)
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Individuo> Individuos => Set<Individuo>();
    public DbSet<Organizacion> Organizaciones => Set<Organizacion>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Provincia> Provincias => Set<Provincia>();
    public DbSet<Distrito> Distritos => Set<Distrito>();
    public DbSet<Ciudad> Ciudades => Set<Ciudad>();
    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
    public DbSet<Alcance> Alcances => Set<Alcance>();
    public DbSet<TipoEmision> TiposEmision => Set<TipoEmision>();
    public DbSet<FuenteEmision> FuentesEmision => Set<FuenteEmision>();
    public DbSet<Domicilio> Domicilios => Set<Domicilio>();
    public DbSet<DomicilioFuente> DomicilioFuentes => Set<DomicilioFuente>();
    public DbSet<Registro> Registros => Set<Registro>();
    public DbSet<DetalleRegistro> DetallesRegistro => Set<DetalleRegistro>();
    public DbSet<Colaboracion> Colaboraciones => Set<Colaboracion>();
    public DbSet<ColaboracionTipoEmision> ColaboracionTiposEmision => Set<ColaboracionTipoEmision>();
    public DbSet<Reporte> Reportes => Set<Reporte>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Rol>(e =>
        {
            e.ToTable("ROL");
            e.HasKey(x => x.IdRol);
            e.Property(x => x.IdRol).HasColumnName("ID_ROL");
            e.Property(x => x.Nombre).HasColumnName("NOMBRE").HasMaxLength(200);
            e.Property(x => x.Descripcion).HasColumnName("DESCRIPCION").HasMaxLength(500);
            e.Property(x => x.EsSistema).HasColumnName("ES_SISTEMA");
        });

        mb.Entity<Permiso>(e =>
        {
            e.ToTable("PERMISOS");
            e.HasKey(x => x.IdPermiso);
            e.Property(x => x.IdPermiso).HasColumnName("ID_PERMISO");
            e.Property(x => x.Nombre).HasColumnName("NOMBRE").HasMaxLength(250);
            e.Property(x => x.Descripcion).HasColumnName("DESCRIPCION").HasMaxLength(500);
        });

        mb.Entity<RolPermiso>(e =>
        {
            e.ToTable("ROL_PERMISO");
            e.HasKey(x => new { x.IdRol, x.IdPermiso });
            e.Property(x => x.IdRol).HasColumnName("ID_ROL");
            e.Property(x => x.IdPermiso).HasColumnName("ID_PERMISO");
            e.HasOne(x => x.Rol).WithMany(r => r.RolPermisos).HasForeignKey(x => x.IdRol);
            e.HasOne(x => x.Permiso).WithMany(p => p.RolPermisos).HasForeignKey(x => x.IdPermiso);
        });

        mb.Entity<Usuario>(e =>
        {
            e.ToTable("USUARIO");
            e.HasKey(x => x.IdUsuario);
            e.Property(x => x.IdUsuario).HasColumnName("ID_USUARIO");
            e.Property(x => x.IdRol).HasColumnName("ID_ROL");
            e.Property(x => x.Email).HasColumnName("EMAIL").HasMaxLength(100);
            e.Property(x => x.Contrasena).HasColumnName("CONTRASENA").HasMaxLength(250);
            e.Property(x => x.Telefono).HasColumnName("TELEFONO").HasMaxLength(30).IsUnicode(false);
            e.Property(x => x.FechaAlta).HasColumnName("FECHA_ALTA").HasColumnType("smalldatetime");
            e.Property(x => x.FechaBaja).HasColumnName("FECHA_BAJA").HasColumnType("smalldatetime");
            e.HasOne(x => x.Rol).WithMany(r => r.Usuarios).HasForeignKey(x => x.IdRol);
            e.HasOne(x => x.Individuo).WithOne(i => i.Usuario).HasForeignKey<Individuo>(i => i.IdUsuario);
            e.HasOne(x => x.Organizacion).WithOne(o => o.Usuario).HasForeignKey<Organizacion>(o => o.IdUsuario);
        });

        mb.Entity<Individuo>(e =>
        {
            e.ToTable("INDIVIDUO");
            e.HasKey(x => x.IdUsuario);
            e.Property(x => x.IdUsuario).HasColumnName("ID_USUARIO").ValueGeneratedNever();
            e.Property(x => x.Nombres).HasColumnName("NOMBRES").HasMaxLength(200);
            e.Property(x => x.Apellidos).HasColumnName("APELLIDOS").HasMaxLength(250);
            e.Property(x => x.Dni).HasColumnName("DNI").HasMaxLength(10).IsUnicode(false);
            e.Property(x => x.FechaNacimiento).HasColumnName("FECHA_NACIMIENTO");
            e.Property(x => x.Genero).HasColumnName("GENERO").HasMaxLength(30);
        });

        mb.Entity<Organizacion>(e =>
        {
            e.ToTable("ORGANIZACION");
            e.HasKey(x => x.IdUsuario);
            e.Property(x => x.IdUsuario).HasColumnName("ID_USUARIO").ValueGeneratedNever();
            e.Property(x => x.RazonSocial).HasColumnName("RAZON_SOCIAL").HasMaxLength(250);
            e.Property(x => x.Cuit).HasColumnName("CUIT").HasMaxLength(13).IsUnicode(false);
            e.Property(x => x.Area).HasColumnName("AREA").HasMaxLength(100);
            e.Property(x => x.Descripcion).HasColumnName("DESCRIPCION").HasMaxLength(1000);
            e.Property(x => x.CantidadMiembros).HasColumnName("CANTIDAD_MIEMBROS");
        });

        mb.Entity<Vehiculo>(e =>
        {
            e.ToTable("VEHICULO");
            e.HasKey(x => x.IdVehiculo);
            e.Property(x => x.IdVehiculo).HasColumnName("ID_VEHICULO");
            e.Property(x => x.IdUsuario).HasColumnName("ID_USUARIO");
            e.Property(x => x.TipoVehiculo).HasColumnName("TIPO_VEHICULO").HasMaxLength(20);
            e.Property(x => x.Marca).HasColumnName("MARCA").HasMaxLength(100);
            e.Property(x => x.Modelo).HasColumnName("MODELO").HasMaxLength(100);
            e.Property(x => x.Patente).HasColumnName("PATENTE").HasMaxLength(10).IsUnicode(false);
            e.Property(x => x.TipoCombustible).HasColumnName("TIPO_COMBUSTIBLE").HasMaxLength(20);
            e.Property(x => x.TipoUso).HasColumnName("TIPO_USO").HasMaxLength(20);
            e.HasOne(x => x.Usuario).WithMany(u => u.Vehiculos).HasForeignKey(x => x.IdUsuario);
        });

        mb.Entity<Provincia>(e =>
        {
            e.ToTable("PROVINCIA");
            e.HasKey(x => x.IdProvincia);
            e.Property(x => x.IdProvincia).HasColumnName("ID_PROVINCIA").ValueGeneratedNever();
            e.Property(x => x.Nombre).HasColumnName("NOMBRE").HasMaxLength(250);
        });

        mb.Entity<Distrito>(e =>
        {
            e.ToTable("DISTRITO");
            e.HasKey(x => x.IdDistrito);
            e.Property(x => x.IdDistrito).HasColumnName("ID_DISTRITO").ValueGeneratedNever();
            e.Property(x => x.IdProvincia).HasColumnName("ID_PROVINCIA");
            e.Property(x => x.Nombre).HasColumnName("NOMBRE").HasMaxLength(250);
            e.HasOne(x => x.Provincia).WithMany(p => p.Distritos).HasForeignKey(x => x.IdProvincia);
        });

        mb.Entity<Ciudad>(e =>
        {
            e.ToTable("CIUDAD");
            e.HasKey(x => x.IdCiudad);
            e.Property(x => x.IdCiudad).HasColumnName("ID_CIUDAD").ValueGeneratedNever();
            e.Property(x => x.IdDistrito).HasColumnName("ID_DISTRITO");
            e.Property(x => x.Nombre).HasColumnName("NOMBRE").HasMaxLength(250);
            e.HasOne(x => x.Distrito).WithMany(d => d.Ciudades).HasForeignKey(x => x.IdDistrito);
        });

        mb.Entity<UnidadMedida>(e =>
        {
            e.ToTable("UNIDAD_MEDIDA");
            e.HasKey(x => x.IdUnidadMedida);
            e.Property(x => x.IdUnidadMedida).HasColumnName("ID_UNIDAD_MEDIDA");
            e.Property(x => x.Nombre).HasColumnName("NOMBRE").HasMaxLength(50);
        });

        mb.Entity<Alcance>(e =>
        {
            e.ToTable("ALCANCE");
            e.HasKey(x => x.IdAlcance);
            e.Property(x => x.IdAlcance).HasColumnName("ID_ALCANCE");
            e.Property(x => x.Nombre).HasColumnName("NOMBRE").HasMaxLength(200);
            e.Property(x => x.Descripcion).HasColumnName("DESCRIPCION").HasMaxLength(500);
        });

        mb.Entity<TipoEmision>(e =>
        {
            e.ToTable("TIPO_EMISION");
            e.HasKey(x => x.IdTipoEmision);
            e.Property(x => x.IdTipoEmision).HasColumnName("ID_TIPO_EMISION");
            e.Property(x => x.IdAlcance).HasColumnName("ID_ALCANCE");
            e.Property(x => x.Nombre).HasColumnName("NOMBRE").HasMaxLength(100);
            e.HasOne(x => x.Alcance).WithMany(a => a.TiposEmision).HasForeignKey(x => x.IdAlcance);
        });

        mb.Entity<FuenteEmision>(e =>
        {
            e.ToTable("FUENTE_EMISION");
            e.HasKey(x => x.IdFuenteEmision);
            e.Property(x => x.IdFuenteEmision).HasColumnName("ID_FUENTE_EMISION");
            e.Property(x => x.IdUnidadMedida).HasColumnName("ID_UNIDAD_MEDIDA");
            e.Property(x => x.IdTipoEmision).HasColumnName("ID_TIPO_EMISION");
            e.Property(x => x.Nombre).HasColumnName("NOMBRE").HasMaxLength(250);
            e.Property(x => x.ValorFactorEmision).HasColumnName("VALOR_FACTOR_EMISION").HasPrecision(18, 6);
            e.Property(x => x.FechaAlta).HasColumnName("FECHA_ALTA").HasColumnType("smalldatetime");
            e.Property(x => x.FechaBaja).HasColumnName("FECHA_BAJA").HasColumnType("smalldatetime");
            e.Property(x => x.FechaModificacion).HasColumnName("FECHA_MODIFICACION").HasColumnType("smalldatetime");
            e.HasOne(x => x.UnidadMedida).WithMany().HasForeignKey(x => x.IdUnidadMedida);
            e.HasOne(x => x.TipoEmision).WithMany().HasForeignKey(x => x.IdTipoEmision);
        });

        mb.Entity<Domicilio>(e =>
        {
            e.ToTable("DOMICILIO");
            e.HasKey(x => x.IdDomicilio);
            e.Property(x => x.IdDomicilio).HasColumnName("ID_DOMICILIO");
            e.Property(x => x.IdCiudad).HasColumnName("ID_CIUDAD");
            e.Property(x => x.IdUsuario).HasColumnName("ID_USUARIO");
            e.Property(x => x.Calle).HasColumnName("CALLE").HasMaxLength(200);
            e.Property(x => x.Numero).HasColumnName("NUMERO");
            e.Property(x => x.Departamento).HasColumnName("DEPARTAMENTO").HasMaxLength(20);
            e.Property(x => x.Piso).HasColumnName("PISO").HasMaxLength(20);
            e.Property(x => x.Block).HasColumnName("BLOCK").HasMaxLength(20);
            e.Property(x => x.Manzana).HasColumnName("MANZANA").HasMaxLength(20);
            e.Property(x => x.CodigoPostal).HasColumnName("CODIGO_POSTAL").HasMaxLength(15);
            e.Property(x => x.Telefono).HasColumnName("TELEFONO").HasMaxLength(30).IsUnicode(false);
            e.Property(x => x.FechaAlta).HasColumnName("FECHA_ALTA").HasColumnType("smalldatetime");
            e.Property(x => x.FechaBaja).HasColumnName("FECHA_BAJA").HasColumnType("smalldatetime");
            e.Property(x => x.TotalKgCo2).HasColumnName("TOTAL_KG_CO2").HasPrecision(18, 6);
            e.HasOne(x => x.Ciudad).WithMany().HasForeignKey(x => x.IdCiudad);
            e.HasOne(x => x.Usuario).WithMany(u => u.Domicilios).HasForeignKey(x => x.IdUsuario);
        });

        mb.Entity<DomicilioFuente>(e =>
        {
            e.ToTable("DOMICILIO_FUENTE");
            e.HasKey(x => new { x.IdDomicilio, x.IdFuenteEmision });
            e.Property(x => x.IdDomicilio).HasColumnName("ID_DOMICILIO");
            e.Property(x => x.IdFuenteEmision).HasColumnName("ID_FUENTE_EMISION");
            e.HasOne(x => x.Domicilio).WithMany(d => d.Fuentes).HasForeignKey(x => x.IdDomicilio);
            e.HasOne(x => x.FuenteEmision).WithMany().HasForeignKey(x => x.IdFuenteEmision);
        });

        mb.Entity<Registro>(e =>
        {
            e.ToTable("REGISTROS");
            e.HasKey(x => x.IdRegistro);
            e.Property(x => x.IdRegistro).HasColumnName("ID_REGISTRO");
            e.Property(x => x.IdDomicilio).HasColumnName("ID_DOMICILIO");
            e.Property(x => x.IdUsuarioCarga).HasColumnName("ID_USUARIO_CARGA");
            e.Property(x => x.Periodo).HasColumnName("PERIODO");
            e.Property(x => x.Observaciones).HasColumnName("OBSERVACIONES").HasMaxLength(500);
            e.Property(x => x.TotalKgCo2).HasColumnName("TOTAL_KG_CO2").HasPrecision(18, 6);
            e.Property(x => x.FechaCreacion).HasColumnName("FECHA_CREACION").HasColumnType("datetime");
            e.Property(x => x.FechaUltimaModificacion).HasColumnName("FECHA_ULTIMA_MODIFICACION").HasColumnType("datetime");
            e.HasOne(x => x.Domicilio).WithMany().HasForeignKey(x => x.IdDomicilio);
            e.HasOne(x => x.UsuarioCarga).WithMany().HasForeignKey(x => x.IdUsuarioCarga);
        });

        mb.Entity<DetalleRegistro>(e =>
        {
            e.ToTable("DETALLE_REGISTRO");
            e.HasKey(x => x.IdDetalle);
            e.Property(x => x.IdDetalle).HasColumnName("ID_DETALLE");
            e.Property(x => x.IdRegistro).HasColumnName("ID_REGISTRO");
            e.Property(x => x.IdFuenteEmision).HasColumnName("ID_FUENTE_EMISION");
            e.Property(x => x.Consumo).HasColumnName("CONSUMO").HasPrecision(18, 4);
            e.Property(x => x.ValorFactorEmision).HasColumnName("VALOR_FACTOR_EMISION").HasPrecision(18, 6);
            e.Property(x => x.KgCo2).HasColumnName("KG_CO2").HasPrecision(18, 6);
            e.HasOne(x => x.Registro).WithMany(r => r.Detalles).HasForeignKey(x => x.IdRegistro).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.FuenteEmision).WithMany().HasForeignKey(x => x.IdFuenteEmision);
        });

        mb.Entity<Colaboracion>(e =>
        {
            e.ToTable("COLABORACION");
            e.HasKey(x => x.IdColaboracion);
            e.Property(x => x.IdColaboracion).HasColumnName("ID_COLABORACION");
            e.Property(x => x.IdDomicilio).HasColumnName("ID_DOMICILIO");
            e.Property(x => x.IdUsuarioColaborador).HasColumnName("ID_USUARIO_COLABORADOR");
            e.Property(x => x.PeriodoDesde).HasColumnName("PERIODO_DESDE");
            e.Property(x => x.PeriodoHasta).HasColumnName("PERIODO_HASTA");
            e.Property(x => x.Estado).HasColumnName("ESTADO").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.Mensaje).HasColumnName("MENSAJE").HasMaxLength(500);
            e.Property(x => x.FechaInvitacion).HasColumnName("FECHA_INVITACION").HasColumnType("datetime");
            e.Property(x => x.FechaRespuesta).HasColumnName("FECHA_RESPUESTA").HasColumnType("datetime");
            e.HasOne(x => x.Domicilio).WithMany().HasForeignKey(x => x.IdDomicilio);
            e.HasOne(x => x.Colaborador).WithMany().HasForeignKey(x => x.IdUsuarioColaborador);
        });

        mb.Entity<ColaboracionTipoEmision>(e =>
        {
            e.ToTable("COLABORACION_TIPO_EMISION");
            e.HasKey(x => new { x.IdColaboracion, x.IdTipoEmision });
            e.Property(x => x.IdColaboracion).HasColumnName("ID_COLABORACION");
            e.Property(x => x.IdTipoEmision).HasColumnName("ID_TIPO_EMISION");
            e.HasOne(x => x.Colaboracion).WithMany(c => c.TiposEmision).HasForeignKey(x => x.IdColaboracion);
            e.HasOne(x => x.TipoEmision).WithMany().HasForeignKey(x => x.IdTipoEmision);
        });

        mb.Entity<Reporte>(e =>
        {
            e.ToTable("REPORTE");
            e.HasKey(x => x.IdReporte);
            e.Property(x => x.IdReporte).HasColumnName("ID_REPORTE");
            e.Property(x => x.IdUsuario).HasColumnName("ID_USUARIO");
            e.Property(x => x.Nombre).HasColumnName("NOMBRE").HasMaxLength(200);
            e.Property(x => x.Descripcion).HasColumnName("DESCRIPCION").HasMaxLength(500);
            e.Property(x => x.Filtros).HasColumnName("FILTROS");
            e.Property(x => x.FechaCreacion).HasColumnName("FECHA_CREACION").HasColumnType("datetime");
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario);
        });
    }
}
