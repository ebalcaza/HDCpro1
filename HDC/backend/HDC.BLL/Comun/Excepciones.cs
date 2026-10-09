namespace HDC.BLL.Comun;

/// <summary>Errores de negocio; la API los traduce al código HTTP correspondiente.</summary>
public abstract class HdcException(string mensaje) : Exception(mensaje);

/// <summary>400 - Datos inválidos o regla de negocio incumplida.</summary>
public class NegocioException(string mensaje) : HdcException(mensaje);

/// <summary>401 - Credenciales inválidas.</summary>
public class NoAutorizadoException(string mensaje) : HdcException(mensaje);

/// <summary>403 - El usuario no puede operar sobre el recurso.</summary>
public class ProhibidoException(string mensaje = "No tenés permiso para realizar esta acción") : HdcException(mensaje);

/// <summary>404 - El recurso no existe.</summary>
public class NoEncontradoException(string mensaje) : HdcException(mensaje);

/// <summary>409 - Conflicto con datos existentes (duplicados, en uso).</summary>
public class ConflictoException(string mensaje) : HdcException(mensaje);
