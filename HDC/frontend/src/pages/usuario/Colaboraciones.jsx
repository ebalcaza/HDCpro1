import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { catalogoApi, colaboracionesApi, convocatoriasApi, domiciliosApi } from '../../api/servicios'
import { PERMISOS as P, useAuth } from '../../context/AuthContext.jsx'
import { useUI } from '../../context/UIContext.jsx'
import { BotonCarga, EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import { Campo, errores, reglas, useFormulario } from '../../components/comunes/Formulario.jsx'
import Modal from '../../components/comunes/Modal.jsx'
import TablaPaginada from '../../components/comunes/TablaPaginada.jsx'
import { SelectorMes, SelectorTipos } from '../../components/comunes/Selectores.jsx'
import { aMes, desdeMes, ESTADOS_COLABORACION, mesActual, numero, periodo } from '../../utils/formato'
import { FormConvocatoria, ModalUnirse, PestanaConvocatorias } from './Convocatorias.jsx'

function Estado({ c }) {
  const e = ESTADOS_COLABORACION[c.estado]
  return (
    <span className={`badge-suave ${e.clase}`}>
      <i className={`bi ${e.icono} me-1`} />{e.texto}{c.estado === 'ACEPTADA' && !c.vigente && ' (fuera de período)'}
    </span>
  )
}

const rango = (c) => `${periodo(c.periodoDesde)} → ${c.periodoHasta ? periodo(c.periodoHasta) : 'sin fin'}`

function FormInvitacion({ abierto, colaboracion, domicilios, tipos, onCerrar, onGuardado }) {
  const ui = useUI()
  const editando = !!colaboracion
  const f = useFormulario(editando ? {
    idDomicilio: colaboracion.idDomicilio, emailColaborador: colaboracion.emailColaborador,
    desde: aMes(colaboracion.periodoDesde), hasta: aMes(colaboracion.periodoHasta), idsTiposEmision: colaboracion.tiposEmision.map((t) => t.idTipoEmision), mensaje: colaboracion.mensaje || '',
  } : {
    idDomicilio: '', emailColaborador: '', desde: mesActual(), hasta: '',
    // Por defecto: transporte de colaboradores (el caso típico de quien viaja al domicilio)
    idsTiposEmision: tipos.filter((t) => /transporte/i.test(t.nombre)).map((t) => t.idTipoEmision), mensaje: '',
  }, (v) => errores({
    idDomicilio: !v.idDomicilio ? 'Elegí el domicilio' : null,
    emailColaborador: editando ? null : reglas.requerido(v.emailColaborador) || reglas.email(v.emailColaborador),
    desde: !v.desde ? 'Elegí desde qué período' : null,
    hasta: v.hasta && v.hasta < v.desde ? 'Debe ser posterior al período desde' : null,
    idsTiposEmision: v.idsTiposEmision.length === 0 ? 'Elegí al menos un tipo de emisión' : null,
  }))

  const guardar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return ui.error('Revisá los campos marcados en rojo', 'Datos incompletos')
    f.setEnviando(true)
    try {
      const base = { periodoDesde: desdeMes(f.valores.desde), periodoHasta: desdeMes(f.valores.hasta), idsTiposEmision: f.valores.idsTiposEmision }
      const r = editando
        ? await colaboracionesApi.actualizar(colaboracion.idColaboracion, base)
        : await colaboracionesApi.invitar({ ...base, idDomicilio: Number(f.valores.idDomicilio), emailColaborador: f.valores.emailColaborador.trim(), mensaje: f.valores.mensaje || null })
      ui.exito(editando ? 'La colaboración se actualizó' : `Se envió la invitación a ${r.colaborador}`)
      onGuardado(r)
    } catch (err) {
      ui.error(err)
      f.aplicarErroresServidor(err.errores)
    } finally { f.setEnviando(false) }
  }

  const t = f.tocados
  return (
    <Modal abierto={abierto} onCerrar={onCerrar} titulo={editando ? 'Editar colaboración' : 'Invitar colaborador'} tamano="lg"
      pie={<><button type="button" className="btn btn-outline-secondary" onClick={onCerrar}>Cancelar</button>
        <BotonCarga cargando={f.enviando} form="form-colab">{editando ? 'Guardar' : 'Enviar invitación'}</BotonCarga></>}>
      <form id="form-colab" noValidate onSubmit={guardar} className="row g-3">
        <Campo className="col-md-6" etiqueta="Domicilio" requerido error={f.errores.idDomicilio} tocado={t.idDomicilio}>
          <select {...f.campo('idDomicilio')} className={f.claseValidacion('idDomicilio', 'form-select')} disabled={editando}>
            <option value="">Seleccioná</option>
            {domicilios.map((d) => <option key={d.idDomicilio} value={d.idDomicilio}>{d.descripcion}</option>)}
          </select>
        </Campo>
        <Campo className="col-md-6" etiqueta="Email del colaborador" requerido error={f.errores.emailColaborador} tocado={t.emailColaborador}
          ayuda="Tiene que estar registrado en la aplicación">
          <input type="email" {...f.campo('emailColaborador')} disabled={editando} />
        </Campo>
        <Campo className="col-md-6" etiqueta="Desde" requerido error={f.errores.desde} tocado={t.desde} exito={null}>
          <SelectorMes valor={f.valores.desde} onChange={(v) => f.cambiar('desde', v)} max={`${new Date().getFullYear() + 1}-12`} />
        </Campo>
        <Campo className="col-md-6" etiqueta="Hasta (opcional)" error={f.errores.hasta} tocado={t.hasta} exito={null} ayuda="Vacío = sin fecha de fin">
          <SelectorMes valor={f.valores.hasta} permitirVacio onChange={(v) => f.cambiar('hasta', v)} max={`${new Date().getFullYear() + 2}-12`} />
        </Campo>
        <Campo className="col-12" etiqueta="Información que puede cargar" requerido error={f.errores.idsTiposEmision} tocado={t.idsTiposEmision} exito={null}
          ayuda="El colaborador solo verá las fuentes de emisión de estos tipos.">
          <SelectorTipos tipos={tipos} seleccion={f.valores.idsTiposEmision} onChange={(v) => f.cambiar('idsTiposEmision', v)} />
        </Campo>
        {!editando && (
          <Campo className="col-12" etiqueta="Mensaje (opcional)" exito={null}>
            <textarea className="form-control" rows={2} maxLength={500} {...f.campo('mensaje')} placeholder="Ej: Cargá los km que hacés para venir a la oficina cada mes." />
          </Campo>
        )}
      </form>
    </Modal>
  )
}

export default function Colaboraciones() {
  const ui = useUI()
  const { tienePermiso } = useAuth()
  const puedeInvitar = tienePermiso(P.GESTIONAR_COLABORACIONES)
  const puedeColaborar = tienePermiso(P.COLABORAR)
  const [query] = useSearchParams()
  const [pestana, setPestana] = useState(query.get('pestana') || (puedeInvitar ? 'enviadas' : 'recibidas'))
  const enviadas = useCarga(() => (puedeInvitar ? colaboracionesApi.enviadas() : Promise.resolve([])), [])
  const convocatorias = useCarga(() => (puedeInvitar ? convocatoriasApi.listar() : Promise.resolve([])), [])
  const [formConv, setFormConv] = useState({ abierto: false, convocatoria: null, clave: 0 })
  const [unirse, setUnirse] = useState({ abierto: false, clave: 0 })
  const recibidas = useCarga(() => (puedeColaborar ? colaboracionesApi.recibidas() : Promise.resolve([])), [])
  const domicilios = useCarga(() => (puedeInvitar ? domiciliosApi.listar() : Promise.resolve([])), [])
  const tipos = useCarga(() => catalogoApi('tipos-emision').listar(), [])
  const [form, setForm] = useState({ abierto: false, colaboracion: null, clave: 0 })
  const [resaltado, setResaltado] = useState(null)

  const pendientes = (recibidas.datos || []).filter((c) => c.estado === 'PENDIENTE').length
  const porAprobar = (convocatorias.datos || []).reduce((s, c) => s + c.solicitudes, 0)
  const abrirFormConv = (convocatoria = null) => setFormConv((f) => ({ abierto: true, convocatoria, clave: f.clave + 1 }))

  const accion = async (c, tipo) => {
    const cfg = {
      aceptar: { titulo: '¿Aceptar la invitación?', mensaje: `Vas a poder cargar consumos en ${c.domicilio} de ${c.titular}.`, tipo: 'info', textoConfirmar: 'Aceptar', fn: colaboracionesApi.aceptar, ok: 'Aceptaste la invitación. Ya podés cargar consumos.' },
      rechazar: { titulo: '¿Rechazar la invitación?', mensaje: `${c.titular} verá que rechazaste la invitación.`, tipo: 'aviso', textoConfirmar: 'Rechazar', fn: colaboracionesApi.rechazar, ok: 'Rechazaste la invitación' },
      revocar: c.estado === 'SOLICITADA'
        ? { titulo: '¿Cancelar tu solicitud?', mensaje: `Dejás de esperar la aprobación de ${c.titular}. Podés volver a sumarte con el código mientras la convocatoria esté abierta.`, tipo: 'aviso', textoConfirmar: 'Cancelar solicitud', fn: colaboracionesApi.revocar, ok: 'Cancelaste tu solicitud' }
        : { titulo: '¿Finalizar la colaboración?', mensaje: 'No se podrán cargar más consumos con esta colaboración. Los registros ya cargados se conservan.', tipo: 'peligro', textoConfirmar: 'Finalizar', fn: colaboracionesApi.revocar, ok: 'La colaboración se finalizó' },
    }[tipo]
    if (!(await ui.confirmar(cfg))) return
    try {
      await cfg.fn(c.idColaboracion)
      ui.exito(cfg.ok)
      setResaltado(c.idColaboracion)
      enviadas.recargar(); recibidas.recargar()
    } catch (err) { ui.error(err) }
  }

  const tiposCol = { clave: 'tipos', titulo: 'Puede cargar', valor: (c) => c.tiposEmision.map((t) => t.nombre).join(' '), render: (c) => (
    <div className="d-flex flex-wrap gap-1">{c.tiposEmision.map((t) => <span key={t.idTipoEmision} className="badge-suave badge-gris" title={t.alcance}>{t.nombre}</span>)}</div>
  ) }

  const columnasEnviadas = [
    { clave: 'colaborador', titulo: 'Colaborador', ordenable: true, render: (c) => <div><div className="fw-semibold">{c.colaborador}</div><div className="small text-muted">{c.emailColaborador}</div></div> },
    { clave: 'domicilio', titulo: 'Domicilio', ordenable: true },
    { clave: 'periodoDesde', titulo: 'Período', ordenable: true, render: rango },
    tiposCol,
    { clave: 'estado', titulo: 'Estado', ordenable: true, render: (c) => <Estado c={c} /> },
    { clave: 'totalKgCo2Aportado', titulo: 'Aporte kg CO₂', numerico: true, ordenable: true, render: (c) => <span title={`${c.cantidadRegistros} registros`}>{numero(c.totalKgCo2Aportado)}</span> },
  ]

  const columnasRecibidas = [
    { clave: 'titular', titulo: 'Titular', ordenable: true, render: (c) => (
      <div>
        <div className="fw-semibold">{c.titular}</div>
        <div className="small text-muted">{c.emailTitular}</div>
        {c.convocatoria && <span className="badge-suave badge-azul" title="Te sumaste con un código"><i className="bi bi-megaphone me-1" />{c.convocatoria}</span>}
      </div>
    ) },
    { clave: 'domicilio', titulo: 'Domicilio', ordenable: true, render: (c) => <div>{c.domicilio}{c.mensaje && <div className="small text-muted fst-italic">“{c.mensaje}”</div>}</div> },
    { clave: 'periodoDesde', titulo: 'Período', ordenable: true, render: rango },
    tiposCol,
    { clave: 'estado', titulo: 'Estado', ordenable: true, render: (c) => <Estado c={c} /> },
    { clave: 'totalKgCo2Aportado', titulo: 'Tu aporte kg CO₂', numerico: true, ordenable: true, render: (c) => numero(c.totalKgCo2Aportado) },
  ]

  const activa = (c) => c.estado === 'PENDIENTE' || c.estado === 'ACEPTADA'

  return (
    <>
      <EncabezadoPagina titulo="Colaboraciones" descripcion="Personas que aportan información de emisiones a tus domicilios, y domicilios donde vos colaborás." icono="bi-people">
        {puedeColaborar && (
          <button type="button" className="btn btn-outline-secondary" onClick={() => setUnirse((u) => ({ abierto: true, clave: u.clave + 1 }))}>
            <i className="bi bi-key me-1" />Sumarme con un código
          </button>
        )}
        {puedeInvitar && (
          <>
            <button type="button" className="btn btn-outline-primary" onClick={() => setForm((f) => ({ abierto: true, colaboracion: null, clave: f.clave + 1 }))}>
              <i className="bi bi-person-plus me-1" />Invitar por email
            </button>
            <button type="button" className="btn btn-primary" onClick={() => abrirFormConv()}>
              <i className="bi bi-megaphone me-1" />Nueva convocatoria
            </button>
          </>
        )}
      </EncabezadoPagina>

      <div className="tarjeta tarjeta-cuerpo mb-4 small texto-2 d-flex gap-3 align-items-start">
        <i className="bi bi-info-circle fs-4" style={{ color: 'var(--azul)' }} />
        <div>
          Quienes trabajan o asisten a tu domicilio también generan emisiones, por ejemplo con sus traslados.
          Lo que carguen <b>suma a la huella del domicilio</b> y lo vas a ver separado en tu dashboard como "Aportes de colaboradores".
          Para pocas personas, <b>invitalas por email</b>. Para muchas (por ejemplo, todo el personal), creá una <b>convocatoria</b> y
          compartí su código: cada uno se suma solo y vos aprobás o das de baja a quien quieras, de a uno o todos juntos.
        </div>
      </div>

      {puedeInvitar && (
        <ul className="nav nav-pills gap-2 mb-3">
          <li className="nav-item"><button type="button" className={`btn ${pestana === 'enviadas' ? 'btn-primary' : 'btn-outline-secondary'}`} onClick={() => setPestana('enviadas')}>Invitaciones por email</button></li>
          <li className="nav-item">
            <button type="button" className={`btn ${pestana === 'convocatorias' ? 'btn-primary' : 'btn-outline-secondary'}`} onClick={() => setPestana('convocatorias')}>
              Convocatorias {porAprobar > 0 && <span className="badge rounded-pill bg-light text-dark ms-1" title="Solicitudes por aprobar">{porAprobar}</span>}
            </button>
          </li>
          {puedeColaborar && (
            <li className="nav-item">
              <button type="button" className={`btn ${pestana === 'recibidas' ? 'btn-primary' : 'btn-outline-secondary'}`} onClick={() => setPestana('recibidas')}>
                Donde colaboro {pendientes > 0 && <span className="badge rounded-pill bg-light text-dark ms-1">{pendientes}</span>}
              </button>
            </li>
          )}
        </ul>
      )}

      {pestana === 'convocatorias' && puedeInvitar ? (
        <PestanaConvocatorias consulta={convocatorias} onEditar={abrirFormConv} onNueva={() => abrirFormConv()} />
      ) : pestana === 'enviadas' && puedeInvitar ? (
        enviadas.error ? <ErrorCarga error={enviadas.error} onReintentar={enviadas.recargar} /> : (
          <TablaPaginada
            columnas={columnasEnviadas} datos={enviadas.datos} cargando={enviadas.cargando}
            idFila={(c) => c.idColaboracion} resaltarId={resaltado}
            vacio="Todavía no invitaste colaboradores" iconoVacio="bi-person-plus"
            acciones={(c) => activa(c) && (
              <>
                <button type="button" className="btn btn-icono" title="Editar" aria-label="Editar" onClick={() => setForm((f) => ({ abierto: true, colaboracion: c, clave: f.clave + 1 }))}><i className="bi bi-pencil-square" /></button>
                <button type="button" className="btn btn-icono peligro" title="Finalizar" aria-label="Finalizar colaboración" onClick={() => accion(c, 'revocar')}><i className="bi bi-slash-circle" /></button>
              </>
            )}
          />
        )
      ) : (
        recibidas.error ? <ErrorCarga error={recibidas.error} onReintentar={recibidas.recargar} /> : (
          <TablaPaginada
            columnas={columnasRecibidas} datos={recibidas.datos} cargando={recibidas.cargando}
            idFila={(c) => c.idColaboracion} resaltarId={resaltado}
            vacio="Todavía no colaborás en ningún domicilio. Si te pasaron un código, usá «Sumarme con un código»." iconoVacio="bi-envelope-paper"
            acciones={(c) => (
              <>
                {c.estado === 'PENDIENTE' && <>
                  <button type="button" className="btn btn-sm btn-primary me-1" onClick={() => accion(c, 'aceptar')}><i className="bi bi-check2 me-1" />Aceptar</button>
                  <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => accion(c, 'rechazar')}>Rechazar</button>
                </>}
                {c.estado === 'SOLICITADA' && (
                  <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => accion(c, 'revocar')}>Cancelar solicitud</button>
                )}
                {c.estado === 'ACEPTADA' && <>
                  {c.vigente && <Link to={`/emisiones/nuevo?domicilio=${c.idDomicilio}`} className="btn btn-sm btn-suave me-1"><i className="bi bi-plus-lg me-1" />Cargar</Link>}
                  <button type="button" className="btn btn-icono peligro" title="Dejar de colaborar" aria-label="Dejar de colaborar" onClick={() => accion(c, 'revocar')}><i className="bi bi-box-arrow-right" /></button>
                </>}
              </>
            )}
          />
        )
      )}

      {puedeInvitar && (
        <FormInvitacion
          key={form.clave}
          abierto={form.abierto}
          colaboracion={form.colaboracion}
          domicilios={domicilios.datos || []}
          tipos={tipos.datos || []}
          onCerrar={() => setForm((f) => ({ ...f, abierto: false }))}
          onGuardado={(c) => { setForm((f) => ({ ...f, abierto: false })); setResaltado(c.idColaboracion); enviadas.recargar() }}
        />
      )}

      {puedeInvitar && (
        <FormConvocatoria
          key={`conv-${formConv.clave}`}
          abierto={formConv.abierto}
          convocatoria={formConv.convocatoria}
          domicilios={domicilios.datos || []}
          tipos={tipos.datos || []}
          onCerrar={() => setFormConv((f) => ({ ...f, abierto: false }))}
          onGuardado={() => { setFormConv((f) => ({ ...f, abierto: false })); setPestana('convocatorias'); convocatorias.recargar() }}
        />
      )}

      {puedeColaborar && (
        <ModalUnirse
          key={`unirse-${unirse.clave}`}
          abierto={unirse.abierto}
          onCerrar={() => setUnirse((u) => ({ ...u, abierto: false }))}
          onUnido={(c) => { setUnirse((u) => ({ ...u, abierto: false })); setPestana('recibidas'); setResaltado(c.idColaboracion); recibidas.recargar() }}
        />
      )}
    </>
  )
}
