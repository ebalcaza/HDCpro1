import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { PERMISOS as P, useAuth } from '../../context/AuthContext.jsx'
import { usePreferencias } from '../../context/PreferenciasContext.jsx'
import { useUI } from '../../context/UIContext.jsx'
import { colaboracionesApi } from '../../api/servicios'
import MenuAccesibilidad from './MenuAccesibilidad.jsx'

const SECCIONES = [
  {
    titulo: 'Mi huella',
    items: [
      { a: '/dashboard', texto: 'Dashboard', icono: 'bi-speedometer2', permisos: [P.VER_DASHBOARD] },
      { a: '/emisiones', texto: 'Mis emisiones', icono: 'bi-cloud-haze2', permisos: [P.GESTIONAR_EMISIONES, P.COLABORAR] },
      { a: '/domicilios', texto: 'Domicilios', icono: 'bi-house-door', permisos: [P.GESTIONAR_DOMICILIOS] },
      { a: '/colaboraciones', texto: 'Colaboraciones', icono: 'bi-people', permisos: [P.GESTIONAR_COLABORACIONES, P.COLABORAR], badge: 'invitaciones' },
      { a: '/reportes', texto: 'Reportes', icono: 'bi-file-earmark-bar-graph', permisos: [P.GENERAR_REPORTES], ocultarSi: [P.REPORTES_GLOBALES] },
    ],
  },
  {
    titulo: 'Administración',
    items: [
      { a: '/admin', texto: 'Panel general', icono: 'bi-grid-1x2', permisos: [P.ADMIN_PANEL], fin: true },
      { a: '/admin/reportes', texto: 'Reportes y métricas', icono: 'bi-clipboard-data', permisos: [P.REPORTES_GLOBALES] },
      { a: '/admin/fuentes', texto: 'Fuentes de emisión', icono: 'bi-fire', permisos: [P.GESTIONAR_FUENTES] },
      { a: '/admin/usuarios', texto: 'Usuarios', icono: 'bi-person-lines-fill', permisos: [P.GESTIONAR_USUARIOS] },
      { a: '/admin/roles', texto: 'Tipos de usuario', icono: 'bi-person-badge', permisos: [P.GESTIONAR_ROLES] },
      { a: '/admin/permisos', texto: 'Permisos', icono: 'bi-shield-lock', permisos: [P.GESTIONAR_ROLES] },
      { a: '/admin/tipos-emision', texto: 'Tipos de emisión', icono: 'bi-diagram-3', permisos: [P.GESTIONAR_CATALOGOS] },
      { a: '/admin/alcances', texto: 'Alcances', icono: 'bi-bullseye', permisos: [P.GESTIONAR_CATALOGOS] },
      { a: '/admin/unidades', texto: 'Unidades de medida', icono: 'bi-rulers', permisos: [P.GESTIONAR_CATALOGOS] },
    ],
  },
  {
    titulo: 'Cuenta',
    items: [{ a: '/perfil', texto: 'Mi perfil', icono: 'bi-person-circle', permisos: [] }],
  },
]

function Navegacion({ alNavegar, invitaciones }) {
  const { tienePermiso } = useAuth()
  return (
    <nav aria-label="Navegación principal">
      {SECCIONES.map((s) => {
        const items = s.items.filter((i) =>
          (i.permisos.length === 0 || tienePermiso(...i.permisos)) && !(i.ocultarSi && tienePermiso(...i.ocultarSi)))
        if (!items.length) return null
        return (
          <div key={s.titulo}>
            <div className="nav-seccion">{s.titulo}</div>
            {items.map((i) => (
              <NavLink key={i.a} to={i.a} end={i.fin} className="nav-enlace" onClick={alNavegar}>
                <i className={`bi ${i.icono}`} aria-hidden="true" />
                <span>{i.texto}</span>
                {i.badge === 'invitaciones' && invitaciones > 0 && (
                  <span className="badge rounded-pill badge-suave badge-marron" title="Invitaciones pendientes">{invitaciones}</span>
                )}
              </NavLink>
            ))}
          </div>
        )
      })}
    </nav>
  )
}

function Marca() {
  return (
    <a href="/" className="marca">
      <span className="marca-icono"><i className="bi bi-leaf" aria-hidden="true" /></span>
      <span>Huella de Carbono</span>
    </a>
  )
}

export default function Layout() {
  const { usuario, cerrarSesion, tienePermiso } = useAuth()
  const { tema, alternarTema } = usePreferencias()
  const ui = useUI()
  const [menuMovil, setMenuMovil] = useState(false)
  const [invitaciones, setInvitaciones] = useState(0)
  const location = useLocation()

  useEffect(() => { setMenuMovil(false) }, [location.pathname])

  // Cantidad de invitaciones pendientes (badge en el menú)
  useEffect(() => {
    if (!tienePermiso(P.COLABORAR)) return
    colaboracionesApi.recibidas()
      .then((l) => setInvitaciones(l.filter((c) => c.estado === 'PENDIENTE').length))
      .catch(() => {})
  }, [location.pathname, tienePermiso])

  // Efecto "gota de agua" en los botones
  useEffect(() => {
    const alPresionar = (e) => {
      const btn = e.target.closest('.btn')
      if (!btn || btn.disabled || document.documentElement.classList.contains('sin-animaciones')) return
      const r = btn.getBoundingClientRect()
      const d = Math.max(r.width, r.height)
      const onda = document.createElement('span')
      onda.className = 'gota-onda'
      onda.style.cssText = `width:${d}px;height:${d}px;left:${e.clientX - r.left - d / 2}px;top:${e.clientY - r.top - d / 2}px`
      btn.appendChild(onda)
      setTimeout(() => onda.remove(), 650)
    }
    document.addEventListener('pointerdown', alPresionar)
    return () => document.removeEventListener('pointerdown', alPresionar)
  }, [])

  const salir = async () => {
    const ok = await ui.confirmar({
      titulo: '¿Cerrar sesión?',
      mensaje: 'Vas a tener que ingresar tus credenciales nuevamente.',
      tipo: 'info',
      textoConfirmar: 'Cerrar sesión',
    })
    if (ok) {
      cerrarSesion()
      ui.exito('Cerraste sesión correctamente', 'Hasta pronto')
    }
  }

  const iniciales = (usuario?.nombreMostrar || '?').split(' ').map((p) => p[0]).slice(0, 2).join('').toUpperCase()

  return (
    <div className="app-shell">
      <aside className="app-sidebar">
        <Marca />
        <Navegacion invitaciones={invitaciones} />
      </aside>

      {/* Menú lateral en móviles */}
      {menuMovil && (
        <>
          <div className="offcanvas offcanvas-start show" style={{ visibility: 'visible', width: 280 }} tabIndex={-1} role="dialog" aria-label="Menú">
            <div className="offcanvas-header pb-0">
              <Marca />
              <button type="button" className="btn-close" aria-label="Cerrar menú" onClick={() => setMenuMovil(false)} />
            </div>
            <div className="offcanvas-body pt-0"><Navegacion invitaciones={invitaciones} alNavegar={() => setMenuMovil(false)} /></div>
          </div>
          <div className="offcanvas-backdrop fade show" onClick={() => setMenuMovil(false)} />
        </>
      )}

      <div className="app-main">
        <header className="app-topbar d-flex align-items-center gap-2">
          <button type="button" className="btn btn-icono d-lg-none" aria-label="Abrir menú" onClick={() => setMenuMovil(true)}>
            <i className="bi bi-list fs-4" />
          </button>
          <div className="d-lg-none fw-bold">Huella de Carbono</div>

          <div className="ms-auto d-flex align-items-center gap-1">
            <button type="button" className="btn btn-icono" onClick={alternarTema}
              aria-label={tema === 'dark' ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'}
              title={tema === 'dark' ? 'Modo claro' : 'Modo oscuro'}>
              <i className={`bi ${tema === 'dark' ? 'bi-sun' : 'bi-moon-stars'}`} />
            </button>
            <MenuAccesibilidad />
            <div className="dropdown">
              <button type="button" className="btn d-flex align-items-center gap-2 px-2" data-bs-toggle="dropdown" aria-expanded="false">
                <span className="marca-icono" style={{ width: 32, height: 32, fontSize: '0.8rem', borderRadius: 10 }}>{iniciales}</span>
                <span className="d-none d-md-inline text-start lh-sm">
                  <span className="d-block fw-bold small">{usuario?.nombreMostrar}</span>
                  <span className="d-block text-muted" style={{ fontSize: '0.72rem' }}>{usuario?.rol}</span>
                </span>
                <i className="bi bi-chevron-down small text-muted" />
              </button>
              <ul className="dropdown-menu dropdown-menu-end">
                <li className="px-3 py-2 small text-muted text-truncate" style={{ maxWidth: 260 }}>{usuario?.email}</li>
                <li><hr className="dropdown-divider" /></li>
                <li><NavLink className="dropdown-item" to="/perfil"><i className="bi bi-person me-2" />Mi perfil</NavLink></li>
                <li><button type="button" className="dropdown-item text-danger" onClick={salir}><i className="bi bi-box-arrow-right me-2" />Cerrar sesión</button></li>
              </ul>
            </div>
          </div>
        </header>

        <main className="app-contenido" id="contenido">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
