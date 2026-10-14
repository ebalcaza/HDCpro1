import { useLayoutEffect, useRef } from 'react'

/*
 * Input numérico con formato argentino que agrega SOLO los puntos de miles mientras se escribe:
 *   el usuario escribe 20000   -> ve "20.000"
 *   el usuario escribe 20000,5 -> ve "20.000,5"
 * Así no hay dudas de si hay que poner puntos o no: los puntos los pone el sistema y
 * la coma (,) es la única forma de escribir decimales.
 *   - La tecla punto (.) se ignora: si alguien escribe "20.000", queda 20.000 igual.
 *   - Letras y otros símbolos ("20k", "20mil") no se pueden escribir.
 *   - Al pegar "1500.75" (punto decimal con 1 o 2 decimales) se interpreta como 1.500,75.
 *
 * El componente recibe y devuelve el valor "canónico" (texto con punto decimal y sin miles,
 * ej: "20000.5"), que se convierte directo con Number(valor) para enviarlo a la API.
 */

/** Decimales permitidos: DETALLE_REGISTRO.CONSUMO es DECIMAL(18,4) */
const DECIMALES = 4
/** Dígitos enteros permitidos: el backend acepta hasta 999.999.999 */
const ENTEROS = 9

/** "20000.5" -> "20.000,5" */
export const aTextoMiles = (canonico) => {
  if (canonico === '' || canonico == null) return ''
  const [ent, dec] = String(canonico).split('.')
  const entero = ent.replace(/\B(?=(\d{3})+(?!\d))/g, '.')
  return dec === undefined ? entero : `${entero},${dec}`
}

/** "20.000,5" -> "20000.5" (descarta todo lo que no sea dígito o la primera coma) */
const aCanonico = (texto) => {
  const limpio = texto.replace(/[^\d,]/g, '')
  const coma = limpio.indexOf(',')
  let ent = (coma === -1 ? limpio : limpio.slice(0, coma)).replace(/^0+(?=\d)/, '').slice(0, ENTEROS)
  const dec = coma === -1 ? undefined : limpio.slice(coma + 1).replace(/,/g, '').slice(0, DECIMALES)
  if (ent === '' && dec !== undefined) ent = '0'
  return dec === undefined ? ent : `${ent}.${dec}`
}

/** Cantidad de dígitos y comas que hay antes de la posición `pos` (sirve para no perder el cursor) */
const significativosAntes = (texto, pos) => texto.slice(0, pos).replace(/[^\d,]/g, '').length

export default function InputMiles({ valor, onChange, className = 'form-control', ...resto }) {
  const ref = useRef(null)
  const cursor = useRef(null)
  const texto = aTextoMiles(valor)

  // Después de reformatear, se vuelve a poner el cursor después del mismo dígito que antes
  useLayoutEffect(() => {
    if (cursor.current == null || !ref.current) return
    let cuenta = 0, pos = 0
    while (pos < texto.length && cuenta < cursor.current) {
      if (/[\d,]/.test(texto[pos])) cuenta++
      pos++
    }
    ref.current.setSelectionRange(pos, pos)
    cursor.current = null
  })

  const actualizar = (nuevoTexto, posCursor) => {
    cursor.current = significativosAntes(nuevoTexto, posCursor)
    onChange(aCanonico(nuevoTexto))
  }

  return (
    <input
      ref={ref}
      type="text"
      inputMode="decimal"
      autoComplete="off"
      className={className}
      value={texto}
      onChange={(e) => actualizar(e.target.value, e.target.selectionStart)}
      // El punto lo pone el sistema: se ignora para que "20.000" no se confunda con decimales
      onKeyDown={(e) => { if (e.key === '.') e.preventDefault() }}
      onPaste={(e) => {
        const pegado = e.clipboardData.getData('text').trim()
        if (/^\d+\.\d{1,2}$/.test(pegado)) { // "1500.75" -> punto decimal
          e.preventDefault()
          onChange(aCanonico(pegado.replace('.', ',')))
        }
      }}
      {...resto}
    />
  )
}
