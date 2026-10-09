import { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react'
import Modal from '../components/comunes/Modal.jsx'

const UIContext = createContext(null)

const ICONOS_TOAST = { exito: 'bi-check-circle-fill', error: 'bi-exclamation-octagon-fill', aviso: 'bi-info-circle-fill' }
const ESTILOS_CONFIRMACION = {
  peligro: { icono: 'bi-trash3', color: 'var(--bs-danger)', boton: 'btn-danger' },
  aviso: { icono: 'bi-exclamation-triangle', color: 'var(--marron)', boton: 'btn-primary' },
  info: { icono: 'bi-question-circle', color: 'var(--azul)', boton: 'btn-primary' },
}

export function UIProvider({ children }) {
  const [toasts, setToasts] = useState([])
  const [confirmacion, setConfirmacion] = useState(null)
  const [opcion, setOpcion] = useState(null)
  const resolver = useRef(null)

  const cerrarToast = useCallback((id) => setToasts((t) => t.filter((x) => x.id !== id)), [])

  const notificar = useCallback((tipo, mensaje, titulo) => {
    const id = Math.random().toString(36).slice(2)
    setToasts((t) => [...t.slice(-3), { id, tipo, mensaje, titulo }])
    setTimeout(() => cerrarToast(id), tipo === 'error' ? 7000 : 4500)
  }, [cerrarToast])

  /**
   * Abre un modal de confirmación. Devuelve una promesa con:
   * - true / false si no hay opciones
   * - el valor de la opción elegida (obligatorio elegir una) o null si se cancela
   */
  const confirmar = useCallback((cfg) => new Promise((resolve) => {
    resolver.current = resolve
    setOpcion(null)
    setConfirmacion({ tipo: 'aviso', textoConfirmar: 'Confirmar', ...cfg })
  }), [])

  const responder = (valor) => {
    resolver.current?.(valor)
    resolver.current = null
    setConfirmacion(null)
  }

  const ui = useMemo(() => ({
    exito: (m, t) => notificar('exito', m, t ?? 'Listo'),
    error: (m, t) => notificar('error', typeof m === 'string' ? m : m?.message || 'Ocurrió un error', t ?? 'No se pudo completar'),
    aviso: (m, t) => notificar('aviso', m, t ?? 'Atención'),
    confirmar,
  }), [notificar, confirmar])

  const est = confirmacion ? ESTILOS_CONFIRMACION[confirmacion.tipo] || ESTILOS_CONFIRMACION.aviso : null
  const requiereOpcion = confirmacion?.opciones?.length > 0

  return (
    <UIContext.Provider value={ui}>
      {children}

      <div className="toast-contenedor" role="status" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className={`toast-hdc ${t.tipo}`}>
            <i className={`bi ${ICONOS_TOAST[t.tipo]} icono`} aria-hidden="true" />
            <div className="flex-grow-1">
              <div className="fw-bold">{t.titulo}</div>
              <div className="small texto-2">{t.mensaje}</div>
            </div>
            <button type="button" className="btn-close btn-sm" aria-label="Cerrar" onClick={() => cerrarToast(t.id)} />
          </div>
        ))}
      </div>

      <Modal
        abierto={!!confirmacion}
        onCerrar={() => responder(requiereOpcion ? null : false)}
        tamano="sm"
        cerrarConFondo={!confirmacion?.obligatorio}
        pie={confirmacion && (
          <>
            {!confirmacion.obligatorio && (
              <button type="button" className="btn btn-outline-secondary" onClick={() => responder(requiereOpcion ? null : false)}>
                Cancelar
              </button>
            )}
            <button
              type="button"
              className={`btn ${est.boton}`}
              disabled={requiereOpcion && opcion == null}
              onClick={() => responder(requiereOpcion ? opcion : true)}
            >
              {confirmacion.textoConfirmar}
            </button>
          </>
        )}
      >
        {confirmacion && (
          <div>
            <div className="modal-icono" style={{ background: `color-mix(in srgb, ${est.color} 14%, transparent)`, color: est.color }}>
              <i className={`bi ${est.icono}`} />
            </div>
            <h5 className="mb-2">{confirmacion.titulo}</h5>
            {confirmacion.mensaje && <p className="texto-2 mb-0">{confirmacion.mensaje}</p>}
            {requiereOpcion && (
              <div className="d-flex flex-column gap-2 mt-3" role="radiogroup">
                {confirmacion.opciones.map((o) => (
                  <label key={String(o.valor)} className={`opcion-modal ${opcion === o.valor ? 'seleccionada' : ''}`}>
                    <input
                      type="radio"
                      className="form-check-input mt-1"
                      name="opcion-confirmacion"
                      checked={opcion === o.valor}
                      onChange={() => setOpcion(o.valor)}
                    />
                    <span>
                      <span className="fw-bold d-block">{o.titulo}</span>
                      {o.descripcion && <span className="small texto-2">{o.descripcion}</span>}
                    </span>
                  </label>
                ))}
                <small className="text-muted"><i className="bi bi-info-circle me-1" />Elegí una opción para continuar.</small>
              </div>
            )}
          </div>
        )}
      </Modal>
    </UIContext.Provider>
  )
}

export const useUI = () => useContext(UIContext)
