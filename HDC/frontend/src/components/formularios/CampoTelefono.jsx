import { Campo } from '../comunes/Formulario.jsx'

/*
 * Teléfono argentino con código de país fijo (+54) y característica según la zona.
 *
 * El valor se guarda como un solo texto: "+54 {característica} {número}", ej: "+54 11 45678901".
 *   - Característica: código de área SIN el 0 (11 = CABA/GBA, 351 = Córdoba...).
 *   - Número: SIN el 15. Característica + número = 10 dígitos (regla `reglas.telefono`).
 * Si el número queda vacío se considera "sin teléfono" (el campo es opcional).
 */

/**
 * Característica principal de cada provincia. La clave es el id de provincia de georef
 * (código INDEC), que es el mismo id que usa la tabla PROVINCIA.
 * Es la de la ciudad más poblada: el usuario la puede cambiar si vive en otra localidad.
 */
export const CARACTERISTICAS = {
  2: '11', // Ciudad Autónoma de Buenos Aires
  6: '11', // Buenos Aires (GBA)
  10: '383', // Catamarca
  14: '351', // Córdoba
  18: '379', // Corrientes
  22: '362', // Chaco
  26: '280', // Chubut
  30: '343', // Entre Ríos
  34: '370', // Formosa
  38: '388', // Jujuy
  42: '2954', // La Pampa
  46: '380', // La Rioja
  50: '261', // Mendoza
  54: '376', // Misiones
  58: '299', // Neuquén
  62: '2920', // Río Negro
  66: '387', // Salta
  70: '264', // San Juan
  74: '266', // San Luis
  78: '2966', // Santa Cruz
  82: '341', // Santa Fe
  86: '385', // Santiago del Estero
  90: '381', // Tucumán
  94: '2901', // Tierra del Fuego
}

/**
 * "+54 11 45678901" -> { area: '11', numero: '45678901' }.
 * Teléfonos viejos guardados sin este formato pasan enteros al número para que el usuario los corrija.
 */
export const separarTelefono = (v) => {
  if (!v) return { area: '', numero: '' }
  const m = /^\+54 (\d*) (\d*)$/.exec(v)
  return m ? { area: m[1], numero: m[2] } : { area: '', numero: String(v).replace(/\D/g, '') }
}

const unirTelefono = (area, numero) => (!area && !numero ? '' : `+54 ${area} ${numero}`)

/** Valor a enviar a la API: null si no se cargó el número */
export const telefonoARequest = (v) => {
  const { area, numero } = separarTelefono(v)
  return numero ? unirTelefono(area, numero) : null
}

/** Devuelve el teléfono con la característica de la provincia, solo si todavía no tenía una */
export const completarCaracteristica = (telefono, idProvincia) => {
  const { area, numero } = separarTelefono(telefono)
  const sugerida = CARACTERISTICAS[Number(idProvincia)]
  return area || !sugerida ? telefono : unirTelefono(sugerida, numero)
}

/** Campo de teléfono para usar con useFormulario. `nombre` es la clave del valor en el formulario. */
export default function CampoTelefono({ f, nombre = 'telefono', etiqueta = 'Teléfono', className = 'col-md-6' }) {
  const { area, numero } = separarTelefono(f.valores[nombre])
  const { onBlur, 'aria-invalid': invalido } = f.campo(nombre)
  const clase = f.claseValidacion(nombre)

  return (
    <Campo className={className} etiqueta={etiqueta} error={f.errores[nombre]} tocado={f.tocados[nombre]} exito={null}
      ayuda="Característica sin 0 y número sin 15 (ej: 11 · 45678901)">
      <div className="input-group">
        <span className="input-group-text">+54</span>
        {/* Característica: solo dígitos, sin ceros iniciales (el 0 es para llamadas nacionales) */}
        <input type="tel" inputMode="numeric" className={clase} style={{ maxWidth: 90 }} placeholder="11" maxLength={4}
          aria-label="Característica" aria-invalid={invalido} value={area} onBlur={onBlur}
          onChange={(e) => f.cambiar(nombre, unirTelefono(e.target.value.replace(/\D/g, '').replace(/^0+/, ''), numero))} />
        <input type="tel" inputMode="numeric" autoComplete="tel-local" className={clase} placeholder="45678901" maxLength={8}
          aria-label="Número" aria-invalid={invalido} value={numero} onBlur={onBlur}
          onChange={(e) => f.cambiar(nombre, unirTelefono(area, e.target.value.replace(/\D/g, '')))} />
      </div>
    </Campo>
  )
}
