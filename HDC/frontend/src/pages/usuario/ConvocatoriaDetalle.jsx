import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { convocatoriasApi } from '../../api/servicios'
import { useUI } from '../../context/UIContext.jsx'
import { EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import TablaPaginada, { consultaAParams } from '../../components/comunes/TablaPaginada.jsx'
import { ESTADOS_COLABORACION, fecha, numero, periodo } from '../../utils/formato'
import { copiarCodigo, EstadoConvocatoria, rangoPeriodos } from './Convocatorias.jsx'

/** Filtro de estado de los miembros (los mismos estados de una colaboración, sin PENDIENTE que es de invitaciones) */
const FILTROS = [
  { valor: '', texto: 'Todos' },
  { valor: 'SOLICITADA', texto: 'Por aprobar' },
  { valor: 'ACEPTADA', texto: 'Activos' },
  { valor: 'REVOCADA', texto: 'Dados de baja' },
  { valor: 'RECHAZADA', texto: 'Rechazados' },
]

/** Acciones sobre miembros: a qué estados aplica cada una y cómo se confirma */
const ACCIONES = {
  aceptar: { texto: 'Aceptar', icono: 'bi-check2', clase: 'btn-primary', aplica: (m) => m.estado === 'SOLICITADA', tipo: 'info',
    explicacion: 'Van a poder cargar consumos en el domicilio.' },
  rechazar: { texto: 'Rechazar', icono: 'bi-x-lg', clase: 'btn-outline-secondary', aplica: (m) => m.estado === 'SOLICITADA', tipo: 'aviso',
    explicacion: 'No van a poder volver a sumarse con el código (podés reactivarlos más adelante).' },
  baja: { texto: 'Dar de baja', icono: 'bi-slash-circle', clase: 'btn-outline-danger', aplica: (m) => m.estado === 'ACEPTADA', tipo: 'peligro',
    explicacion: 'Dejás de recibir sus aportes: no podrán cargar más consumos ni volver a sumarse con el código. Lo que ya cargaron se conserva.' },
  reactivar: { texto: 'Reactivar', icono: 'bi-arrow-counterclockwise', clase: 'btn-outline-secondary', aplica: (m) => (m.estado === 'REVOCADA' || m.estado === 'RECHAZADA') && m.bajaPorTitular, tipo: 'info',
    explicacion: 'Vuelven a quedar activos y pueden cargar consumos.' },
}

/**
 * Gestión de los miembros de una convocatoria (pueden ser miles).
 * La tabla pide cada página al servidor; la selección permite actuar sobre varios a la vez, y
 * "aplicar a todos los filtrados" actúa sobre TODOS los que cumplen el filtro, aunque no estén en la página.
 */
export default function ConvocatoriaDetalle() {
  const { id } = useParams()
  const ui = useUI()
  const conv = useCarga(() => convocatoriasApi.obtener(id), [id])
  // Al volver a esta pestaña se actualizan los conteos: los colaboradores pudieron cargar mientras tanto
  useEffect(() => {
    const actualizar = () => conv.recargar()
    window.addEventListener('focus', actualizar)
    return () => window.removeEventListener('focus', actualizar)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])
  const [estado, setEstado] = useState('')
  const [consulta, setConsulta] = useState(null)
  const [seleccion, setSeleccion] = useState(() => new Set())
  const [procesando, setProcesando] = useState(false)
  // Pestaña inferior y persona elegida para ver solo sus aportes
  const [vista, setVista] = useState('colaboradores')
  const [persona, setPersona] = useState(null)
  const [consultaAportes, setConsultaAportes] = useState(null)

  const miembros = useCarga(
    () => (consulta ? convocatoriasApi.miembros(id, { ...consultaAParams(consulta), estado: estado || undefined }) : Promise.resolve(null)),
    [consulta],
  )
  const aportes = useCarga(
    () => (vista === 'aportes' && consultaAportes
      ? convocatoriasApi.aportes(id, { ...consultaAParams(consultaAportes), idUsuario: persona?.idUsuario || undefined })
      : Promise.resolve(null)),
    [consultaAportes, vista],
  )
  const verAportes = (m) => { setPersona(m ? { idUsuario: m.idColaborador, nombre: m.colaborador } : null); setVista('aportes') }
  const columnasAportes = [
    { clave: 'colaborador', titulo: 'Colaborador', ordenable: true, render: (a) => <div><div className="fw-semibold">{a.colaborador}</div><div className="small text-muted">{a.rol || a.email}</div></div> },
    { clave: 'periodo', titulo: 'Mes', ordenable: true, render: (a) => periodo(a.periodo) },
    { clave: 'fuente', titulo: 'Fuente', ordenable: true, render: (a) => <div><div>{a.fuente}</div><div className="small text-muted">{a.tipoEmision}</div></div> },
    { clave: 'declarado', titulo: 'Declarado', render: (a) => (a.distanciaKm != null ? <span className="small">{numero(a.distanciaKm)} km por día, {numero(a.diasSemana, 1)} días por semana</span> : <span className="text-muted">—</span>) },
    { clave: 'consumo', titulo: 'Cantidad', numerico: true, render: (a) => `${numero(a.consumo)} ${a.unidad}` },
    { clave: 'kgCo2', titulo: 'kg CO₂', numerico: true, ordenable: true, clase: 'fw-bold', render: (a) => numero(a.kgCo2) },
  ]

  const items = miembros.datos?.items || []
  const total = miembros.datos?.total ?? 0
  const finalizada = conv.datos?.estado === 'FINALIZADA'

  const recargar = () => { setSeleccion(new Set()); miembros.recargar(); conv.recargar() }

  const alternar = (idColab) => setSeleccion((s) => { const n = new Set(s); n.has(idColab) ? n.delete(idColab) : n.add(idColab); return n })
  const todosPagina = items.length > 0 && items.every((m) => seleccion.has(m.idColaboracion))
  const alternarPagina = () => setSeleccion((s) => {
    const n = new Set(s)
    items.forEach((m) => (todosPagina ? n.delete(m.idColaboracion) : n.add(m.idColaboracion)))
    return n
  })

  /** Ejecuta una acción sobre la selección (ids) o sobre todos los filtrados (todos = true) */
  const ejecutar = async (accion, { ids, todos, cantidad }) => {
    const a = ACCIONES[accion]
    const quienes = todos ? `los ${cantidad} colaboradores que coinciden con el filtro` : cantidad === 1 ? 'este colaborador' : `los ${cantidad} colaboradores seleccionados`
    const ok = await ui.confirmar({
      titulo: `¿${a.texto} a ${quienes}?`,
      mensaje: `${a.explicacion} Solo se aplica a quienes estén en un estado que lo permita.`,
      tipo: a.tipo, textoConfirmar: a.texto,
    })
    if (!ok) return
    setProcesando(true)
    try {
      const r = await convocatoriasApi.accion(id, todos
        ? { accion, todos: true, estado: estado || null, busqueda: consulta?.texto?.trim() || null }
        : { accion, ids })
      r.afectados > 0 ? ui.exito(r.mensaje) : ui.error(r.mensaje)
      recargar()
    } catch (err) { ui.error(err) } finally { setProcesando(false) }
  }

  const columnas = [
    { clave: 'sel', titulo: (
      <input type="checkbox" className="form-check-input" checked={todosPagina} onChange={alternarPagina} aria-label="Seleccionar toda la página" disabled={finalizada} />
    ), render: (m) => (
      <input type="checkbox" className="form-check-input" checked={seleccion.has(m.idColaboracion)} onChange={() => alternar(m.idColaboracion)}
        onClick={(e) => e.stopPropagation()} aria-label={`Seleccionar a ${m.colaborador}`} disabled={finalizada} />
    ) },
    { clave: 'colaborador', titulo: 'Colaborador', ordenable: true, render: (m) => <div><div className="fw-semibold">{m.colaborador}</div><div className="small text-muted">{m.email}</div></div> },
    { clave: 'estado', titulo: 'Estado', ordenable: true, render: (m) => {
      const e = ESTADOS_COLABORACION[m.estado]
      const quien = (m.estado === 'REVOCADA' || m.estado === 'RECHAZADA') ? (m.bajaPorTitular ? ' (por vos)' : ' (por el colaborador)') : ''
      return <span className={`badge-suave ${e.clase}`}><i className={`bi ${e.icono} me-1`} />{m.estado === 'REVOCADA' ? 'Dado de baja' : e.texto}{quien}</span>
    } },
    { clave: 'fechaSolicitud', titulo: 'Se sumó', ordenable: true, render: (m) => <span className="small">{fecha(m.fechaSolicitud)}</span> },
    { clave: 'totalKgCo2Aportado', titulo: 'Aporte kg CO₂', numerico: true, ordenable: true, render: (m) => <span title={`${m.cantidadRegistros} registros`}>{numero(m.totalKgCo2Aportado)}</span> },
  ]

  if (conv.error) return <ErrorCarga error={conv.error} onReintentar={conv.recargar} />
  const c = conv.datos

  return (
    <>
      <EncabezadoPagina titulo={c ? c.nombre : 'Convocatoria'} icono="bi-megaphone"
        descripcion={c ? `${c.domicilio} · ${rangoPeriodos(c)}` : 'Cargando...'}>
        <Link to="/colaboraciones?pestana=convocatorias" className="btn btn-outline-secondary"><i className="bi bi-arrow-left me-1" />Volver</Link>
      </EncabezadoPagina>

      {c && (
        <div className="tarjeta tarjeta-cuerpo mb-3 d-flex flex-wrap gap-4 align-items-center">
          <EstadoConvocatoria estado={c.estado} />
          {!finalizada && (
            <div className="d-flex align-items-center gap-2">
              <span className="small text-muted">Código</span>
              <span className="fs-5 fw-bold font-monospace">{c.codigo}</span>
              <button type="button" className="btn btn-sm btn-suave" onClick={() => copiarCodigo(c.codigo, ui)}><i className="bi bi-clipboard" /></button>
            </div>
          )}
          <div className="small"><span className="text-muted">Aprobación:</span> {c.aprobacionAutomatica ? 'automática' : 'manual'}</div>
          <div className="d-flex flex-wrap gap-1">{c.tiposEmision.map((t) => <span key={t.idTipoEmision} className="badge-suave badge-gris">{t.nombre}</span>)}</div>
          <div className="ms-auto d-flex gap-3 text-center small">
            <div><div className="fw-bold fs-6">{c.aceptados}</div><div className="text-muted">Activos</div></div>
            <div><div className="fw-bold fs-6">{c.solicitudes}</div><div className="text-muted">Por aprobar</div></div>
            <div><div className="fw-bold fs-6">{c.bajas}</div><div className="text-muted">Bajas</div></div>
            <div><div className="fw-bold fs-6">{numero(c.totalKgCo2Aportado, 0)}</div><div className="text-muted">kg CO₂ aportados</div></div>
          </div>
        </div>
      )}

      {/* Resultados por rol: colaboradores que eligieron cada rol y kg CO₂ de sus traslados */}
      {c?.roles.length > 0 && (
        <div className="tarjeta tarjeta-cuerpo mb-3">
          <h2 className="tarjeta-titulo mb-1">Traslados por rol</h2>
          <div className="form-text mb-3">Colaboradores: quienes eligieron el rol al sumarse o cargaron con él. Los kg CO₂ suman lo que ya cargaron.</div>
          <div className="table-responsive">
            <table className="table table-sm align-middle mb-0">
              <thead><tr><th>Rol</th><th className="text-end">Colaboradores</th><th className="text-end">kg CO₂</th></tr></thead>
              <tbody>
                {c.roles.map((r) => (
                  <tr key={r.idConvocatoriaRol}><td>{r.nombre}</td><td className="text-end">{r.colaboradores}</td><td className="text-end">{numero(r.kgCo2)}</td></tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Barra de acciones masivas: aparece cuando hay seleccionados */}
      {seleccion.size > 0 && (
        <div className="tarjeta tarjeta-cuerpo mb-3 d-flex flex-wrap gap-2 align-items-center" role="region" aria-label="Acciones sobre los seleccionados">
          <b className="me-2">{seleccion.size} seleccionado{seleccion.size === 1 ? '' : 's'}</b>
          {Object.entries(ACCIONES).map(([k, a]) => (
            <button key={k} type="button" className={`btn btn-sm ${a.clase}`} disabled={procesando}
              onClick={() => ejecutar(k, { ids: [...seleccion], cantidad: seleccion.size })}>
              <i className={`bi ${a.icono} me-1`} />{a.texto}
            </button>
          ))}
          <button type="button" className="btn btn-sm btn-link ms-auto" onClick={() => setSeleccion(new Set())}>Quitar selección</button>
        </div>
      )}

      {/* Dos vistas: quiénes participan y qué cargó cada persona, mes a mes */}
      <div className="btn-group mb-3" role="group" aria-label="Elegir vista">
        <button type="button" className={`btn btn-sm ${vista === 'colaboradores' ? 'btn-primary' : 'btn-outline-secondary'}`} onClick={() => setVista('colaboradores')}>
          <i className="bi bi-people me-1" />Colaboradores
        </button>
        <button type="button" className={`btn btn-sm ${vista === 'aportes' ? 'btn-primary' : 'btn-outline-secondary'}`} onClick={() => verAportes(null)}>
          <i className="bi bi-list-ul me-1" />Detalle de aportes
        </button>
      </div>

      {vista === 'aportes' && (aportes.error ? <ErrorCarga error={aportes.error} onReintentar={aportes.recargar} /> : (
        <>
          {persona && (
            <div className="small mb-2 d-flex align-items-center gap-2">
              <span>Mostrando los aportes de <b>{persona.nombre}</b>.</span>
              <button type="button" className="btn btn-sm btn-link p-0" onClick={() => setPersona(null)}>Ver los de todos</button>
            </div>
          )}
          <TablaPaginada
            columnas={columnasAportes}
            datos={aportes.datos?.items || []}
            total={aportes.datos?.total ?? 0}
            onConsulta={setConsultaAportes}
            claveFiltros={persona?.idUsuario || 'todos'}
            cargando={aportes.cargando}
            idFila={(a) => a.idDetalle}
            textoBusqueda="Buscar por colaborador, rol o fuente..."
            vacio={persona ? 'Esta persona todavía no cargó nada.' : 'Todavía no hay aportes cargados.'}
            iconoVacio="bi-list-ul"
          />
        </>
      ))}

      {vista === 'colaboradores' && (miembros.error ? <ErrorCarga error={miembros.error} onReintentar={miembros.recargar} /> : (
        <TablaPaginada
          columnas={columnas}
          datos={items}
          total={total}
          onConsulta={setConsulta}
          claveFiltros={estado}
          cargando={miembros.cargando}
          idFila={(m) => m.idColaboracion}
          textoBusqueda="Buscar por nombre, DNI o email..."
          vacio={estado ? 'No hay colaboradores en ese estado' : 'Todavía no se sumó nadie. Compartí el código.'}
          iconoVacio="bi-people"
          barra={<>
            <div className="btn-group btn-group-sm" role="group" aria-label="Filtrar por estado">
              {FILTROS.map((f) => (
                <button key={f.valor} type="button" className={`btn ${estado === f.valor ? 'btn-primary' : 'btn-outline-secondary'}`}
                  onClick={() => { setEstado(f.valor); setSeleccion(new Set()) }}>{f.texto}</button>
              ))}
            </div>
            {/* Acción sobre TODOS los filtrados (no solo la página): por ejemplo, aceptar todas las solicitudes de una vez */}
            {!finalizada && total > 0 && (
              <div className="dropdown">
                <button type="button" className="btn btn-sm btn-outline-secondary dropdown-toggle" data-bs-toggle="dropdown" disabled={procesando}>
                  Aplicar a los {total} filtrados
                </button>
                <ul className="dropdown-menu">
                  {Object.entries(ACCIONES).map(([k, a]) => (
                    <li key={k}><button type="button" className="dropdown-item" onClick={() => ejecutar(k, { todos: true, cantidad: total })}>
                      <i className={`bi ${a.icono} me-2`} />{a.texto}
                    </button></li>
                  ))}
                </ul>
              </div>
            )}
          </>}
          acciones={(m) => !finalizada && (
            <div className="d-flex gap-1 justify-content-end">
              <button type="button" className="btn btn-sm btn-outline-secondary" title="Ver lo que cargó" onClick={() => verAportes(m)}>
                <i className="bi bi-eye" /><span className="d-none d-xl-inline ms-1">Ver aportes</span>
              </button>
              {Object.entries(ACCIONES).filter(([, a]) => a.aplica(m)).map(([k, a]) => (
                <button key={k} type="button" className={`btn btn-sm ${a.clase}`} title={a.texto} disabled={procesando}
                  onClick={() => ejecutar(k, { ids: [m.idColaboracion], cantidad: 1 })}>
                  <i className={`bi ${a.icono}`} /><span className="d-none d-xl-inline ms-1">{a.texto}</span>
                </button>
              ))}
            </div>
          )}
        />
      ))}
    </>
  )
}
