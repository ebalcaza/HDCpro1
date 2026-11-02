import { useEffect, useRef } from 'react'

/**
 * Lista de fuentes de emisión siempre visible: se elige con un click, sin escribir ni filtrar.
 * Muestra solo las fuentes que recibe en `opciones` (las que el usuario puede cargar en ese domicilio).
 * Valor: idFuenteEmision (number) o ''.
 *
 * - usadas: ids elegidos en otras filas; se ven deshabilitados para no repetir una fuente.
 * - nombreActual: nombre de la fuente guardada, para no perderla al editar si fue dada de baja.
 * - mensajeVacio: texto que se muestra cuando no hay ninguna fuente para elegir.
 */
export default function SelectorFuente({ id, etiqueta, opciones, valor, nombreActual, usadas, onChange, invalido, requerido, mensajeVacio }) {
  const lista = useRef(null)
  const idElegido = Number(valor)
  // La fuente guardada ya no está disponible: se agrega al principio para que se siga viendo cuál era
  const fuenteDeBaja = !!idElegido && !!nombreActual && !opciones.some((o) => o.idFuenteEmision === idElegido)
  const hayOpciones = opciones.length > 0 || fuenteDeBaja

  // Si la fuente elegida quedó fuera de la parte visible (ej: al editar), se desplaza la lista hasta ella
  useEffect(() => {
    const elegida = lista.current?.querySelector('input:checked')?.closest('label')
    if (elegida) lista.current.scrollTop = elegida.offsetTop
  }, [hayOpciones])

  return (
    <div>
      <div id={`${id}-titulo`} className="form-label small">{etiqueta}{requerido && <span className="req">*</span>}</div>
      {!hayOpciones ? (
        <div className={`lista-fuentes-vacia ${invalido ? 'invalida' : ''}`}>{mensajeVacio}</div>
      ) : (
        <div ref={lista} role="radiogroup" aria-labelledby={`${id}-titulo`} aria-required={requerido} className={`lista-fuentes ${invalido ? 'invalida' : ''}`}>
          {fuenteDeBaja && (
            <label className="opcion-fuente activa">
              <input type="radio" className="form-check-input" name={id} checked readOnly />
              <span className="flex-grow-1">{nombreActual}</span>
              <span className="small text-muted">Ya no disponible</span>
            </label>
          )}
          {opciones.map((o) => {
            const elegida = o.idFuenteEmision === idElegido
            const enOtraFila = !elegida && usadas?.has(o.idFuenteEmision)
            return (
              <label key={o.idFuenteEmision} className={`opcion-fuente ${elegida ? 'activa' : ''} ${enOtraFila ? 'deshabilitada' : ''}`}>
                <input type="radio" className="form-check-input" name={id} checked={elegida} disabled={enOtraFila} onChange={() => onChange(o.idFuenteEmision)} />
                <span className="flex-grow-1">{o.nombre}</span>
                <span className="small text-muted text-end">{enOtraFila ? 'Ya está en otro consumo' : `${o.tipoEmision} · ${o.unidadMedida}`}</span>
              </label>
            )
          })}
        </div>
      )}
    </div>
  )
}
