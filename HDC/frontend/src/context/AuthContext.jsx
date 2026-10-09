import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { almacenToken, registrarExpiracion } from '../api/cliente'
import { authApi } from '../api/servicios'
import { useUI } from './UIContext.jsx'

const AuthContext = createContext(null)

export const PERMISOS = {
  ADMIN_PANEL: 'ADMIN_PANEL',
  GESTIONAR_FUENTES: 'GESTIONAR_FUENTES',
  GESTIONAR_USUARIOS: 'GESTIONAR_USUARIOS',
  GESTIONAR_ROLES: 'GESTIONAR_ROLES',
  GESTIONAR_CATALOGOS: 'GESTIONAR_CATALOGOS',
  REPORTES_GLOBALES: 'REPORTES_GLOBALES',
  VER_DASHBOARD: 'VER_DASHBOARD',
  GESTIONAR_DOMICILIOS: 'GESTIONAR_DOMICILIOS',
  GESTIONAR_EMISIONES: 'GESTIONAR_EMISIONES',
  GESTIONAR_COLABORACIONES: 'GESTIONAR_COLABORACIONES',
  COLABORAR: 'COLABORAR',
  GENERAR_REPORTES: 'GENERAR_REPORTES',
}

export function AuthProvider({ children }) {
  const [usuario, setUsuario] = useState(null)
  const [iniciando, setIniciando] = useState(true)
  const navigate = useNavigate()
  const ui = useUI()

  const cerrarSesion = useCallback((motivo) => {
    almacenToken.borrar()
    setUsuario(null)
    navigate('/login', { replace: true })
    if (motivo) ui.aviso(motivo, 'Sesión finalizada')
  }, [navigate, ui])

  useEffect(() => {
    registrarExpiracion(() => cerrarSesion('Tu sesión expiró. Iniciá sesión nuevamente.'))
  }, [cerrarSesion])

  // Restaura la sesión guardada al recargar la página
  useEffect(() => {
    if (!almacenToken.obtener()) { setIniciando(false); return }
    authApi.sesion()
      .then(setUsuario)
      .catch(() => almacenToken.borrar())
      .finally(() => setIniciando(false))
  }, [])

  const aplicar = (respuesta) => {
    almacenToken.guardar(respuesta.token)
    setUsuario(respuesta.usuario)
    return respuesta.usuario
  }

  const valor = useMemo(() => ({
    usuario,
    iniciando,
    autenticado: !!usuario,
    login: async (datos) => aplicar(await authApi.login(datos)),
    registrar: async (tipo, datos) =>
      aplicar(await (tipo === 'ORGANIZACION' ? authApi.registrarOrganizacion(datos) : authApi.registrarIndividuo(datos))),
    cerrarSesion,
    tienePermiso: (...permisos) => !!usuario && permisos.some((p) => usuario.permisos.includes(p)),
    esAdmin: !!usuario?.permisos.includes(PERMISOS.ADMIN_PANEL),
    actualizarNombre: (nombreMostrar) => setUsuario((u) => (u ? { ...u, nombreMostrar } : u)),
  }), [usuario, iniciando, cerrarSesion])

  return <AuthContext.Provider value={valor}>{children}</AuthContext.Provider>
}

export const useAuth = () => useContext(AuthContext)

/** Pantalla inicial según el tipo de usuario */
export const rutaInicio = (u) => (u?.permisos.includes(PERMISOS.VER_DASHBOARD) ? '/dashboard'
  : u?.permisos.includes(PERMISOS.ADMIN_PANEL) ? '/admin' : '/perfil')
