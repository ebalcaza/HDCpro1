import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { rutaInicio, useAuth } from '../../context/AuthContext.jsx'
import { useUI } from '../../context/UIContext.jsx'
import { Campo, errores, reglas, useFormulario } from '../../components/comunes/Formulario.jsx'
import { BotonCarga } from '../../components/comunes/Basicos.jsx'
import PanelLateral from './PanelLateral.jsx'

export default function Login() {
  const { login } = useAuth()
  const ui = useUI()
  const navigate = useNavigate()
  const [verClave, setVerClave] = useState(false)
  const f = useFormulario({ email: '', contrasena: '' }, (v) => errores({
    email: reglas.requerido(v.email, 'Ingresá tu email') || reglas.email(v.email),
    contrasena: reglas.requerido(v.contrasena, 'Ingresá tu contraseña'),
  }))

  const enviar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return
    f.setEnviando(true)
    try {
      const u = await login(f.valores)
      ui.exito(`¡Hola, ${u.nombreMostrar}!`, 'Sesión iniciada')
      navigate(rutaInicio(u), { replace: true })
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
          <h2 className="mb-1">Iniciar sesión</h2>
          <p className="text-muted mb-4">Ingresá con tu email y contraseña.</p>
          <form noValidate onSubmit={enviar} className="tarjeta tarjeta-cuerpo p-4 d-flex flex-column gap-3">
            <Campo etiqueta="Email" requerido error={f.errores.email} tocado={f.tocados.email} exito={null}>
              <input type="email" autoComplete="email" placeholder="nombre@correo.com" autoFocus {...f.campo('email')} />
            </Campo>
            <Campo etiqueta="Contraseña" requerido error={f.errores.contrasena} tocado={f.tocados.contrasena} exito={null}>
              <div className="position-relative">
                <input type={verClave ? 'text' : 'password'} autoComplete="current-password" {...f.campo('contrasena')} />
                <button type="button" className="btn btn-icono position-absolute" style={{ right: 4, top: 3 }}
                  onClick={() => setVerClave((v) => !v)} aria-label={verClave ? 'Ocultar contraseña' : 'Mostrar contraseña'}>
                  <i className={`bi ${verClave ? 'bi-eye-slash' : 'bi-eye'}`} />
                </button>
              </div>
            </Campo>
            <BotonCarga cargando={f.enviando} className="btn btn-primary btn-lg mt-2">Ingresar</BotonCarga>
          </form>
          <p className="text-center mt-4 mb-0">
            ¿No tenés cuenta? <Link to="/registro" className="fw-bold">Registrate</Link>
          </p>
        </div>
      </section>
    </div>
  )
}
