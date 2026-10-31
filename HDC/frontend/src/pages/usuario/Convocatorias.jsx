import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { convocatoriasApi } from '../../api/servicios'
import { useUI } from '../../context/UIContext.jsx'
import { BotonCarga, ErrorCarga, Vacio } from '../../components/comunes/Basicos.jsx'
import { Campo, errores, reglas, useFormulario } from '../../components/comunes/Formulario.jsx'
import Modal from '../../components/comunes/Modal.jsx'
import { SelectorMes, SelectorTipos } from '../../components/comunes/Selectores.jsx'
import { aMes, desdeMes, ESTADOS_COLABORACION, ESTADOS_CONVOCATORIA, mesActual, numero, periodo } from '../../utils/formato'

/*
 * Convocatorias (v2.2): colaboración masiva por código.
 * El titular crea una convocatoria para un domicilio (qué se puede cargar y en qué períodos) y comparte
 * el código; cada persona se suma escribiéndolo en "Sumarme con un código".
 * Este archivo tiene: el formulario de la convocatoria, el modal para sumarse y la pestaña con el listado.
 * La gestión de los miembros está en ConvocatoriaDetalle.jsx.
 */

/** Texto del rango de períodos, ej. "jul 2026 → sin fin" */
export const rangoPeriodos = (c) => `${periodo(c.periodoDesde)} → ${c.periodoHasta ? periodo(c.periodoHasta) : 'sin fin'}`

/** Copia el código al portapapeles (si el navegador no lo permite, avisa para copiarlo a mano) */
export async function copiarCodigo(codigo, ui) {
  try {
    await navigator.clipboard.writeText(codigo)
    ui.exito(`Código ${codigo} copiado. Compartilo con quienes quieras sumar.`)
  } catch {
    ui.error(`No se pudo copiar automáticamente. El código es ${codigo}`)
  }
}

/** Etiqueta de color con el estado de la convocatoria (Abierta, Cerrada o Finalizada); al pasar el mouse explica qué significa */
export function EstadoConvocatoria({ estado }) {
  const e = ESTADOS_CONVOCATORIA[estado]
  return <span className={`badge-suave ${e.clase}`} title={e.ayuda}><i className={`bi ${e.icono} me-1`} />{e.texto}</span>
}

/** Alta y edición de una convocatoria */
export function FormConvocatoria({ abierto, convocatoria, domicilios, tipos, onCerrar, onGuardado }) {
  const ui = useUI()
  const editando = !!convocatoria
  const f = useFormulario(editando ? {
    idDomicilio: convocatoria.idDomicilio, nombre: convocatoria.nombre, descripcion: convocatoria.descripcion || '',
    desde: aMes(convocatoria.periodoDesde), hasta: aMes(convocatoria.periodoHasta),
    idsTiposEmision: convocatoria.tiposEmision.map((t) => t.idTipoEmision), aprobacionAutomatica: convocatoria.aprobacionAutomatica,
  } : {
    idDomicilio: domicilios.length === 1 ? domicilios[0].idDomicilio : '', nombre: '', descripcion: '', desde: mesActual(), hasta: '',
    // Por defecto: transporte de colaboradores (el caso típico: el personal que viaja al domicilio)
    idsTiposEmision: tipos.filter((t) => /transporte/i.test(t.nombre)).map((t) => t.idTipoEmision), aprobacionAutomatica: false,
  }, (v) => errores({
    idDomicilio: !v.idDomicilio ? 'Elegí el domicilio' : null,
    nombre: reglas.requerido(v.nombre) || reglas.maximo(v.nombre, 150),
    descripcion: reglas.maximo(v.descripcion, 500),
    desde: !v.desde ? 'Elegí desde qué período' : null,
    hasta: v.hasta && v.hasta < v.desde ? 'Debe ser posterior al período desde' : null,
    idsTiposEmision: v.idsTiposEmision.length === 0 ? 'Elegí al menos un tipo de emisión' : null,
  }))

  const guardar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return ui.error('Revisá los campos marcados en rojo', 'Datos incompletos')
    f.setEnviando(true)
    try {
      const datos = {
        nombre: f.valores.nombre.trim(), descripcion: f.valores.descripcion.trim() || null,
        periodoDesde: desdeMes(f.valores.desde), periodoHasta: desdeMes(f.valores.hasta),
        idsTiposEmision: f.valores.idsTiposEmision, aprobacionAutomatica: f.valores.aprobacionAutomatica,
      }
      const r = editando
        ? await convocatoriasApi.actualizar(convocatoria.idConvocatoria, datos)
        : await convocatoriasApi.crear({ ...datos, idDomicilio: Number(f.valores.idDomicilio) })
      ui.exito(editando ? 'Los cambios se aplicaron a todos los colaboradores de la convocatoria' : `Convocatoria creada. Su código es ${r.codigo}`)
      onGuardado(r)
    } catch (err) {
      ui.error(err)
      f.aplicarErroresServidor(err.errores)
    } finally { f.setEnviando(false) }
  }

  const t = f.tocados
  return (
    <Modal abierto={abierto} onCerrar={onCerrar} titulo={editando ? 'Editar convocatoria' : 'Nueva convocatoria'} tamano="lg"
      pie={<><button type="button" className="btn btn-outline-secondary" onClick={onCerrar}>Cancelar</button>
        <BotonCarga cargando={f.enviando} form="form-convocatoria">{editando ? 'Guardar' : 'Crear y obtener código'}</BotonCarga></>}>
      <form id="form-convocatoria" noValidate onSubmit={guardar} className="row g-3">
        <Campo className="col-md-6" etiqueta="Domicilio" requerido error={f.errores.idDomicilio} tocado={t.idDomicilio}
          ayuda={editando ? 'El domicilio no se puede cambiar' : 'Donde van a sumar sus consumos'}>
          <select {...f.campo('idDomicilio')} className={f.claseValidacion('idDomicilio', 'form-select')} disabled={editando}>
            <option value="">Seleccioná</option>
            {domicilios.map((d) => <option key={d.idDomicilio} value={d.idDomicilio}>{d.descripcion}</option>)}
          </select>
        </Campo>
        <Campo className="col-md-6" etiqueta="Nombre" requerido error={f.errores.nombre} tocado={t.nombre} ayuda="Lo ven quienes se suman">
          <input {...f.campo('nombre')} maxLength={150} placeholder="Ej: Transporte del personal 2026" />
        </Campo>
        <Campo className="col-12" etiqueta="Instrucciones para los colaboradores (opcional)" error={f.errores.descripcion} tocado={t.descripcion} exito={null}>
          <textarea rows={2} maxLength={500} {...f.campo('descripcion')} placeholder="Ej: Cargá los km que hacés cada mes para venir a la oficina." />
        </Campo>
        <Campo className="col-md-6" etiqueta="Desde" requerido error={f.errores.desde} tocado={t.desde} exito={null}>
          <SelectorMes valor={f.valores.desde} onChange={(v) => f.cambiar('desde', v)} max={`${new Date().getFullYear() + 1}-12`} />
        </Campo>
        <Campo className="col-md-6" etiqueta="Hasta (opcional)" error={f.errores.hasta} tocado={t.hasta} exito={null} ayuda="Vacío = sin fecha de fin">
          <SelectorMes valor={f.valores.hasta} permitirVacio onChange={(v) => f.cambiar('hasta', v)} max={`${new Date().getFullYear() + 2}-12`} />
        </Campo>
        <Campo className="col-12" etiqueta="Información que pueden cargar" requerido error={f.errores.idsTiposEmision} tocado={t.idsTiposEmision} exito={null}
          ayuda="Los colaboradores solo verán las fuentes de emisión de estos tipos.">
          <SelectorTipos tipos={tipos} seleccion={f.valores.idsTiposEmision} onChange={(v) => f.cambiar('idsTiposEmision', v)} />
        </Campo>
        <div className="col-12">
          <div className="form-check form-switch">
            <input className="form-check-input" type="checkbox" id="aprob-auto" checked={f.valores.aprobacionAutomatica}
              onChange={(e) => f.cambiar('aprobacionAutomatica', e.target.checked)} />
            <label className="form-check-label" htmlFor="aprob-auto"><b>Aprobación automática</b></label>
          </div>
          <div className="small text-muted ms-1">
            {f.valores.aprobacionAutomatica
              ? 'Quien escriba el código queda aceptado al instante. Igual podés darlo de baja cuando quieras.'
              : 'Quien escriba el código queda "esperando aprobación" hasta que lo aceptes (de a uno o todos juntos).'}
          </div>
        </div>
        {editando && (
          <div className="col-12 small text-muted"><i className="bi bi-info-circle me-1" />
            Los cambios de períodos y tipos se aplican a <b>todos</b> los colaboradores de la convocatoria. Los registros ya cargados no cambian.
          </div>
        )}
      </form>
    </Modal>
  )
}

/**
 * "Sumarme con un código": paso 1 escribir el código y ver a qué corresponde, paso 2 confirmar.
 * La vista previa evita sumarse por error a otra organización si el código se tipeó mal.
 */
export function ModalUnirse({ abierto, onCerrar, onUnido }) {
  const ui = useUI()
  const [codigo, setCodigo] = useState('')
  const [vista, setVista] = useState(null)
  const [cargando, setCargando] = useState(false)

  const buscar = async (e) => {
    e.preventDefault()
    if (!codigo.trim()) return ui.error('Escribí el código que te compartieron')
    setCargando(true)
    try { setVista(await convocatoriasApi.vistaPrevia(codigo)) } catch (err) { ui.error(err) } finally { setCargando(false) }
  }

  const confirmar = async () => {
    setCargando(true)
    try {
      const r = await convocatoriasApi.unirse(vista.codigo)
      ui.exito(r.estado === 'ACEPTADA'
        ? `Te sumaste a "${vista.nombre}". Ya podés cargar consumos.`
        : `Te sumaste a "${vista.nombre}". ${vista.titular} tiene que aprobar tu solicitud.`)
      onUnido(r)
    } catch (err) { ui.error(err) } finally { setCargando(false) }
  }

  const yaEsta = vista?.estadoActual === 'ACEPTADA' || vista?.estadoActual === 'SOLICITADA'
  return (
    <Modal abierto={abierto} onCerrar={onCerrar} titulo="Sumarme con un código"
      pie={vista ? (
        <>
          <button type="button" className="btn btn-outline-secondary" onClick={() => setVista(null)}>Otro código</button>
          <BotonCarga type="button" cargando={cargando} onClick={confirmar} disabled={yaEsta}>Sumarme</BotonCarga>
        </>
      ) : (
        <>
          <button type="button" className="btn btn-outline-secondary" onClick={onCerrar}>Cancelar</button>
          <BotonCarga cargando={cargando} form="form-codigo">Buscar</BotonCarga>
        </>
      )}>
      {!vista ? (
        <form id="form-codigo" noValidate onSubmit={buscar} className="d-flex flex-column gap-2">
          <p className="mb-1">Escribí el código que te compartió la persona u organización con la que vas a colaborar.</p>
          <input className="form-control form-control-lg text-center fw-bold" style={{ letterSpacing: '0.2em' }} autoFocus
            maxLength={12} placeholder="XXXX-XXXX" value={codigo} onChange={(e) => setCodigo(e.target.value.toUpperCase())} aria-label="Código de la convocatoria" />
          <div className="small text-muted">Da igual si lo escribís con o sin guion, en mayúsculas o minúsculas.</div>
        </form>
      ) : (
        <div className="d-flex flex-column gap-3">
          <div>
            <div className="small text-muted">Convocatoria</div>
            <div className="fs-5 fw-bold">{vista.nombre}</div>
            <div>{vista.titular} · {vista.domicilio}</div>
          </div>
          {vista.descripcion && <div className="p-2 rounded-3 small fst-italic" style={{ background: 'var(--bs-tertiary-bg)' }}>“{vista.descripcion}”</div>}
          <div className="row g-2 small">
            <div className="col-sm-6"><div className="text-muted">Períodos</div>{rangoPeriodos(vista)}</div>
            <div className="col-sm-6"><div className="text-muted">Aprobación</div>{vista.aprobacionAutomatica ? 'Automática' : 'La tiene que aprobar el titular'}</div>
          </div>
          <div className="small">
            <div className="text-muted mb-1">Vas a poder cargar</div>
            <div className="d-flex flex-wrap gap-1">{vista.tiposEmision.map((t) => <span key={t.idTipoEmision} className="badge-suave badge-gris" title={t.alcance}>{t.nombre}</span>)}</div>
          </div>
          {vista.estadoActual && (
            <div className="small"><i className="bi bi-info-circle me-1" />
              {yaEsta ? `Ya formás parte de esta convocatoria (${ESTADOS_COLABORACION[vista.estadoActual].texto.toLowerCase()}).` : 'Ya estuviste en esta convocatoria.'}
            </div>
          )}
          <div className="small text-muted">Al sumarte, {vista.titular} va a ver tu nombre, tu email y lo que cargues para su domicilio.</div>
        </div>
      )}
    </Modal>
  )
}

/** Pestaña "Convocatorias" del titular: tarjetas con el código, los conteos y las acciones */
export function PestanaConvocatorias({ consulta, onEditar, onNueva }) {
  const ui = useUI()
  const navigate = useNavigate()
  const { datos, cargando, error, recargar } = consulta

  const cambiarEstado = async (c, estado) => {
    const cfg = {
      CERRADA: { titulo: '¿Cerrar la convocatoria?', mensaje: 'Nadie más podrá sumarse con el código. Los colaboradores aceptados siguen cargando consumos. Podés volver a abrirla cuando quieras.', tipo: 'aviso', textoConfirmar: 'Cerrar' },
      ABIERTA: { titulo: '¿Abrir la convocatoria?', mensaje: 'Las personas podrán volver a sumarse con el código.', tipo: 'info', textoConfirmar: 'Abrir' },
      FINALIZADA: { titulo: '¿Finalizar la convocatoria?', mensaje: 'Todos los colaboradores dejarán de poder cargar consumos y el código dejará de funcionar. No se puede deshacer. Los registros ya cargados se conservan.', tipo: 'peligro', textoConfirmar: 'Finalizar' },
    }[estado]
    if (!(await ui.confirmar(cfg))) return
    try { await convocatoriasApi.cambiarEstado(c.idConvocatoria, estado); ui.exito(`La convocatoria quedó ${ESTADOS_CONVOCATORIA[estado].texto.toLowerCase()}`); recargar() } catch (err) { ui.error(err) }
  }
  const regenerar = async (c) => {
    if (!(await ui.confirmar({ titulo: '¿Generar un código nuevo?', mensaje: `El código ${c.codigo} dejará de funcionar. Quienes ya se sumaron siguen en la convocatoria.`, tipo: 'aviso', textoConfirmar: 'Generar' }))) return
    try { const r = await convocatoriasApi.regenerarCodigo(c.idConvocatoria); ui.exito(`Nuevo código: ${r.codigo}`); recargar() } catch (err) { ui.error(err) }
  }
  const eliminar = async (c) => {
    if (!(await ui.confirmar({ titulo: `¿Eliminar "${c.nombre}"?`, mensaje: 'Solo se puede eliminar si nadie se sumó todavía.', tipo: 'peligro', textoConfirmar: 'Eliminar' }))) return
    try { await convocatoriasApi.eliminar(c.idConvocatoria); ui.exito('Convocatoria eliminada'); recargar() } catch (err) { ui.error(err) }
  }

  if (error) return <ErrorCarga error={error} onReintentar={recargar} />
  if (cargando && !datos) return <div className="text-muted small py-4">Cargando convocatorias...</div>
  if (!datos?.length) {
    return (
      <Vacio icono="bi-megaphone" titulo="Todavía no creaste convocatorias">
        Creá una convocatoria y compartí su código para sumar a muchas personas a la vez, sin invitarlas una por una.
        <div className="mt-3"><button type="button" className="btn btn-primary" onClick={onNueva}><i className="bi bi-plus-lg me-1" />Nueva convocatoria</button></div>
      </Vacio>
    )
  }

  return (
    <div className="row g-3">
      {datos.map((c) => {
        const finalizada = c.estado === 'FINALIZADA'
        return (
          <div key={c.idConvocatoria} className="col-lg-6">
            <div className="tarjeta tarjeta-cuerpo h-100 d-flex flex-column gap-3">
              <div className="d-flex justify-content-between align-items-start gap-2">
                <div>
                  <h3 className="h6 fw-bold mb-1">{c.nombre}</h3>
                  <div className="small text-muted">{c.domicilio} · {rangoPeriodos(c)}</div>
                </div>
                <EstadoConvocatoria estado={c.estado} />
              </div>

              {!finalizada && (
                <div className="d-flex align-items-center gap-2 flex-wrap">
                  <span className="small text-muted">Código:</span>
                  <span className="fs-5 fw-bold font-monospace" style={{ letterSpacing: '0.1em' }}>{c.codigo}</span>
                  <button type="button" className="btn btn-sm btn-suave" onClick={() => copiarCodigo(c.codigo, ui)}><i className="bi bi-clipboard me-1" />Copiar</button>
                  {c.aprobacionAutomatica && <span className="badge-suave badge-azul" title="Quien escribe el código queda aceptado al instante">Aprobación automática</span>}
                </div>
              )}

              <div className="d-flex flex-wrap gap-1">{c.tiposEmision.map((t) => <span key={t.idTipoEmision} className="badge-suave badge-gris" title={t.alcance}>{t.nombre}</span>)}</div>

              <div className="row g-2 text-center small">
                <div className="col-3"><div className="fs-5 fw-bold">{c.aceptados}</div><div className="text-muted">Activos</div></div>
                <div className="col-3"><div className={`fs-5 fw-bold ${c.solicitudes > 0 ? 'text-primary' : ''}`}>{c.solicitudes}</div><div className="text-muted">Por aprobar</div></div>
                <div className="col-3"><div className="fs-5 fw-bold">{c.bajas}</div><div className="text-muted">Bajas</div></div>
                <div className="col-3"><div className="fs-5 fw-bold">{numero(c.totalKgCo2Aportado, 0)}</div><div className="text-muted">kg CO₂</div></div>
              </div>

              <div className="d-flex flex-wrap gap-2 mt-auto">
                <button type="button" className="btn btn-sm btn-primary" onClick={() => navigate(`/colaboraciones/convocatorias/${c.idConvocatoria}`)}>
                  <i className="bi bi-people me-1" />Colaboradores{c.solicitudes > 0 && <span className="badge rounded-pill bg-light text-dark ms-1">{c.solicitudes}</span>}
                </button>
                {!finalizada && <>
                  <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => onEditar(c)}><i className="bi bi-pencil me-1" />Editar</button>
                  {c.estado === 'ABIERTA'
                    ? <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => cambiarEstado(c, 'CERRADA')}><i className="bi bi-door-closed me-1" />Cerrar</button>
                    : <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => cambiarEstado(c, 'ABIERTA')}><i className="bi bi-door-open me-1" />Abrir</button>}
                  <div className="dropdown">
                    <button type="button" className="btn btn-sm btn-icono" data-bs-toggle="dropdown" aria-label="Más acciones"><i className="bi bi-three-dots-vertical" /></button>
                    <ul className="dropdown-menu dropdown-menu-end">
                      <li><button type="button" className="dropdown-item" onClick={() => regenerar(c)}><i className="bi bi-arrow-repeat me-2" />Generar código nuevo</button></li>
                      <li><button type="button" className="dropdown-item text-danger" onClick={() => cambiarEstado(c, 'FINALIZADA')}><i className="bi bi-flag me-2" />Finalizar</button></li>
                      {c.aceptados + c.solicitudes + c.bajas === 0 && (
                        <li><button type="button" className="dropdown-item text-danger" onClick={() => eliminar(c)}><i className="bi bi-trash3 me-2" />Eliminar</button></li>
                      )}
                    </ul>
                  </div>
                </>}
              </div>
            </div>
          </div>
        )
      })}
      <div className="col-12 small text-muted">
        <i className="bi bi-info-circle me-1" />¿Cómo se suman? Compartí el código (por email, WhatsApp, intranet…). Cada persona entra a
        {' '}<Link to="/colaboraciones">Colaboraciones</Link> → <b>Sumarme con un código</b>.
      </div>
    </div>
  )
}
