import { useState } from 'react'
import { permisosApi } from '../../api/servicios'
import { PERMISOS } from '../../context/AuthContext.jsx'
import { useUI } from '../../context/UIContext.jsx'
import { BotonCarga, EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import { Campo, errores, reglas, useFormulario } from '../../components/comunes/Formulario.jsx'
import Modal from '../../components/comunes/Modal.jsx'
import TablaPaginada from '../../components/comunes/TablaPaginada.jsx'

const DEL_SISTEMA = new Set(Object.values(PERMISOS))

function FormPermiso({ permiso, abierto, onCerrar, onGuardado }) {
  const ui = useUI()
  const sistema = permiso && DEL_SISTEMA.has(permiso.nombre)
  const f = useFormulario(permiso ? { nombre: permiso.nombre, descripcion: permiso.descripcion || '' } : { nombre: '', descripcion: '' },
    (v) => errores({ nombre: reglas.requerido(v.nombre) || (!/^[A-Z0-9_]+$/.test(v.nombre) ? 'Usá MAYÚSCULAS, números y _ (ej: VER_REPORTES)' : null) }))
  const guardar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return
    f.setEnviando(true)
    try {
      const datos = { nombre: f.valores.nombre, descripcion: f.valores.descripcion || null }
      const r = permiso ? await permisosApi.actualizar(permiso.idPermiso, datos) : await permisosApi.crear(datos)
      ui.exito(`El permiso ${r.nombre} se ${permiso ? 'actualizó' : 'creó'}`)
      onGuardado(r)
    } catch (err) { ui.error(err); f.aplicarErroresServidor(err.errores) } finally { f.setEnviando(false) }
  }
  return (
    <Modal abierto={abierto} onCerrar={onCerrar} titulo={permiso ? 'Editar permiso' : 'Nuevo permiso'} tamano="sm"
      pie={<><button type="button" className="btn btn-outline-secondary" onClick={onCerrar}>Cancelar</button><BotonCarga cargando={f.enviando} form="form-permiso">Guardar</BotonCarga></>}>
      <form id="form-permiso" noValidate onSubmit={guardar} className="d-flex flex-column gap-3">
        <Campo etiqueta="Nombre (código)" requerido error={f.errores.nombre} tocado={f.tocados.nombre}
          ayuda={sistema ? 'Permiso usado por el sistema: solo se puede editar la descripción' : null}>
          <input {...f.campo('nombre')} disabled={sistema} onChange={(e) => f.cambiar('nombre', e.target.value.toUpperCase().replace(/\s/g, '_'))} />
        </Campo>
        <Campo etiqueta="Descripción" exito={null}><textarea rows={3} maxLength={500} {...f.campo('descripcion')} /></Campo>
      </form>
    </Modal>
  )
}

/** RFA19-22 */
export default function Permisos() {
  const ui = useUI()
  const { datos, cargando, error, recargar } = useCarga(() => permisosApi.listar(), [])
  const [form, setForm] = useState({ abierto: false, permiso: null, k: 0 })
  const [resaltado, setResaltado] = useState(null)

  const eliminar = async (p) => {
    if (!(await ui.confirmar({ titulo: `¿Eliminar el permiso ${p.nombre}?`, mensaje: 'Se quitará de todos los tipos de usuario que lo tengan.', tipo: 'peligro', textoConfirmar: 'Eliminar' }))) return
    try { const r = await permisosApi.eliminar(p.idPermiso); ui.exito(r.mensaje); recargar() } catch (err) { ui.error(err) }
  }

  const columnas = [
    { clave: 'nombre', titulo: 'Permiso', ordenable: true, render: (p) => <div><code className="fw-bold" style={{ color: 'var(--verde)' }}>{p.nombre}</code>{DEL_SISTEMA.has(p.nombre) && <span className="badge-suave badge-gris ms-2">Sistema</span>}</div> },
    { clave: 'descripcion', titulo: 'Descripción', render: (p) => <span className="texto-2">{p.descripcion || '—'}</span> },
    { clave: 'roles', titulo: 'Asignado a', valor: (p) => p.roles.join(' '), render: (p) => (
      <div className="d-flex flex-wrap gap-1">{p.roles.length ? p.roles.map((r) => <span key={r} className="badge-suave badge-azul">{r}</span>) : <span className="text-muted small">Ninguno</span>}</div>
    ) },
  ]

  return (
    <>
      <EncabezadoPagina titulo="Permisos" descripcion="Acciones que puede realizar cada tipo de usuario." icono="bi-shield-lock">
        <button type="button" className="btn btn-primary" onClick={() => setForm((f) => ({ abierto: true, permiso: null, k: f.k + 1 }))}><i className="bi bi-plus-lg me-1" />Nuevo permiso</button>
      </EncabezadoPagina>
      {error ? <ErrorCarga error={error} onReintentar={recargar} /> : (
        <TablaPaginada columnas={columnas} datos={datos} cargando={cargando} idFila={(p) => p.idPermiso} resaltarId={resaltado}
          acciones={(p) => (
            <>
              <button type="button" className="btn btn-icono" title="Editar" aria-label="Editar" onClick={() => setForm((f) => ({ abierto: true, permiso: p, k: f.k + 1 }))}><i className="bi bi-pencil-square" /></button>
              {!DEL_SISTEMA.has(p.nombre) && <button type="button" className="btn btn-icono peligro" title="Eliminar" aria-label="Eliminar" onClick={() => eliminar(p)}><i className="bi bi-trash3" /></button>}
            </>
          )} />
      )}
      <FormPermiso key={form.k} permiso={form.permiso} abierto={form.abierto}
        onCerrar={() => setForm((f) => ({ ...f, abierto: false }))}
        onGuardado={(r) => { setForm((f) => ({ ...f, abierto: false })); setResaltado(r.idPermiso); recargar() }} />
    </>
  )
}
