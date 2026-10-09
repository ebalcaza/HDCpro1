import { useState } from 'react'
import { catalogoApi } from '../../api/servicios'
import { useUI } from '../../context/UIContext.jsx'
import { BotonCarga, EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import { Campo, errores, reglas, useFormulario } from '../../components/comunes/Formulario.jsx'
import Modal from '../../components/comunes/Modal.jsx'
import TablaPaginada from '../../components/comunes/TablaPaginada.jsx'

/** Configuración de cada catálogo (RFA23-34) */
const CONFIG = {
  alcances: {
    titulo: 'Alcances', singular: 'alcance', icono: 'bi-bullseye', id: 'idAlcance',
    descripcion: 'Clasificación de las emisiones según su origen (GHG Protocol).',
    vacio: { nombre: '', descripcion: '' },
    validar: (v) => errores({ nombre: reglas.requerido(v.nombre) }),
    columnas: [
      { clave: 'nombre', titulo: 'Nombre', ordenable: true, render: (x) => <b>{x.nombre}</b> },
      { clave: 'descripcion', titulo: 'Descripción', render: (x) => <span className="texto-2">{x.descripcion || '—'}</span> },
      { clave: 'cantidadTipos', titulo: 'Tipos de emisión', numerico: true, ordenable: true },
    ],
  },
  unidades: {
    titulo: 'Unidades de medida', singular: 'unidad de medida', icono: 'bi-rulers', id: 'idUnidadMedida',
    descripcion: 'Unidades en las que se expresan los consumos (m3, kg, L...).',
    vacio: { nombre: '' },
    validar: (v) => errores({ nombre: reglas.requerido(v.nombre) }),
    columnas: [
      { clave: 'nombre', titulo: 'Unidad', ordenable: true, render: (x) => <b>{x.nombre}</b> },
      { clave: 'cantidadFuentes', titulo: 'Fuentes que la usan', numerico: true, ordenable: true },
    ],
  },
  'tipos-emision': {
    titulo: 'Tipos de emisión', singular: 'tipo de emisión', icono: 'bi-diagram-3', id: 'idTipoEmision',
    descripcion: 'Agrupan las fuentes de emisión dentro de cada alcance.',
    vacio: { nombre: '', idAlcance: '' },
    validar: (v) => errores({ nombre: reglas.requerido(v.nombre), idAlcance: reglas.requerido(v.idAlcance, 'Elegí el alcance') }),
    columnas: [
      { clave: 'nombre', titulo: 'Nombre', ordenable: true, render: (x) => <b>{x.nombre}</b> },
      { clave: 'alcance', titulo: 'Alcance', ordenable: true, render: (x) => <span className="badge-suave badge-verde">{x.alcance}</span> },
      { clave: 'cantidadFuentes', titulo: 'Fuentes activas', numerico: true, ordenable: true },
    ],
  },
}

function Formulario({ cfg, tipo, item, alcances, abierto, onCerrar, onGuardado }) {
  const ui = useUI()
  const f = useFormulario(item ? { ...cfg.vacio, ...item, descripcion: item.descripcion || '' } : cfg.vacio, cfg.validar)
  const guardar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return
    f.setEnviando(true)
    try {
      const datos = tipo === 'tipos-emision' ? { nombre: f.valores.nombre, idAlcance: Number(f.valores.idAlcance) }
        : tipo === 'alcances' ? { nombre: f.valores.nombre, descripcion: f.valores.descripcion || null } : { nombre: f.valores.nombre }
      const api = catalogoApi(tipo)
      const r = item ? await api.actualizar(item[cfg.id], datos) : await api.crear(datos)
      ui.exito(`El ${cfg.singular} "${r.nombre}" se ${item ? 'actualizó' : 'creó'} correctamente`)
      onGuardado(r)
    } catch (err) { ui.error(err); f.aplicarErroresServidor(err.errores) } finally { f.setEnviando(false) }
  }
  return (
    <Modal abierto={abierto} onCerrar={onCerrar} titulo={`${item ? 'Editar' : 'Nuevo'} ${cfg.singular}`} tamano="sm"
      pie={<><button type="button" className="btn btn-outline-secondary" onClick={onCerrar}>Cancelar</button><BotonCarga cargando={f.enviando} form="form-catalogo">Guardar</BotonCarga></>}>
      <form id="form-catalogo" noValidate onSubmit={guardar} className="d-flex flex-column gap-3">
        <Campo etiqueta="Nombre" requerido error={f.errores.nombre} tocado={f.tocados.nombre}><input autoFocus {...f.campo('nombre')} /></Campo>
        {tipo === 'tipos-emision' && (
          <Campo etiqueta="Alcance" requerido error={f.errores.idAlcance} tocado={f.tocados.idAlcance}>
            <select {...f.campo('idAlcance')} className={f.claseValidacion('idAlcance', 'form-select')}>
              <option value="">Seleccioná</option>
              {alcances.map((a) => <option key={a.idAlcance} value={a.idAlcance}>{a.nombre}</option>)}
            </select>
          </Campo>
        )}
        {tipo === 'alcances' && <Campo etiqueta="Descripción" exito={null}><textarea rows={3} maxLength={500} {...f.campo('descripcion')} /></Campo>}
      </form>
    </Modal>
  )
}

export default function Catalogos({ tipo }) {
  const cfg = CONFIG[tipo]
  const ui = useUI()
  const { datos, cargando, error, recargar } = useCarga(() => catalogoApi(tipo).listar(), [tipo])
  const alcances = useCarga(() => (tipo === 'tipos-emision' ? catalogoApi('alcances').listar() : Promise.resolve([])), [tipo])
  const [form, setForm] = useState({ abierto: false, item: null, k: 0 })
  const [resaltado, setResaltado] = useState(null)

  const eliminar = async (x) => {
    if (!(await ui.confirmar({ titulo: `¿Eliminar "${x.nombre}"?`, mensaje: `Solo se puede eliminar si ningún otro dato depende de este ${cfg.singular}.`, tipo: 'peligro', textoConfirmar: 'Eliminar' }))) return
    try { const r = await catalogoApi(tipo).eliminar(x[cfg.id]); ui.exito(r.mensaje); recargar() } catch (err) { ui.error(err) }
  }

  return (
    <>
      <EncabezadoPagina titulo={cfg.titulo} descripcion={cfg.descripcion} icono={cfg.icono}>
        <button type="button" className="btn btn-primary" onClick={() => setForm((f) => ({ abierto: true, item: null, k: f.k + 1 }))}>
          <i className="bi bi-plus-lg me-1" />Nuevo {cfg.singular}
        </button>
      </EncabezadoPagina>
      {error ? <ErrorCarga error={error} onReintentar={recargar} /> : (
        <TablaPaginada columnas={cfg.columnas} datos={datos} cargando={cargando} idFila={(x) => x[cfg.id]} resaltarId={resaltado}
          acciones={(x) => (
            <>
              <button type="button" className="btn btn-icono" title="Editar" aria-label="Editar" onClick={() => setForm((f) => ({ abierto: true, item: x, k: f.k + 1 }))}><i className="bi bi-pencil-square" /></button>
              <button type="button" className="btn btn-icono peligro" title="Eliminar" aria-label="Eliminar" onClick={() => eliminar(x)}><i className="bi bi-trash3" /></button>
            </>
          )} />
      )}
      <Formulario key={`${tipo}-${form.k}`} cfg={cfg} tipo={tipo} item={form.item} alcances={alcances.datos || []} abierto={form.abierto}
        onCerrar={() => setForm((f) => ({ ...f, abierto: false }))}
        onGuardado={(r) => { setForm((f) => ({ ...f, abierto: false })); setResaltado(r[cfg.id]); recargar() }} />
    </>
  )
}
