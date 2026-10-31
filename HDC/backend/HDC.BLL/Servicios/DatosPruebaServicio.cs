using HDC.BLL.Comun;
using HDC.DAL;
using HDC.Entity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HDC.BLL.Servicios;

/// <summary>
/// Carga DATOS DE PRUEBA para demostrar el sistema: 2 individuos y 1 organización, cada uno con su
/// domicilio y 12 meses de consumos, más una colaboración aceptada y una invitación pendiente.
///
/// - Solo corre si DatosPrueba:Habilitado = true (variable DATOS_PRUEBA del .env). En producción: false.
/// - Es idempotente: si el usuario de prueba principal ya existe, no hace nada.
/// - Necesita la geografía (ciudades) cargada desde georef, por eso se ejecuta DESPUÉS de esa descarga.
/// - Los valores de consumo son inventados pero realistas (más gas en invierno, más luz en verano).
///
/// Usa el DbContext directamente (y no los repositorios) porque es una carga masiva de una sola vez,
/// que se guarda en una única transacción con SaveChanges.
/// </summary>
public class DatosPruebaServicio(HdcDbContext ctx, IConfiguration config, ILogger<DatosPruebaServicio> logger)
{
    /// <summary>Contraseña de todas las cuentas de prueba (cumple la regla: letras y números, sin espacios).</summary>
    public const string ContrasenaPrueba = "Prueba2026";

    private const string EmailMaria = "maria.gomez@prueba.com";
    private const string EmailJuan = "juan.perez@prueba.com";
    private const string EmailEcoSur = "contacto@ecosur.prueba.com";

    /// <summary>Ciudades preferidas para los domicilios de prueba (si no están, se usa cualquiera).</summary>
    private static readonly string[] CiudadesPreferidas = ["mar del plata", "la plata", "rosario", "córdoba"];

    public async Task CrearAsync()
    {
        if (!config.GetValue<bool>("DatosPrueba:Habilitado")) return;
        // Cada bloque es idempotente por separado: así una base creada con una versión anterior
        // recibe los datos de prueba nuevos (ej. la convocatoria de la v2.2) sin duplicar los viejos.
        if (!await ctx.Usuarios.AnyAsync(u => u.Email == EmailMaria)) await CrearUsuariosYConsumosAsync();
        await CrearConvocatoriaAsync();
    }

    /// <summary>Usuarios de prueba, domicilios, 12 meses de consumos y colaboraciones individuales.</summary>
    private async Task CrearUsuariosYConsumosAsync()
    {

        var ciudades = await ctx.Ciudades.AsNoTracking()
            .Where(c => CiudadesPreferidas.Contains(c.Nombre.ToLower()))
            .ToListAsync();
        var ciudad = ciudades.FirstOrDefault() ?? await ctx.Ciudades.AsNoTracking().OrderBy(c => c.IdCiudad).FirstOrDefaultAsync();
        if (ciudad is null)
        {
            logger.LogWarning("No hay ciudades cargadas (georef): no se pueden crear los datos de prueba todavía.");
            return;
        }

        // Fuentes de emisión por nombre (las carga el script SQL)
        var fuentes = await ctx.FuentesEmision.AsNoTracking().ToDictionaryAsync(f => f.Nombre);
        FuenteEmision F(string nombre) => fuentes[nombre];

        var ahora = DateTime.Now;
        var hash = AuthServicio.HashContrasena(ContrasenaPrueba);

        // ---------------- Usuarios ----------------
        var maria = new Usuario
        {
            IdRol = RolesSistema.Individuo, Email = EmailMaria, Contrasena = hash, Telefono = "2235551234", FechaAlta = ahora.AddMonths(-13),
            Individuo = new Individuo { Nombres = "María", Apellidos = "Gómez", Dni = "30111222", FechaNacimiento = new DateOnly(1985, 4, 12), Genero = "Femenino" }
        };
        var juan = new Usuario
        {
            IdRol = RolesSistema.Individuo, Email = EmailJuan, Contrasena = hash, FechaAlta = ahora.AddMonths(-13),
            Individuo = new Individuo { Nombres = "Juan", Apellidos = "Pérez", Dni = "28999888", FechaNacimiento = new DateOnly(1981, 9, 3), Genero = "Masculino" }
        };
        var ecoSur = new Usuario
        {
            IdRol = RolesSistema.Organizacion, Email = EmailEcoSur, Contrasena = hash, Telefono = "2234990000", FechaAlta = ahora.AddMonths(-13),
            Organizacion = new Organizacion
            {
                RazonSocial = "EcoSur S.A.", Cuit = Mapeos.NormalizarCuit("30712345678"), Area = "Industria",
                Descripcion = "Empresa de prueba: planta de reciclado de plásticos", CantidadMiembros = 45
            }
        };
        ctx.Usuarios.AddRange(maria, juan, ecoSur);

        // ---------------- Domicilios ----------------
        Domicilio NuevoDomicilio(Usuario u, string calle, int numero, string cp) => new()
        {
            Usuario = u, IdCiudad = ciudad.IdCiudad, Calle = calle, Numero = numero, CodigoPostal = cp, FechaAlta = ahora.AddMonths(-13)
        };
        var casaMaria = NuevoDomicilio(maria, "Av. Colón", 2450, "7600");
        var casaJuan = NuevoDomicilio(juan, "Belgrano", 1180, "7600");
        var planta = NuevoDomicilio(ecoSur, "Ruta 88 km", 4, "7600");
        ctx.Domicilios.AddRange(casaMaria, casaJuan, planta);

        // ---------------- Registros: 12 meses hasta el mes actual ----------------
        var mesActual = Mapeos.MesActual();
        for (var i = 11; i >= 0; i--)
        {
            var periodo = mesActual.AddMonths(-i);
            var invierno = Invierno(periodo.Month);  // 1 en julio, 0 en enero
            var verano = 1 - invierno;

            Registrar(casaMaria, maria, periodo, ahora,
                (F("Gas natural de red"), 35 + 85 * invierno),
                (F("Energía eléctrica de red"), 170 + 90 * verano),
                (F("Agua de red"), 11 + 4 * verano),
                (F("Residuos sólidos urbanos"), 24),
                (F("Colectivo urbano"), 320));

            Registrar(casaJuan, juan, periodo, ahora,
                (F("Gas natural de red"), 45 + 100 * invierno),
                (F("Energía eléctrica de red"), 210 + 120 * verano),
                (F("Agua de red"), 14 + 5 * verano),
                (F("Nafta - vehículo propio"), 60 + (periodo.Month == 1 ? 90 : 0))); // vacaciones en enero

            var detallesPlanta = new List<(FuenteEmision, decimal)>
            {
                (F("Energía eléctrica de red"), 9500 + 1500 * verano),
                (F("Gas natural de red"), 600 + 900 * invierno),
                (F("Gasoil - vehículo propio"), 850),
                (F("Residuos sólidos urbanos"), 1200),
                (F("Auto particular (nafta)"), 5200)
            };
            if (periodo.Month == 11) detallesPlanta.Add((F("Recarga de refrigerante R-410A"), 3)); // mantenimiento anual del aire
            Registrar(planta, ecoSur, periodo, ahora, [.. detallesPlanta]);
        }

        // ---------------- Colaboraciones ----------------
        // Juan colabora en la casa de María cargando "Transporte" (aceptada, desde hace 3 meses)
        var desde = mesActual.AddMonths(-2);
        ctx.Colaboraciones.Add(new Colaboracion
        {
            Domicilio = casaMaria, Colaborador = juan, PeriodoDesde = desde, Estado = EstadoColaboracion.Aceptada,
            Mensaje = "Cargá los viajes del auto compartido, por favor", FechaInvitacion = ahora.AddMonths(-3), FechaRespuesta = ahora.AddMonths(-3).AddDays(1),
            TiposEmision = [new ColaboracionTipoEmision { IdTipoEmision = F("Auto particular (nafta)").IdTipoEmision }]
        });
        for (var p = desde; p <= mesActual; p = p.AddMonths(1))
            Registrar(casaMaria, juan, p, ahora, (F("Auto particular (nafta)"), 140));

        // EcoSur invitó a María a cargar la electricidad de la planta (todavía pendiente de respuesta)
        ctx.Colaboraciones.Add(new Colaboracion
        {
            Domicilio = planta, Colaborador = maria, PeriodoDesde = mesActual, Estado = EstadoColaboracion.Pendiente,
            Mensaje = "Hola María, ¿podés cargar las facturas de luz de la planta?", FechaInvitacion = ahora.AddDays(-2),
            TiposEmision = [new ColaboracionTipoEmision { IdTipoEmision = F("Energía eléctrica de red").IdTipoEmision }]
        });

        // Total acumulado de cada domicilio (la aplicación lo mantiene actualizado al cargar registros)
        foreach (var d in new[] { casaMaria, casaJuan, planta })
            d.TotalKgCo2 = ctx.ChangeTracker.Entries<Registro>().Where(e => e.Entity.Domicilio == d).Sum(e => e.Entity.TotalKgCo2);

        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        logger.LogInformation("Datos de prueba creados ({Emails}) con la contraseña {Contrasena}",
            string.Join(", ", EmailMaria, EmailJuan, EmailEcoSur), ContrasenaPrueba);
    }

    /// <summary>Código fijo de la convocatoria de prueba, para poder escribirlo en la documentación.</summary>
    public const string CodigoConvocatoriaPrueba = "ECSR2K26";
    private const int EmpleadosPrueba = 30;

    /// <summary>
    /// Convocatoria de prueba (v2.2): EcoSur invita a su personal a cargar el transporte con el código ECSR-2K26.
    /// 30 empleados de prueba: 20 aceptados (con consumos cargados), 7 solicitudes pendientes y 3 dados de baja.
    /// </summary>
    private async Task CrearConvocatoriaAsync()
    {
        if (await ctx.Convocatorias.AnyAsync(c => c.Codigo == CodigoConvocatoriaPrueba)) return; // ya se cargó
        var ecoSur = await ctx.Usuarios.FirstOrDefaultAsync(u => u.Email == EmailEcoSur);
        var planta = ecoSur is null ? null : await ctx.Domicilios.FirstOrDefaultAsync(d => d.IdUsuario == ecoSur.IdUsuario);
        if (ecoSur is null || planta is null) return;

        var fuentes = await ctx.FuentesEmision.AsNoTracking().ToDictionaryAsync(f => f.Nombre);
        var colectivo = fuentes["Colectivo urbano"];
        var auto = fuentes["Auto particular (nafta)"];
        var ahora = DateTime.Now;
        var mesActual = Mapeos.MesActual();
        var desde = mesActual.AddMonths(-3);

        var convocatoria = new Convocatoria
        {
            Domicilio = planta, Nombre = "Transporte del personal 2026", Codigo = CodigoConvocatoriaPrueba,
            Descripcion = "Cargá los kilómetros que hacés cada mes para venir a la planta (colectivo, auto o moto).",
            PeriodoDesde = desde, Estado = EstadoConvocatoria.Abierta, AprobacionAutomatica = false, FechaCreacion = ahora.AddMonths(-3),
            // Colectivo y auto particular son del mismo tipo: "Transporte de colaboradores"
            TiposEmision = [new ConvocatoriaTipoEmision { IdTipoEmision = colectivo.IdTipoEmision }]
        };
        ctx.Convocatorias.Add(convocatoria);

        string[] nombres = ["Lucía", "Martín", "Sofía", "Diego", "Valentina", "Pablo", "Camila", "Nicolás", "Julieta", "Tomás"];
        string[] apellidos = ["Fernández", "Rodríguez", "López", "Martínez", "García", "Romero", "Sosa", "Álvarez", "Torres", "Ruiz"];
        var hash = AuthServicio.HashContrasena(ContrasenaPrueba);

        for (var i = 1; i <= EmpleadosPrueba; i++)
        {
            var empleado = new Usuario
            {
                IdRol = RolesSistema.Individuo, Email = $"empleado{i:D2}@ecosur.prueba.com", Contrasena = hash, FechaAlta = ahora.AddMonths(-3),
                Individuo = new Individuo { Nombres = nombres[(i - 1) % 10], Apellidos = apellidos[(i * 3 + i / 10) % 10], Dni = (40_000_000 + i).ToString() }
            };
            ctx.Usuarios.Add(empleado);

            // 1-20 aceptados · 21-27 esperando aprobación · 28-30 dados de baja por EcoSur
            var estado = i <= 20 ? EstadoColaboracion.Aceptada : i <= 27 ? EstadoColaboracion.Solicitada : EstadoColaboracion.Revocada;
            ctx.Colaboraciones.Add(new Colaboracion
            {
                Domicilio = planta, Colaborador = empleado, Convocatoria = convocatoria, PeriodoDesde = desde,
                Estado = estado, FechaInvitacion = ahora.AddDays(-80 + i), FechaRespuesta = estado == EstadoColaboracion.Solicitada ? null : ahora.AddDays(-79 + i),
                IdUsuarioBaja = estado == EstadoColaboracion.Revocada ? ecoSur.IdUsuario : null
            });

            // Los aceptados ya cargaron sus km de los últimos meses (colectivo los pares, auto los impares)
            if (estado != EstadoColaboracion.Aceptada) continue;
            for (var p = desde; p <= mesActual; p = p.AddMonths(1))
                Registrar(planta, empleado, p, ahora, (i % 2 == 0 ? colectivo : auto, 180 + i * 7));
        }

        await ctx.SaveChangesAsync();

        // Se recalcula el total del domicilio con todos sus registros (los de la planta y los de los empleados)
        planta.TotalKgCo2 = await ctx.Registros.Where(r => r.IdDomicilio == planta.IdDomicilio).SumAsync(r => r.TotalKgCo2);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        logger.LogInformation("Convocatoria de prueba creada (código {Codigo}) con {N} empleados", CodigoConvocatoriaPrueba, EmpleadosPrueba);
    }

    /// <summary>Crea un registro con sus detalles, calculando kg CO2 = consumo × factor (igual que RegistroServicio).</summary>
    private void Registrar(Domicilio d, Usuario cargadoPor, DateOnly periodo, DateTime ahora, params (FuenteEmision Fuente, decimal Consumo)[] consumos)
    {
        var r = new Registro
        {
            Domicilio = d, UsuarioCarga = cargadoPor, Periodo = periodo,
            Observaciones = "Dato de prueba", FechaCreacion = ahora, FechaUltimaModificacion = ahora
        };
        foreach (var (fuente, consumo) in consumos)
        {
            var c = Math.Round(consumo, 2);
            r.Detalles.Add(new DetalleRegistro
            {
                IdFuenteEmision = fuente.IdFuenteEmision, Consumo = c,
                ValorFactorEmision = fuente.ValorFactorEmision, KgCo2 = Math.Round(c * fuente.ValorFactorEmision, 6)
            });
        }
        r.TotalKgCo2 = r.Detalles.Sum(x => x.KgCo2);
        ctx.Registros.Add(r);
    }

    /// <summary>Qué tan "invierno" es el mes en Argentina: 1 en julio, 0 en enero (curva coseno).</summary>
    private static decimal Invierno(int mes) => (decimal)((1 - Math.Cos(2 * Math.PI * (mes - 1) / 12.0)) / 2);
}
