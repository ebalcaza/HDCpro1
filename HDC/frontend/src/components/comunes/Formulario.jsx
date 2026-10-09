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

// Validadores reutilizables
export const reglas = {
  requerido: (v, msg = 'Campo obligatorio') => (v === null || v === undefined || String(v).trim() === '' ? msg : null),
  email: (v) => (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(v || '').trim()) ? 'Ingresá un email válido' : null),
  minimo: (v, n) => (String(v || '').length < n ? `Debe tener al menos ${n} caracteres` : null),
  dni: (v) => (!/^\d{7,8}$/.test(String(v || '')) ? 'El DNI debe tener 7 u 8 dígitos, sin puntos' : null),
  cuit: (v) => (!/^\d{2}-?\d{8}-?\d$/.test(String(v || '')) ? 'Formato: 20-12345678-9' : null),
  patente: (v) => (!/^([A-Za-z]{3}\s?\d{3}|[A-Za-z]{2}\s?\d{3}\s?[A-Za-z]{2}|[A-Za-z]\d{3}[A-Za-z]{3}|\d{3}[A-Za-z]{3})$/.test(String(v || '').trim())
    ? 'Patente inválida (ej: ABC123 o AB123CD)' : null),
  positivo: (v) => (v === '' || v == null || isNaN(Number(v)) || Number(v) <= 0 ? 'Debe ser un número mayor a 0' : null),
  noNegativo: (v) => (v === '' || v == null || isNaN(Number(v)) || Number(v) < 0 ? 'Debe ser un número mayor o igual a 0' : null),
  telefono: (v) => (v && !/^[\d\s()+-]{6,30}$/.test(v) ? 'Teléfono inválido' : null),
}

/** Junta los errores no nulos en un objeto */
export const errores = (mapa) => Object.fromEntries(Object.entries(mapa).filter(([, v]) => v))
