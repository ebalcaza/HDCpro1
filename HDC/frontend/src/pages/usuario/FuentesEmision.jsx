import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { catalogoApi } from '../../api/servicios'
import { PERMISOS as P, useAuth } from '../../context/AuthContext.jsx'
import { EncabezadoPagina, ErrorCarga, useCarga, Vacio } from '../../components/comunes/Basicos.jsx'
import { PuntosCarga } from '../../components/comunes/Esqueleto.jsx'
import { kg, numero } from '../../utils/formato'

/** Quita tildes y pasa a minúscula, para que "electrica" encuentre "Eléctrica" */
const normalizar = (s) => String(s || '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase()

/** Color del alcance: el mismo de los gráficos del Dashboard (--serie-1, -2, -3...) */
const colorAlcance = (idAlcance) => `var(--serie-${idAlcance}, var(--gris))`

/**
 * Una fuente de emisión: qué es, su factor y una calculadora rápida
 * (consumo × factor = kg CO₂) para que el usuario estime antes de cargar.
 */
function TarjetaFuente({ fuente, color, puedeCargar }) {
  const [consumo, setConsumo] = useState('')
  const valor = Number(consumo)
  const resultado = consumo !== '' && valor > 0 ? valor * fuente.valorFactorEmision : null

  return (
    <div className="tarjeta tarjeta-cuerpo h-100 d-flex flex-column gap-2" style={{ borderTop: `3px solid ${color}` }}>
      <div className="d-flex justify-content-between align-items-start gap-2">
        <h3 className="h6 fw-bold mb-0">{fuente.nombre}</h3>
        <span className="badge-suave badge-gris flex-shrink-0" title="Unidad en la que se carga el consumo">{fuente.unidadMedida}</span>
      </div>

      <div className="small">
        <i className="bi bi-calculator me-1" aria-hidden="true" />
        1 {fuente.unidadMedida} = <b>{numero(fuente.valorFactorEmision, 3)} kg CO₂</b>
      </div>

      <p className="small text-muted mb-0 flex-grow-1">
        {fuente.descripcion || 'Cargá el consumo del mes en la unidad indicada.'}
      </p>

      {/* Calculadora rápida: no guarda nada, solo muestra la estimación */}
      <div className="input-group input-group-sm">
        <input type="number" min="0" step="any" inputMode="decimal" className="form-control" placeholder="Probá un consumo"
          value={consumo} onChange={(e) => setConsumo(e.target.value)} aria-label={`Consumo de ${fuente.nombre} en ${fuente.unidadMedida}`} />
        <span className="input-group-text">{fuente.unidadMedida}</span>
      </div>
      <div className="small d-flex justify-content-between align-items-center gap-2" aria-live="polite">
        <span className={resultado == null ? 'text-muted' : 'fw-bold'}>
          {resultado == null ? '= ? kg CO₂' : `= ${kg(resultado)} CO₂`}
        </span>
        {puedeCargar && (
          <Link className="btn btn-sm btn-suave"
            to={`/emisiones/nuevo?fuente=${fuente.idFuenteEmision}${resultado != null ? `&consumo=${valor}` : ''}`}>
            <i className="bi bi-plus-lg me-1" />Cargar
          </Link>
        )}
      </div>
    </div>
  )
}

/**
 * Sección informativa "Fuentes de emisión" (menú Mi huella).
 * Muestra todas las fuentes activas que el usuario puede cargar, agrupadas por alcance y tipo de emisión,
 * con su factor, una descripción de qué es y dónde encontrar el dato, y una calculadora rápida.
 * Usa los mismos catálogos (cacheados) que el formulario de carga de emisiones; no tiene endpoint propio.
 */
export default function FuentesEmision() {
  const { tienePermiso } = useAuth()
  const puedeCargar = tienePermiso(P.GESTIONAR_EMISIONES, P.COLABORAR)
  const [busqueda, setBusqueda] = useState('')
  const [idAlcance, setIdAlcance] = useState('')

  const { datos, cargando, error, recargar } = useCarga(() => Promise.all([
    catalogoApi('fuentes').listar(),      // sin bajas: solo las que se pueden cargar
    catalogoApi('alcances').listar(),
    catalogoApi('tipos-emision').listar(),
  ]).then(([fuentes, alcances, tipos]) => ({ fuentes, alcances, tipos })), [])

  /** Alcance → tipos de emisión → fuentes, aplicando el buscador y el filtro de alcance */
  const grupos = useMemo(() => {
    if (!datos) return []
    const t = normalizar(busqueda.trim())
    const coincide = (f) => !t || [f.nombre, f.descripcion, f.tipoEmision, f.alcance, f.unidadMedida].some((x) => normalizar(x).includes(t))
    const fuentes = datos.fuentes.filter((f) => (!idAlcance || f.idAlcance === Number(idAlcance)) && coincide(f))

    return datos.alcances
      .map((a) => ({
        ...a,
        tipos: datos.tipos
          .filter((ti) => ti.idAlcance === a.idAlcance)
          .map((ti) => ({ ...ti, fuentes: fuentes.filter((f) => f.idTipoEmision === ti.idTipoEmision).sort((x, y) => x.nombre.localeCompare(y.nombre)) }))
          .filter((ti) => ti.fuentes.length > 0),
      }))
      .filter((a) => a.tipos.length > 0)
  }, [datos, busqueda, idAlcance])

  const total = grupos.reduce((s, a) => s + a.tipos.reduce((s2, ti) => s2 + ti.fuentes.length, 0), 0)

  return (
    <>
      <EncabezadoPagina titulo="Fuentes de emisión" icono="bi-lightbulb"
        descripcion="Todo lo que podés cargar en tus registros, cómo se calcula y dónde encontrar cada dato.">
        {puedeCargar && <Link to="/emisiones/nuevo" className="btn btn-primary"><i className="bi bi-plus-lg me-1" />Cargar consumos</Link>}
      </EncabezadoPagina>

      {/* Cómo se calcula */}
      <div className="tarjeta tarjeta-cuerpo mb-4 aparecer">
        <div className="row g-3 align-items-center">
          <div className="col-lg-5">
            <div className="small text-muted mb-1">Cómo se calcula tu huella</div>
            <div className="fs-5 fw-bold">kg CO₂ = consumo × factor de emisión</div>
            <div className="small text-muted mt-1">
              Ejemplo: 120 m³ de gas × 1,95 = 234 kg CO₂. Cada registro guarda el factor con el que se cargó,
              así tus meses anteriores no cambian si el factor se actualiza.
            </div>
          </div>
          <div className="col-lg-7">
            <div className="row g-2">
              {(datos?.alcances || []).map((a) => (
                <div key={a.idAlcance} className="col-md-4">
                  <div className="h-100 p-2 rounded-3 small" style={{ borderLeft: `3px solid ${colorAlcance(a.idAlcance)}`, background: 'var(--bs-tertiary-bg)' }}>
                    <div className="fw-bold">{a.nombre}</div>
                    <div className="text-muted">{a.descripcion}</div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Filtros */}
      <div className="d-flex flex-wrap gap-2 align-items-center mb-3">
        <div className="position-relative flex-grow-1" style={{ maxWidth: 420 }}>
          <i className="bi bi-search position-absolute text-muted" style={{ left: 12, top: '50%', transform: 'translateY(-50%)' }} aria-hidden="true" />
          <input type="search" className="form-control" style={{ paddingLeft: 36 }} placeholder="Buscar: luz, garrafa, colectivo, kWh..."
            value={busqueda} onChange={(e) => setBusqueda(e.target.value)} aria-label="Buscar fuente de emisión" />
        </div>
        <select className="form-select w-auto" value={idAlcance} onChange={(e) => setIdAlcance(e.target.value)} aria-label="Filtrar por alcance">
          <option value="">Todos los alcances</option>
          {(datos?.alcances || []).map((a) => <option key={a.idAlcance} value={a.idAlcance}>{a.nombre}</option>)}
        </select>
        {datos && <span className="small text-muted ms-auto">{total} {total === 1 ? 'fuente' : 'fuentes'}</span>}
      </div>

      {error ? <ErrorCarga error={error} onReintentar={recargar} />
        : cargando ? <div className="d-grid py-5" style={{ placeItems: 'center' }}><PuntosCarga texto="Cargando fuentes" /></div>
        : grupos.length === 0 ? (
          <Vacio icono="bi-search" titulo="No encontramos fuentes con esa búsqueda">
            Probá con otra palabra o elegí "Todos los alcances". Si la fuente que necesitás no existe, pedile al administrador que la agregue.
          </Vacio>
        ) : grupos.map((a) => (
          <section key={a.idAlcance} className="mb-4 aparecer" aria-labelledby={`alcance-${a.idAlcance}`}>
            <h2 id={`alcance-${a.idAlcance}`} className="h5 fw-bold mb-3 d-flex align-items-center gap-2">
              <span className="rounded-circle d-inline-block" style={{ width: 12, height: 12, background: colorAlcance(a.idAlcance) }} aria-hidden="true" />
              {a.nombre}
            </h2>
            {a.tipos.map((ti) => (
              <div key={ti.idTipoEmision} className="mb-3">
                <h3 className="h6 text-muted mb-2">{ti.nombre}</h3>
                <div className="row g-3">
                  {ti.fuentes.map((f) => (
                    <div key={f.idFuenteEmision} className="col-md-6 col-xl-4">
                      <TarjetaFuente fuente={f} color={colorAlcance(a.idAlcance)} puedeCargar={puedeCargar} />
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </section>
        ))}
    </>
  )
}
