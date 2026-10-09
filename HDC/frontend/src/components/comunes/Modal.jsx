import { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'

/** Modal controlado por React con la estética de Bootstrap (animación de entrada/salida). */
export default function Modal({ abierto, onCerrar, titulo, children, pie, tamano, cerrarConFondo = true, centrado = true }) {
  const [montado, setMontado] = useState(abierto)
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    if (abierto) {
      setMontado(true)
      const t = requestAnimationFrame(() => setVisible(true))
      return () => cancelAnimationFrame(t)
    }
    setVisible(false)
    const t = setTimeout(() => setMontado(false), 220)
    return () => clearTimeout(t)
  }, [abierto])

  useEffect(() => {
    if (!abierto) return
    const alTeclear = (e) => { if (e.key === 'Escape' && cerrarConFondo) onCerrar?.() }
    document.addEventListener('keydown', alTeclear)
    document.body.classList.add('modal-open')
    return () => {
      document.removeEventListener('keydown', alTeclear)
      document.body.classList.remove('modal-open')
    }
  }, [abierto, cerrarConFondo, onCerrar])

  if (!montado) return null

  const clasesTamano = tamano === 'sm' ? 'modal-md' : tamano === 'lg' ? 'modal-lg' : tamano === 'xl' ? 'modal-xl' : ''

  return createPortal(
    <>
      <div
        className={`modal fade d-block ${visible ? 'show' : ''}`}
        role="dialog"
        aria-modal="true"
        tabIndex={-1}
        onMouseDown={(e) => { if (e.target === e.currentTarget && cerrarConFondo) onCerrar?.() }}
      >
        <div className={`modal-dialog modal-dialog-scrollable ${centrado ? 'modal-dialog-centered' : ''} ${clasesTamano} modal-fullscreen-sm-down`}>
          <div className="modal-content">
            {titulo && (
              <div className="modal-header">
                <h5 className="modal-title fw-bold">{titulo}</h5>
                <button type="button" className="btn-close" aria-label="Cerrar" onClick={onCerrar} />
              </div>
            )}
            <div className="modal-body">{children}</div>
            {pie && <div className="modal-footer">{pie}</div>}
          </div>
        </div>
      </div>
      <div className={`modal-backdrop fade ${visible ? 'show' : ''}`} />
    </>,
    document.body,
  )
}
