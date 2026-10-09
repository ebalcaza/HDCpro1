import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { dashboardApi, domiciliosApi } from '../../api/servicios'
import { useAuth, PERMISOS as P } from '../../context/AuthContext.jsx'
import { EncabezadoPagina, ErrorCarga, useCarga, Vacio } from '../../components/comunes/Basicos.jsx'
import { EsqueletoDona, EsqueletoTarjeta } from '../../components/comunes/Esqueleto.jsx'
import Destello, { ValorAnimado } from '../../components/comunes/Destello.jsx'
import TarjetaDesplegable from '../../components/comunes/TarjetaDesplegable.jsx'
import { SelectorMes } from '../../components/comunes/Selectores.jsx'
import { GraficoBarras, GraficoDona, GraficoEvolucion, ListaProgreso } from '../../components/graficos/Graficos.jsx'
import { desdeMes, mesActual, numero, periodo, porcentaje, ppm, toneladas } from '../../utils/formato'

function Kpi({ etiqueta, valor, unidad, color, icono, children, cambio }) {
  return (
    <Destello valor={cambio} className={`tarjeta tarjeta-hover kpi kpi-${color} h-100`}>
      <div className="kpi-icono"><i className={`bi ${icono}`} aria-hidden="true" /></div>
      <div className="kpi-etiqueta mb-1">{etiqueta}</div>
      <div className="kpi-valor"><ValorAnimado valor={valor}>{valor}</ValorAnimado><span className="kpi-unidad">{unidad}</span></div>
      <div className="small texto-2 mt-1">{children}</div>
    </Destello>
  )
}

function Variacion({ pct }) {
  if (pct == null) return <span className="text-muted">Sin datos del mes anterior</span>
  const sube = pct > 0
  return (
    <span className={`badge-suave ${sube ? 'badge-marron' : 'badge-verde'}`}>
      <i className={`bi ${sube ? 'bi-arrow-up-right' : 'bi-arrow-down-right'} me-1`} />
      {sube ? '+' : ''}{numero(pct)} % vs. mes anterior
    </span>
  )
}

export default function Dashboard() {
  const { usuario, tienePermiso } = useAuth()
  const [filtros, setFiltros] = useState({ idDomicilio: '', desde: '', hasta: '' })
  const domicilios = useCarga(() => domiciliosApi.listar(true), [])
  const params = { idDomicilio: filtros.idDomicilio || undefined, desde: desdeMes(filtros.desde) || undefined, hasta: desdeMes(filtros.hasta) || undefined }
  const { datos: d, cargando, error, recargar } = useCarga(() => dashboardApi.obtener(params), [filtros.idDomicilio, filtros.desde, filtros.hasta])

  const ordenAlcances = useMemo(() => (d?.porAlcance || []).map((a) => a.id).sort((a, b) => a - b), [d])
  const sinDatos = d && d.totalKgCo2 === 0
  const hayFiltros = filtros.idDomicilio || filtros.desde || filtros.hasta

  return (
    <>
      <EncabezadoPagina titulo={`Hola, ${usuario.nombreMostrar.split(' ')[0]}`} descripcion="Así viene la huella de carbono de tus domicilios." icono="bi-speedometer2">
        {tienePermiso(P.GESTIONAR_EMISIONES) && (
          <Link to="/emisiones/nuevo" className="btn btn-primary"><i className="bi bi-plus-lg me-1" />Nuevo registro</Link>
        )}
      </EncabezadoPagina>

      {/* Filtros en una sola fila sobre los gráficos */}
      <div className="tarjeta tarjeta-cuerpo mb-4 d-flex flex-wrap gap-3 align-items-end aparecer">
        <div style={{ minWidth: 220 }} className="flex-grow-1 flex-md-grow-0">
          <label className="form-label">Domicilio</label>
          <select className="form-select" value={filtros.idDomicilio} onChange={(e) => setFiltros((f) => ({ ...f, idDomicilio: e.target.value }))}>
            <option value="">Todos los domicilios</option>
            {(domicilios.datos || []).map((x) => <option key={x.idDomicilio} value={x.idDomicilio}>{x.descripcion}{x.fechaBaja ? ' (baja)' : ''}</option>)}
          </select>
        </div>
        <div>
          <label className="form-label">Desde</label>
          <SelectorMes valor={filtros.desde} permitirVacio max={filtros.hasta || mesActual()} onChange={(v) => setFiltros((f) => ({ ...f, desde: v }))} />
        </div>
        <div>
          <label className="form-label">Hasta</label>
          <SelectorMes valor={filtros.hasta} permitirVacio min={filtros.desde} max={mesActual()} onChange={(v) => setFiltros((f) => ({ ...f, hasta: v }))} />
        </div>
        {hayFiltros && (
          <button type="button" className="btn btn-outline-secondary" onClick={() => setFiltros({ idDomicilio: '', desde: '', hasta: '' })}>
            <i className="bi bi-x-lg me-1" />Limpiar
          </button>
        )}
      </div>

      {error ? <ErrorCarga error={error} onReintentar={recargar} /> : cargando && !d ? (
        <div className="row g-4">
          {[0, 1, 2].map((i) => <div key={i} className="col-md-4"><EsqueletoTarjeta alto={110} /></div>)}
          <div className="col-lg-5"><EsqueletoDona /></div>
          <div className="col-lg-7"><EsqueletoTarjeta alto={300} /></div>
        </div>
      ) : sinDatos ? (
        <div className="tarjeta">
          <Vacio icono="bi-cloud-plus" titulo={hayFiltros ? 'No hay emisiones para los filtros elegidos' : 'Todavía no cargaste consumos'}>
            <p className="mb-3">Cargá los consumos mensuales de tus domicilios para ver tus estadísticas.</p>
            {!hayFiltros && tienePermiso(P.GESTIONAR_EMISIONES) && (
              <Link to="/emisiones/nuevo" className="btn btn-primary"><i className="bi bi-plus-lg me-1" />Cargar mi primer registro</Link>
            )}
          </Vacio>
        </div>
      ) : (
        <div className={`row g-4 ${cargando ? 'opacity-75' : ''}`} style={{ transition: 'opacity .2s' }}>
          {/* Tres tarjetas iniciales */}
          <div className="col-md-4 aparecer">
            <Kpi etiqueta="Emisiones totales" valor={toneladas(d.totalKgCo2)} unidad="t CO₂" color="verde" icono="bi-cloud-haze2" cambio={d.totalKgCo2}>
              {numero(d.totalKgCo2)} kg · <span title="1 ppm = 17.600 millones de t de CO₂">{ppm(d.totalKgCo2)}</span>
            </Kpi>
          </div>
          <div className="col-md-4 aparecer" style={{ animationDelay: '60ms' }}>
            <Kpi etiqueta={`Mes corriente · ${periodo(mesActual(), true)}`} valor={toneladas(d.mesActualKgCo2)} unidad="t CO₂" color="azul" icono="bi-calendar3" cambio={d.mesActualKgCo2}>
              <Variacion pct={d.variacionMensualPct} />
            </Kpi>
          </div>
          <div className="col-md-4 aparecer" style={{ animationDelay: '120ms' }}>
            <Kpi etiqueta="Alcance principal" valor={d.alcancePrincipal?.nombre || '—'} unidad="" color="marron" icono="bi-bullseye" cambio={d.alcancePrincipal?.id}>
              {d.alcancePrincipal && <>{porcentaje(d.alcancePrincipal.porcentaje)} del total · {toneladas(d.alcancePrincipal.kgCo2)} t CO₂</>}
            </Kpi>
          </div>

          {/* Dona por alcance */}
          <div className="col-xl-5 aparecer">
            <TarjetaDesplegable
              titulo="Emisiones por alcance"
              subtitulo="Parámetros: kg CO₂ = consumo × factor, agrupado por alcance"
              icono="bi-pie-chart"
              valorCambio={d.porAlcance}
              resumen={<GraficoDona items={d.porAlcance} ordenIds={ordenAlcances} tituloCentro="Total" />}
              detalle={
                <table className="table tabla-hdc table-sm mb-0">
                  <thead><tr><th>Tipo de emisión</th><th>Alcance</th><th className="text-end">kg CO₂</th><th className="text-end">%</th></tr></thead>
                  <tbody>
                    {d.porTipoEmision.map((t) => (
                      <tr key={t.id}><td>{t.nombre}</td><td className="text-muted">{t.grupo}</td><td className="num">{numero(t.kgCo2)}</td><td className="num">{porcentaje(t.porcentaje)}</td></tr>
                    ))}
                  </tbody>
                </table>
              }
            />
          </div>

          {/* Evolución en el tiempo */}
          <div className="col-xl-7 aparecer">
            <TarjetaDesplegable
              titulo="Evolución de las emisiones"
              subtitulo="kg CO₂ por mes: total y por alcance"
              icono="bi-graph-up"
              valorCambio={d.evolucion.length}
              resumen={<GraficoEvolucion puntos={d.evolucion} alto={290} />}
              detalle={
                <div className="tabla-contenedor" style={{ maxHeight: 280 }}>
                  <table className="table tabla-hdc table-sm mb-0">
                    <thead><tr><th>Período</th><th className="text-end">kg CO₂</th><th className="text-end">t CO₂</th></tr></thead>
                    <tbody>
                      {[...d.evolucion].reverse().map((p) => (
                        <tr key={p.periodo}><td>{periodo(p.periodo, true)}</td><td className="num">{numero(p.kgCo2)}</td><td className="num">{toneladas(p.kgCo2)}</td></tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              }
            />
          </div>

          {/* Por tipo de emisión */}
          <div className="col-lg-4 aparecer">
            <TarjetaDesplegable
              titulo="Por tipo de emisión"
              icono="bi-diagram-3"
              valorCambio={d.porTipoEmision}
              resumen={<ListaProgreso items={d.porTipoEmision} max={4} />}
              detalle={d.porTipoEmision.length > 0 && (
                <ul className="list-unstyled mb-0 d-flex flex-column gap-2 small">
                  {d.porTipoEmision.map((t) => (
                    <li key={t.id} className="d-flex justify-content-between gap-2 border-bottom pb-2" style={{ borderColor: 'var(--borde)' }}>
                      <span><span className="fw-semibold">{t.nombre}</span><span className="d-block text-muted">{t.grupo}</span></span>
                      <span className="text-end text-nowrap"><b>{numero(t.kgCo2)} kg</b><span className="d-block text-muted">{porcentaje(t.porcentaje)}</span></span>
                    </li>
                  ))}
                </ul>
              )}
            />
          </div>

          {/* Fuentes que más emiten */}
          <div className="col-lg-4 aparecer">
            <TarjetaDesplegable
              titulo="Fuentes que más emiten"
              icono="bi-fire"
              valorCambio={d.topFuentes}
              resumen={<ListaProgreso items={d.topFuentes} max={5} colorFijo="var(--marron)" />}
              detalle={
                <>
                  <GraficoBarras items={d.topFuentes.slice(0, 10)} color="var(--serie-3)" />
                  <table className="table tabla-hdc table-sm mt-3 mb-0">
                    <thead><tr><th>Fuente</th><th className="text-end">Consumo</th><th className="text-end">kg CO₂</th></tr></thead>
                    <tbody>
                      {d.topFuentes.map((f) => (
                        <tr key={f.id}><td>{f.nombre}</td><td className="num">{numero(f.consumo)} {f.unidad}</td><td className="num">{numero(f.kgCo2)}</td></tr>
                      ))}
                    </tbody>
                  </table>
                </>
              }
            />
          </div>

          {/* Lectura rápida */}
          <div className="col-lg-4 aparecer">
            <TarjetaDesplegable
              titulo="Lectura rápida"
              icono="bi-lightning-charge"
              valorCambio={d.lecturaRapida}
              resumen={
                <div className="d-flex flex-column gap-2">
                  <div className="d-flex justify-content-between"><span className="texto-2">Promedio mensual</span><b>{numero(d.lecturaRapida.promedioMensualKgCo2)} kg</b></div>
                  <div className="d-flex justify-content-between"><span className="texto-2">Mes con más emisiones</span>
                    <b>{d.lecturaRapida.mesMaximo ? periodo(d.lecturaRapida.mesMaximo.periodo) : '—'}</b></div>
                </div>
              }
              detalle={
                <div className="d-flex flex-column gap-2 small">
                  {d.lecturaRapida.mesMaximo && <div className="d-flex justify-content-between"><span className="texto-2">Máximo</span><span>{periodo(d.lecturaRapida.mesMaximo.periodo, true)} · <b>{numero(d.lecturaRapida.mesMaximo.kgCo2)} kg</b></span></div>}
                  {d.lecturaRapida.mesMinimo && <div className="d-flex justify-content-between"><span className="texto-2">Mínimo</span><span>{periodo(d.lecturaRapida.mesMinimo.periodo, true)} · <b>{numero(d.lecturaRapida.mesMinimo.kgCo2)} kg</b></span></div>}
                  <div className="d-flex justify-content-between"><span className="texto-2">Meses con datos</span><b>{d.lecturaRapida.cantidadPeriodos}</b></div>
                  <div className="d-flex justify-content-between"><span className="texto-2">Registros cargados</span><b>{d.lecturaRapida.cantidadRegistros}</b></div>
                  <div className="d-flex justify-content-between"><span className="texto-2">Domicilios con emisiones</span><b>{d.lecturaRapida.cantidadDomicilios}</b></div>
                  <div className="d-flex justify-content-between"><span className="texto-2">Colaboradores que aportaron</span><b>{d.lecturaRapida.cantidadColaboradores}</b></div>
                  {d.porDomicilio.length > 1 && (
                    <>
                      <div className="separador my-2" />
                      <div className="dato-etiqueta">Por domicilio</div>
                      {d.porDomicilio.map((x) => (
                        <div key={x.id} className="d-flex justify-content-between gap-2"><span className="text-truncate">{x.nombre}</span><b className="text-nowrap">{porcentaje(x.porcentaje)}</b></div>
                      ))}
                    </>
                  )}
                </div>
              }
            />
          </div>

          {d.porColaborador.length > 0 && (
            <div className="col-12 aparecer">
              <TarjetaDesplegable
                titulo="Aportes de colaboradores"
                subtitulo="Emisiones cargadas por personas que trabajan o asisten a tus domicilios"
                icono="bi-people"
                valorCambio={d.porColaborador}
                resumen={<ListaProgreso items={d.porColaborador} max={3} colorFijo="var(--azul)" />}
                detalle={
                  <table className="table tabla-hdc table-sm mb-0">
                    <thead><tr><th>Colaborador</th><th className="text-end">kg CO₂</th><th className="text-end">% del total</th></tr></thead>
                    <tbody>{d.porColaborador.map((c) => <tr key={c.id}><td>{c.nombre}</td><td className="num">{numero(c.kgCo2)}</td><td className="num">{porcentaje(c.porcentaje)}</td></tr>)}</tbody>
                  </table>
                }
              />
            </div>
          )}
        </div>
      )}
    </>
  )
}
