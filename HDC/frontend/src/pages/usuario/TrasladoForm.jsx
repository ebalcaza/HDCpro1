import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { catalogoApi, colaboracionesApi, domiciliosApi, trasladosApi } from '../../api/servicios'
import { PERMISOS as P, useAuth } from '../../context/AuthContext.jsx'
import { useUI } from '../../context/UIContext.jsx'
import { BotonCarga, EncabezadoPagina, ErrorCarga } from '../../components/comunes/Basicos.jsx'
import { EsqueletoTarjeta } from '../../components/comunes/Esqueleto.jsx'
import { SelectorMes } from '../../components/comunes/Selectores.jsx'
import InputMiles from '../../components/comunes/InputMiles.jsx'
import { aMes, desdeMes, mesActual, numero, periodo, PERIODICIDADES } from '../../utils/formato'

/** Suma meses a un texto "YYYY-MM" */
const sumarMeses = (mes, cantidad) => {
  const [a, m] = mes.split('-').map(Number)
  const d = new Date(a, m - 1 + cantidad, 1)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
}

/** Lista de meses entre dos textos "YYYY-MM", ambos incluidos */
const mesesEntre = (desde, hasta) => {
  const lista = []
  for (let m = desde; m <= hasta; m = sumarMeses(m, 1)) lista.push(m)
  return lista
}

let siguienteClave = 1
const medioVacio = (idFuenteEmision = '') => ({ clave: siguienteClave++, idFuenteEmision, distanciaKm: '', diasSemana: '' })

/**
 * Traslados (v2.6, ampliado en v2.8): cómo viaja una persona hasta el domicilio.
 * Admite varios medios de transporte, cada uno con su distancia y sus días por semana.
 * - En una convocatoria, el período y las semanas los define el organizador: quien colabora elige si carga
 *   todo el período de una vez o un mes por vez.
 * - El titular y quien fue invitado por email eligen período mensual, semestral o anual.
 * Solo se generan meses hasta el actual. Quien colabora no ve los kg de CO₂ en este formulario.
 * Ver TrasladoServicio.cs en el backend.
 */
export default function TrasladoForm() {
  const { id } = useParams()
  const [query] = useSearchParams()
  const editando = !!id
  // Llegó con el botón Cargar de "Donde colaboro": el domicilio y la convocatoria ya están definidos
  const convocatoriaElegida = editando ? '' : query.get('convocatoria') || ''
  const navigate = useNavigate()
  const ui = useUI()
  const { tienePermiso } = useAuth()

  const [cat, setCat] = useState(null)
  const [opciones, setOpciones] = useState(null)
  const [error, setError] = useState(null)
  const [enviando, setEnviando] = useState(false)
  const [intentoEnvio, setIntentoEnvio] = useState(false)
  const [nombresGuardados, setNombresGuardados] = useState({})
  const [medios, setMedios] = useState([medioVacio(Number(query.get('fuente')) || '')])
  const [cambiarRol, setCambiarRol] = useState(false)
  // Meses de la carga que se está editando: no cuentan como "ya cargados" para sí misma
  const [mesesDeEstaCarga, setMesesDeEstaCarga] = useState(() => new Set())
  const [v, setV] = useState({
    idDomicilio: query.get('domicilio') || '', idConvocatoria: convocatoriaElegida, idConvocatoriaRol: '',
    // En convocatorias: PERIODO (todo de una vez) o MENSUAL. Fuera de ellas: MENSUAL, SEMESTRAL o ANUAL.
    periodicidad: convocatoriaElegida ? 'PERIODO' : 'MENSUAL', desde: mesActual(), semanas: String(PERIODICIDADES.MENSUAL.semanas), cantidadPersonas: '1',
  })
  const cambiar = (cambios) => setV((x) => ({ ...x, ...cambios }))
  const cambiarMedio = (clave, cambios) => setMedios((ms) => ms.map((m) => (m.clave === clave ? { ...m, ...cambios } : m)))

  useEffect(() => {
    Promise.all([
      catalogoApi('fuentes').listar(),
      tienePermiso(P.GESTIONAR_DOMICILIOS) ? domiciliosApi.listar() : Promise.resolve([]),
      tienePermiso(P.COLABORAR) ? colaboracionesApi.recibidas() : Promise.resolve([]),
      editando ? trasladosApi.obtener(id) : Promise.resolve(null),
    ]).then(([fuentes, domicilios, colaboraciones, carga]) => {
      setCat({ medios: fuentes.filter((f) => f.modoCarga === 'TRASLADO'), domicilios, colaboraciones: colaboraciones.filter((c) => c.estado === 'ACEPTADA') })
      if (carga) {
        // Se conservan los nombres por si alguno de los medios guardados ya fue dado de baja
        setNombresGuardados(Object.fromEntries(carga.medios.map((m) => [m.idFuenteEmision, m.fuente])))
        setMesesDeEstaCarga(new Set(mesesEntre(aMes(carga.periodoDesde), aMes(carga.periodoHasta))))
        setMedios(carga.medios.map((m) => ({ ...medioVacio(m.idFuenteEmision), distanciaKm: String(Number(m.distanciaKm)), diasSemana: String(Number(m.diasSemana)) })))
        setV({
          idDomicilio: String(carga.idDomicilio), idConvocatoria: carga.idConvocatoria ? String(carga.idConvocatoria) : '',
          idConvocatoriaRol: carga.idConvocatoriaRol ? String(carga.idConvocatoriaRol) : '', periodicidad: carga.periodicidad,
          desde: aMes(carga.periodoDesde), semanas: String(Number(carga.semanas)), cantidadPersonas: String(carga.cantidadPersonas),
        })
      }
    }).catch(setError)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  // Las convocatorias, los roles y los meses disponibles dependen del domicilio y de la persona
  useEffect(() => {
    setOpciones(null)
    if (!v.idDomicilio) return
    let vigente = true
    trasladosApi.opciones(v.idDomicilio).then((o) => {
      if (!vigente) return
      setOpciones(o)
      setV((x) => {
        // Se respeta la convocatoria guardada o la que trajo el botón, si la persona puede cargar en ella
        if (x.idConvocatoria && (editando || o.convocatorias.some((c) => String(c.idConvocatoria) === x.idConvocatoria))) return x
        // Si participa en una sola convocatoria, ya queda elegida y se propone cargar todo el período
        if (o.convocatorias.length === 1) return { ...x, idConvocatoria: String(o.convocatorias[0].idConvocatoria), periodicidad: 'PERIODO' }
        return x.idConvocatoria ? { ...x, idConvocatoria: '' } : x
      })
    }).catch((err) => { if (vigente) ui.error(err) })
    return () => { vigente = false }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [v.idDomicilio])

  // Medios habilitados: el titular ve todos; quien colabora, los de los tipos que le habilitaron
  const mediosDisponibles = useMemo(() => {
    if (!cat || !v.idDomicilio) return []
    if (cat.domicilios.some((d) => d.idDomicilio === Number(v.idDomicilio))) return cat.medios
    const tipos = new Set(cat.colaboraciones.filter((c) => c.idDomicilio === Number(v.idDomicilio)).flatMap((c) => c.tiposEmision.map((t) => t.idTipoEmision)))
    return cat.medios.filter((m) => tipos.has(m.idTipoEmision))
  }, [cat, v.idDomicilio])

  const domiciliosColaboracion = useMemo(() => {
    if (!cat) return []
    const tiposTraslado = new Set(cat.medios.map((m) => m.idTipoEmision))
    return [...new Map(cat.colaboraciones.filter((c) => c.tiposEmision.some((t) => tiposTraslado.has(t.idTipoEmision))).map((c) => [c.idDomicilio, c])).values()]
  }, [cat])

  const esTitular = opciones?.esTitular ?? false
  const convocatorias = opciones?.convocatorias || []
  const conv = convocatorias.find((c) => c.idConvocatoria === Number(v.idConvocatoria))
  const enConvocatoria = !!conv
  const roles = enConvocatoria ? conv.roles : opciones?.roles || []
  const hayVariasConvocatorias = !enConvocatoria && new Set(roles.map((r) => r.convocatoria)).size > 1

  // Se propone el rol que la persona ya eligió en la convocatoria, para no pedírselo en cada carga
  useEffect(() => {
    if (!editando && conv?.idRolElegido) setV((x) => (x.idConvocatoriaRol ? x : { ...x, idConvocatoriaRol: String(conv.idRolElegido) }))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [conv?.idConvocatoria, conv?.idRolElegido])

  // Con un rol ya elegido en la convocatoria no se vuelve a pedir: se muestra, y se puede cambiar si hace falta
  const rolFijado = !editando && !cambiarRol && !!conv?.idRolElegido && String(conv.idRolElegido) === v.idConvocatoriaRol

  // ---------- Período a cargar y meses que se van a generar ----------
  const hoy = mesActual()
  const configPeriodo = PERIODICIDADES[v.periodicidad]
  const mesesConvocatoria = useMemo(() => (conv ? mesesEntre(aMes(conv.periodoDesde), aMes(conv.periodoHasta)) : []), [conv])
  const mesesElegidos = useMemo(() => {
    if (enConvocatoria) return v.periodicidad === 'PERIODO' ? mesesConvocatoria : [v.desde]
    return Array.from({ length: configPeriodo?.meses || 1 }, (_, i) => sumarMeses(v.desde, i))
  }, [enConvocatoria, v.periodicidad, v.desde, mesesConvocatoria, configPeriodo])
  const meses = mesesElegidos.filter((m) => m <= hoy)
  const ultimoMes = meses[meses.length - 1]
  const mesesCargados = useMemo(() => new Set((conv?.mesesCargados || []).map(aMes)), [conv])
  const mesesFaltantes = mesesConvocatoria.filter((m) => m <= hoy && !mesesCargados.has(m))

  // ---------- Lo ya cargado no se vuelve a grabar: solo se cambia editando esa carga (v2.11) ----------
  // Meses que se pueden elegir: los que faltan y, al editar, los de esta misma carga
  const mesesLibres = useMemo(() => mesesConvocatoria.filter((m) => m <= hoy && (!mesesCargados.has(m) || mesesDeEstaCarga.has(m))), [mesesConvocatoria, hoy, mesesCargados, mesesDeEstaCarga])
  // Cargar todo el período chocaría con cualquier mes ya cargado en otra carga
  const hayMesesEnOtraCarga = mesesConvocatoria.some((m) => mesesCargados.has(m) && !mesesDeEstaCarga.has(m))
  // Ya no queda ningún mes por cargar: el formulario se bloquea y solo ofrece editar
  const trasladoCompleto = enConvocatoria && !editando && mesesCargados.size > 0 && mesesLibres.length === 0
  // Cargas ya guardadas en esta convocatoria (o fuera de ellas), con enlace para editarlas
  const otrasCargas = (opciones?.cargasPropias || []).filter((x) => x.idConvocatoria === (conv?.idConvocatoria ?? null) && String(x.idCargaTraslado) !== id)
  // Invitación por email: cada quien elige sus meses, así que el choque se detecta con los meses elegidos.
  // Una edición que no suma meses queda exceptuada, igual que en TrasladoServicio
  const chocaConOtraCarga = !esTitular && !enConvocatoria && !meses.every((m) => mesesDeEstaCarga.has(m))
    && otrasCargas.some((x) => meses.some((m) => m >= aMes(x.periodoDesde) && m <= aMes(x.periodoHasta)))

  // Con meses ya cargados no se puede cargar todo el período: se pasa a un mes por vez, en uno que falte
  useEffect(() => {
    if (!enConvocatoria || mesesLibres.length === 0) return
    if (v.periodicidad === 'PERIODO' && hayMesesEnOtraCarga) cambiar({ periodicidad: 'MENSUAL', desde: mesesLibres[0] })
    else if (v.periodicidad === 'MENSUAL' && !mesesLibres.includes(v.desde)) cambiar({ desde: mesesLibres[0] })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [enConvocatoria, mesesLibres, hayMesesEnOtraCarga, v.periodicidad])

  const semanasPeriodo = enConvocatoria ? conv.semanasTraslado : Number(v.semanas)
  const mesesDelPeriodo = enConvocatoria ? conv.mesesPeriodo : configPeriodo?.meses || 1
  const semanasPorMes = semanasPeriodo / mesesDelPeriodo
  const personas = esTitular ? Number(v.cantidadPersonas) : 1

  /** Km de un medio en todos los meses que se van a generar */
  const kmMedio = (m) => Number(m.distanciaKm) * Number(m.diasSemana) * semanasPorMes * personas * meses.length
  const kmTotal = medios.reduce((a, m) => a + kmMedio(m), 0)
  const factorDe = (m) => mediosDisponibles.find((x) => x.idFuenteEmision === Number(m.idFuenteEmision))?.valorFactorEmision ?? 0
  const kgTotal = medios.reduce((a, m) => a + kmMedio(m) * factorDe(m), 0)

  // ---------- Validaciones ----------
  const elegidos = medios.map((m) => m.idFuenteEmision).filter(Boolean)
  const errorMedio = (m) => ({
    medio: !m.idFuenteEmision ? 'Elegí el medio de transporte' : elegidos.filter((x) => x === m.idFuenteEmision).length > 1 ? 'Ya está en otra fila' : null,
    distancia: !(Number(m.distanciaKm) > 0) ? 'Debe ser mayor a 0' : Number(m.distanciaKm) > 100000 ? 'Revisá la distancia' : null,
    dias: !(Number(m.diasSemana) >= 1 && Number(m.diasSemana) <= 7) ? 'Entre 1 y 7' : null,
  })
  const hayMedioIncompleto = medios.some((m) => Object.values(errorMedio(m)).some(Boolean))

  const desdeMinimo = opciones?.desdeMinimo ? aMes(opciones.desdeMinimo) : null
  const hastaMaximo = opciones?.hastaMaximo ? aMes(opciones.hastaMaximo) : hoy
  const errores = {
    domicilio: !v.idDomicilio ? 'Elegí el domicilio' : null,
    convocatoria: convocatorias.length > 0 && !esTitular && !enConvocatoria ? 'Elegí la convocatoria' : null,
    rol: !esTitular && roles.length > 0 && !v.idConvocatoriaRol ? 'Elegí tu rol' : null,
    desde: trasladoCompleto ? 'Ya cargaste tu traslado en esta convocatoria'
      : enConvocatoria
      ? (v.periodicidad === 'MENSUAL' && !mesesConvocatoria.includes(v.desde) ? 'Elegí un mes del período de la convocatoria'
        : v.periodicidad === 'MENSUAL' && v.desde > hoy ? 'No puede ser un mes futuro'
          : v.periodicidad === 'MENSUAL' && !mesesLibres.includes(v.desde) ? 'Ese mes ya está cargado: editá esa carga'
            : meses.length === 0 ? 'El período todavía no empezó' : null)
      : !v.desde ? 'Elegí el mes' : v.desde > hoy ? 'No puede ser un mes futuro'
        : (desdeMinimo && v.desde < desdeMinimo) || (ultimoMes && ultimoMes > hastaMaximo)
          ? `Tu colaboración permite cargar de ${periodo(desdeMes(desdeMinimo || v.desde))} a ${periodo(desdeMes(hastaMaximo))}`
          : chocaConOtraCarga ? 'Ya cargaste tu traslado en alguno de esos meses: editá esa carga' : null,
    semanas: enConvocatoria ? null : !(Number(v.semanas) > 0) ? 'Debe ser mayor a 0'
      : configPeriodo && Number(v.semanas) > configPeriodo.semanasMaximas ? `Máximo ${configPeriodo.semanasMaximas}` : null,
    personas: esTitular && !(Number.isInteger(personas) && personas >= 1) ? 'Debe ser un número entero, 1 o más' : null,
  }
  const hayErrores = Object.values(errores).some(Boolean) || hayMedioIncompleto
  const clase = (base, err) => (intentoEnvio ? `${base} ${err ? 'is-invalid' : 'is-valid'}` : base)
  const mensaje = (err) => (intentoEnvio && err ? <div className="invalid-feedback d-block">{err}</div> : null)

  const enviar = async (e) => {
    e.preventDefault()
    setIntentoEnvio(true)
    if (hayErrores) return ui.error('Revisá los campos marcados en rojo', 'Datos incompletos')
    const datos = {
      idDomicilio: Number(v.idDomicilio), idConvocatoria: enConvocatoria ? conv.idConvocatoria : null,
      idConvocatoriaRol: v.idConvocatoriaRol ? Number(v.idConvocatoriaRol) : null, periodicidad: v.periodicidad,
      periodoDesde: desdeMes(enConvocatoria && v.periodicidad === 'PERIODO' ? aMes(conv.periodoDesde) : v.desde),
      semanas: semanasPeriodo, cantidadPersonas: personas,
      medios: medios.map((m) => ({ idFuenteEmision: Number(m.idFuenteEmision), distanciaKm: Number(m.distanciaKm), diasSemana: Number(m.diasSemana) })),
    }
    setEnviando(true)
    try {
      const r = editando ? await trasladosApi.actualizar(id, datos) : await trasladosApi.crear(datos)
      ui.exito(`Se ${editando ? 'actualizó' : 'guardó'} el traslado de ${r.mesesCargados === 1 ? '1 mes' : `${r.mesesCargados} meses`}`, 'Traslado guardado')
      navigate(`/emisiones/${r.idPrimerRegistro}`)
    } catch (err) { ui.error(err) } finally { setEnviando(false) }
  }

  const eliminar = async () => {
    const ok = await ui.confirmar({
      titulo: '¿Eliminar este traslado?', tipo: 'peligro', textoConfirmar: 'Eliminar',
      mensaje: 'Se eliminan los registros de todos los meses de esta carga. Esta acción no se puede deshacer.',
    })
    if (!ok) return
    try { await trasladosApi.eliminar(id); ui.exito('Traslado eliminado'); navigate('/emisiones') } catch (err) { ui.error(err) }
  }

  if (error) return <ErrorCarga error={error} />
  if (!cat) return <><EncabezadoPagina titulo="Traslado" icono="bi-bus-front" /><EsqueletoTarjeta alto={320} /></>

  /** Enlaces para editar lo ya cargado: es la única forma de volver a grabar esos meses */
  const enlacesEditar = otrasCargas.length > 0 && (
    <div className="d-flex flex-wrap gap-2 mt-2">
      {otrasCargas.map((x) => (
        <Link key={x.idCargaTraslado} to={`/traslados/${x.idCargaTraslado}/editar`} className="btn btn-sm btn-suave">
          <i className="bi bi-pencil-square me-1" />Editar {periodo(x.periodoDesde)}{x.periodoHasta !== x.periodoDesde ? ` a ${periodo(x.periodoHasta)}` : ''}
        </Link>
      ))}
    </div>
  )

  return (
    <form noValidate onSubmit={enviar}>
      <EncabezadoPagina titulo={editando ? 'Editar traslado' : 'Cargar traslado'} icono="bi-bus-front"
        descripcion="Contá cómo llegás al lugar: con qué medios, a qué distancia y cuántos días por semana.">
        <Link to="/emisiones" className="btn btn-outline-secondary"><i className="bi bi-arrow-left me-1" />Volver</Link>
      </EncabezadoPagina>

      <div className="row g-4">
        <div className="col-lg-8 d-flex flex-column gap-4">
          <div className="tarjeta tarjeta-cuerpo">
            <div className="row g-3">
              <div className="col-12">
                <label className="form-label" htmlFor="traslado-domicilio">Domicilio<span className="req">*</span></label>
                <select id="traslado-domicilio" className={clase('form-select', errores.domicilio)} value={v.idDomicilio} disabled={editando || !!convocatoriaElegida}
                  onChange={(e) => cambiar({ idDomicilio: e.target.value, idConvocatoria: '', idConvocatoriaRol: '' })}>
                  <option value="">Seleccioná</option>
                  {cat.domicilios.length > 0 && (
                    <optgroup label="Mis domicilios">
                      {cat.domicilios.map((d) => <option key={d.idDomicilio} value={d.idDomicilio}>{d.descripcion}</option>)}
                    </optgroup>
                  )}
                  {domiciliosColaboracion.length > 0 && (
                    <optgroup label="Como colaborador">
                      {domiciliosColaboracion.map((c) => <option key={c.idDomicilio} value={c.idDomicilio}>{c.domicilio} — {c.titular}</option>)}
                    </optgroup>
                  )}
                </select>
                {cat.domicilios.length === 0 && domiciliosColaboracion.length === 0 && (
                  <div className="form-text">Necesitás un domicilio propio o una colaboración aceptada para cargar traslados.</div>
                )}
                {mensaje(errores.domicilio)}
              </div>

              {convocatoriaElegida && enConvocatoria && (
                <div className="col-12">
                  <div className="form-label mb-0">Convocatoria</div>
                  <div className="fw-semibold">{conv.nombre}</div>
                </div>
              )}

              {!esTitular && convocatorias.length > 1 && !(convocatoriaElegida && enConvocatoria) && (
                <div className="col-12">
                  <label className="form-label" htmlFor="traslado-convocatoria">Convocatoria<span className="req">*</span></label>
                  <select id="traslado-convocatoria" className={clase('form-select', errores.convocatoria)} value={v.idConvocatoria} disabled={editando}
                    onChange={(e) => cambiar({ idConvocatoria: e.target.value, idConvocatoriaRol: '', periodicidad: 'PERIODO' })}>
                    <option value="">Seleccioná</option>
                    {convocatorias.map((c) => <option key={c.idConvocatoria} value={c.idConvocatoria}>{c.nombre}</option>)}
                  </select>
                  {mensaje(errores.convocatoria)}
                </div>
              )}

              {rolFijado && (
                <div className="col-12">
                  <div className="form-label mb-0">Tu rol</div>
                  <div className="d-flex align-items-center gap-2">
                    <span className="fw-semibold">{roles.find((r) => String(r.idConvocatoriaRol) === v.idConvocatoriaRol)?.nombre}</span>
                    {!trasladoCompleto && <button type="button" className="btn btn-sm btn-link p-0" onClick={() => setCambiarRol(true)}>Cambiar</button>}
                  </div>
                </div>
              )}

              {roles.length > 0 && !rolFijado && (
                <div className="col-12">
                  <label className="form-label" htmlFor="traslado-rol">Tu rol{!esTitular && <span className="req">*</span>}</label>
                  <select id="traslado-rol" className={clase('form-select', errores.rol)} value={v.idConvocatoriaRol} disabled={trasladoCompleto} onChange={(e) => cambiar({ idConvocatoriaRol: e.target.value })}>
                    <option value="">{esTitular ? 'Sin rol' : 'Seleccioná'}</option>
                    {roles.map((r) => <option key={r.idConvocatoriaRol} value={r.idConvocatoriaRol}>{r.nombre}{hayVariasConvocatorias ? ` — ${r.convocatoria}` : ''}</option>)}
                  </select>
                  <div className="form-text">{conv?.idRolElegido && String(conv.idRolElegido) === v.idConvocatoriaRol
                    ? 'Es el rol que ya elegiste en esta convocatoria. Si cambió, elegí otro y queda guardado para las próximas cargas.'
                    : 'Indicá tu relación con el lugar. Ayuda a ver los resultados de cada grupo.'}</div>
                  {mensaje(errores.rol)}
                </div>
              )}

              {esTitular && (
                <div className="col-md-6">
                  <label className="form-label" htmlFor="traslado-personas">Cantidad de personas<span className="req">*</span></label>
                  <InputMiles id="traslado-personas" className={clase('form-control', errores.personas)} valor={v.cantidadPersonas} onChange={(cantidadPersonas) => cambiar({ cantidadPersonas })} />
                  {intentoEnvio && errores.personas
                    ? mensaje(errores.personas)
                    : <div className="form-text">Más de 1 si cargás un grupo con los mismos hábitos de viaje (por ejemplo, datos de una encuesta).</div>}
                </div>
              )}
            </div>
          </div>

          {/* ---------- Período: en una convocatoria lo define el organizador ---------- */}
          <div className="tarjeta tarjeta-cuerpo">
            <h2 className="tarjeta-titulo mb-3">Período</h2>
            {enConvocatoria ? (
              <>
                <div className="p-3 rounded-3 mb-3 small" style={{ background: 'var(--superficie-2)', border: '1px solid var(--borde)' }}>
                  <div><i className="bi bi-info-circle me-1" />El organizador de <b>{conv.nombre}</b> definió este período:</div>
                  <div className="fw-bold fs-6 my-1">{periodo(conv.periodoDesde, true)} a {periodo(conv.periodoHasta, true)}</div>
                  <div>Tiene {conv.mesesPeriodo === 1 ? '1 mes' : `${conv.mesesPeriodo} meses`} y {numero(conv.semanasTraslado)} semanas de actividad.</div>
                  {conv.descripcion && <div className="mt-2 fst-italic">“{conv.descripcion}”</div>}
                </div>

                {!trasladoCompleto && <>
                  <div className="form-label">¿Cómo querés cargarlo?<span className="req">*</span></div>
                  <div className="d-flex flex-column gap-2 mb-3">
                    <label className="form-check">
                      <input type="radio" className="form-check-input" name="modo-carga" checked={v.periodicidad === 'PERIODO'} disabled={hayMesesEnOtraCarga} onChange={() => cambiar({ periodicidad: 'PERIODO' })} />
                      <span className="form-check-label"><b>Todo el período de una vez.</b> <span className="text-muted">{hayMesesEnOtraCarga ? 'No disponible porque ya cargaste algunos meses.' : 'Lo más rápido, si viajaste de la misma forma todo el período.'}</span></span>
                    </label>
                    <label className="form-check">
                      <input type="radio" className="form-check-input" name="modo-carga" checked={v.periodicidad === 'MENSUAL'}
                        onChange={() => cambiar({ periodicidad: 'MENSUAL', desde: mesesLibres[0] || hoy })} />
                      <span className="form-check-label"><b>Un mes por vez.</b> <span className="text-muted">Conviene si tus viajes cambiaron de un mes a otro. Repetís la carga por cada mes.</span></span>
                    </label>
                  </div>

                  {v.periodicidad === 'MENSUAL' && (
                    <div className="mb-3">
                      <label className="form-label" htmlFor="traslado-mes">Mes que vas a cargar<span className="req">*</span></label>
                      {/* Solo se ofrecen los meses disponibles: los ya cargados no se pueden volver a grabar */}
                      <select id="traslado-mes" className={clase('form-select', errores.desde)} value={v.desde} onChange={(e) => cambiar({ desde: e.target.value })}>
                        {mesesLibres.map((m) => <option key={m} value={m}>{periodo(desdeMes(m), true)}</option>)}
                      </select>
                      <div className="form-text">Cada mes cuenta como {numero(semanasPorMes)} semanas de actividad, según el período de la convocatoria.</div>
                      {mensaje(errores.desde)}
                    </div>
                  )}
                  {v.periodicidad === 'PERIODO' && mensaje(errores.desde)}
                </>}

                {trasladoCompleto ? (
                  <div className="p-3 rounded-3 small" role="status" style={{ background: 'var(--superficie-2)', border: '1px solid var(--borde)' }}>
                    <div className="fw-bold fs-6"><i className="bi bi-check2-circle me-1" />Ya cargaste tu traslado</div>
                    <div>No te falta ningún mes de los que ya empezaron. Para cambiar algún dato, editá la carga que ya guardaste.</div>
                    {enlacesEditar}
                  </div>
                ) : mesesCargados.size > 0 && (
                  <div className="small">
                    <i className="bi bi-calendar-check me-1" />Ya cargaste {mesesCargados.size === 1 ? '1 mes' : `${mesesCargados.size} meses`}.{' '}
                    {mesesFaltantes.length > 0
                      ? <>Te falta: {mesesFaltantes.map((m) => periodo(desdeMes(m))).join(', ')}.</>
                      : 'No te falta ningún mes de los que ya empezaron.'}
                    {!editando && <> Para cambiar lo que ya guardaste, editá esa carga.</>}
                    {enlacesEditar}
                  </div>
                )}
              </>
            ) : (
              <>
                <div className="btn-group w-100 mb-3" role="group" aria-label="Duración del período">
                  {Object.entries(PERIODICIDADES).map(([clave, p]) => (
                    <button key={clave} type="button" className={`btn ${v.periodicidad === clave ? 'btn-primary' : 'btn-outline-secondary'}`} aria-pressed={v.periodicidad === clave}
                      onClick={() => cambiar({ periodicidad: clave, semanas: String(p.semanas) })}>{p.texto}</button>
                  ))}
                </div>
                <div className="row g-3">
                  <div className="col-md-7">
                    <label className="form-label">{v.periodicidad === 'MENSUAL' ? 'Mes' : 'Primer mes del período'}<span className="req">*</span></label>
                    <SelectorMes valor={v.desde} max={hoy} min={desdeMinimo || undefined} onChange={(desde) => cambiar({ desde })} invalido={intentoEnvio && errores.desde} valido={intentoEnvio && !errores.desde} />
                    {mensaje(errores.desde)}
                  </div>
                  <div className="col-md-5">
                    <label className="form-label" htmlFor="traslado-semanas">Semanas con asistencia<span className="req">*</span></label>
                    <InputMiles id="traslado-semanas" className={clase('form-control', errores.semanas)} valor={v.semanas} onChange={(s) => cambiar({ semanas: s })} />
                    {intentoEnvio && errores.semanas
                      ? mensaje(errores.semanas)
                      : <div className="form-text">Si hubo receso o vacaciones, restá esas semanas.</div>}
                  </div>
                </div>
                {chocaConOtraCarga && enlacesEditar}
              </>
            )}
          </div>

          {/* ---------- Medios: se puede usar más de uno. El fieldset los desactiva todos si ya no queda nada por cargar ---------- */}
          <fieldset disabled={trasladoCompleto}>
          <div className="tarjeta">
            <div className="p-3 pb-0">
              <h2 className="tarjeta-titulo">Medios de transporte</h2>
              <div className="form-text mt-1">
                Agregá todos los que usás en el mes. Por ejemplo, si venís 3 días en colectivo y 2 en bicicleta, cargá dos filas.
                Si en un mismo viaje combinás colectivo y tren, cargá cada uno en su fila, con los mismos días.
                Cada medio va una sola vez: si tomás 3 colectivos para llegar, cargá una fila con el total de km de los tres.
              </div>
            </div>
            <div className="p-3 d-flex flex-column gap-3">
              {medios.map((m, i) => {
                const err = errorMedio(m)
                const usados = new Set(medios.filter((x) => x.clave !== m.clave && x.idFuenteEmision).map((x) => Number(x.idFuenteEmision)))
                const dadoDeBaja = m.idFuenteEmision && !mediosDisponibles.some((x) => x.idFuenteEmision === Number(m.idFuenteEmision))
                return (
                  <div key={m.clave} className="p-3 rounded-3" style={{ border: '1px solid var(--borde)', background: 'var(--superficie-2)' }}>
                    <div className="d-flex align-items-center mb-2">
                      <span className="badge-suave badge-gris">Medio {i + 1}</span>
                      <button type="button" className="btn btn-icono peligro ms-auto" aria-label={`Quitar el medio ${i + 1}`} title="Quitar"
                        disabled={medios.length === 1} onClick={() => setMedios((ms) => ms.filter((x) => x.clave !== m.clave))}>
                        <i className="bi bi-x-lg" />
                      </button>
                    </div>
                    {/* El medio va en su propia línea; distancia y días comparten la siguiente, con la misma altura y alineados */}
                    <div className="row g-3">
                      <div className="col-12">
                        <label className="form-label" htmlFor={`medio-${m.clave}`}>Medio de transporte<span className="req">*</span></label>
                        <select id={`medio-${m.clave}`} className={clase('form-select', err.medio)} value={m.idFuenteEmision}
                          disabled={!v.idDomicilio} onChange={(e) => cambiarMedio(m.clave, { idFuenteEmision: Number(e.target.value) || '' })}>
                          <option value="">{v.idDomicilio ? 'Seleccioná' : 'Elegí primero el domicilio'}</option>
                          {dadoDeBaja && <option value={m.idFuenteEmision}>{nombresGuardados[m.idFuenteEmision] || 'Medio guardado'} (ya no disponible)</option>}
                          {mediosDisponibles.map((x) => <option key={x.idFuenteEmision} value={x.idFuenteEmision} disabled={usados.has(x.idFuenteEmision)}>{x.nombre}</option>)}
                        </select>
                        {mensaje(err.medio)}
                      </div>
                      <div className="col-sm-6">
                        <label className="form-label" htmlFor={`distancia-${m.clave}`}>Distancia por día<span className="req">*</span></label>
                        <div className="input-group">
                          <InputMiles id={`distancia-${m.clave}`} className={clase('form-control', err.distancia)} placeholder="Ej: 20" valor={m.distanciaKm} onChange={(distanciaKm) => cambiarMedio(m.clave, { distanciaKm })} />
                          <span className="input-group-text">km</span>
                        </div>
                        {intentoEnvio && err.distancia ? mensaje(err.distancia) : <div className="form-text">Total en este medio, con todas las líneas que tomás, ida y vuelta.</div>}
                      </div>
                      <div className="col-sm-6">
                        <label className="form-label" htmlFor={`dias-${m.clave}`}>Días por semana<span className="req">*</span></label>
                        <div className="input-group">
                          <InputMiles id={`dias-${m.clave}`} className={clase('form-control', err.dias)} placeholder="Ej: 3" valor={m.diasSemana} onChange={(diasSemana) => cambiarMedio(m.clave, { diasSemana })} />
                          <span className="input-group-text">días</span>
                        </div>
                        {intentoEnvio && err.dias ? mensaje(err.dias) : <div className="form-text">Entre 1 y 7.</div>}
                      </div>
                    </div>
                  </div>
                )
              })}
              <button type="button" className="btn btn-suave align-self-start" disabled={!v.idDomicilio || medios.length >= mediosDisponibles.length}
                onClick={() => setMedios((ms) => [...ms, medioVacio()])}>
                <i className="bi bi-plus-lg me-1" />Agregar otro medio
              </button>
            </div>
          </div>
          </fieldset>
        </div>

        <div className="col-lg-4">
          <div className="tarjeta tarjeta-cuerpo position-sticky d-flex flex-column gap-3" style={{ top: 80 }}>
            {trasladoCompleto ? (
              <div>
                <div className="dato-etiqueta">Vas a cargar</div>
                <div className="fw-bold">Nada: ya está todo cargado</div>
              </div>
            ) : (
              <div>
                <div className="dato-etiqueta">Vas a cargar</div>
                <div className="fw-bold">{meses.length === 1 ? '1 mes' : `${meses.length} meses`}</div>
                {ultimoMes && <div className="text-muted small">{periodo(desdeMes(meses[0]), true)}{meses.length > 1 ? ` a ${periodo(desdeMes(ultimoMes), true)}` : ''}</div>}
                {meses.length < mesesElegidos.length && (
                  <div className="form-text">Los meses que todavía no empezaron no se cargan. Podés editar esta carga cuando pasen.</div>
                )}
              </div>
            )}
            {kmTotal > 0 && (
              <div>
                <div className="dato-etiqueta">Distancia total</div>
                <div className="fw-bold">{numero(kmTotal)} km</div>
                {/* Ejemplo del cálculo con la primera fila, para que se entienda de dónde sale el número */}
                <div className="form-text">
                  Se calcula así: distancia × días por semana × semanas. Medio 1: {numero(Number(medios[0].distanciaKm))} km × {numero(Number(medios[0].diasSemana), 1)} días × {numero(semanasPorMes * meses.length)} semanas{personas > 1 ? ` × ${personas} personas` : ''}.
                </div>
              </div>
            )}
            {/* Igual que en el formulario de consumos, los kg de CO₂ no se le muestran a quien colabora */}
            {esTitular && kmTotal > 0 && (
              <div><div className="dato-etiqueta">Emisiones estimadas</div><div className="fw-bold">{numero(kgTotal)} kg CO₂</div></div>
            )}
            <BotonCarga cargando={enviando} disabled={trasladoCompleto || chocaConOtraCarga} className="btn btn-primary w-100"><i className="bi bi-save me-1" />{editando ? 'Guardar cambios' : 'Guardar traslado'}</BotonCarga>
            {editando && (
              <button type="button" className="btn btn-outline-secondary text-danger w-100" onClick={eliminar}><i className="bi bi-trash3 me-1" />Eliminar traslado</button>
            )}
          </div>
        </div>
      </div>
    </form>
  )
}
