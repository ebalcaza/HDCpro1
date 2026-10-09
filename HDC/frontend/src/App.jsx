import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { PERMISOS as P, rutaInicio, useAuth } from './context/AuthContext.jsx'
import Layout from './components/layout/Layout.jsx'
import { PuntosCarga } from './components/comunes/Esqueleto.jsx'

const Login = lazy(() => import('./pages/publicas/Login.jsx'))
const Registro = lazy(() => import('./pages/publicas/Registro.jsx'))
const Dashboard = lazy(() => import('./pages/usuario/Dashboard.jsx'))
const Domicilios = lazy(() => import('./pages/usuario/Domicilios.jsx'))
const MisEmisiones = lazy(() => import('./pages/usuario/MisEmisiones.jsx'))
const RegistroForm = lazy(() => import('./pages/usuario/RegistroForm.jsx'))
const RegistroDetalle = lazy(() => import('./pages/usuario/RegistroDetalle.jsx'))
const Colaboraciones = lazy(() => import('./pages/usuario/Colaboraciones.jsx'))
const Reportes = lazy(() => import('./pages/usuario/Reportes.jsx'))
const Perfil = lazy(() => import('./pages/usuario/Perfil.jsx'))
const AdminPanel = lazy(() => import('./pages/admin/AdminPanel.jsx'))
const Fuentes = lazy(() => import('./pages/admin/Fuentes.jsx'))
const Usuarios = lazy(() => import('./pages/admin/Usuarios.jsx'))
const UsuarioDetalle = lazy(() => import('./pages/admin/UsuarioDetalle.jsx'))
const Roles = lazy(() => import('./pages/admin/Roles.jsx'))
const Permisos = lazy(() => import('./pages/admin/Permisos.jsx'))
const Catalogos = lazy(() => import('./pages/admin/Catalogos.jsx'))

function Cargando() {
  return <div className="d-grid" style={{ minHeight: '50vh', placeItems: 'center' }}><PuntosCarga texto="Cargando" /></div>
}

/** Requiere sesión y (opcionalmente) alguno de los permisos indicados */
function Protegida({ permisos, children }) {
  const { autenticado, iniciando, tienePermiso, usuario } = useAuth()
  if (iniciando) return <Cargando />
  if (!autenticado) return <Navigate to="/login" replace />
  if (permisos && !tienePermiso(...permisos)) return <Navigate to={rutaInicio(usuario)} replace />
  return children
}

function SoloPublica({ children }) {
  const { autenticado, iniciando, usuario } = useAuth()
  if (iniciando) return <Cargando />
  return autenticado ? <Navigate to={rutaInicio(usuario)} replace /> : children
}

function Inicio() {
  const { usuario, autenticado, iniciando } = useAuth()
  if (iniciando) return <Cargando />
  return <Navigate to={autenticado ? rutaInicio(usuario) : '/login'} replace />
}

export default function App() {
  const emisiones = [P.GESTIONAR_EMISIONES, P.COLABORAR]
  return (
    <Suspense fallback={<Cargando />}>
      <Routes>
        <Route path="/" element={<Inicio />} />
        <Route path="/login" element={<SoloPublica><Login /></SoloPublica>} />
        <Route path="/registro" element={<SoloPublica><Registro /></SoloPublica>} />

        <Route element={<Protegida><Layout /></Protegida>}>
          <Route path="/dashboard" element={<Protegida permisos={[P.VER_DASHBOARD]}><Dashboard /></Protegida>} />
          <Route path="/domicilios" element={<Protegida permisos={[P.GESTIONAR_DOMICILIOS]}><Domicilios /></Protegida>} />
          <Route path="/emisiones" element={<Protegida permisos={emisiones}><MisEmisiones /></Protegida>} />
          <Route path="/emisiones/nuevo" element={<Protegida permisos={emisiones}><RegistroForm /></Protegida>} />
          <Route path="/emisiones/:id" element={<Protegida permisos={emisiones}><RegistroDetalle /></Protegida>} />
          <Route path="/emisiones/:id/editar" element={<Protegida permisos={emisiones}><RegistroForm /></Protegida>} />
          <Route path="/colaboraciones" element={<Protegida permisos={[P.GESTIONAR_COLABORACIONES, P.COLABORAR]}><Colaboraciones /></Protegida>} />
          <Route path="/reportes" element={<Protegida permisos={[P.GENERAR_REPORTES]}><Reportes /></Protegida>} />
          <Route path="/perfil" element={<Perfil />} />

          <Route path="/admin" element={<Protegida permisos={[P.ADMIN_PANEL]}><AdminPanel /></Protegida>} />
          <Route path="/admin/reportes" element={<Protegida permisos={[P.REPORTES_GLOBALES]}><Reportes global /></Protegida>} />
          <Route path="/admin/fuentes" element={<Protegida permisos={[P.GESTIONAR_FUENTES]}><Fuentes /></Protegida>} />
          <Route path="/admin/usuarios" element={<Protegida permisos={[P.GESTIONAR_USUARIOS]}><Usuarios /></Protegida>} />
          <Route path="/admin/usuarios/:id" element={<Protegida permisos={[P.GESTIONAR_USUARIOS]}><UsuarioDetalle /></Protegida>} />
          <Route path="/admin/roles" element={<Protegida permisos={[P.GESTIONAR_ROLES]}><Roles /></Protegida>} />
          <Route path="/admin/permisos" element={<Protegida permisos={[P.GESTIONAR_ROLES]}><Permisos /></Protegida>} />
          <Route path="/admin/tipos-emision" element={<Protegida permisos={[P.GESTIONAR_CATALOGOS]}><Catalogos tipo="tipos-emision" /></Protegida>} />
          <Route path="/admin/alcances" element={<Protegida permisos={[P.GESTIONAR_CATALOGOS]}><Catalogos tipo="alcances" /></Protegida>} />
          <Route path="/admin/unidades" element={<Protegida permisos={[P.GESTIONAR_CATALOGOS]}><Catalogos tipo="unidades" /></Protegida>} />
        </Route>

        <Route path="*" element={<Inicio />} />
      </Routes>
    </Suspense>
  )
}
