import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { convocatoriasApi } from '../../api/servicios'
import { useUI } from '../../context/UIContext.jsx'
import { EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import TablaPaginada, { consultaAParams } from '../../components/comunes/TablaPaginada.jsx'
import { ESTADOS_COLABORACION, fecha, numero } from '../../utils/formato'
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
  const [estado, setEstado] = useState('')
  const [consulta, setConsulta] = useState(null)
  const [seleccion, setSeleccion] = useState(() => new Set())
  const [procesando, setProcesando] = useState(false)

  const miembros = useCarga(
    () => (consulta ? convocatoriasApi.miembros(id, { ...consultaAParams(consulta), estado: estado || undefined }) : Promise.resolve(null)),
    [consulta],
  )
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

      {miembros.error ? <ErrorCarga error={miembros.error} onReintentar={miembros.recargar} /> : (
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
              {Object.entries(ACCIONES).filter(([, a]) => a.aplica(m)).map(([k, a]) => (
                <button key={k} type="button" className={`btn btn-sm ${a.clase}`} title={a.texto} disabled={procesando}
                  onClick={() => ejecutar(k, { ids: [m.idColaboracion], cantidad: 1 })}>
                  <i className={`bi ${a.icono}`} /><span className="d-none d-xl-inline ms-1">{a.texto}</span>
                </button>
              ))}
            </div>
          )}
        />
      )}
    </>
  )
}
