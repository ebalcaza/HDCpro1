using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HDC.Entity.Validaciones;

/*
 * =====================================================================================
 *  REGLAS DE VALIDACIÓN DEL BACKEND
 * =====================================================================================
 *  Este archivo concentra las reglas que se aplican a los datos que llegan a la API.
 *  Se usan como atributos sobre las propiedades de los DTOs (ej: [NombrePersona(200)]).
 *  Si un dato no cumple, ASP.NET responde 400 con el mensaje de error, y el frontend
 *  lo muestra debajo del campo correspondiente.
 *
 *  IMPORTANTE: el frontend tiene las MISMAS reglas en
 *  frontend/src/components/comunes/Formulario.jsx (objeto `reglas`). Si cambiás una
 *  regla acá, cambiala también allá para que los mensajes coincidan.
 *
 *  Los largos máximos salen de la base de datos (Scripts/HDC_base_datos.sql):
 *    INDIVIDUO.NOMBRES   NVARCHAR(200)     INDIVIDUO.APELLIDOS NVARCHAR(250)
 *    ORGANIZACION.AREA   NVARCHAR(100)     DOMICILIO.CALLE     NVARCHAR(200)
 *    USUARIO.TELEFONO    VARCHAR(30)       DOMICILIO.TELEFONO  VARCHAR(30)
 * =====================================================================================
 */
public static class Reglas
{
    // Letras con acentos, diéresis y ñ (español).
    private const string Letras = "A-Za-zÁÉÍÓÚÜÑáéíóúüñ";

    /// <summary>
    /// Nombres y apellidos: solo letras, espacios y el apóstrofe (') como único símbolo
    /// (ej: "O'Connor", "D'Alessandro"). No admite dos apóstrofes seguidos ni que empiece
    /// o termine con apóstrofe. Los espacios sobrantes se corrigen al guardar (ver
    /// <see cref="NormalizarNombre"/>), por eso acá no se rechazan.
    /// </summary>
    public static readonly Regex NombrePersona = new($"^\\s*[{Letras}]+(?:'[{Letras}]+|\\s+[{Letras}]+)*\\s*$");

    /// <summary>
    /// Calles: letras, números y espacios simples (ej: "Avenida 9 de Julio").
    /// No admite espacios consecutivos ni espacios al inicio o al final.
    /// </summary>
    public static readonly Regex Calle = new($"^[{Letras}0-9]+(?: [{Letras}0-9]+)*$");

    /// <summary>
    /// Contraseña: sin espacios, con al menos una letra, un número y un símbolo especial
    /// (cualquier carácter que no sea letra, número ni espacio).
    /// Devuelve el mensaje de la primera regla que no cumple, o null si es válida.
    /// (El largo mínimo de 8 se valida aparte con [MinLength]).
    /// </summary>
    public static string? ErrorContrasena(string s)
    {
        if (Regex.IsMatch(s, @"\s")) return "La contraseña no puede tener espacios";
        if (!Regex.IsMatch(s, $"[{Letras}]")) return "La contraseña debe contener al menos una letra";
        if (!Regex.IsMatch(s, @"\d")) return "La contraseña debe contener al menos un número";
        if (!Regex.IsMatch(s, $"[^{Letras}0-9]")) return "La contraseña debe contener al menos un símbolo especial (ej: ! @ # $ %)";
        return null;
    }

    /// <summary>
    /// Teléfono argentino en formato internacional: "+54 {característica} {número}".
    /// La característica va SIN el 0 (11 = CABA/GBA, 351 = Córdoba, 2954 = Santa Rosa...)
    /// y el número SIN el 15. Característica + número siempre suman 10 dígitos.
    /// </summary>
    public static readonly Regex Telefono = new(@"^\+54 (\d{2,4}) (\d{6,8})$");

    /// <summary>
    /// Teléfono del perfil (cualquier país): un "+" opcional al inicio y solo números.
    /// Máximo 15 dígitos (estándar internacional E.164), o sea 16 caracteres con el "+".
    /// </summary>
    public static readonly Regex TelefonoInternacional = new(@"^\+?\d{1,15}$");

    /// <summary>Opciones del select "Área" de las organizaciones (mismo listado que el frontend).</summary>
    public static readonly string[] AreasOrganizacion =
    [
        "Educación", "Industria", "Energía", "Agro", "Comercio", "Servicios", "Salud",
        "Construcción", "Transporte", "Tecnología", "Gobierno", "ONG", "Otra"
    ];

    /// <summary>
    /// Deja el nombre listo para guardar: sin espacios al inicio/final, sin espacios
    /// contiguos y con cada palabra en mayúscula inicial ("juan  o'connor " -> "Juan O'Connor").
    /// </summary>
    public static string NormalizarNombre(string valor)
    {
        var cultura = CultureInfo.GetCultureInfo("es-AR");
        var palabras = valor.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        // Cada parte separada por apóstrofe también lleva mayúscula: "o'connor" -> "O'Connor"
        return string.Join(' ', palabras.Select(p =>
            string.Join('\'', p.Split('\'').Select(parte => Capitalizar(parte, cultura)))));
    }

    private static string Capitalizar(string s, CultureInfo cultura) =>
        s.Length == 0 ? s : char.ToUpper(s[0], cultura) + s[1..].ToLower(cultura);

    // ---------------------------------------------------------------------------------
    //  ESPACIO PARA TUS PROPIAS REGLAS
    //  1. Declarás la expresión regular o el método acá arriba.
    //  2. Creás un atributo abajo (copiá cualquiera de los existentes como plantilla).
    //  3. Lo ponés sobre la propiedad del DTO: [MiRegla] public string Campo { get; set; }
    // ---------------------------------------------------------------------------------
}

/// <summary>Valida nombres/apellidos (ver <see cref="Reglas.NombrePersona"/>) y su largo máximo.</summary>
public class NombrePersonaAttribute(int largoMaximo) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return ValidationResult.Success; // [Required] se ocupa del vacío
        if (!Reglas.NombrePersona.IsMatch(s))
            return new ValidationResult("Solo se permiten letras, espacios y apóstrofe (')", [ctx.MemberName!]);
        if (Reglas.NormalizarNombre(s).Length > largoMaximo)
            return new ValidationResult($"Máximo {largoMaximo} caracteres", [ctx.MemberName!]);
        return ValidationResult.Success;
    }
}

/// <summary>Valida calles (ver <see cref="Reglas.Calle"/>).</summary>
public class CalleAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;
        return Reglas.Calle.IsMatch(s)
            ? ValidationResult.Success
            : new ValidationResult("Solo letras, números y espacios simples (sin espacios dobles ni al inicio/final)", [ctx.MemberName!]);
    }
}

/// <summary>Valida la composición de la contraseña (ver <see cref="Reglas.ErrorContrasena"/>).</summary>
public class ContrasenaAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is not string s || s.Length == 0) return ValidationResult.Success;
        var error = Reglas.ErrorContrasena(s);
        return error is null ? ValidationResult.Success : new ValidationResult(error, [ctx.MemberName!]);
    }
}

/// <summary>Valida teléfonos argentinos (ver <see cref="Reglas.Telefono"/>). Vacío = sin teléfono.</summary>
public class TelefonoArgentinoAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;
        var m = Reglas.Telefono.Match(s.Trim());
        if (m.Success && m.Groups[1].Length + m.Groups[2].Length == 10) return ValidationResult.Success;
        return new ValidationResult("Teléfono inválido: característica sin 0 + número sin 15 (10 dígitos en total)", [ctx.MemberName!]);
    }
}

/// <summary>Valida el teléfono del perfil (ver <see cref="Reglas.TelefonoInternacional"/>). Vacío = sin teléfono.</summary>
public class TelefonoInternacionalAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;
        return Reglas.TelefonoInternacional.IsMatch(s.Trim())
            ? ValidationResult.Success
            : new ValidationResult("Teléfono inválido: solo números y un + opcional al inicio (máximo 15 dígitos)", [ctx.MemberName!]);
    }
}

/// <summary>El área de la organización debe ser una de <see cref="Reglas.AreasOrganizacion"/>.</summary>
public class AreaOrganizacionAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;
        return Reglas.AreasOrganizacion.Contains(s.Trim())
            ? ValidationResult.Success
            : new ValidationResult("Elegí un área de la lista", [ctx.MemberName!]);
    }
}
