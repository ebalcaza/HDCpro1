import { Link } from 'react-router-dom'
import { reportesApi, sincronizarGeoref, usuariosApi } from '../../api/servicios'
import { PERMISOS as P, useAuth } from '../../context/AuthContext.jsx'
import { useUI } from '../../context/UIContext.jsx'
import { EncabezadoPagina, useCarga } from '../../components/comunes/Basicos.jsx'
import { EsqueletoTarjeta } from '../../components/comunes/Esqueleto.jsx'
import TarjetaDesplegable from '../../components/comunes/TarjetaDesplegable.jsx'
import { GraficoDona, GraficoEvolucion, ListaProgreso } from '../../components/graficos/Graficos.jsx'
import { numero, ppm, toneladas } from '../../utils/formato'

const ACCESOS = [
  { a: '/admin/reportes', t: 'Reportes y métricas', d: 'Filtrá por tipo de usuario, zona y período', i: 'bi-clipboard-data', p: P.REPORTES_GLOBALES },
  { a: '/admin/fuentes', t: 'Fuentes de emisión', d: 'Factores de emisión del cálculo', i: 'bi-fire', p: P.GESTIONAR_FUENTES },
  { a: '/admin/usuarios', t: 'Usuarios', d: 'Cuentas registradas', i: 'bi-person-lines-fill', p: P.GESTIONAR_USUARIOS },
  { a: '/admin/roles', t: 'Tipos de usuario', d: 'Roles y permisos asignados', i: 'bi-person-badge', p: P.GESTIONAR_ROLES },
  { a: '/admin/tipos-emision', t: 'Tipos de emisión', d: 'Agrupación por alcance', i: 'bi-diagram-3', p: P.GESTIONAR_CATALOGOS },
  { a: '/admin/unidades', t: 'Unidades de medida', d: 'm3, kg, L, kWh, km...', i: 'bi-rulers', p: P.GESTIONAR_CATALOGOS },
]

export default function AdminPanel() {
  const ui = useUI()
  const { tienePermiso } = useAuth()
  const usuarios = useCarga(() => (tienePermiso(P.GESTIONAR_USUARIOS) ? usuariosApi.listar() : Promise.resolve(null)), [])
  const global = useCarga(() => (tienePermiso(P.REPORTES_GLOBALES) ? reportesApi.generar({}, true) : Promise.resolve(null)), [])

  const u = usuarios.datos
  const g = global.datos

  const sincronizar = async () => {
    try { const r = await sincronizarGeoref(); ui.exito(r.mensaje, 'Georef') } catch (err) { ui.error(err) }
  }

  return (
    <>
      <EncabezadoPagina titulo="Panel de administración" descripcion="Vista general del sistema y accesos rápidos." icono="bi-grid-1x2">
        {tienePermiso(P.GESTIONAR_CATALOGOS) && (
          <button type="button" className="btn btn-outline-secondary" onClick={sincronizar} title="Carga provincias, distritos y ciudades si todavía no están">
            <i className="bi bi-globe-americas me-1" />Cargar geografía (georef)
          </button>
        )}
      </EncabezadoPagina>

      <div className="row g-4 mb-4">
        {u && (
          <>
            <div className="col-md-4"><div className="tarjeta kpi kpi-verde aparecer h-100"><div className="kpi-icono"><i className="bi bi-people" /></div>
              <div className="kpi-etiqueta">Usuarios registrados</div><div className="kpi-valor">{u.length}</div>
              <div className="small texto-2">{u.filter((x) => x.activo).length} activos</div></div></div>
          </>
        )}
        {g && (
          <>
            <div className="col-md-4"><div className="tarjeta kpi kpi-azul aparecer h-100"><div className="kpi-icono"><i className="bi bi-cloud-haze2" /></div>
              <div className="kpi-etiqueta">Emisiones totales del sistema</div><div className="kpi-valor">{toneladas(g.resumen.totalKgCo2)}<span className="kpi-unidad">t CO₂</span></div>
              <div className="small texto-2">{ppm(g.resumen.totalKgCo2)}</div></div></div>
            <div className="col-md-4"><div className="tarjeta kpi kpi-marron aparecer h-100"><div className="kpi-icono"><i className="bi bi-receipt" /></div>
              <div className="kpi-etiqueta">Registros cargados</div><div className="kpi-valor">{numero(g.resumen.cantidadRegistros, 0)}</div>
              <div className="small texto-2">{g.resumen.cantidadDomicilios} domicilios con datos</div></div></div>
          </>
        )}
        {(usuarios.cargando || global.cargando) && [0, 1, 2].map((i) => <div key={i} className="col-md-4"><EsqueletoTarjeta alto={110} /></div>)}
      </div>

      {g && g.resumen.cantidadDetalles > 0 && (
        <div className="row g-4 mb-4">
          <div className="col-xl-5">
            <TarjetaDesplegable titulo="Emisiones por alcance" icono="bi-pie-chart" resumen={<GraficoDona items={g.porAlcance} ordenIds={g.porAlcance.map((x) => x.id).sort((a, b) => a - b)} />}
              detalle={<><div className="dato-etiqueta mb-2">Por tipo de usuario</div><ListaProgreso items={g.porRol} max={10} colorFijo="var(--azul)" /></>} />
          </div>
          <div className="col-xl-7">
            <TarjetaDesplegable titulo="Evolución del sistema" icono="bi-graph-up"
              resumen={<GraficoEvolucion alto={260} mostrarAlcances={false} puntos={g.porPeriodo.map((p) => ({ periodo: p.periodo, kgCo2: p.kgCo2, porAlcance: {} }))} />}
              detalle={<><div className="dato-etiqueta mb-2">Provincias con más emisiones</div><ListaProgreso items={g.porProvincia} max={8} /></>} />
          </div>
        </div>
      )}

      <div className="row g-3">
        {ACCESOS.filter((x) => tienePermiso(x.p)).map((x) => (
          <div key={x.a} className="col-sm-6 col-xl-4">
            <Link to={x.a} className="tarjeta tarjeta-hover tarjeta-cuerpo d-flex gap-3 align-items-center text-decoration-none h-100">
              <span className="marca-icono" style={{ width: 44, height: 44 }}><i className={`bi ${x.i}`} /></span>
              <span><span className="fw-bold d-block" style={{ color: 'var(--texto)' }}>{x.t}</span><span className="small text-muted">{x.d}</span></span>
              <i className="bi bi-chevron-right ms-auto text-muted" />
            </Link>
          </div>
        ))}
      </div>
    </>
  )
}
