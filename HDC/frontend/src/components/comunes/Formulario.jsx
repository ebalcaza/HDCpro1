import { useCallback, useState } from 'react'

/**
 * Estado de formulario con validación estilo Bootstrap:
 * los campos tocados muestran is-valid (verde) o is-invalid (rojo).
 * `validar(valores)` devuelve { campo: 'mensaje' } con los errores.
 */
export function useFormulario(inicial, validar) {
  const [valores, setValores] = useState(inicial)
  const [tocados, setTocados] = useState({})
  const [erroresServidor, setErroresServidor] = useState({})
  const [enviando, setEnviando] = useState(false)

  const errores = { ...(validar ? validar(valores) : {}), ...erroresServidor }

  const cambiar = useCallback((campo, valor) => {
    setValores((v) => ({ ...v, [campo]: valor }))
    setTocados((t) => ({ ...t, [campo]: true }))
    setErroresServidor((e) => {
      if (!e[campo]) return e
      const { [campo]: _, ...resto } = e
      return resto
    })
  }, [])

  /** Props para inputs simples: value, onChange, onBlur y clase de validación */
  const campo = (nombre, { tipo } = {}) => ({
    name: nombre,
    value: valores[nombre] ?? '',
    onChange: (e) => cambiar(nombre, tipo === 'numero' ? (e.target.value === '' ? '' : e.target.value) : e.target.value),
    onBlur: () => setTocados((t) => ({ ...t, [nombre]: true })),
    className: claseValidacion(nombre),
    'aria-invalid': tocados[nombre] && errores[nombre] ? true : undefined,
  })

  function claseValidacion(nombre, base) {
    const b = base ?? 'form-control'
    if (!tocados[nombre]) return b
    return `${b} ${errores[nombre] ? 'is-invalid' : 'is-valid'}`
  }

  /** Marca todo como tocado; devuelve true si no hay errores */
  const validarTodo = () => {
    const todos = Object.keys({ ...valores, ...errores }).reduce((a, k) => ({ ...a, [k]: true }), {})
    setTocados(todos)
    return Object.keys(errores).length === 0
  }

  /** Mapea los errores de validación de la API ({ campo: [msg] }) a los campos */
  const aplicarErroresServidor = (errs) => {
    if (!errs) return
    const mapa = {}
    for (const [k, v] of Object.entries(errs)) {
      const clave = k.split('.').pop()
      mapa[clave.charAt(0).toLowerCase() + clave.slice(1)] = Array.isArray(v) ? v[0] : v
    }
    setErroresServidor(mapa)
    setTocados((t) => ({ ...t, ...Object.keys(mapa).reduce((a, k) => ({ ...a, [k]: true }), {}) }))
  }

  const reiniciar = (nuevos = inicial) => { setValores(nuevos); setTocados({}); setErroresServidor({}) }

  return {
    valores, setValores, errores, tocados, cambiar, campo, claseValidacion,
    validarTodo, aplicarErroresServidor, reiniciar, enviando, setEnviando,
  }
}

/** Contenedor de campo con etiqueta y mensajes de validación */
export function Campo({ etiqueta, requerido, error, tocado, ayuda, children, className = '', exito = 'Correcto' }) {
  return (
    <div className={className}>
      {etiqueta && (
        <label className="form-label">
          {etiqueta}{requerido && <span className="req" aria-hidden="true">*</span>}
        </label>
      )}
      {children}
      {tocado && error && <div className="invalid-feedback d-block">{error}</div>}
      {tocado && !error && exito && <div className="valid-feedback d-block">{exito}</div>}
      {ayuda && !(tocado && error) && <div className="form-text">{ayuda}</div>}
    </div>
  )
}

/*
 * =====================================================================================
 *  VALIDADORES REUTILIZABLES DEL FRONTEND
 * =====================================================================================
 *  Cada regla recibe el valor del campo y devuelve:
 *    - null            -> el valor es válido
 *    - 'mensaje'       -> el valor es inválido; el mensaje se muestra debajo del campo
 *  Se combinan con || para encadenarlas: reglas.requerido(v) || reglas.calle(v)
 *  (se muestra el primer error que aparezca).
 *
 *  IMPORTANTE: el backend repite estas mismas reglas en
 *  backend/HDC.Entity/Validaciones/Reglas.cs. El frontend avisa rápido al usuario,
 *  pero la validación que manda es la del backend. Si cambiás una, cambiá la otra.
 *
 *  Largos máximos = largo de la columna en la base de datos (01_esquema.sql).
 * =====================================================================================
 */

// Letras del español (con acentos, diéresis y ñ). Se reutiliza en varias reglas.
const LETRAS = 'A-Za-zÁÉÍÓÚÜÑáéíóúüñ'

/** Largos máximos de las columnas en la BD */
export const LARGOS = { nombres: 200, apellidos: 250, calle: 200, area: 100, contrasena: 100 }

/** Opciones del select "Área" de las organizaciones (mismo listado que Reglas.AreasOrganizacion en el backend) */
export const AREAS_ORGANIZACION = [
  'Educación', 'Industria', 'Energía', 'Agro', 'Comercio', 'Servicios', 'Salud',
  'Construcción', 'Transporte', 'Tecnología', 'Gobierno', 'ONG', 'Otra',
]

export const reglas = {
  requerido: (v, msg = 'Campo obligatorio') => (v === null || v === undefined || String(v).trim() === '' ? msg : null),
  email: (v) => (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(v || '').trim()) ? 'Ingresá un email válido' : null),
  minimo: (v, n) => (String(v || '').length < n ? `Debe tener al menos ${n} caracteres` : null),
  maximo: (v, n) => (String(v || '').trim().length > n ? `Máximo ${n} caracteres` : null),
  dni: (v) => (!/^\d{7,8}$/.test(String(v || '')) ? 'El DNI debe tener 7 u 8 dígitos, sin puntos' : null),
  cuit: (v) => (!/^\d{2}-?\d{8}-?\d$/.test(String(v || '')) ? 'Formato: 20-12345678-9' : null),
  positivo: (v) => (v === '' || v == null || isNaN(Number(v)) || Number(v) <= 0 ? 'Debe ser un número mayor a 0' : null),
  noNegativo: (v) => (v === '' || v == null || isNaN(Number(v)) || Number(v) < 0 ? 'Debe ser un número mayor o igual a 0' : null),

  /**
   * Nombres y apellidos: solo letras, espacios y el apóstrofe (') como único símbolo
   * (ej: "O'Connor"). No puede empezar/terminar con apóstrofe ni tener dos seguidos.
   * Los espacios de más NO dan error: se corrigen solos con `normalizarNombre` al guardar.
   */
  nombrePersona: (v) => (v && !new RegExp(`^\\s*[${LETRAS}]+(?:'[${LETRAS}]+|\\s+[${LETRAS}]+)*\\s*$`).test(v)
    ? "Solo letras, espacios y apóstrofe (')" : null),

  /**
   * Calles: letras, números y espacios simples (ej: "Avenida 9 de Julio").
   * Da error con espacios dobles; los espacios al inicio/final se quitan al guardar.
   */
  calle: (v) => {
    const s = String(v || '').trim()
    if (!s) return null
    if (/\s{2,}/.test(s)) return 'No se permiten espacios consecutivos'
    return new RegExp(`^[${LETRAS}0-9 ]+$`).test(s) ? null : 'Solo letras, números y espacios'
  },

  /** Contraseña: sin espacios y con al menos una letra y un número (el mínimo de 8 va con `minimo`) */
  contrasena: (v) => {
    const s = String(v || '')
    if (/\s/.test(s)) return 'La contraseña no puede tener espacios'
    if (!new RegExp(`[${LETRAS}]`).test(s) || !/\d/.test(s)) return 'Debe combinar letras y números'
    return null
  },

  /**
   * Teléfono argentino "+54 {característica} {número}" (lo arma el componente CampoTelefono).
   * Característica sin 0 y número sin 15: entre los dos suman 10 dígitos.
   * Sin número cargado = sin teléfono (es opcional), aunque la característica esté precargada.
   */
  telefono: (v) => {
    if (!v || /^\+54 \d* $/.test(v)) return null
    const m = /^\+54 (\d{2,4}) (\d{6,8})$/.exec(v)
    return m && m[1].length + m[2].length === 10 ? null : 'Característica + número deben sumar 10 dígitos'
  },

  // ---------------------------------------------------------------------------------
  //  ESPACIO PARA TUS PROPIAS REGLAS
  //  Copiá este molde, cambiale el nombre y la condición:
  //
  //  miRegla: (v) => (CONDICION_DE_ERROR ? 'Mensaje para el usuario' : null),
  //
  //  Después usala en el validador del formulario: campo: reglas.requerido(v.campo) || reglas.miRegla(v.campo)
  // ---------------------------------------------------------------------------------
}

/**
 * Deja un nombre listo para guardar: sin espacios al inicio/final, sin espacios
 * contiguos y con mayúscula inicial en cada palabra ("juan  o'connor " -> "Juan O'Connor").
 * El backend hace lo mismo (Reglas.NormalizarNombre), esto es para que el usuario lo vea.
 */
export const normalizarNombre = (v) => String(v || '')
  .trim()
  .split(/\s+/)
  .filter(Boolean)
  .map((palabra) => palabra.split("'").map((p) => p.charAt(0).toLocaleUpperCase('es-AR') + p.slice(1).toLocaleLowerCase('es-AR')).join("'"))
  .join(' ')

/** Props para un input de nombre/apellido: igual que f.campo, pero al salir del campo lo normaliza */
export const campoNombre = (f, nombre, largoMaximo) => ({
  ...f.campo(nombre),
  maxLength: largoMaximo,
  onBlur: () => f.cambiar(nombre, normalizarNombre(f.valores[nombre])),
})

/** Validación completa de un nombre/apellido obligatorio */
export const validarNombre = (v, largoMaximo) => reglas.requerido(v) || reglas.nombrePersona(v) || reglas.maximo(normalizarNombre(v), largoMaximo)

/** Junta los errores no nulos en un objeto */
export const errores = (mapa) => Object.fromEntries(Object.entries(mapa).filter(([, v]) => v))
