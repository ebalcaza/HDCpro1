using HDC.BLL.Comun;
using HDC.DAL;
using HDC.Entity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HDC.BLL.Servicios;

/// <summary>
/// Carga DATOS DE PRUEBA para demostrar el sistema con el catálogo definitivo (v2.7). Entre todos los
/// escenarios se usan TODAS las fuentes de emisión:
///   - 2 individuos y una planta industrial, con 12 meses de consumos y colaboraciones individuales.
///   - La convocatoria de la planta: 30 empleados, con traslados mensuales y dos roles.
///   - Una facultad: consumos institucionales, viajes y una convocatoria con estudiantes (semestral),
///     personal docente (anual) y personal no docente (mensual).
///
/// - Solo corre si DatosPrueba:Habilitado = true (variable DATOS_PRUEBA del .env). En producción: false.
/// - Es idempotente por bloques: un bloque que ya se cargó no se vuelve a crear.
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
    private const string EmailFacultad = "contacto@facultad.prueba.com";

    /// <summary>Ciudades preferidas para los domicilios de prueba (si no están, se usa cualquiera).</summary>
    private static readonly string[] CiudadesPreferidas = ["mar del plata", "la plata", "rosario", "córdoba"];

    public async Task CrearAsync()
    {
        if (!config.GetValue<bool>("DatosPrueba:Habilitado")) return;
        // Cada bloque es idempotente por separado: así una base creada con una versión anterior
        // recibe los datos de prueba nuevos (ej. la convocatoria de la v2.2) sin duplicar los viejos.
        if (!await ctx.Usuarios.AnyAsync(u => u.Email == EmailMaria)) await CrearUsuariosYConsumosAsync();
        await CrearConvocatoriaAsync();
        if (!await ctx.Usuarios.AnyAsync(u => u.Email == EmailFacultad)) await CrearFacultadAsync();
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

            // María completa con garrafas en los meses más fríos
            var detallesMaria = new List<(FuenteEmision, decimal)>
            {
                (F("Gas natural de red"), 35 + 85 * invierno),
                (F("Electricidad de la red"), 170 + 90 * verano),
                (F("Agua potable"), 11 + 4 * verano),
                (F("Residuos sólidos urbanos"), 24)
            };
            if (invierno > 0.5m) detallesMaria.Add((F("Gas envasado (GLP) en kg"), 10));
            Registrar(casaMaria, maria, periodo, ahora, [.. detallesMaria]);

            // Juan compra el gas envasado por litros y también anota los km del auto
            var detallesJuan = new List<(FuenteEmision, decimal)>
            {
                (F("Gas natural de red"), 45 + 100 * invierno),
                (F("Electricidad de la red"), 210 + 120 * verano),
                (F("Agua potable"), 14 + 5 * verano),
                (F("Nafta para vehículos"), 60 + (periodo.Month == 1 ? 90 : 0)) // vacaciones en enero
            };
            if (invierno > 0.5m) detallesJuan.Add((F("Gas envasado (GLP) en litros"), 15));
            var registroJuan = Registrar(casaJuan, juan, periodo, ahora, [.. detallesJuan]);
            AnotarKilometros(registroJuan, F("Nafta para vehículos"), 650 + (periodo.Month == 1 ? 1000 : 0));

            var detallesPlanta = new List<(FuenteEmision, decimal)>
            {
                (F("Electricidad de la red"), 9500 + 1500 * verano),
                (F("Electricidad de energías renovables"), 1200),
                (F("Gas natural de red"), 600 + 900 * invierno),
                (F("Gasoil para vehículos"), 850),
                (F("Gasoil para grupos electrógenos"), 60),
                (F("Residuos sólidos urbanos"), 1200),
                (F("Plásticos reciclados"), 3500),
                (F("Papel y cartón reciclado"), 300),
                (F("Agua potable"), 180)
            };
            if (periodo.Month == 11) detallesPlanta.Add((F("R-410A (aire y climatización central)"), 3)); // mantenimiento anual del aire
            var registroPlanta = Registrar(planta, ecoSur, periodo, ahora, [.. detallesPlanta]);
            AnotarKilometros(registroPlanta, F("Gasoil para vehículos"), 7200);
        }

        // ---------------- Colaboraciones ----------------
        // Juan colabora en la casa de María cargando traslados (aceptada, desde hace 3 meses)
        var desde = mesActual.AddMonths(-2);
        ctx.Colaboraciones.Add(new Colaboracion
        {
            Domicilio = casaMaria, Colaborador = juan, PeriodoDesde = desde, Estado = EstadoColaboracion.Aceptada,
            Mensaje = "Cargá los viajes del auto compartido, por favor", FechaInvitacion = ahora.AddMonths(-3), FechaRespuesta = ahora.AddMonths(-3).AddDays(1),
            TiposEmision = [new ColaboracionTipoEmision { IdTipoEmision = F("Automóvil a nafta").IdTipoEmision }]
        });
        for (var p = desde; p <= mesActual; p = p.AddMonths(1))
            RegistrarTraslado(casaMaria, juan, Periodicidad.Mensual, p, 1, 4, null, null, ahora, 1, (F("Automóvil a nafta"), 15, 2));

        // EcoSur invitó a María a cargar la electricidad de la planta (todavía pendiente de respuesta)
        ctx.Colaboraciones.Add(new Colaboracion
        {
            Domicilio = planta, Colaborador = maria, PeriodoDesde = mesActual, Estado = EstadoColaboracion.Pendiente,
            Mensaje = "Hola María, ¿podés cargar las facturas de luz de la planta?", FechaInvitacion = ahora.AddDays(-2),
            TiposEmision = [new ColaboracionTipoEmision { IdTipoEmision = F("Electricidad de la red").IdTipoEmision }]
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

    /// <summary>Código fijo de la convocatoria de la facultad, para poder escribirlo en la documentación.</summary>
    public const string CodigoConvocatoriaFacultad = "FACU2K26";

    /// <summary>
    /// Facultad de prueba (v2.7): el caso de fix8. Carga los consumos institucionales y abre una convocatoria con los
    /// roles estudiantes, personal docente y personal no docente. Cada rol usa un período distinto, y entre todos usan
    /// los 8 medios de transporte. También tiene las fuentes que no aparecen en los otros escenarios.
    /// </summary>
    private async Task CrearFacultadAsync()
    {
        var ciudad = await ctx.Ciudades.AsNoTracking().Where(c => c.Nombre.ToLower() == "ezeiza").FirstOrDefaultAsync()
            ?? await ctx.Ciudades.AsNoTracking().OrderBy(c => c.IdCiudad).FirstOrDefaultAsync();
        if (ciudad is null) return; // sin geografía todavía: se reintenta en el próximo arranque

        var fuentes = await ctx.FuentesEmision.AsNoTracking().ToDictionaryAsync(f => f.Nombre);
        FuenteEmision F(string nombre) => fuentes[nombre];

        var ahora = DateTime.Now;
        var mesActual = Mapeos.MesActual();
        var hash = AuthServicio.HashContrasena(ContrasenaPrueba);

        var facultad = new Usuario
        {
            IdRol = RolesSistema.Organizacion, Email = EmailFacultad, Contrasena = hash, Telefono = "1145550000", FechaAlta = ahora.AddMonths(-13),
            Organizacion = new Organizacion
            {
                RazonSocial = "Facultad de Prueba", Cuit = Mapeos.NormalizarCuit("30798765432"), Area = "Educación",
                Descripcion = "Institución de prueba: sede con aulas y laboratorios", CantidadMiembros = 500
            }
        };
        var sede = new Domicilio { Usuario = facultad, IdCiudad = ciudad.IdCiudad, Calle = "Av. de la Prueba", Numero = 100, CodigoPostal = "1804", FechaAlta = ahora.AddMonths(-13) };
        ctx.Usuarios.Add(facultad);
        ctx.Domicilios.Add(sede);

        // ---------------- Consumos de la sede: 12 meses hasta el mes actual ----------------
        for (var i = 11; i >= 0; i--)
        {
            var periodo = mesActual.AddMonths(-i);
            var invierno = Invierno(periodo.Month);
            var verano = 1 - invierno;
            // Enero y febrero tienen muy poca actividad en la sede
            var actividad = periodo.Month is 1 or 2 ? 0.3m : 1m;

            var detalles = new List<(FuenteEmision, decimal)>
            {
                (F("Gas natural de red"), (300 + 1100 * invierno) * actividad),
                (F("Electricidad de la red"), (6000 + 3000 * verano) * actividad),
                (F("Electricidad de energías renovables"), 800),
                (F("Agua potable"), 90 * actividad),
                (F("Gasoil para grupos electrógenos"), 40),
                (F("Nafta para vehículos"), 120),
                (F("Gasoil para vehículos"), 90),
                (F("Residuos sólidos urbanos"), 900 * actividad),
                (F("Papel y cartón reciclado"), 180 * actividad),
                (F("Plásticos reciclados"), 60 * actividad),
                (F("Vidrio reciclado"), 25 * actividad),
                (F("Residuos peligrosos de laboratorio"), 15 * actividad),
                (F("Papel A4 virgen"), 40 * actividad),
                (F("Papel A4 reciclado"), 25 * actividad),
                (F("Cartuchos de tinta"), 4)
            };
            // Compras de cada trimestre
            if (periodo.Month % 3 == 0)
                detalles.AddRange([(F("Papel A3 virgen"), 6), (F("Papel A3 reciclado"), 3), (F("Tóner para impresoras"), 8), (F("Tóner remanufacturado"), 5), (F("Insumos de oficina (opcional)"), 20)]);
            // Disposiciones anuales
            if (periodo.Month == 12) detalles.AddRange([(F("Residuos electrónicos (RAEE)"), 120), (F("Aceites usados"), 40)]);
            // Mantenimiento anual de aires, heladeras y freezers
            if (periodo.Month == 3)
                detalles.AddRange([
                    (F("R-410A (aire y climatización central)"), 4), (F("R-32 (aire y climatización central)"), 2),
                    (F("R-22 (aire y climatización central)"), 1.5m), (F("R-290 / propano (aire y climatización central)"), 0.5m),
                    (F("R-134a (heladeras y freezers)"), 0.4m), (F("R-600a / isobutano (heladeras y freezers)"), 0.2m)]);
            // El laboratorio usa gas envasado medido por kilos
            if (periodo.Month % 2 == 0) detalles.Add((F("Gas envasado (GLP) en kg"), 45));

            var registro = Registrar(sede, facultad, periodo, ahora, [.. detalles]);
            AnotarKilometros(registro, F("Nafta para vehículos"), 1500);
        }

        // ---------------- Viajes institucionales: un registro por viaje. Suman emisiones, salvo el avión (v2.9) ----------------
        (string Fuente, decimal Km)[] viajes =
        [
            ("Viaje en automóvil a nafta", 320), ("Viaje en automóvil a gasoil", 540), ("Viaje en motocicleta a nafta", 60),
            ("Viaje en colectivo a gasoil", 800), ("Viaje en tren eléctrico", 120), ("Viaje en subte eléctrico", 18), ("Viaje en avión", 1300)
        ];
        for (var i = 0; i < viajes.Length; i++)
        {
            // Empieza en el mes anterior: así la fecha nunca es futura
            var periodo = mesActual.AddMonths(-(i + 1));
            var registro = Registrar(sede, facultad, periodo, ahora, (F(viajes[i].Fuente), viajes[i].Km));
            registro.Detalles.Single().FechaViaje = periodo.AddDays(9);
            registro.Observaciones = "Dato de prueba: viaje institucional";
        }

        // ---------------- Convocatoria con roles ----------------
        var desde = mesActual.AddMonths(-11);
        var convocatoria = new Convocatoria
        {
            Domicilio = sede, Nombre = "Traslados de la comunidad 2026", Codigo = CodigoConvocatoriaFacultad,
            Descripcion = "Elegí tu rol y cargá cómo venís a la sede: medio de transporte, distancia de ida y vuelta y días por semana.",
            // Como pasará en la práctica: el período completo ya transcurrió cuando se carga
            PeriodoDesde = desde, PeriodoHasta = mesActual, SemanasTraslado = 32, Estado = EstadoConvocatoria.Abierta, AprobacionAutomatica = true, FechaCreacion = ahora.AddMonths(-11),
            TiposEmision = [new ConvocatoriaTipoEmision { IdTipoEmision = F("Colectivo").IdTipoEmision }]
        };
        var rolEstudiantes = new ConvocatoriaRol { Nombre = "Estudiantes" };
        var rolDocentes = new ConvocatoriaRol { Nombre = "Personal docente" };
        var rolNoDocentes = new ConvocatoriaRol { Nombre = "Personal no docente" };
        convocatoria.Roles.Add(rolEstudiantes);
        convocatoria.Roles.Add(rolDocentes);
        convocatoria.Roles.Add(rolNoDocentes);
        ctx.Convocatorias.Add(convocatoria);

        // Medio de transporte y distancia de ida y vuelta típica
        (string Fuente, decimal Km)[] medios =
        [
            ("Colectivo", 18), ("Tren", 34), ("Subte", 9), ("Automóvil a nafta", 26),
            ("Automóvil a gasoil", 30), ("Motocicleta a nafta", 14), ("Bicicleta", 6), ("Caminata", 2)
        ];
        string[] nombres = ["Agustina", "Bruno", "Carla", "Damián", "Elena", "Facundo", "Gabriela", "Hernán", "Inés", "Joaquín"];
        string[] apellidos = ["Acosta", "Benítez", "Castro", "Domínguez", "Espinoza", "Flores", "Giménez", "Herrera", "Ibáñez", "Juárez"];
        var dni = 42_000_000;

        Usuario Miembro(string prefijo, int n, string estado)
        {
            var u = new Usuario
            {
                IdRol = RolesSistema.Individuo, Email = $"{prefijo}{n:D2}@facultad.prueba.com", Contrasena = hash, FechaAlta = ahora.AddMonths(-11),
                Individuo = new Individuo { Nombres = nombres[(dni + n) % 10], Apellidos = apellidos[(dni / 3 + n * 3) % 10], Dni = (++dni).ToString() }
            };
            ctx.Usuarios.Add(u);
            ctx.Colaboraciones.Add(new Colaboracion
            {
                Domicilio = sede, Colaborador = u, Convocatoria = convocatoria, PeriodoDesde = desde, Estado = estado,
                FechaInvitacion = ahora.AddMonths(-10), FechaRespuesta = estado == EstadoColaboracion.Aceptada ? ahora.AddMonths(-10) : null
            });
            return u;
        }

        // Estudiantes: cargan todo el período de una vez. Los pares combinan dos medios de transporte
        for (var n = 1; n <= 16; n++)
        {
            var medio = medios[(n - 1) % medios.Length];
            var otro = medios[n % medios.Length];
            var estudiante = Miembro("estudiante", n, EstadoColaboracion.Aceptada);
            if (n % 2 == 0)
                RegistrarTraslado(sede, estudiante, Periodicidad.PeriodoCompleto, desde, 12, 32, convocatoria, rolEstudiantes, ahora, 1,
                    (F(medio.Fuente), medio.Km + n % 4, 2), (F(otro.Fuente), otro.Km, 1));
            else
                RegistrarTraslado(sede, estudiante, Periodicidad.PeriodoCompleto, desde, 12, 32, convocatoria, rolEstudiantes, ahora, 1,
                    (F(medio.Fuente), medio.Km + n % 4, 2 + n % 3));
        }
        // Dos estudiantes se sumaron pero todavía no cargaron su traslado
        Miembro("estudiante", 17, EstadoColaboracion.Aceptada);
        Miembro("estudiante", 18, EstadoColaboracion.Aceptada);

        // Docentes: también cargan todo el período
        for (var n = 1; n <= 8; n++)
        {
            var medio = medios[(n - 1) % medios.Length];
            RegistrarTraslado(sede, Miembro("docente", n, EstadoColaboracion.Aceptada), Periodicidad.PeriodoCompleto, desde, 12, 32, convocatoria, rolDocentes, ahora, 1,
                (F(medio.Fuente), medio.Km + n % 3, 2));
        }

        // No docentes: cargan un mes por vez; ya tienen los últimos 3 meses y les faltan los anteriores
        for (var n = 1; n <= 6; n++)
        {
            var medio = medios[(n - 1) % medios.Length];
            var miembro = Miembro("nodocente", n, EstadoColaboracion.Aceptada);
            for (var p = mesActual.AddMonths(-2); p <= mesActual; p = p.AddMonths(1))
                RegistrarTraslado(sede, miembro, Periodicidad.Mensual, p, 12, 32, convocatoria, rolNoDocentes, ahora, 1, (F(medio.Fuente), medio.Km, 5));
        }

        // El titular carga un grupo con los datos de una encuesta: muestra el campo "cantidad de personas"
        RegistrarTraslado(sede, facultad, Periodicidad.Semestral, mesActual.AddMonths(-5), 6, 16, null, null, ahora, 150, (F("Colectivo"), 20, 3));

        await ctx.SaveChangesAsync();

        sede.TotalKgCo2 = await ctx.Registros.Where(r => r.IdDomicilio == sede.IdDomicilio).SumAsync(r => r.TotalKgCo2);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        logger.LogInformation("Facultad de prueba creada ({Email}), con la convocatoria {Codigo}", EmailFacultad, CodigoConvocatoriaFacultad);
    }

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
        var colectivo = fuentes["Colectivo"];
        var auto = fuentes["Automóvil a nafta"];
        var ahora = DateTime.Now;
        var mesActual = Mapeos.MesActual();
        var desde = mesActual.AddMonths(-3);

        var convocatoria = new Convocatoria
        {
            Domicilio = planta, Nombre = "Transporte del personal 2026", Codigo = CodigoConvocatoriaPrueba,
            Descripcion = "Cargá cómo venís a la planta: medio de transporte, distancia de ida y vuelta y días por semana.",
            PeriodoDesde = desde, PeriodoHasta = mesActual, SemanasTraslado = 16, Estado = EstadoConvocatoria.Abierta, AprobacionAutomatica = false, FechaCreacion = ahora.AddMonths(-3),
            // Colectivo y automóvil son del mismo tipo: "Traslado de colaboradores"
            TiposEmision = [new ConvocatoriaTipoEmision { IdTipoEmision = colectivo.IdTipoEmision }]
        };
        var rolOperario = new ConvocatoriaRol { Nombre = "Operario" };
        var rolAdministrativo = new ConvocatoriaRol { Nombre = "Administrativo" };
        convocatoria.Roles.Add(rolOperario);
        convocatoria.Roles.Add(rolAdministrativo);
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

            // Los aceptados ya cargaron sus traslados de los últimos meses (colectivo los pares, auto los impares)
            if (estado != EstadoColaboracion.Aceptada) continue;
            for (var p = desde; p <= mesActual; p = p.AddMonths(1))
                RegistrarTraslado(planta, empleado, Periodicidad.Mensual, p, 4, 16, convocatoria, i % 2 == 0 ? rolOperario : rolAdministrativo, ahora, 1, (i % 2 == 0 ? colectivo : auto, 10 + i, 5));
        }

        await ctx.SaveChangesAsync();

        // Se recalcula el total del domicilio con todos sus registros (los de la planta y los de los empleados)
        planta.TotalKgCo2 = await ctx.Registros.Where(r => r.IdDomicilio == planta.IdDomicilio).SumAsync(r => r.TotalKgCo2);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        logger.LogInformation("Convocatoria de prueba creada (código {Codigo}) con {N} empleados", CodigoConvocatoriaPrueba, EmpleadosPrueba);
    }

    /// <summary>
    /// Crea una carga de traslado con un registro por mes, sin pasar del mes actual (mismo cálculo que TrasladoServicio).
    /// </summary>
    private void RegistrarTraslado(Domicilio d, Usuario cargadoPor, string periodicidad, DateOnly desde, int mesesPeriodo, decimal semanas,
        Convocatoria? convocatoria, ConvocatoriaRol? rol, DateTime ahora, int personas, params (FuenteEmision Fuente, decimal DistanciaKm, decimal DiasSemana)[] medios)
    {
        var mesActual = Mapeos.MesActual();
        // Una carga mensual cubre un solo mes, aunque las semanas sean las del período completo
        var cantidad = periodicidad == Periodicidad.Mensual ? 1 : mesesPeriodo;
        var meses = Enumerable.Range(0, cantidad).Select(desde.AddMonths).Where(m => m <= mesActual).ToList();
        var carga = new CargaTraslado
        {
            Domicilio = d, UsuarioCarga = cargadoPor, Convocatoria = convocatoria, ConvocatoriaRol = rol,
            Periodicidad = periodicidad, PeriodoDesde = desde, PeriodoHasta = meses[^1], MesesPeriodo = mesesPeriodo,
            Semanas = semanas, CantidadPersonas = personas, FechaCreacion = ahora
        };
        foreach (var m in medios)
            carga.Medios.Add(new CargaTrasladoMedio { IdFuenteEmision = m.Fuente.IdFuenteEmision, DistanciaKm = m.DistanciaKm, DiasSemana = m.DiasSemana, ValorFactorEmision = m.Fuente.ValorFactorEmision });

        foreach (var mes in meses)
        {
            var r = new Registro
            {
                Domicilio = d, UsuarioCarga = cargadoPor, Convocatoria = convocatoria, Periodo = mes, Observaciones = "Dato de prueba",
                FechaCreacion = ahora, FechaUltimaModificacion = ahora
            };
            foreach (var m in medios)
            {
                var km = Math.Round(m.DistanciaKm * m.DiasSemana * (semanas / mesesPeriodo) * personas, 4);
                r.Detalles.Add(new DetalleRegistro { IdFuenteEmision = m.Fuente.IdFuenteEmision, Consumo = km, ValorFactorEmision = m.Fuente.ValorFactorEmision, KgCo2 = Math.Round(km * m.Fuente.ValorFactorEmision, 6) });
            }
            r.TotalKgCo2 = r.Detalles.Sum(x => x.KgCo2);
            carga.Registros.Add(r);
        }
        ctx.CargasTraslado.Add(carga);
    }

    /// <summary>Anota los km recorridos de un vehículo: son un dato adicional y no cambian el cálculo.</summary>
    private static void AnotarKilometros(Registro r, FuenteEmision vehiculo, decimal kilometros) =>
        r.Detalles.Single(d => d.IdFuenteEmision == vehiculo.IdFuenteEmision).Kilometros = kilometros;

    /// <summary>Crea un registro con sus detalles, calculando kg CO2 = consumo × factor (igual que RegistroServicio).</summary>
    private Registro Registrar(Domicilio d, Usuario cargadoPor, DateOnly periodo, DateTime ahora, params (FuenteEmision Fuente, decimal Consumo)[] consumos)
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
        return r;
    }

    /// <summary>Qué tan "invierno" es el mes en Argentina: 1 en julio, 0 en enero (curva coseno).</summary>
    private static decimal Invierno(int mes) => (decimal)((1 - Math.Cos(2 * Math.PI * (mes - 1) / 12.0)) / 2);
}
