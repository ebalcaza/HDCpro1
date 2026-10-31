import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { authApi } from '../../api/servicios.js'
import { useUI } from '../../context/UIContext.jsx'
import { Campo, errores, reglas, useFormulario } from '../../components/comunes/Formulario.jsx'
import { BotonCarga } from '../../components/comunes/Basicos.jsx'
import PanelLateral from './PanelLateral.jsx'

/**
 * Recuperación de contraseña, paso 2: se llega desde el link del email
 * (/restablecer-contrasena?token=...). El usuario elige la nueva contraseña y el
 * servidor valida que el token exista, no haya vencido y no se haya usado.
 */
export default function RestablecerContrasena() {
  const ui = useUI()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const token = params.get('token') || ''
  // Formato del token: 64 caracteres hexadecimales (si el link llegó cortado, se avisa sin llamar al servidor)
  const tokenValido = /^[0-9a-fA-F]{64}$/.test(token)
  const [verClave, setVerClave] = useState(false)

  // Mismas reglas que el registro y el cambio de contraseña desde el perfil
  const f = useFormulario({ contrasenaNueva: '', confirmacion: '' }, (v) => errores({
    contrasenaNueva: reglas.requerido(v.contrasenaNueva) || reglas.minimo(v.contrasenaNueva, 8) || reglas.contrasena(v.contrasenaNueva),
    confirmacion: v.confirmacion !== v.contrasenaNueva ? 'Las contraseñas no coinciden' : null,
  }))

  const enviar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return
    f.setEnviando(true)
    try {
      const r = await authApi.restablecerContrasena(token, f.valores.contrasenaNueva)
      ui.exito(r.mensaje, 'Contraseña actualizada')
      navigate('/login', { replace: true })
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
          <h2 className="mb-1">Crear nueva contraseña</h2>
          {!tokenValido ? (
            <div className="tarjeta tarjeta-cuerpo p-4 mt-3 d-flex flex-column gap-3">
              <div className="d-flex gap-3 align-items-start">
                <i className="bi bi-exclamation-triangle fs-2 text-warning" />
                <p className="mb-0">El link no es válido. Copialo completo desde el email o pedí uno nuevo.</p>
              </div>
              <Link to="/olvide-contrasena" className="btn btn-primary">Pedir un link nuevo</Link>
            </div>
          ) : (
            <>
              <p className="text-muted mb-4">Elegí una contraseña nueva para tu cuenta.</p>
              <form noValidate onSubmit={enviar} className="tarjeta tarjeta-cuerpo p-4 d-flex flex-column gap-3">
                <Campo etiqueta="Nueva contraseña" requerido error={f.errores.contrasenaNueva} tocado={f.tocados.contrasenaNueva}
                  ayuda="Mínimo 8 caracteres, con letras y números, sin espacios">
                  <div className="position-relative input-con-boton">
                    <input type={verClave ? 'text' : 'password'} autoComplete="new-password" autoFocus {...f.campo('contrasenaNueva')} />
                    <button type="button" className="btn btn-icono position-absolute" style={{ right: 4, top: 3 }}
                      onClick={() => setVerClave((v) => !v)} aria-label={verClave ? 'Ocultar contraseña' : 'Mostrar contraseña'}>
                      <i className={`bi ${verClave ? 'bi-eye-slash' : 'bi-eye'}`} />
                    </button>
                  </div>
                </Campo>
                <Campo etiqueta="Repetí la contraseña" requerido error={f.errores.confirmacion} tocado={f.tocados.confirmacion}>
                  <input type={verClave ? 'text' : 'password'} autoComplete="new-password" {...f.campo('confirmacion')} />
                </Campo>
                <BotonCarga cargando={f.enviando} className="btn btn-primary btn-lg mt-2">Guardar contraseña</BotonCarga>
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
