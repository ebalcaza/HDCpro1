import { useMemo, useRef, useState } from 'react'
import { catalogoApi, domiciliosApi, reportesApi, rolesApi } from '../../api/servicios'
import { PERMISOS as P, useAuth } from '../../context/AuthContext.jsx'
import { useUI } from '../../context/UIContext.jsx'
import { BotonCarga, EncabezadoPagina, useCarga, Vacio } from '../../components/comunes/Basicos.jsx'
import { Chips, SelectorGeo, SelectorMes } from '../../components/comunes/Selectores.jsx'
import { EsqueletoDona, EsqueletoTarjeta } from '../../components/comunes/Esqueleto.jsx'
import Modal from '../../components/comunes/Modal.jsx'
import TablaPaginada from '../../components/comunes/TablaPaginada.jsx'
import TarjetaDesplegable from '../../components/comunes/TarjetaDesplegable.jsx'
import { GraficoBarras, GraficoDona, GraficoEvolucion, ListaProgreso } from '../../components/graficos/Graficos.jsx'
import { aMes, desdeMes, fecha, mesActual, numero, periodo, porcentaje, ppm, toneladas } from '../../utils/formato'
import { pdfReporte } from '../../utils/pdf'

const filtrosVacios = { desde: '', hasta: '', idsAlcances: [], idsTiposEmision: [], idsFuentes: [], idsDomicilios: [], idsRoles: [], idProvincia: '', idDistrito: '', idCiudad: '' }

const aApi = (f) => ({
  desde: desdeMes(f.desde), hasta: desdeMes(f.hasta),
  idsAlcances: f.idsAlcances, idsTiposEmision: f.idsTiposEmision, idsFuentes: f.idsFuentes,
  idsDomicilios: f.idsDomicilios, idsRoles: f.idsRoles, idsUsuarios: [],
  idProvincia: f.idProvincia || null, idDistrito: f.idDistrito || null, idCiudad: f.idCiudad || null,
})

const desdeApi = (f) => ({
  ...filtrosVacios, ...f,
  desde: aMes(f.desde), hasta: aMes(f.hasta),
  idProvincia: f.idProvincia || '', idDistrito: f.idDistrito || '', idCiudad: f.idCiudad || '',
})

function TablaSerie({ items, grupo }) {
  return (
    <table className="table tabla-hdc table-sm mb-0">
      <thead><tr><th className="col-num">#</th><th>Nombre</th>{grupo && <th>{grupo}</th>}<th className="text-end">kg CO₂</th><th className="text-end">%</th></tr></thead>
      <tbody>{items.map((s, i) => (
        <tr key={s.id}><td className="col-num">{i + 1}</td><td>{s.nombre}</td>{grupo && <td className="text-muted">{s.grupo}</td>}<td className="num">{numero(s.kgCo2)}</td><td className="num">{porcentaje(s.porcentaje)}</td></tr>
      ))}</tbody>
    </table>
  )
}

export default function Reportes({ global = false }) {
  const ui = useUI()
  const { tienePermiso } = useAuth()
  const [f, setF] = useState(filtrosVacios)
  const [resultado, setResultado] = useState(null)
  const [generando, setGenerando] = useState(false)
  const [guardar, setGuardar] = useState({ abierto: false, nombre: '', descripcion: '', tocado: false })
  const refEvolucion = useRef(null)
  const refAlcances = useRef(null)

  const cat = useCarga(() => Promise.all([
    catalogoApi('alcances').listar(),
    catalogoApi('tipos-emision').listar(),
    catalogoApi('fuentes').listar({ incluirBajas: true }),
    !global && tienePermiso(P.GESTIONAR_DOMICILIOS) ? domiciliosApi.listar(true) : Promise.resolve([]),
    global && tienePermiso(P.GESTIONAR_ROLES) ? rolesApi.listar() : Promise.resolve([]),
  ]).then(([alcances, tipos, fuentes, domicilios, roles]) => ({ alcances, tipos, fuentes, domicilios, roles })), [global])
  const guardados = useCarga(() => reportesApi.guardados(global), [global])

  const c = cat.datos
  const tiposVisibles = useMemo(() => (c?.tipos || []).filter((t) => !f.idsAlcances.length || f.idsAlcances.includes(t.idAlcance)), [c, f.idsAlcances])
  const fuentesVisibles = useMemo(() => (c?.fuentes || []).filter((x) =>
    (!f.idsAlcances.length || f.idsAlcances.includes(x.idAlcance)) && (!f.idsTiposEmision.length || f.idsTiposEmision.includes(x.idTipoEmision))), [c, f.idsAlcances, f.idsTiposEmision])

  const set = (campo) => (v) => setF((x) => ({ ...x, [campo]: v }))

  const generar = async (filtros = f) => {
    setGenerando(true)
    try {
      const r = await reportesApi.generar(aApi(filtros), global)
      setResultado(r)
      if (r.resumen.cantidadDetalles === 0) ui.aviso('No hay emisiones que cumplan con los filtros elegidos', 'Reporte vacío')
      else ui.exito(`Reporte generado con ${r.resumen.cantidadRegistros} registro(s)`, 'Reporte listo')
    } catch (err) { ui.error(err) } finally { setGenerando(false) }
  }

  const guardarReporte = async (e) => {
    e.preventDefault()
    setGuardar((g) => ({ ...g, tocado: true }))
    if (!guardar.nombre.trim()) return
    try {
      await reportesApi.guardar({ nombre: guardar.nombre.trim(), descripcion: guardar.descripcion || null, esGlobal: global, filtros: aApi(f) })
      ui.exito('Los filtros del reporte quedaron guardados')
      setGuardar({ abierto: false, nombre: '', descripcion: '', tocado: false })
      guardados.recargar()
    } catch (err) { ui.error(err) }
  }

  const eliminarGuardado = async (r) => {
    if (!(await ui.confirmar({ titulo: `¿Eliminar "${r.nombre}"?`, mensaje: 'Solo se borran los filtros guardados.', tipo: 'peligro', textoConfirmar: 'Eliminar' }))) return
    try { await reportesApi.eliminar(r.idReporte); ui.exito('Reporte guardado eliminado'); guardados.recargar() } catch (err) { ui.error(err) }
  }

  const cargarGuardado = (r) => {
    const nuevos = desdeApi(r.filtros)
    setF(nuevos)
    generar(nuevos)
  }

  const descargar = () => {
    pdfReporte(resultado, {
      nombre: global ? 'Reporte global de emisiones' : 'Reporte de emisiones',
      graficos: { evolucion: refEvolucion.current, alcances: refAlcances.current },
    })
    ui.exito('El PDF del reporte se descargó', 'Descarga lista')
  }

  const r = resultado
  const ordenAlcances = (c?.alcances || []).map((a) => a.idAlcance).sort((a, b) => a - b)

  const columnasFilas = [
    { clave: 'periodo', titulo: 'Período', ordenable: true, render: (x) => periodo(x.periodo) },
    ...(global ? [{ clave: 'titular', titulo: 'Titular', ordenable: true, render: (x) => <div>{x.titular}<div className="small text-muted">{x.rol}</div></div> }] : []),
    { clave: 'domicilio', titulo: 'Domicilio', ordenable: true, render: (x) => <div>{x.domicilio}<div className="small text-muted">{x.ciudad}, {x.provincia}</div></div> },
    { clave: 'alcance', titulo: 'Alcance', ordenable: true },
    { clave: 'fuente', titulo: 'Fuente', ordenable: true, render: (x) => <div>{x.fuente}<div className="small text-muted">{x.tipoEmision}</div></div> },
    { clave: 'consumo', titulo: 'Consumo', numerico: true, ordenable: true, render: (x) => `${numero(x.consumo, 4)} ${x.unidadMedida}` },
    { clave: 'kgCo2', titulo: 'kg CO₂', numerico: true, ordenable: true, render: (x) => <b>{numero(x.kgCo2)}</b> },
  ]

  return (
    <>
      <EncabezadoPagina
        titulo={global ? 'Reportes y métricas' : 'Reportes'}
        descripcion={global
          ? 'Métricas de todas las emisiones del sistema: combiná filtros por período, tipo de usuario, zona geográfica y categorías.'
          : 'Generá reportes de tus emisiones filtrando por período, alcance, tipo y domicilio, y descargalos en PDF.'}
        icono={global ? 'bi-clipboard-data' : 'bi-file-earmark-bar-graph'}>
        {r && <button type="button" className="btn btn-primary" onClick={descargar} disabled={!r.filas.length}><i className="bi bi-file-earmark-pdf me-1" />Descargar PDF</button>}
      </EncabezadoPagina>

      <div className="row g-4">
        <div className="col-xl-9">
          <div className="tarjeta tarjeta-cuerpo">
            <div className="d-flex align-items-center mb-3"><i className="bi bi-funnel me-2" style={{ color: 'var(--verde)' }} /><h2 className="tarjeta-titulo">Filtros</h2>
              <span className="small text-muted ms-2">Todos son opcionales y se pueden combinar</span></div>
            {cat.cargando ? <EsqueletoTarjeta alto={160} /> : (
              <div className="row g-3">
                <div className="col-md-6"><label className="form-label">Desde</label><SelectorMes valor={f.desde} permitirVacio max={f.hasta || mesActual()} onChange={set('desde')} /></div>
                <div className="col-md-6"><label className="form-label">Hasta</label><SelectorMes valor={f.hasta} permitirVacio max={mesActual()} onChange={set('hasta')} /></div>
                <div className="col-12">
                  <label className="form-label">Alcances</label>
                  <Chips opciones={c.alcances.map((a) => ({ valor: a.idAlcance, texto: a.nombre }))} seleccion={f.idsAlcances}
                    onChange={(v) => setF((x) => ({ ...x, idsAlcances: v, idsTiposEmision: [], idsFuentes: [] }))} />
                </div>
                <div className="col-12">
                  <label className="form-label">Tipos de emisión</label>
                  <Chips opciones={tiposVisibles.map((t) => ({ valor: t.idTipoEmision, texto: t.nombre }))} seleccion={f.idsTiposEmision}
                    onChange={(v) => setF((x) => ({ ...x, idsTiposEmision: v, idsFuentes: [] }))} />
                </div>
                <div className="col-md-6">
                  <label className="form-label">Fuentes de emisión <span className="text-muted fw-normal">(Ctrl + click para varias)</span></label>
                  <select multiple className="form-select" size={5} value={f.idsFuentes.map(String)}
                    onChange={(e) => set('idsFuentes')([...e.target.selectedOptions].map((o) => Number(o.value)))}>
                    {fuentesVisibles.map((x) => <option key={x.idFuenteEmision} value={x.idFuenteEmision}>{x.nombre}{x.activa ? '' : ' (baja)'}</option>)}
                  </select>
                </div>
                {!global && c.domicilios.length > 0 && (
                  <div className="col-md-6">
                    <label className="form-label">Domicilios</label>
                    <Chips opciones={c.domicilios.map((d) => ({ valor: d.idDomicilio, texto: d.descripcion }))} seleccion={f.idsDomicilios} onChange={set('idsDomicilios')} />
                  </div>
                )}
                {global && c.roles.length > 0 && (
                  <div className="col-md-6">
                    <label className="form-label">Tipos de usuario</label>
                    <Chips opciones={c.roles.map((x) => ({ valor: x.idRol, texto: x.nombre }))} seleccion={f.idsRoles} onChange={set('idsRoles')} />
                  </div>
                )}
                {global && (
                  <>
                    <div className="col-12"><div className="dato-etiqueta">Zona geográfica</div></div>
                    <SelectorGeo opcional valor={{ idProvincia: f.idProvincia, idDistrito: f.idDistrito, idCiudad: f.idCiudad }}
                      onChange={(g) => setF((x) => ({ ...x, ...g }))} />
                  </>
                )}
                <div className="col-12 d-flex flex-wrap gap-2 justify-content-end">
                  <button type="button" className="btn btn-outline-secondary" onClick={() => { setF(filtrosVacios); setResultado(null) }}><i className="bi bi-x-lg me-1" />Limpiar</button>
                  <button type="button" className="btn btn-outline-secondary" onClick={() => setGuardar((g) => ({ ...g, abierto: true }))}><i className="bi bi-bookmark-star me-1" />Guardar filtros</button>
                  <BotonCarga type="button" cargando={generando} onClick={() => generar()}><i className="bi bi-bar-chart-line me-1" />Generar reporte</BotonCarga>
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="col-xl-3">
          <div className="tarjeta tarjeta-cuerpo h-100">
            <h2 className="tarjeta-titulo mb-3"><i className="bi bi-bookmark-star me-2" style={{ color: 'var(--marron)' }} />Reportes guardados</h2>
            {guardados.cargando ? <EsqueletoTarjeta alto={100} /> : !(guardados.datos || []).length ? (
              <div className="small text-muted">Guardá combinaciones de filtros que uses seguido para generarlas con un click.</div>
            ) : (
              <div className="d-flex flex-column gap-2" style={{ maxHeight: 360, overflowY: 'auto' }}>
                {guardados.datos.map((g) => (
                  <div key={g.idReporte} className="d-flex align-items-center gap-2 p-2 rounded-3" style={{ border: '1px solid var(--borde)' }}>
                    <button type="button" className="btn btn-link p-0 text-start text-decoration-none flex-grow-1 ancho-min-0" onClick={() => cargarGuardado(g)}>
                      <div className="fw-bold text-truncate">{g.nombre}</div>
                      <div className="small text-muted text-truncate">{g.descripcion || fecha(g.fechaCreacion)}</div>
                    </button>
                    <button type="button" className="btn btn-icono peligro" aria-label="Eliminar" onClick={() => eliminarGuardado(g)}><i className="bi bi-trash3" /></button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {generando && !r && (
        <div className="row g-4 mt-1"><div className="col-lg-6"><EsqueletoDona /></div><div className="col-lg-6"><EsqueletoTarjeta alto={260} /></div></div>
      )}

      {r && (
        <div className={`mt-4 ${generando ? 'opacity-75' : ''}`}>
          <div className="tarjeta tarjeta-cuerpo mb-4 aparecer">
            <div className="d-flex flex-wrap gap-2 mb-3">
              {r.filtrosAplicados.map((x) => <span key={x} className="badge-suave badge-gris"><i className="bi bi-funnel me-1" />{x}</span>)}
            </div>
            <div className="row g-3 text-center text-md-start">
              <div className="col-6 col-md-3"><div className="dato-etiqueta">Total</div><div className="fs-4 fw-bold">{toneladas(r.resumen.totalKgCo2)} <small className="fs-6 text-muted">t CO₂</small></div><div className="small text-muted">{ppm(r.resumen.totalKgCo2)}</div></div>
              <div className="col-6 col-md-3"><div className="dato-etiqueta">Promedio mensual</div><div className="fs-4 fw-bold">{numero(r.resumen.promedioMensualKgCo2)} <small className="fs-6 text-muted">kg</small></div></div>
              <div className="col-6 col-md-3"><div className="dato-etiqueta">Registros</div><div className="fs-4 fw-bold">{r.resumen.cantidadRegistros}</div><div className="small text-muted">{r.resumen.cantidadDetalles} consumos</div></div>
              <div className="col-6 col-md-3"><div className="dato-etiqueta">{global ? 'Usuarios / domicilios' : 'Domicilios'}</div>
                <div className="fs-4 fw-bold">{global ? `${r.resumen.cantidadUsuarios} / ${r.resumen.cantidadDomicilios}` : r.resumen.cantidadDomicilios}</div>
                {r.resumen.periodoDesde && <div className="small text-muted">{periodo(r.resumen.periodoDesde)} → {periodo(r.resumen.periodoHasta)}</div>}</div>
            </div>
          </div>

          {r.filas.length === 0 ? <div className="tarjeta"><Vacio icono="bi-search" titulo="Sin resultados">Probá con otros filtros.</Vacio></div> : (
            <div className="row g-4">
              <div className="col-xl-5">
                <TarjetaDesplegable titulo="Por alcance" icono="bi-pie-chart" valorCambio={r.porAlcance}
                  resumen={<GraficoDona items={r.porAlcance} ordenIds={ordenAlcances} refGrafico={refAlcances} />}
                  detalle={<TablaSerie items={r.porTipoEmision} grupo="Alcance" />} />
              </div>
              <div className="col-xl-7">
                <TarjetaDesplegable titulo="Evolución por período" icono="bi-graph-up" valorCambio={r.porPeriodo}
                  resumen={<GraficoEvolucion refGrafico={refEvolucion} alto={280} mostrarAlcances={false}
                    puntos={r.porPeriodo.map((p) => ({ periodo: p.periodo, kgCo2: p.kgCo2, porAlcance: {} }))} />}
                  detalle={<table className="table tabla-hdc table-sm mb-0"><thead><tr><th>Período</th><th className="text-end">kg CO₂</th></tr></thead>
                    <tbody>{r.porPeriodo.map((p) => <tr key={p.periodo}><td>{periodo(p.periodo, true)}</td><td className="num">{numero(p.kgCo2)}</td></tr>)}</tbody></table>} />
              </div>
              <div className="col-lg-6">
                <TarjetaDesplegable titulo="Por fuente de emisión" icono="bi-fire" valorCambio={r.porFuente}
                  resumen={<ListaProgreso items={r.porFuente} max={5} colorFijo="var(--marron)" />}
                  detalle={<><GraficoBarras items={r.porFuente.slice(0, 12)} color="var(--serie-3)" /><div className="mt-3"><TablaSerie items={r.porFuente} grupo="Tipo" /></div></>} />
              </div>
              <div className="col-lg-6">
                <TarjetaDesplegable titulo={global ? 'Por zona geográfica' : 'Por domicilio'} icono={global ? 'bi-globe-americas' : 'bi-house-door'}
                  valorCambio={global ? r.porProvincia : r.porDomicilio}
                  resumen={<ListaProgreso items={global ? r.porProvincia : r.porDomicilio} max={5} />}
                  detalle={global
                    ? <><GraficoBarras items={r.porProvincia} /><div className="mt-3 dato-etiqueta">Por distrito</div><TablaSerie items={r.porDistrito} grupo="Provincia" /></>
                    : <TablaSerie items={r.porDomicilio} />} />
              </div>
              {global && (
                <>
                  <div className="col-lg-6">
                    <TarjetaDesplegable titulo="Por tipo de usuario" icono="bi-person-badge" valorCambio={r.porRol}
                      resumen={<GraficoDona items={r.porRol} ordenIds={(c?.roles || []).map((x) => x.idRol)} tituloCentro="Usuarios" alto={170} />} />
                  </div>
                  <div className="col-lg-6">
                    <TarjetaDesplegable titulo="Usuarios que más emiten" icono="bi-people" valorCambio={r.porUsuario}
                      resumen={<ListaProgreso items={r.porUsuario} max={5} colorFijo="var(--azul)" />}
                      detalle={<TablaSerie items={r.porUsuario} grupo="Tipo" />} />
                  </div>
                </>
              )}
              <div className="col-12">
                <h2 className="tarjeta-titulo mb-2">Detalle de registros</h2>
                <TablaPaginada columnas={columnasFilas} datos={r.filas} idFila={(x) => `${x.idRegistro}-${x.fuente}`} textoBusqueda="Buscar en el detalle..." />
              </div>
            </div>
          )}
        </div>
      )}

      <Modal abierto={guardar.abierto} onCerrar={() => setGuardar((g) => ({ ...g, abierto: false }))} titulo="Guardar filtros del reporte" tamano="sm"
        pie={<><button type="button" className="btn btn-outline-secondary" onClick={() => setGuardar((g) => ({ ...g, abierto: false }))}>Cancelar</button>
          <button type="submit" form="form-guardar" className="btn btn-primary">Guardar</button></>}>
        <form id="form-guardar" noValidate onSubmit={guardarReporte} className="d-flex flex-column gap-3">
          <div>
            <label className="form-label">Nombre<span className="req">*</span></label>
            <input className={`form-control ${guardar.tocado ? (guardar.nombre.trim() ? 'is-valid' : 'is-invalid') : ''}`} maxLength={200}
              value={guardar.nombre} onChange={(e) => setGuardar((g) => ({ ...g, nombre: e.target.value, tocado: true }))} placeholder="Ej: Alcance 2 del último año" />
            {guardar.tocado && !guardar.nombre.trim() && <div className="invalid-feedback d-block">El nombre es obligatorio</div>}
          </div>
          <div>
            <label className="form-label">Descripción</label>
            <input className="form-control" maxLength={500} value={guardar.descripcion} onChange={(e) => setGuardar((g) => ({ ...g, descripcion: e.target.value }))} />
          </div>
        </form>
      </Modal>
    </>
  )
}
