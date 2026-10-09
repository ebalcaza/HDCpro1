import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { rutaInicio, useAuth } from '../../context/AuthContext.jsx'
import { useUI } from '../../context/UIContext.jsx'
import { Campo, errores, reglas, useFormulario } from '../../components/comunes/Formulario.jsx'
import { BotonCarga } from '../../components/comunes/Basicos.jsx'
import CamposDomicilio, { domicilioARequest, domicilioVacio, validarDomicilio } from '../../components/formularios/CamposDomicilio.jsx'
import CamposVehiculo, { validarVehiculo, vehiculoVacio } from '../../components/formularios/CamposVehiculo.jsx'
import PanelLateral from './PanelLateral.jsx'

const PASOS = ['Cuenta', 'Perfil', 'Domicilio']

/** Campos que valida cada paso */
const CAMPOS_PASO = [
  ['email', 'contrasena', 'confirmacion', 'telefono'],
  ['nombres', 'apellidos', 'dni', 'fechaNacimiento', 'genero', 'razonSocial', 'cuit', 'area', 'descripcion', 'cantidadMiembros'],
  ['calle', 'numero', 'codigoPostal', 'idCiudad', 'telefono'],
]

export default function Registro() {
  const { registrar } = useAuth()
  const ui = useUI()
  const navigate = useNavigate()
  const [tipo, setTipo] = useState('INDIVIDUO')
  const [paso, setPaso] = useState(0)
  const [conVehiculo, setConVehiculo] = useState(false)

  const f = useFormulario({
    email: '', contrasena: '', confirmacion: '', telefono: '',
    nombres: '', apellidos: '', dni: '', fechaNacimiento: '', genero: '',
    razonSocial: '', cuit: '', area: '', descripcion: '', cantidadMiembros: '',
    ...domicilioVacio,
  }, (v) => errores({
    email: reglas.requerido(v.email) || reglas.email(v.email),
    contrasena: reglas.requerido(v.contrasena) || reglas.minimo(v.contrasena, 8),
    confirmacion: v.confirmacion !== v.contrasena ? 'Las contraseñas no coinciden' : null,
    telefono: reglas.telefono(v.telefono),
    ...(tipo === 'INDIVIDUO' ? {
      nombres: reglas.requerido(v.nombres),
      apellidos: reglas.requerido(v.apellidos),
      dni: reglas.requerido(v.dni) || reglas.dni(v.dni),
      fechaNacimiento: v.fechaNacimiento && new Date(v.fechaNacimiento) > new Date() ? 'La fecha no puede ser futura' : null,
    } : {
      razonSocial: reglas.requerido(v.razonSocial),
      cuit: reglas.requerido(v.cuit) || reglas.cuit(v.cuit),
      area: reglas.requerido(v.area),
      cantidadMiembros: v.cantidadMiembros !== '' && (Number(v.cantidadMiembros) < 0 || !Number.isInteger(Number(v.cantidadMiembros))) ? 'Cantidad inválida' : null,
    }),
    ...validarDomicilio(v),
  }))

  const fv = useFormulario(vehiculoVacio, validarVehiculo)

  const pasoValido = (p) => {
    const campos = CAMPOS_PASO[p]
    const conError = campos.filter((c) => f.errores[c])
    // Marca como tocados los campos del paso
    campos.forEach((c) => f.cambiar(c, f.valores[c]))
    return conError.length === 0
  }

  const siguiente = () => { if (pasoValido(paso)) setPaso((p) => p + 1) }

  const enviar = async (e) => {
    e.preventDefault()
    if (paso < PASOS.length - 1) return siguiente()
    const okVehiculo = !conVehiculo || fv.validarTodo()
    if (!f.validarTodo() || !okVehiculo) {
      ui.error('Revisá los campos marcados en rojo', 'Datos incompletos')
      return
    }
    const v = f.valores
    const base = { email: v.email.trim(), contrasena: v.contrasena, telefono: v.telefono || null, domicilio: domicilioARequest(v) }
    const datos = tipo === 'INDIVIDUO'
      ? {
        ...base, nombres: v.nombres.trim(), apellidos: v.apellidos.trim(), dni: v.dni.trim(),
        fechaNacimiento: v.fechaNacimiento || null, genero: v.genero || null,
        vehiculo: conVehiculo ? fv.valores : null,
      }
      : {
        ...base, razonSocial: v.razonSocial.trim(), cuit: v.cuit.trim(), area: v.area.trim(),
        descripcion: v.descripcion || null, cantidadMiembros: v.cantidadMiembros === '' ? null : Number(v.cantidadMiembros),
      }

    f.setEnviando(true)
    try {
      const u = await registrar(tipo, datos)
      ui.exito('Tu cuenta fue creada. ¡Ya podés cargar tus consumos!', 'Registro completo')
      navigate(rutaInicio(u), { replace: true })
    } catch (err) {
      ui.error(err)
      f.aplicarErroresServidor(err.errores)
    } finally {
      f.setEnviando(false)
    }
  }

  const t = f.tocados

  return (
    <div className="auth-pagina">
      <PanelLateral />
      <section className="auth-form">
        <div className="auth-caja ancha aparecer">
          <h2 className="mb-1">Crear cuenta</h2>
          <p className="text-muted mb-3">Paso {paso + 1} de {PASOS.length}: {PASOS[paso]}</p>
          <div className="pasos" aria-hidden="true">{PASOS.map((p, i) => <div key={p} className={`paso ${i <= paso ? 'hecho' : ''}`} />)}</div>

          <form noValidate onSubmit={enviar} className="tarjeta tarjeta-cuerpo p-4">
            {paso === 0 && (
              <div className="row g-3 aparecer">
                <div className="col-12">
                  <label className="form-label">Tipo de cuenta<span className="req">*</span></label>
                  <div className="row g-2">
                    {[
                      ['INDIVIDUO', 'bi-person', 'Individuo', 'Medí la huella de tu hogar'],
                      ['ORGANIZACION', 'bi-building', 'Organización', 'Empresas e instituciones'],
                    ].map(([valor, icono, titulo, desc]) => (
                      <div className="col-sm-6" key={valor}>
                        <label className={`opcion-modal h-100 ${tipo === valor ? 'seleccionada' : ''}`}>
                          <input type="radio" className="form-check-input mt-1" name="tipo" checked={tipo === valor} onChange={() => setTipo(valor)} />
                          <span><i className={`bi ${icono} me-1`} /><b>{titulo}</b><span className="d-block small texto-2">{desc}</span></span>
                        </label>
                      </div>
                    ))}
                  </div>
                </div>
                <Campo className="col-md-6" etiqueta="Email" requerido error={f.errores.email} tocado={t.email}>
                  <input type="email" autoComplete="email" {...f.campo('email')} />
                </Campo>
                <Campo className="col-md-6" etiqueta="Teléfono" error={f.errores.telefono} tocado={t.telefono} exito={null}>
                  <input type="tel" autoComplete="tel" {...f.campo('telefono')} />
                </Campo>
                <Campo className="col-md-6" etiqueta="Contraseña" requerido error={f.errores.contrasena} tocado={t.contrasena} ayuda="Mínimo 8 caracteres">
                  <input type="password" autoComplete="new-password" {...f.campo('contrasena')} />
                </Campo>
                <Campo className="col-md-6" etiqueta="Repetir contraseña" requerido error={f.errores.confirmacion} tocado={t.confirmacion}>
                  <input type="password" autoComplete="new-password" {...f.campo('confirmacion')} />
                </Campo>
              </div>
            )}

            {paso === 1 && tipo === 'INDIVIDUO' && (
              <div className="row g-3 aparecer">
                <Campo className="col-md-6" etiqueta="Nombres" requerido error={f.errores.nombres} tocado={t.nombres}><input {...f.campo('nombres')} /></Campo>
                <Campo className="col-md-6" etiqueta="Apellidos" requerido error={f.errores.apellidos} tocado={t.apellidos}><input {...f.campo('apellidos')} /></Campo>
                <Campo className="col-md-4" etiqueta="DNI" requerido error={f.errores.dni} tocado={t.dni}><input inputMode="numeric" maxLength={8} {...f.campo('dni')} /></Campo>
                <Campo className="col-md-4" etiqueta="Fecha de nacimiento" error={f.errores.fechaNacimiento} tocado={t.fechaNacimiento} exito={null}>
                  <input type="date" max={new Date().toISOString().slice(0, 10)} {...f.campo('fechaNacimiento')} />
                </Campo>
                <Campo className="col-md-4" etiqueta="Género" exito={null}>
                  <select {...f.campo('genero')} className="form-select">
                    <option value="">Prefiero no decir</option>
                    <option>Femenino</option><option>Masculino</option><option>No binario</option><option>Otro</option>
                  </select>
                </Campo>
                <div className="col-12">
                  <div className="form-check form-switch">
                    <input className="form-check-input" type="checkbox" id="con-vehiculo" checked={conVehiculo} onChange={(e) => setConVehiculo(e.target.checked)} />
                    <label className="form-check-label" htmlFor="con-vehiculo">Quiero registrar un vehículo (opcional)</label>
                  </div>
                </div>
                {conVehiculo && <div className="col-12 aparecer"><CamposVehiculo f={fv} /></div>}
              </div>
            )}

            {paso === 1 && tipo === 'ORGANIZACION' && (
              <div className="row g-3 aparecer">
                <Campo className="col-md-8" etiqueta="Razón social o nombre" requerido error={f.errores.razonSocial} tocado={t.razonSocial}><input {...f.campo('razonSocial')} /></Campo>
                <Campo className="col-md-4" etiqueta="CUIT" requerido error={f.errores.cuit} tocado={t.cuit}><input placeholder="30-12345678-9" {...f.campo('cuit')} /></Campo>
                <Campo className="col-md-8" etiqueta="Área" requerido error={f.errores.area} tocado={t.area}><input placeholder="Ej: Educación, Industria, Servicios" {...f.campo('area')} /></Campo>
                <Campo className="col-md-4" etiqueta="Cantidad de miembros" error={f.errores.cantidadMiembros} tocado={t.cantidadMiembros} exito={null}>
                  <input type="number" min="0" {...f.campo('cantidadMiembros')} />
                </Campo>
                <Campo className="col-12" etiqueta="Descripción" exito={null}><textarea rows={3} maxLength={1000} {...f.campo('descripcion')} /></Campo>
              </div>
            )}

            {paso === 2 && (
              <div className="aparecer">
                <p className="text-muted small">Este es el primer domicilio donde vas a medir tus emisiones. Después podés agregar más.</p>
                <CamposDomicilio f={f} />
              </div>
            )}

            <div className="d-flex justify-content-between gap-2 mt-4">
              {paso > 0
                ? <button type="button" className="btn btn-outline-secondary" onClick={() => setPaso((p) => p - 1)}><i className="bi bi-arrow-left me-1" />Atrás</button>
                : <span />}
              {paso < PASOS.length - 1
                ? <button type="button" className="btn btn-primary" onClick={siguiente}>Siguiente<i className="bi bi-arrow-right ms-1" /></button>
                : <BotonCarga cargando={f.enviando}><i className="bi bi-check2 me-1" />Crear cuenta</BotonCarga>}
            </div>
          </form>
          <p className="text-center mt-4 mb-0">¿Ya tenés cuenta? <Link to="/login" className="fw-bold">Iniciá sesión</Link></p>
        </div>
      </section>
    </div>
  )
}
