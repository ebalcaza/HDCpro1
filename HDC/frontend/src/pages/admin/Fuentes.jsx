import { useMemo, useState } from 'react'
import { catalogoApi, fuentesApi } from '../../api/servicios'
import { useUI } from '../../context/UIContext.jsx'
import { BotonCarga, EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import { Campo, errores, reglas, useFormulario } from '../../components/comunes/Formulario.jsx'
import Modal from '../../components/comunes/Modal.jsx'
import TablaPaginada from '../../components/comunes/TablaPaginada.jsx'
import { fecha, numero } from '../../utils/formato'

function FormFuente({ fuente, cat, abierto, onCerrar, onGuardado }) {
  const ui = useUI()
  const [alcance, setAlcance] = useState(fuente ? String(fuente.idAlcance) : '')
  const f = useFormulario(fuente
    ? { nombre: fuente.nombre, valorFactorEmision: String(fuente.valorFactorEmision), idUnidadMedida: fuente.idUnidadMedida, idTipoEmision: fuente.idTipoEmision }
    : { nombre: '', valorFactorEmision: '', idUnidadMedida: '', idTipoEmision: '' },
  (v) => errores({
    nombre: reglas.requerido(v.nombre),
    valorFactorEmision: reglas.requerido(v.valorFactorEmision) || reglas.noNegativo(v.valorFactorEmision),
    idUnidadMedida: reglas.requerido(v.idUnidadMedida, 'Elegí la unidad'),
    idTipoEmision: reglas.requerido(v.idTipoEmision, 'Elegí el tipo de emisión'),
  }))
  const tipos = cat.tipos.filter((t) => !alcance || t.idAlcance === Number(alcance))

  const guardar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return ui.error('Revisá los campos marcados en rojo', 'Datos incompletos')
    f.setEnviando(true)
    try {
      const datos = { nombre: f.valores.nombre.trim(), valorFactorEmision: Number(f.valores.valorFactorEmision), idUnidadMedida: Number(f.valores.idUnidadMedida), idTipoEmision: Number(f.valores.idTipoEmision) }
      const r = fuente ? await fuentesApi.actualizar(fuente.idFuenteEmision, datos) : await fuentesApi.crear(datos)
      ui.exito(`La fuente "${r.nombre}" se ${fuente ? 'actualizó' : 'creó'}`)
      onGuardado(r)
    } catch (err) { ui.error(err); f.aplicarErroresServidor(err.errores) } finally { f.setEnviando(false) }
  }
  const unidad = cat.unidades.find((u) => u.idUnidadMedida === Number(f.valores.idUnidadMedida))?.nombre

  return (
    <Modal abierto={abierto} onCerrar={onCerrar} titulo={fuente ? 'Editar fuente de emisión' : 'Nueva fuente de emisión'}
      pie={<><button type="button" className="btn btn-outline-secondary" onClick={onCerrar}>Cancelar</button><BotonCarga cargando={f.enviando} form="form-fuente">Guardar</BotonCarga></>}>
      <form id="form-fuente" noValidate onSubmit={guardar} className="row g-3">
        <Campo className="col-12" etiqueta="Nombre de la fuente" requerido error={f.errores.nombre} tocado={f.tocados.nombre}><input autoFocus {...f.campo('nombre')} maxLength={250} /></Campo>
        <div className="col-md-6">
          <label className="form-label">Alcance (filtro)</label>
          <select className="form-select" value={alcance} onChange={(e) => { setAlcance(e.target.value); f.cambiar('idTipoEmision', '') }}>
            <option value="">Todos</option>
            {cat.alcances.map((a) => <option key={a.idAlcance} value={a.idAlcance}>{a.nombre}</option>)}
          </select>
        </div>
        <Campo className="col-md-6" etiqueta="Tipo de emisión" requerido error={f.errores.idTipoEmision} tocado={f.tocados.idTipoEmision}>
          <select {...f.campo('idTipoEmision')} className={f.claseValidacion('idTipoEmision', 'form-select')}>
            <option value="">Seleccioná</option>
            {tipos.map((t) => <option key={t.idTipoEmision} value={t.idTipoEmision}>{t.nombre} ({t.alcance})</option>)}
          </select>
        </Campo>
        <Campo className="col-md-6" etiqueta="Unidad de medida" requerido error={f.errores.idUnidadMedida} tocado={f.tocados.idUnidadMedida}>
          <select {...f.campo('idUnidadMedida')} className={f.claseValidacion('idUnidadMedida', 'form-select')}>
            <option value="">Seleccioná</option>
            {cat.unidades.map((u) => <option key={u.idUnidadMedida} value={u.idUnidadMedida}>{u.nombre}</option>)}
          </select>
        </Campo>
        <Campo className="col-md-6" etiqueta="Factor de emisión" requerido error={f.errores.valorFactorEmision} tocado={f.tocados.valorFactorEmision}
          ayuda={`kg CO₂ por ${unidad || 'unidad'}`}>
          <input type="number" min="0" step="any" inputMode="decimal" {...f.campo('valorFactorEmision')} />
        </Campo>
        {fuente && <div className="col-12 small text-muted"><i className="bi bi-info-circle me-1" />Cambiar el factor no altera los registros ya cargados: conservan el factor con el que se calcularon.</div>}
      </form>
    </Modal>
  )
}

/** RFA01-04 */
export default function Fuentes() {
  const ui = useUI()
  const [verBajas, setVerBajas] = useState(false)
  const [filtroAlcance, setFiltroAlcance] = useState('')
  const { datos, cargando, error, recargar } = useCarga(() => fuentesApi.listar({ incluirBajas: verBajas }), [verBajas])
  const cat = useCarga(() => Promise.all([catalogoApi('alcances').listar(), catalogoApi('tipos-emision').listar(), catalogoApi('unidades').listar()])
    .then(([alcances, tipos, unidades]) => ({ alcances, tipos, unidades })), [])
  const [form, setForm] = useState({ abierto: false, fuente: null, k: 0 })
  const [resaltado, setResaltado] = useState(null)

  const filtradas = useMemo(() => (datos || []).filter((x) => !filtroAlcance || x.idAlcance === Number(filtroAlcance)), [datos, filtroAlcance])

  const eliminar = async (x) => {
    if (!(await ui.confirmar({
      titulo: `¿Eliminar "${x.nombre}"?`,
      mensaje: 'Si la fuente ya se usó en registros, se dará de baja para conservar el historial y dejará de estar disponible para nuevas cargas.',
      tipo: 'peligro', textoConfirmar: 'Eliminar',
    }))) return
    try { const r = await fuentesApi.eliminar(x.idFuenteEmision); ui.exito(r.mensaje); recargar() } catch (err) { ui.error(err) }
  }
  const reactivar = async (x) => {
    try { await fuentesApi.reactivar(x.idFuenteEmision); ui.exito(`"${x.nombre}" vuelve a estar disponible`); setResaltado(x.idFuenteEmision); recargar() } catch (err) { ui.error(err) }
  }

  const columnas = [
    { clave: 'nombre', titulo: 'Fuente', ordenable: true, render: (x) => <div><b>{x.nombre}</b>{!x.activa && <span className="badge-suave badge-gris ms-2">Baja</span>}</div> },
    { clave: 'tipoEmision', titulo: 'Tipo de emisión', ordenable: true },
    { clave: 'alcance', titulo: 'Alcance', ordenable: true, render: (x) => <span className="badge-suave badge-verde">{x.alcance}</span> },
    { clave: 'unidadMedida', titulo: 'Unidad', ordenable: true },
    { clave: 'valorFactorEmision', titulo: 'Factor (kg CO₂/u)', numerico: true, ordenable: true, render: (x) => numero(x.valorFactorEmision, 6) },
    { clave: 'fechaModificacion', titulo: 'Modificada', ordenable: true, valor: (x) => x.fechaModificacion || x.fechaAlta, render: (x) => <span className="small text-muted">{fecha(x.fechaModificacion || x.fechaAlta)}</span> },
  ]

  return (
    <>
      <EncabezadoPagina titulo="Fuentes de emisión" descripcion="Parametrización de los factores de emisión que usa el cálculo." icono="bi-fire">
        <button type="button" className="btn btn-primary" disabled={!cat.datos} onClick={() => setForm((f) => ({ abierto: true, fuente: null, k: f.k + 1 }))}><i className="bi bi-plus-lg me-1" />Nueva fuente</button>
      </EncabezadoPagina>
      {error ? <ErrorCarga error={error} onReintentar={recargar} /> : (
        <TablaPaginada columnas={columnas} datos={filtradas} cargando={cargando} idFila={(x) => x.idFuenteEmision} resaltarId={resaltado}
          textoBusqueda="Buscar fuente, tipo, unidad..."
          barra={<>
            <select className="form-select form-select-sm w-auto" value={filtroAlcance} onChange={(e) => setFiltroAlcance(e.target.value)} aria-label="Filtrar por alcance">
              <option value="">Todos los alcances</option>
              {(cat.datos?.alcances || []).map((a) => <option key={a.idAlcance} value={a.idAlcance}>{a.nombre}</option>)}
            </select>
            <div className="form-check form-switch m-0 align-self-center">
              <input className="form-check-input" type="checkbox" id="bajas-f" checked={verBajas} onChange={(e) => setVerBajas(e.target.checked)} />
              <label className="form-check-label small" htmlFor="bajas-f">Ver dadas de baja</label>
            </div>
          </>}
          acciones={(x) => x.activa ? (
            <>
              <button type="button" className="btn btn-icono" title="Editar" aria-label="Editar" onClick={() => setForm((f) => ({ abierto: true, fuente: x, k: f.k + 1 }))}><i className="bi bi-pencil-square" /></button>
              <button type="button" className="btn btn-icono peligro" title="Eliminar" aria-label="Eliminar" onClick={() => eliminar(x)}><i className="bi bi-trash3" /></button>
            </>
          ) : (
            <button type="button" className="btn btn-sm btn-suave" onClick={() => reactivar(x)}><i className="bi bi-arrow-repeat me-1" />Reactivar</button>
          )} />
      )}
      {cat.datos && (
        <FormFuente key={form.k} fuente={form.fuente} cat={cat.datos} abierto={form.abierto}
          onCerrar={() => setForm((f) => ({ ...f, abierto: false }))}
          onGuardado={(r) => { setForm((f) => ({ ...f, abierto: false })); setResaltado(r.idFuenteEmision); recargar() }} />
      )}
    </>
  )
}
