import { useEffect, useId, useRef, useState } from 'react'

const MAX_SUGERENCIAS = 8

// Compara sin mayúsculas ni tildes: "electrica" encuentra "Energía eléctrica"
const normalizar = (s) => (s || '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim()

/**
 * Input con sugerencias de fuentes de emisión a medida que se escribe.
 * Solo se acepta una fuente de la lista: si el texto no coincide con ninguna, el valor queda vacío.
 * Valor: idFuenteEmision (number) o ''.
 */
export default function BuscadorFuente({ id, etiqueta, opciones, valor, nombreActual, onChange, className = '', requerido }) {
  const idLista = useId()
  const [texto, setTexto] = useState('')
  const [abierto, setAbierto] = useState(false)
  const [activo, setActivo] = useState(0)
  const lista = useRef(null)

  const elegida = opciones.find((o) => o.idFuenteEmision === Number(valor))

  // Al cargar un registro para editar (o al elegir una sugerencia) el texto muestra el nombre de la fuente
  useEffect(() => {
    if (valor) setTexto(elegida?.nombre || nombreActual || '')
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [valor])

  const buscado = normalizar(texto)
  const sugerencias = (elegida && texto === elegida.nombre ? opciones : opciones.filter((o) => normalizar(o.nombre).includes(buscado)))
    .slice(0, MAX_SUGERENCIAS)

  const elegir = (o) => {
    setTexto(o.nombre)
    setAbierto(false)
    onChange(o.idFuenteEmision)
  }

  const escribir = (e) => {
    const t = e.target.value
    setTexto(t)
    setAbierto(true)
    setActivo(0)
    const exacta = opciones.find((o) => normalizar(o.nombre) === normalizar(t))
    onChange(exacta ? exacta.idFuenteEmision : '')
  }

  const teclado = (e) => {
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      e.preventDefault()
      if (!abierto) return setAbierto(true)
      const n = sugerencias.length
      if (n) setActivo((a) => (a + (e.key === 'ArrowDown' ? 1 : n - 1)) % n)
    } else if (e.key === 'Enter' && abierto && sugerencias[activo]) {
      e.preventDefault()
      elegir(sugerencias[activo])
    } else if (e.key === 'Escape') {
      setAbierto(false)
    }
  }

  useEffect(() => {
    lista.current?.children[activo]?.scrollIntoView({ block: 'nearest' })
  }, [activo])

  return (
    <div className="position-relative">
      <label className="form-label small" htmlFor={id}>{etiqueta}{requerido && <span className="req">*</span>}</label>
      <input
        id={id}
        type="text"
        className={`form-control form-control-sm ${className}`}
        role="combobox"
        aria-expanded={abierto && sugerencias.length > 0}
        aria-controls={idLista}
        aria-autocomplete="list"
        aria-activedescendant={abierto && sugerencias[activo] ? `${idLista}-${activo}` : undefined}
        autoComplete="off"
        placeholder="Escribí para buscar, ej: gas natural"
        value={texto}
        onChange={escribir}
        onFocus={() => setAbierto(true)}
        onBlur={() => setAbierto(false)}
        onKeyDown={teclado}
      />
      {abierto && (
        <ul ref={lista} id={idLista} role="listbox" className="sugerencias">
          {sugerencias.length === 0 ? (
            <li className="sugerencia-vacia">No hay fuentes que coincidan</li>
          ) : sugerencias.map((o, i) => (
            <li key={o.idFuenteEmision} id={`${idLista}-${i}`} role="option" aria-selected={i === activo}
              className={`sugerencia ${i === activo ? 'activa' : ''}`}
              onMouseDown={(e) => { e.preventDefault(); elegir(o) }}
              onMouseEnter={() => setActivo(i)}>
              <span>{o.nombre}</span>
              <span className="small text-muted">{o.tipoEmision} · {o.unidadMedida}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
