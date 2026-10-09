import { useState } from 'react'
import { permisosApi, rolesApi } from '../../api/servicios'
import { useUI } from '../../context/UIContext.jsx'
import { BotonCarga, EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import { Campo, errores, reglas, useFormulario } from '../../components/comunes/Formulario.jsx'
import Modal from '../../components/comunes/Modal.jsx'
import TablaPaginada from '../../components/comunes/TablaPaginada.jsx'

function FormRol({ rol, permisos, abierto, onCerrar, onGuardado }) {
  const ui = useUI()
  const f = useFormulario(
    rol ? { nombre: rol.nombre, descripcion: rol.descripcion || '', idsPermisos: rol.permisos.map((p) => p.idPermiso) } : { nombre: '', descripcion: '', idsPermisos: [] },
    (v) => errores({ nombre: reglas.requerido(v.nombre) }))
  const alternar = (id) => f.cambiar('idsPermisos', f.valores.idsPermisos.includes(id) ? f.valores.idsPermisos.filter((x) => x !== id) : [...f.valores.idsPermisos, id])

  const guardar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return
    f.setEnviando(true)
    try {
      const datos = { nombre: f.valores.nombre.trim(), descripcion: f.valores.descripcion || null, idsPermisos: f.valores.idsPermisos }
      const r = rol ? await rolesApi.actualizar(rol.idRol, datos) : await rolesApi.crear(datos)
      ui.exito(`El tipo de usuario "${r.nombre}" se ${rol ? 'actualizó' : 'creó'}. Los cambios de permisos aplican al próximo inicio de sesión.`)
      onGuardado(r)
    } catch (err) { ui.error(err); f.aplicarErroresServidor(err.errores) } finally { f.setEnviando(false) }
  }

  return (
    <Modal abierto={abierto} onCerrar={onCerrar} titulo={rol ? 'Editar tipo de usuario' : 'Nuevo tipo de usuario'} tamano="lg"
      pie={<><button type="button" className="btn btn-outline-secondary" onClick={onCerrar}>Cancelar</button><BotonCarga cargando={f.enviando} form="form-rol">Guardar</BotonCarga></>}>
      <form id="form-rol" noValidate onSubmit={guardar} className="row g-3">
        <Campo className="col-md-5" etiqueta="Nombre" requerido error={f.errores.nombre} tocado={f.tocados.nombre}
          ayuda={rol?.esSistema ? 'Tipo del sistema: no se puede renombrar' : null}>
          <input {...f.campo('nombre')} disabled={rol?.esSistema} maxLength={200} />
        </Campo>
        <Campo className="col-md-7" etiqueta="Descripción" exito={null}><input {...f.campo('descripcion')} maxLength={500} /></Campo>
        <div className="col-12">
          <div className="d-flex align-items-center mb-2">
            <label className="form-label mb-0">Permisos ({f.valores.idsPermisos.length})</label>
            <button type="button" className="btn btn-link btn-sm ms-auto" onClick={() => f.cambiar('idsPermisos', permisos.map((p) => p.idPermiso))}>Todos</button>
            <button type="button" className="btn btn-link btn-sm" onClick={() => f.cambiar('idsPermisos', [])}>Ninguno</button>
          </div>
          <div className="row g-2">
            {permisos.map((p) => (
              <div key={p.idPermiso} className="col-md-6">
                <label className={`opcion-modal h-100 ${f.valores.idsPermisos.includes(p.idPermiso) ? 'seleccionada' : ''}`}>
                  <input type="checkbox" className="form-check-input mt-1" checked={f.valores.idsPermisos.includes(p.idPermiso)} onChange={() => alternar(p.idPermiso)} />
                  <span><span className="fw-bold small d-block">{p.nombre}</span><span className="small texto-2">{p.descripcion}</span></span>
                </label>
              </div>
            ))}
          </div>
        </div>
      </form>
    </Modal>
  )
}

/** RFA15-18 */
export default function Roles() {
  const ui = useUI()
  const { datos, cargando, error, recargar } = useCarga(() => rolesApi.listar(), [])
  const permisos = useCarga(() => permisosApi.listar(), [])
  const [form, setForm] = useState({ abierto: false, rol: null, k: 0 })
  const [resaltado, setResaltado] = useState(null)

  const eliminar = async (r) => {
    if (!(await ui.confirmar({ titulo: `¿Eliminar el tipo "${r.nombre}"?`, mensaje: 'Solo se puede eliminar si no tiene usuarios asignados.', tipo: 'peligro', textoConfirmar: 'Eliminar' }))) return
    try { const x = await rolesApi.eliminar(r.idRol); ui.exito(x.mensaje); recargar() } catch (err) { ui.error(err) }
  }

  const columnas = [
    { clave: 'nombre', titulo: 'Tipo de usuario', ordenable: true, render: (r) => (
      <div><b>{r.nombre}</b>{r.esSistema && <span className="badge-suave badge-gris ms-2">Sistema</span>}<div className="small text-muted">{r.descripcion}</div></div>
    ) },
    { clave: 'permisos', titulo: 'Permisos', valor: (r) => r.permisos.map((p) => p.nombre).join(' '), render: (r) => (
      <div className="d-flex flex-wrap gap-1" style={{ maxWidth: 520 }}>{r.permisos.map((p) => <span key={p.idPermiso} className="badge-suave badge-azul">{p.nombre}</span>)}</div>
    ) },
    { clave: 'cantidadUsuarios', titulo: 'Usuarios', numerico: true, ordenable: true },
  ]

  return (
    <>
      <EncabezadoPagina titulo="Tipos de usuario" descripcion="Definí qué puede hacer cada tipo de usuario asignándole permisos." icono="bi-person-badge">
        <button type="button" className="btn btn-primary" disabled={!permisos.datos} onClick={() => setForm((f) => ({ abierto: true, rol: null, k: f.k + 1 }))}><i className="bi bi-plus-lg me-1" />Nuevo tipo</button>
      </EncabezadoPagina>
      {error ? <ErrorCarga error={error} onReintentar={recargar} /> : (
        <TablaPaginada columnas={columnas} datos={datos} cargando={cargando} idFila={(r) => r.idRol} resaltarId={resaltado}
          acciones={(r) => (
            <>
              <button type="button" className="btn btn-icono" title="Editar" aria-label="Editar" disabled={!permisos.datos} onClick={() => setForm((f) => ({ abierto: true, rol: r, k: f.k + 1 }))}><i className="bi bi-pencil-square" /></button>
              {!r.esSistema && <button type="button" className="btn btn-icono peligro" title="Eliminar" aria-label="Eliminar" onClick={() => eliminar(r)}><i className="bi bi-trash3" /></button>}
            </>
          )} />
      )}
      <FormRol key={form.k} rol={form.rol} permisos={permisos.datos || []} abierto={form.abierto}
        onCerrar={() => setForm((f) => ({ ...f, abierto: false }))}
        onGuardado={(r) => { setForm((f) => ({ ...f, abierto: false })); setResaltado(r.idRol); recargar() }} />
    </>
  )
}
