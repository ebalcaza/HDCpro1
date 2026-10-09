import { useEffect, useState } from 'react'
import { geoApi } from '../../api/servicios'

const MESES = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre']

/**
 * Selector de período (mes + año) con selects minimalistas. Valor: "YYYY-MM" o "".
 * Se usan selects en lugar de input type=month para que se vea igual en todos los navegadores.
 */
export function SelectorMes({ valor, onChange, permitirVacio, max, min, className = '', invalido, valido, id }) {
  const [anio, mes] = valor ? valor.split('-') : ['', '']
  const anioMax = max ? Number(max.slice(0, 4)) : new Date().getFullYear()
  const anioMin = min ? Number(min.slice(0, 4)) : anioMax - 10
  const anios = []
  for (let a = anioMax; a >= anioMin; a--) anios.push(a)

  const emitir = (a, m) => {
    if (!a && !m) return onChange('')
    const aa = a || String(anioMax)
    const mm = m || '01'
    let v = `${aa}-${mm}`
    if (max && v > max) v = max
    onChange(v)
  }

  const estado = invalido ? 'is-invalid' : valido ? 'is-valid' : ''

  return (
    <div className={`d-flex gap-2 ${className}`}>
      <select id={id} className={`form-select ${estado}`} value={mes} onChange={(e) => emitir(anio, e.target.value)} aria-label="Mes">
        {permitirVacio ? <option value="">Mes</option> : !mes && <option value="" disabled>Mes</option>}
        {MESES.map((n, i) => {
          const m = String(i + 1).padStart(2, '0')
          const deshabilitado = max && anio && `${anio}-${m}` > max
          return <option key={m} value={m} disabled={deshabilitado}>{n}</option>
        })}
      </select>
      <select className={`form-select ${estado}`} style={{ maxWidth: 110 }} value={anio} onChange={(e) => emitir(e.target.value, mes)} aria-label="Año">
        {permitirVacio ? <option value="">Año</option> : !anio && <option value="" disabled>Año</option>}
        {anios.map((a) => <option key={a} value={a}>{a}</option>)}
      </select>
      {permitirVacio && valor && (
        <button type="button" className="btn btn-icono" onClick={() => onChange('')} aria-label="Limpiar período" title="Limpiar">
          <i className="bi bi-x-lg" />
        </button>
      )}
    </div>
  )
}

/**
 * Provincia → Distrito → Ciudad (datos de georef).
 * valor: { idProvincia, idDistrito, idCiudad }
 */
export function SelectorGeo({ valor, onChange, tocado, error, requerido = true, opcional, columnas = 'col-md-4' }) {
  const [provincias, setProvincias] = useState([])
  const [distritos, setDistritos] = useState([])
  const [ciudades, setCiudades] = useState([])
  const [cargando, setCargando] = useState({ p: true, d: false, c: false })

  useEffect(() => {
    geoApi.provincias().then(setProvincias).catch(() => setProvincias([])).finally(() => setCargando((c) => ({ ...c, p: false })))
  }, [])

  useEffect(() => {
    if (!valor.idProvincia) { setDistritos([]); return }
    setCargando((c) => ({ ...c, d: true }))
    geoApi.distritos(valor.idProvincia).then(setDistritos).catch(() => setDistritos([])).finally(() => setCargando((c) => ({ ...c, d: false })))
  }, [valor.idProvincia])

  useEffect(() => {
    if (!valor.idDistrito) { setCiudades([]); return }
    setCargando((c) => ({ ...c, c: true }))
    geoApi.ciudades(valor.idDistrito).then(setCiudades).catch(() => setCiudades([])).finally(() => setCargando((c) => ({ ...c, c: false })))
  }, [valor.idDistrito])

  const clase = (campo) => {
    if (!tocado || opcional) return 'form-select'
    return `form-select ${valor[campo] ? 'is-valid' : 'is-invalid'}`
  }
  const req = requerido && !opcional ? <span className="req">*</span> : null

  return (
    <>
      <div className={columnas}>
        <label className="form-label">Provincia{req}</label>
        <select className={clase('idProvincia')} value={valor.idProvincia || ''} disabled={cargando.p}
          onChange={(e) => onChange({ idProvincia: e.target.value ? Number(e.target.value) : '', idDistrito: '', idCiudad: '' })}>
          <option value="">{cargando.p ? 'Cargando…' : provincias.length ? (opcional ? 'Todas' : 'Seleccioná') : 'Sin datos (cargando georef)'}</option>
          {provincias.map((p) => <option key={p.idProvincia} value={p.idProvincia}>{p.nombre}</option>)}
        </select>
      </div>
      <div className={columnas}>
        <label className="form-label">Distrito / Departamento{req}</label>
        <select className={clase('idDistrito')} value={valor.idDistrito || ''} disabled={!valor.idProvincia || cargando.d}
          onChange={(e) => onChange({ ...valor, idDistrito: e.target.value ? Number(e.target.value) : '', idCiudad: '' })}>
          <option value="">{cargando.d ? 'Cargando…' : opcional ? 'Todos' : 'Seleccioná'}</option>
          {distritos.map((d) => <option key={d.idDistrito} value={d.idDistrito}>{d.nombre}</option>)}
        </select>
      </div>
      <div className={columnas}>
        <label className="form-label">Ciudad / Localidad{req}</label>
        <select className={clase('idCiudad')} value={valor.idCiudad || ''} disabled={!valor.idDistrito || cargando.c}
          onChange={(e) => onChange({ ...valor, idCiudad: e.target.value ? Number(e.target.value) : '' })}>
          <option value="">{cargando.c ? 'Cargando…' : opcional ? 'Todas' : 'Seleccioná'}</option>
          {ciudades.map((c) => <option key={c.idCiudad} value={c.idCiudad}>{c.nombre}</option>)}
        </select>
        {tocado && error && <div className="invalid-feedback d-block">{error}</div>}
      </div>
    </>
  )
}

/** Grupo de chips multi-selección */
export function Chips({ opciones, seleccion, onChange, color }) {
  const alternar = (v) => onChange(seleccion.includes(v) ? seleccion.filter((x) => x !== v) : [...seleccion, v])
  return (
    <div className="d-flex flex-wrap gap-2">
      {opciones.map((o) => (
        <button key={o.valor} type="button" className={`chip ${seleccion.includes(o.valor) ? 'activo' : ''}`} onClick={() => alternar(o.valor)} aria-pressed={seleccion.includes(o.valor)}>
          {color && <span className="punto-serie" style={{ background: color(o) }} />}
          {seleccion.includes(o.valor) && <i className="bi bi-check2" />}
          {o.texto}
        </button>
      ))}
    </div>
  )
}
