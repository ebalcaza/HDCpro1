import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { catalogoApi, colaboracionesApi, domiciliosApi, registrosApi } from '../../api/servicios'
import { PERMISOS as P, useAuth } from '../../context/AuthContext.jsx'
import { useUI } from '../../context/UIContext.jsx'
import { BotonCarga, EncabezadoPagina, ErrorCarga } from '../../components/comunes/Basicos.jsx'
import { EsqueletoTarjeta } from '../../components/comunes/Esqueleto.jsx'
import { SelectorMes } from '../../components/comunes/Selectores.jsx'
import { ValorAnimado } from '../../components/comunes/Destello.jsx'
import { aMes, desdeMes, mesActual, numero, periodo, toneladas } from '../../utils/formato'

let siguienteClave = 1
const filaVacia = (idFuenteEmision = '') => ({ clave: siguienteClave++, idAlcance: '', idTipoEmision: '', idFuenteEmision, consumo: '' })

/**
 * RFR01 / RFR03: alta y edición de un registro de emisiones.
 * Cada fila permite filtrar por alcance y tipo de emisión para acortar la lista de fuentes.
 */
export default function RegistroForm() {
  const { id } = useParams()
  const [query] = useSearchParams()
  const editando = !!id
  const navigate = useNavigate()
  const ui = useUI()
  const { tienePermiso } = useAuth()

  const [cat, setCat] = useState(null)
  const [error, setError] = useState(null)
  const [idDomicilio, setIdDomicilio] = useState(query.get('domicilio') || '')
  const [mes, setMes] = useState(mesActual())
  const [observaciones, setObservaciones] = useState('')
  const [filas, setFilas] = useState([filaVacia()])
  const [intentoEnvio, setIntentoEnvio] = useState(false)
  const [enviando, setEnviando] = useState(false)

  useEffect(() => {
    Promise.all([
      catalogoApi('fuentes').listar(),
      catalogoApi('alcances').listar(),
      catalogoApi('tipos-emision').listar(),
      tienePermiso(P.GESTIONAR_DOMICILIOS) ? domiciliosApi.listar() : Promise.resolve([]),
      tienePermiso(P.COLABORAR) ? colaboracionesApi.recibidas() : Promise.resolve([]),
      editando ? registrosApi.obtener(id) : Promise.resolve(null),
    ]).then(([fuentes, alcances, tipos, domicilios, colaboraciones, registro]) => {
      setCat({ fuentes, alcances, tipos, domicilios, colaboraciones: colaboraciones.filter((c) => c.estado === 'ACEPTADA') })
      if (registro) {
        if (!registro.puedeEditar) { ui.error('No podés editar este registro'); navigate('/emisiones'); return }
        setIdDomicilio(String(registro.idDomicilio))
        setMes(aMes(registro.periodo))
        setObservaciones(registro.observaciones || '')
        setFilas(registro.detalles.map((d) => ({ ...filaVacia(d.idFuenteEmision), consumo: String(d.consumo), factorGuardado: d.valorFactorEmision, nombreFuente: d.fuente })))
      }
    }).catch(setError)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  /** Opciones de domicilio: propios + colaboraciones aceptadas */
  const colaboracion = useMemo(() => {
    if (!cat || !idDomicilio) return null
    const propio = cat.domicilios.some((d) => d.idDomicilio === Number(idDomicilio))
    if (propio) return null
    const colabs = cat.colaboraciones.filter((c) => c.idDomicilio === Number(idDomicilio))
    return colabs.length ? { tipos: new Set(colabs.flatMap((c) => c.tiposEmision.map((t) => t.idTipoEmision))), colabs } : null
  }, [cat, idDomicilio])

  // Al elegir un domicilio propio (alta), se precargan sus fuentes habituales
  const elegirDomicilio = (valor) => {
    setIdDomicilio(valor)
    if (editando || !cat) return
    const d = cat.domicilios.find((x) => x.idDomicilio === Number(valor))
    const vacias = filas.every((f) => !f.idFuenteEmision && !f.consumo)
    if (d?.fuentes.length && vacias) {
      setFilas(d.fuentes.map((f) => filaVacia(f.idFuenteEmision)))
      ui.aviso(`Se precargaron ${d.fuentes.length} fuentes habituales del domicilio. Completá los consumos.`, 'Fuentes precargadas')
    }
  }

  const fuentesPermitidas = useMemo(() => {
    if (!cat) return []
    return cat.fuentes.filter((f) => !colaboracion || colaboracion.tipos.has(f.idTipoEmision))
  }, [cat, colaboracion])

  const mapaFuentes = useMemo(() => new Map((cat?.fuentes || []).map((f) => [f.idFuenteEmision, f])), [cat])

  const actualizarFila = (clave, cambios) => setFilas((fs) => fs.map((f) => (f.clave === clave ? { ...f, ...cambios } : f)))

  const kgFila = (f) => {
    const fuente = mapaFuentes.get(Number(f.idFuenteEmision))
    const consumo = Number(f.consumo)
    if (!consumo || consumo <= 0) return 0
    const factor = f.factorGuardado ?? fuente?.valorFactorEmision ?? 0
    return consumo * factor
  }
  const total = filas.reduce((a, f) => a + kgFila(f), 0)

  const repetidas = useMemo(() => {
    const vistos = new Set(), rep = new Set()
    filas.forEach((f) => { if (f.idFuenteEmision) { if (vistos.has(f.idFuenteEmision)) rep.add(f.idFuenteEmision); vistos.add(f.idFuenteEmision) } })
    return rep
  }, [filas])

  const errorFila = (f) => ({
    fuente: !f.idFuenteEmision ? 'Elegí una fuente' : repetidas.has(f.idFuenteEmision) ? 'Fuente repetida' : null,
    consumo: !f.consumo || Number(f.consumo) <= 0 ? 'Mayor a 0' : null,
  })
  const erroresGenerales = {
    domicilio: !idDomicilio ? 'Elegí el domicilio' : null,
    periodo: !mes ? 'Elegí el período' : mes > mesActual() ? 'No puede ser futuro' : null,
  }
  const hayErrores = Object.values(erroresGenerales).some(Boolean) || filas.length === 0 || filas.some((f) => Object.values(errorFila(f)).some(Boolean))

  const clase = (base, err) => (intentoEnvio ? `${base} ${err ? 'is-invalid' : 'is-valid'}` : base)

  const enviar = async (e) => {
    e.preventDefault()
    setIntentoEnvio(true)
    if (hayErrores) return ui.error('Revisá los campos marcados en rojo', 'Datos incompletos')
    const datos = {
      idDomicilio: Number(idDomicilio),
      periodo: desdeMes(mes),
      observaciones: observaciones || null,
      detalles: filas.map((f) => ({ idFuenteEmision: Number(f.idFuenteEmision), consumo: Number(f.consumo) })),
    }
    setEnviando(true)
    try {
      const r = editando ? await registrosApi.actualizar(id, datos) : await registrosApi.crear(datos)
      ui.exito(`${editando ? 'Se actualizó' : 'Se guardó'} el registro de ${periodo(r.periodo, true)}: ${numero(r.totalKgCo2)} kg CO₂`, editando ? 'Registro actualizado' : 'Registro guardado')
      navigate(`/emisiones/${r.idRegistro}`)
    } catch (err) {
      ui.error(err)
    } finally {
      setEnviando(false)
    }
  }

  if (error) return <ErrorCarga error={error} />
  if (!cat) return <><EncabezadoPagina titulo={editando ? 'Editar registro' : 'Nuevo registro'} icono="bi-cloud-plus" /><EsqueletoTarjeta alto={320} /></>

  const sinDomicilios = cat.domicilios.length === 0 && cat.colaboraciones.length === 0

  return (
    <form noValidate onSubmit={enviar}>
      <EncabezadoPagina
        titulo={editando ? 'Editar registro de emisiones' : 'Nuevo registro de emisiones'}
        descripcion="Cargá los consumos del mes. Las emisiones se calculan como consumo × factor de emisión."
        icono="bi-cloud-plus">
        <Link to="/emisiones" className="btn btn-outline-secondary"><i className="bi bi-arrow-left me-1" />Volver</Link>
      </EncabezadoPagina>

      {sinDomicilios ? (
        <div className="tarjeta tarjeta-cuerpo text-center py-5">
          <i className="bi bi-house-add fs-1 text-muted d-block mb-2" />
          <p>Necesitás un domicilio propio o una colaboración aceptada para cargar emisiones.</p>
          {tienePermiso(P.GESTIONAR_DOMICILIOS) && <Link to="/domicilios" className="btn btn-primary">Agregar domicilio</Link>}
        </div>
      ) : (
        <div className="row g-4">
          <div className="col-lg-8">
            <div className="tarjeta tarjeta-cuerpo mb-4">
              <div className="row g-3">
                <div className="col-md-7">
                  <label className="form-label">Domicilio<span className="req">*</span></label>
                  <select className={clase('form-select', erroresGenerales.domicilio)} value={idDomicilio} disabled={editando} onChange={(e) => elegirDomicilio(e.target.value)}>
                    <option value="">Seleccioná</option>
                    {cat.domicilios.length > 0 && (
                      <optgroup label="Mis domicilios">
                        {cat.domicilios.map((d) => <option key={d.idDomicilio} value={d.idDomicilio}>{d.descripcion}</option>)}
                      </optgroup>
                    )}
                    {cat.colaboraciones.length > 0 && (
                      <optgroup label="Como colaborador">
                        {[...new Map(cat.colaboraciones.map((c) => [c.idDomicilio, c])).values()].map((c) => (
                          <option key={c.idDomicilio} value={c.idDomicilio}>{c.domicilio} — {c.titular}</option>
                        ))}
                      </optgroup>
                    )}
                  </select>
                  {intentoEnvio && erroresGenerales.domicilio && <div className="invalid-feedback d-block">{erroresGenerales.domicilio}</div>}
                </div>
                <div className="col-md-5">
                  <label className="form-label">Período<span className="req">*</span></label>
                  <SelectorMes valor={mes} max={mesActual()} onChange={setMes} invalido={intentoEnvio && erroresGenerales.periodo} valido={intentoEnvio && !erroresGenerales.periodo} />
                  {intentoEnvio && erroresGenerales.periodo && <div className="invalid-feedback d-block">{erroresGenerales.periodo}</div>}
                </div>
              </div>
              {colaboracion && (
                <div className="alert alert-info mt-3 mb-0 small d-flex gap-2" style={{ background: 'color-mix(in srgb, var(--azul) 10%, transparent)', borderColor: 'transparent', color: 'var(--texto)' }}>
                  <i className="bi bi-people" />
                  <span>Cargás como <b>colaborador</b>. Solo podés usar: {[...new Set(colaboracion.colabs.flatMap((c) => c.tiposEmision.map((t) => t.nombre)))].join(', ')}.</span>
                </div>
              )}
            </div>

            <div className="tarjeta">
              <div className="d-flex align-items-center p-3 pb-0">
                <h2 className="tarjeta-titulo">Consumos</h2>
                <span className="text-muted small ms-2">({filas.length})</span>
              </div>
              <div className="p-3 d-flex flex-column gap-3">
                {filas.map((f, i) => {
                  const err = errorFila(f)
                  const tiposVisibles = cat.tipos.filter((t) => (!f.idAlcance || t.idAlcance === Number(f.idAlcance)) && (!colaboracion || colaboracion.tipos.has(t.idTipoEmision)))
                  const opciones = fuentesPermitidas.filter((x) =>
                    (!f.idAlcance || x.idAlcance === Number(f.idAlcance)) && (!f.idTipoEmision || x.idTipoEmision === Number(f.idTipoEmision)))
                  const fuente = mapaFuentes.get(Number(f.idFuenteEmision))
                  // Si la fuente guardada fue dada de baja, se sigue mostrando para no perderla al editar
                  const incluyeActual = !f.idFuenteEmision || opciones.some((o) => o.idFuenteEmision === Number(f.idFuenteEmision))
                  return (
                    <div key={f.clave} className="p-3 rounded-3 aparecer" style={{ border: '1px solid var(--borde)', background: 'var(--superficie-2)' }}>
                      <div className="d-flex align-items-center mb-2">
                        <span className="badge-suave badge-gris">#{i + 1}</span>
                        {fuente && <span className="small text-muted ms-2">{fuente.tipoEmision} · {fuente.alcance}</span>}
                        <button type="button" className="btn btn-icono peligro ms-auto" aria-label="Quitar consumo" title="Quitar"
                          disabled={filas.length === 1} onClick={() => setFilas((fs) => fs.filter((x) => x.clave !== f.clave))}>
                          <i className="bi bi-x-lg" />
                        </button>
                      </div>
                      <div className="row g-2">
                        <div className="col-6 col-md-3">
                          <label className="form-label small">Alcance</label>
                          <select className="form-select form-select-sm" value={f.idAlcance}
                            onChange={(e) => actualizarFila(f.clave, { idAlcance: e.target.value, idTipoEmision: '' })}>
                            <option value="">Todos</option>
                            {cat.alcances.map((a) => <option key={a.idAlcance} value={a.idAlcance}>{a.nombre}</option>)}
                          </select>
                        </div>
                        <div className="col-6 col-md-3">
                          <label className="form-label small">Tipo de emisión</label>
                          <select className="form-select form-select-sm" value={f.idTipoEmision}
                            onChange={(e) => actualizarFila(f.clave, { idTipoEmision: e.target.value })}>
                            <option value="">Todos</option>
                            {tiposVisibles.map((t) => <option key={t.idTipoEmision} value={t.idTipoEmision}>{t.nombre}</option>)}
                          </select>
                        </div>
                        <div className="col-md-6">
                          <label className="form-label small">Fuente de emisión<span className="req">*</span></label>
                          <select className={clase('form-select form-select-sm', err.fuente)} value={f.idFuenteEmision}
                            onChange={(e) => actualizarFila(f.clave, { idFuenteEmision: e.target.value ? Number(e.target.value) : '', factorGuardado: undefined })}>
                            <option value="">Seleccioná ({opciones.length})</option>
                            {!incluyeActual && <option value={f.idFuenteEmision}>{f.nombreFuente || 'Fuente actual'}</option>}
                            {opciones.map((o) => <option key={o.idFuenteEmision} value={o.idFuenteEmision}>{o.nombre}</option>)}
                          </select>
                          {intentoEnvio && err.fuente && <div className="invalid-feedback d-block">{err.fuente}</div>}
                        </div>
                        <div className="col-md-6">
                          <label className="form-label small">Consumo<span className="req">*</span></label>
                          <div className="input-group input-group-sm">
                            <input type="number" min="0" step="any" inputMode="decimal" className={clase('form-control', err.consumo)}
                              value={f.consumo} onChange={(e) => actualizarFila(f.clave, { consumo: e.target.value })} placeholder="0" />
                            <span className="input-group-text">{fuente?.unidadMedida || 'unidad'}</span>
                          </div>
                          {intentoEnvio && err.consumo && <div className="invalid-feedback d-block">{err.consumo}</div>}
                        </div>
                        <div className="col-md-6 d-flex align-items-end justify-content-md-end">
                          <div className="text-md-end small">
                            <span className="text-muted">Factor {numero(f.factorGuardado ?? fuente?.valorFactorEmision ?? 0, 6)} kg/{fuente?.unidadMedida || 'u'} → </span>
                            <b className="fs-6"><ValorAnimado valor={kgFila(f)}>{numero(kgFila(f))}</ValorAnimado> kg CO₂</b>
                          </div>
                        </div>
                      </div>
                    </div>
                  )
                })}
                <button type="button" className="btn btn-suave align-self-start" onClick={() => setFilas((fs) => [...fs, filaVacia()])}>
                  <i className="bi bi-plus-lg me-1" />Agregar consumo
                </button>
              </div>
            </div>
          </div>

          <div className="col-lg-4">
            <div className="tarjeta tarjeta-cuerpo position-sticky" style={{ top: 80 }}>
              <div className="dato-etiqueta">Total estimado</div>
              <div className="kpi-valor fs-2 fw-bold"><ValorAnimado valor={Math.round(total)}>{toneladas(total)}</ValorAnimado> <small className="fs-6 text-muted">t CO₂</small></div>
              <div className="text-muted mb-3">{numero(total)} kg CO₂ · {periodo(mes, true)}</div>
              <label className="form-label">Observaciones</label>
              <textarea className="form-control mb-3" rows={3} maxLength={500} value={observaciones} onChange={(e) => setObservaciones(e.target.value)}
                placeholder="Ej: factura de gas bimestral dividida en dos meses" />
              <BotonCarga cargando={enviando} className="btn btn-primary w-100">
                <i className="bi bi-save me-1" />{editando ? 'Guardar cambios' : 'Guardar registro'}
              </BotonCarga>
              {editando && <div className="form-text mt-2">Las fuentes que ya estaban conservan el factor con el que se cargaron.</div>}
            </div>
          </div>
        </div>
      )}
    </form>
  )
}
