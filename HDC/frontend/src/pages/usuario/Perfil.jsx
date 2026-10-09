import { useState } from 'react'
import { perfilApi } from '../../api/servicios'
import { useAuth } from '../../context/AuthContext.jsx'
import { useUI } from '../../context/UIContext.jsx'
import { BotonCarga, Dato, EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import { EsqueletoTarjeta } from '../../components/comunes/Esqueleto.jsx'
import { Campo, errores, reglas, useFormulario } from '../../components/comunes/Formulario.jsx'
import Modal from '../../components/comunes/Modal.jsx'
import Destello from '../../components/comunes/Destello.jsx'
import CamposVehiculo, { validarVehiculo, vehiculoVacio } from '../../components/formularios/CamposVehiculo.jsx'
import { fecha, toneladas } from '../../utils/formato'

function FormPerfil({ perfil, onGuardado, onCancelar }) {
  const ui = useUI()
  const esInd = perfil.tipoPerfil === 'INDIVIDUO'
  const esOrg = perfil.tipoPerfil === 'ORGANIZACION'
  const f = useFormulario({
    telefono: perfil.telefono || '',
    ...(esInd ? { ...perfil.individuo, fechaNacimiento: perfil.individuo.fechaNacimiento || '', genero: perfil.individuo.genero || '' } : {}),
    ...(esOrg ? { ...perfil.organizacion, descripcion: perfil.organizacion.descripcion || '', cantidadMiembros: perfil.organizacion.cantidadMiembros ?? '' } : {}),
  }, (v) => errores({
    telefono: reglas.telefono(v.telefono),
    ...(esInd ? { nombres: reglas.requerido(v.nombres), apellidos: reglas.requerido(v.apellidos), dni: reglas.requerido(v.dni) || reglas.dni(v.dni) } : {}),
    ...(esOrg ? { razonSocial: reglas.requerido(v.razonSocial), cuit: reglas.requerido(v.cuit) || reglas.cuit(v.cuit), area: reglas.requerido(v.area) } : {}),
  }))

  const guardar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return ui.error('Revisá los campos marcados en rojo', 'Datos incompletos')
    const v = f.valores
    f.setEnviando(true)
    try {
      const r = await perfilApi.actualizar({
        telefono: v.telefono || null,
        individuo: esInd ? { nombres: v.nombres, apellidos: v.apellidos, dni: v.dni, fechaNacimiento: v.fechaNacimiento || null, genero: v.genero || null } : null,
        organizacion: esOrg ? { razonSocial: v.razonSocial, cuit: v.cuit, area: v.area, descripcion: v.descripcion || null, cantidadMiembros: v.cantidadMiembros === '' ? null : Number(v.cantidadMiembros) } : null,
      })
      ui.exito('Tus datos se actualizaron', 'Perfil guardado')
      onGuardado(r)
    } catch (err) { ui.error(err); f.aplicarErroresServidor(err.errores) } finally { f.setEnviando(false) }
  }

  const t = f.tocados
  return (
    <form noValidate onSubmit={guardar} className="row g-3 aparecer">
      {esInd && <>
        <Campo className="col-md-6" etiqueta="Nombres" requerido error={f.errores.nombres} tocado={t.nombres}><input {...f.campo('nombres')} /></Campo>
        <Campo className="col-md-6" etiqueta="Apellidos" requerido error={f.errores.apellidos} tocado={t.apellidos}><input {...f.campo('apellidos')} /></Campo>
        <Campo className="col-md-4" etiqueta="DNI" requerido error={f.errores.dni} tocado={t.dni}><input inputMode="numeric" maxLength={8} {...f.campo('dni')} /></Campo>
        <Campo className="col-md-4" etiqueta="Fecha de nacimiento" exito={null}><input type="date" max={new Date().toISOString().slice(0, 10)} {...f.campo('fechaNacimiento')} /></Campo>
        <Campo className="col-md-4" etiqueta="Género" exito={null}>
          <select {...f.campo('genero')} className="form-select"><option value="">Prefiero no decir</option><option>Femenino</option><option>Masculino</option><option>No binario</option><option>Otro</option></select>
        </Campo>
      </>}
      {esOrg && <>
        <Campo className="col-md-8" etiqueta="Razón social" requerido error={f.errores.razonSocial} tocado={t.razonSocial}><input {...f.campo('razonSocial')} /></Campo>
        <Campo className="col-md-4" etiqueta="CUIT" requerido error={f.errores.cuit} tocado={t.cuit}><input {...f.campo('cuit')} /></Campo>
        <Campo className="col-md-8" etiqueta="Área" requerido error={f.errores.area} tocado={t.area}><input {...f.campo('area')} /></Campo>
        <Campo className="col-md-4" etiqueta="Cantidad de miembros" exito={null}><input type="number" min="0" {...f.campo('cantidadMiembros')} /></Campo>
        <Campo className="col-12" etiqueta="Descripción" exito={null}><textarea rows={3} maxLength={1000} {...f.campo('descripcion')} /></Campo>
      </>}
      <Campo className="col-md-6" etiqueta="Teléfono" error={f.errores.telefono} tocado={t.telefono} exito={null}><input type="tel" {...f.campo('telefono')} /></Campo>
      <div className="col-12 d-flex justify-content-end gap-2">
        <button type="button" className="btn btn-outline-secondary" onClick={onCancelar}>Cancelar</button>
        <BotonCarga cargando={f.enviando}>Guardar cambios</BotonCarga>
      </div>
    </form>
  )
}

function FormContrasena({ abierto, onCerrar }) {
  const ui = useUI()
  const f = useFormulario({ contrasenaActual: '', contrasenaNueva: '', confirmacion: '' }, (v) => errores({
    contrasenaActual: reglas.requerido(v.contrasenaActual),
    contrasenaNueva: reglas.requerido(v.contrasenaNueva) || reglas.minimo(v.contrasenaNueva, 8),
    confirmacion: v.confirmacion !== v.contrasenaNueva ? 'Las contraseñas no coinciden' : null,
  }))
  const guardar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return
    f.setEnviando(true)
    try {
      await perfilApi.cambiarContrasena({ contrasenaActual: f.valores.contrasenaActual, contrasenaNueva: f.valores.contrasenaNueva })
      ui.exito('Tu contraseña se cambió correctamente')
      f.reiniciar(); onCerrar()
    } catch (err) { ui.error(err) } finally { f.setEnviando(false) }
  }
  return (
    <Modal abierto={abierto} onCerrar={onCerrar} titulo="Cambiar contraseña" tamano="sm"
      pie={<><button type="button" className="btn btn-outline-secondary" onClick={onCerrar}>Cancelar</button><BotonCarga cargando={f.enviando} form="form-clave">Cambiar</BotonCarga></>}>
      <form id="form-clave" noValidate onSubmit={guardar} className="d-flex flex-column gap-3">
        <Campo etiqueta="Contraseña actual" requerido error={f.errores.contrasenaActual} tocado={f.tocados.contrasenaActual} exito={null}>
          <input type="password" autoComplete="current-password" {...f.campo('contrasenaActual')} />
        </Campo>
        <Campo etiqueta="Nueva contraseña" requerido error={f.errores.contrasenaNueva} tocado={f.tocados.contrasenaNueva} ayuda="Mínimo 8 caracteres">
          <input type="password" autoComplete="new-password" {...f.campo('contrasenaNueva')} />
        </Campo>
        <Campo etiqueta="Repetir nueva contraseña" requerido error={f.errores.confirmacion} tocado={f.tocados.confirmacion}>
          <input type="password" autoComplete="new-password" {...f.campo('confirmacion')} />
        </Campo>
      </form>
    </Modal>
  )
}

function FormVehiculo({ abierto, vehiculo, onCerrar, onGuardado }) {
  const ui = useUI()
  const f = useFormulario(vehiculo || vehiculoVacio, validarVehiculo)
  const guardar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return ui.error('Revisá los campos marcados en rojo', 'Datos incompletos')
    f.setEnviando(true)
    try {
      const { idVehiculo, ...datos } = f.valores
      if (vehiculo) await perfilApi.actualizarVehiculo(vehiculo.idVehiculo, datos)
      else await perfilApi.agregarVehiculo(datos)
      ui.exito(vehiculo ? 'El vehículo se actualizó' : 'El vehículo se agregó')
      onGuardado()
    } catch (err) { ui.error(err); f.aplicarErroresServidor(err.errores) } finally { f.setEnviando(false) }
  }
  return (
    <Modal abierto={abierto} onCerrar={onCerrar} titulo={vehiculo ? 'Editar vehículo' : 'Agregar vehículo'} tamano="lg"
      pie={<><button type="button" className="btn btn-outline-secondary" onClick={onCerrar}>Cancelar</button><BotonCarga cargando={f.enviando} form="form-vehiculo">Guardar</BotonCarga></>}>
      <form id="form-vehiculo" noValidate onSubmit={guardar}><CamposVehiculo f={f} /></form>
    </Modal>
  )
}

/** RFU04 / RFU05 */
export default function Perfil() {
  const ui = useUI()
  const { actualizarNombre } = useAuth()
  const { datos: p, setDatos, cargando, error, recargar } = useCarga(() => perfilApi.obtener(), [])
  const [editando, setEditando] = useState(false)
  const [clave, setClave] = useState(false)
  const [vehiculo, setVehiculo] = useState({ abierto: false, v: null, k: 0 })

  if (error) return <ErrorCarga error={error} onReintentar={recargar} />
  if (cargando || !p) return <><EncabezadoPagina titulo="Mi perfil" icono="bi-person-circle" /><EsqueletoTarjeta alto={260} /></>

  const eliminarVehiculo = async (v) => {
    if (!(await ui.confirmar({ titulo: `¿Eliminar el vehículo ${v.patente}?`, tipo: 'peligro', textoConfirmar: 'Eliminar' }))) return
    try { await perfilApi.eliminarVehiculo(v.idVehiculo); ui.exito('Vehículo eliminado'); recargar() } catch (err) { ui.error(err) }
  }

  const i = p.individuo, o = p.organizacion

  return (
    <>
      <EncabezadoPagina titulo="Mi perfil" descripcion="Tus datos de cuenta y de perfil." icono="bi-person-circle">
        <button type="button" className="btn btn-outline-secondary" onClick={() => setClave(true)}><i className="bi bi-key me-1" />Cambiar contraseña</button>
        {p.tipoPerfil && !editando && <button type="button" className="btn btn-primary" onClick={() => setEditando(true)}><i className="bi bi-pencil me-1" />Editar perfil</button>}
      </EncabezadoPagina>

      <div className="row g-4">
        <div className="col-lg-4">
          <div className="tarjeta tarjeta-cuerpo text-center">
            <div className="marca-icono mx-auto mb-3" style={{ width: 72, height: 72, fontSize: '1.6rem', borderRadius: 22 }}>
              <i className={`bi ${p.tipoPerfil === 'ORGANIZACION' ? 'bi-building' : p.tipoPerfil ? 'bi-person' : 'bi-shield-lock'}`} />
            </div>
            <h2 className="h5 mb-1">{p.nombreMostrar}</h2>
            <div className="text-muted mb-2">{p.email}</div>
            <span className="badge-suave badge-verde">{p.rol}</span>
            <div className="separador" />
            <div className="row text-start g-3">
              <Dato className="col-6" etiqueta="Alta">{fecha(p.fechaAlta)}</Dato>
              <Dato className="col-6" etiqueta="Teléfono">{p.telefono}</Dato>
              {p.tipoPerfil && <>
                <Dato className="col-6" etiqueta="Domicilios activos">{p.cantidadDomicilios}</Dato>
                <Dato className="col-6" etiqueta="Total emitido">{toneladas(p.totalKgCo2)} t CO₂</Dato>
              </>}
            </div>
          </div>
        </div>

        <div className="col-lg-8 d-flex flex-column gap-4">
          <Destello valor={p} className="tarjeta tarjeta-cuerpo">
            <h2 className="tarjeta-titulo mb-3">{o ? 'Datos de la organización' : 'Datos personales'}</h2>
            {!p.tipoPerfil ? <p className="text-muted mb-0">Cuenta de administración: no tiene datos de perfil adicionales.</p>
              : editando ? (
                <FormPerfil perfil={p} onCancelar={() => setEditando(false)}
                  onGuardado={(r) => { setDatos(r); setEditando(false); actualizarNombre(r.nombreMostrar) }} />
              ) : i ? (
                <div className="row g-3">
                  <Dato className="col-md-6" etiqueta="Nombres">{i.nombres}</Dato>
                  <Dato className="col-md-6" etiqueta="Apellidos">{i.apellidos}</Dato>
                  <Dato className="col-md-4" etiqueta="DNI">{i.dni}</Dato>
                  <Dato className="col-md-4" etiqueta="Fecha de nacimiento">{i.fechaNacimiento ? fecha(`${i.fechaNacimiento}T00:00`) : null}</Dato>
                  <Dato className="col-md-4" etiqueta="Género">{i.genero}</Dato>
                </div>
              ) : (
                <div className="row g-3">
                  <Dato className="col-md-8" etiqueta="Razón social">{o.razonSocial}</Dato>
                  <Dato className="col-md-4" etiqueta="CUIT">{o.cuit}</Dato>
                  <Dato className="col-md-8" etiqueta="Área">{o.area}</Dato>
                  <Dato className="col-md-4" etiqueta="Cantidad de miembros">{o.cantidadMiembros}</Dato>
                  <Dato className="col-12" etiqueta="Descripción">{o.descripcion}</Dato>
                </div>
              )}
          </Destello>

          {p.tipoPerfil && (
            <div className="tarjeta tarjeta-cuerpo">
              <div className="d-flex align-items-center mb-3">
                <h2 className="tarjeta-titulo">Vehículos</h2>
                <button type="button" className="btn btn-suave btn-sm ms-auto" onClick={() => setVehiculo((x) => ({ abierto: true, v: null, k: x.k + 1 }))}>
                  <i className="bi bi-plus-lg me-1" />Agregar
                </button>
              </div>
              {p.vehiculos.length === 0 ? <div className="text-muted small">No registraste vehículos.</div> : (
                <div className="row g-3">
                  {p.vehiculos.map((v) => (
                    <div key={v.idVehiculo} className="col-md-6">
                      <div className="p-3 rounded-3 d-flex gap-3 align-items-start tarjeta-hover" style={{ border: '1px solid var(--borde)' }}>
                        <i className={`bi ${v.tipoVehiculo === 'Motocicleta' ? 'bi-bicycle' : v.tipoVehiculo === 'Camión' ? 'bi-truck' : 'bi-car-front'} fs-3`} style={{ color: 'var(--azul)' }} />
                        <div className="flex-grow-1">
                          <div className="fw-bold">{v.marca} {v.modelo}</div>
                          <div className="small text-muted">{v.tipoVehiculo} · {v.patente}</div>
                          <div className="d-flex gap-1 mt-1"><span className="badge-suave badge-marron">{v.tipoCombustible}</span><span className="badge-suave badge-gris">{v.tipoUso}</span></div>
                        </div>
                        <button type="button" className="btn btn-icono" aria-label="Editar" onClick={() => setVehiculo((x) => ({ abierto: true, v, k: x.k + 1 }))}><i className="bi bi-pencil-square" /></button>
                        <button type="button" className="btn btn-icono peligro" aria-label="Eliminar" onClick={() => eliminarVehiculo(v)}><i className="bi bi-trash3" /></button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      <FormContrasena abierto={clave} onCerrar={() => setClave(false)} />
      <FormVehiculo key={vehiculo.k} abierto={vehiculo.abierto} vehiculo={vehiculo.v}
        onCerrar={() => setVehiculo((x) => ({ ...x, abierto: false }))}
        onGuardado={() => { setVehiculo((x) => ({ ...x, abierto: false })); recargar() }} />
    </>
  )
}
