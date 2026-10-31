import { useState } from 'react'
import { Link } from 'react-router-dom'
import { authApi } from '../../api/servicios.js'
import { useUI } from '../../context/UIContext.jsx'
import { Campo, errores, reglas, useFormulario } from '../../components/comunes/Formulario.jsx'
import { BotonCarga } from '../../components/comunes/Basicos.jsx'
import PanelLateral from './PanelLateral.jsx'

/**
 * Recuperación de contraseña, paso 1: el usuario escribe su email y se le envía un link.
 * El servidor responde siempre el mismo mensaje (exista o no la cuenta), por eso
 * acá se muestra la confirmación sin indicar si el email está registrado.
 */
export default function OlvideContrasena() {
  const ui = useUI()
  // Mensaje de confirmación del servidor; mientras sea null se muestra el formulario
  const [enviado, setEnviado] = useState(null)
  const f = useFormulario({ email: '' }, (v) => errores({
    email: reglas.requerido(v.email, 'Ingresá tu email') || reglas.email(v.email),
  }))

  const enviar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return
    f.setEnviando(true)
    try {
      const r = await authApi.solicitarRecuperacion(f.valores.email.trim())
      setEnviado(r.mensaje)
    } catch (err) {
      ui.error(err)
      f.aplicarErroresServidor(err.errores)
    } finally {
      f.setEnviando(false)
    }
  }

  return (
    <div className="auth-pagina">
      <PanelLateral />
      <section className="auth-form">
        <div className="auth-caja aparecer">
          <h2 className="mb-1">¿Olvidaste tu contraseña?</h2>
          {enviado ? (
            <div className="tarjeta tarjeta-cuerpo p-4 mt-3 d-flex flex-column gap-3">
              <div className="d-flex gap-3 align-items-start">
                <i className="bi bi-envelope-check fs-2 text-success" />
                <p className="mb-0">{enviado}</p>
              </div>
              <p className="text-muted small mb-0">
                El link vence en unos minutos y sirve una sola vez. Si no te llega, podés pedir otro.
              </p>
              <button type="button" className="btn btn-outline-secondary" onClick={() => setEnviado(null)}>
                Enviar de nuevo
              </button>
            </div>
          ) : (
            <>
              <p className="text-muted mb-4">Escribí el email de tu cuenta y te enviamos un link para crear una nueva contraseña.</p>
              <form noValidate onSubmit={enviar} className="tarjeta tarjeta-cuerpo p-4 d-flex flex-column gap-3">
                <Campo etiqueta="Email" requerido error={f.errores.email} tocado={f.tocados.email} exito={null}>
                  <input type="email" autoComplete="email" placeholder="nombre@correo.com" autoFocus {...f.campo('email')} />
                </Campo>
                <BotonCarga cargando={f.enviando} className="btn btn-primary btn-lg mt-2">Enviar link</BotonCarga>
              </form>
            </>
          )}
          <p className="text-center mt-4 mb-0">
            <Link to="/login" className="fw-bold"><i className="bi bi-arrow-left me-1" />Volver a iniciar sesión</Link>
          </p>
        </div>
      </section>
    </div>
  )
}
