import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { usuariosApi } from '../../api/servicios'
import { useAuth } from '../../context/AuthContext.jsx'
import { useUI } from '../../context/UIContext.jsx'
import { EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import TablaPaginada from '../../components/comunes/TablaPaginada.jsx'
import { fecha, toneladas } from '../../utils/formato'

/** RFA05 */
export default function Usuarios() {
  const ui = useUI()
  const navigate = useNavigate()
  const { usuario } = useAuth()
  const { datos, cargando, error, recargar } = useCarga(() => usuariosApi.listar(), [])
  const [rol, setRol] = useState('')
  const [estado, setEstado] = useState('')
  const [resaltado, setResaltado] = useState(null)

  const roles = useMemo(() => [...new Set((datos || []).map((u) => u.rol))].sort(), [datos])
  const filtrados = useMemo(() => (datos || []).filter((u) =>
    (!rol || u.rol === rol) && (!estado || (estado === 'activos' ? u.activo : !u.activo))), [datos, rol, estado])

  const cambiarEstado = async (u) => {
    const ok = await ui.confirmar({
      titulo: u.activo ? `¿Deshabilitar a ${u.nombreMostrar}?` : `¿Habilitar a ${u.nombreMostrar}?`,
      mensaje: u.activo ? 'No podrá iniciar sesión hasta que se lo vuelva a habilitar. Sus datos se conservan.' : 'Podrá volver a iniciar sesión.',
      tipo: u.activo ? 'peligro' : 'info',
      textoConfirmar: u.activo ? 'Deshabilitar' : 'Habilitar',
    })
    if (!ok) return
    try { const r = await usuariosApi.cambiarEstado(u.idUsuario, !u.activo); ui.exito(r.mensaje); setResaltado(u.idUsuario); recargar() } catch (err) { ui.error(err) }
  }

  const columnas = [
    { clave: 'nombreMostrar', titulo: 'Usuario', ordenable: true, valor: (u) => `${u.nombreMostrar} ${u.email}`, render: (u) => (
      <div><div className="fw-semibold">{u.nombreMostrar}</div><div className="small text-muted">{u.email}</div></div>
    ) },
    { clave: 'rol', titulo: 'Tipo', ordenable: true, render: (u) => <span className={`badge-suave ${u.tipoPerfil === 'ORGANIZACION' ? 'badge-azul' : u.tipoPerfil ? 'badge-verde' : 'badge-marron'}`}>{u.rol}</span> },
    { clave: 'cantidadDomicilios', titulo: 'Domicilios', numerico: true, ordenable: true },
    { clave: 'totalKgCo2', titulo: 't CO₂', numerico: true, ordenable: true, render: (u) => toneladas(u.totalKgCo2) },
    { clave: 'fechaAlta', titulo: 'Alta', ordenable: true, render: (u) => fecha(u.fechaAlta) },
    { clave: 'activo', titulo: 'Estado', valor: (u) => (u.activo ? 'Activo' : 'Deshabilitado'), render: (u) => (
      u.activo ? <span className="badge-suave badge-verde">Activo</span> : <span className="badge-suave badge-rojo">Deshabilitado</span>
    ) },
  ]

  return (
    <>
      <EncabezadoPagina titulo="Usuarios" descripcion="Todas las cuentas registradas en la aplicación." icono="bi-person-lines-fill" />
      {error ? <ErrorCarga error={error} onReintentar={recargar} /> : (
        <TablaPaginada columnas={columnas} datos={filtrados} cargando={cargando} idFila={(u) => u.idUsuario} resaltarId={resaltado}
          textoBusqueda="Buscar por nombre o email..."
          alClickFila={(u) => navigate(`/admin/usuarios/${u.idUsuario}`)}
          barra={<>
            <select className="form-select form-select-sm w-auto" value={rol} onChange={(e) => setRol(e.target.value)} aria-label="Filtrar por tipo">
              <option value="">Todos los tipos</option>{roles.map((r) => <option key={r}>{r}</option>)}
            </select>
            <select className="form-select form-select-sm w-auto" value={estado} onChange={(e) => setEstado(e.target.value)} aria-label="Filtrar por estado">
              <option value="">Todos los estados</option><option value="activos">Activos</option><option value="inactivos">Deshabilitados</option>
            </select>
          </>}
          acciones={(u) => (
            <>
              <button type="button" className="btn btn-icono" title="Ver más información" aria-label="Ver" onClick={() => navigate(`/admin/usuarios/${u.idUsuario}`)}><i className="bi bi-eye" /></button>
              {u.idUsuario !== usuario.idUsuario && (
                <button type="button" className={`btn btn-icono ${u.activo ? 'peligro' : ''}`} title={u.activo ? 'Deshabilitar' : 'Habilitar'}
                  aria-label={u.activo ? 'Deshabilitar' : 'Habilitar'} onClick={() => cambiarEstado(u)}>
                  <i className={`bi ${u.activo ? 'bi-person-x' : 'bi-person-check'}`} />
                </button>
              )}
            </>
          )} />
      )}
    </>
  )
}
