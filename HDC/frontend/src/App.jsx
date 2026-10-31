import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { PERMISOS as P, rutaInicio, useAuth } from './context/AuthContext.jsx'
import Layout from './components/layout/Layout.jsx'
import { PuntosCarga } from './components/comunes/Esqueleto.jsx'

const Login = lazy(() => import('./pages/publicas/Login.jsx'))
const Registro = lazy(() => import('./pages/publicas/Registro.jsx'))
const OlvideContrasena = lazy(() => import('./pages/publicas/OlvideContrasena.jsx'))
const RestablecerContrasena = lazy(() => import('./pages/publicas/RestablecerContrasena.jsx'))
const Dashboard = lazy(() => import('./pages/usuario/Dashboard.jsx'))
const Domicilios = lazy(() => import('./pages/usuario/Domicilios.jsx'))
const MisEmisiones = lazy(() => import('./pages/usuario/MisEmisiones.jsx'))
const RegistroForm = lazy(() => import('./pages/usuario/RegistroForm.jsx'))
const RegistroDetalle = lazy(() => import('./pages/usuario/RegistroDetalle.jsx'))
const FuentesEmision = lazy(() => import('./pages/usuario/FuentesEmision.jsx'))
const ConvocatoriaDetalle = lazy(() => import('./pages/usuario/ConvocatoriaDetalle.jsx'))
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
        {/* Recuperación de contraseña: pedir el link y abrirlo desde el email */}
        <Route path="/olvide-contrasena" element={<SoloPublica><OlvideContrasena /></SoloPublica>} />
        <Route path="/restablecer-contrasena" element={<SoloPublica><RestablecerContrasena /></SoloPublica>} />

        <Route element={<Protegida><Layout /></Protegida>}>
          <Route path="/dashboard" element={<Protegida permisos={[P.VER_DASHBOARD]}><Dashboard /></Protegida>} />
          <Route path="/domicilios" element={<Protegida permisos={[P.GESTIONAR_DOMICILIOS]}><Domicilios /></Protegida>} />
          <Route path="/emisiones" element={<Protegida permisos={emisiones}><MisEmisiones /></Protegida>} />
          <Route path="/emisiones/nuevo" element={<Protegida permisos={emisiones}><RegistroForm /></Protegida>} />
          <Route path="/emisiones/:id" element={<Protegida permisos={emisiones}><RegistroDetalle /></Protegida>} />
          <Route path="/emisiones/:id/editar" element={<Protegida permisos={emisiones}><RegistroForm /></Protegida>} />
          {/* Sección informativa: qué fuentes se pueden cargar y cómo se calculan */}
          <Route path="/fuentes" element={<Protegida permisos={emisiones}><FuentesEmision /></Protegida>} />
          <Route path="/colaboraciones" element={<Protegida permisos={[P.GESTIONAR_COLABORACIONES, P.COLABORAR]}><Colaboraciones /></Protegida>} />
          {/* Gestión de los miembros de una convocatoria (titular) */}
          <Route path="/colaboraciones/convocatorias/:id" element={<Protegida permisos={[P.GESTIONAR_COLABORACIONES]}><ConvocatoriaDetalle /></Protegida>} />
          <Route path="/reportes" element={<Protegida permisos={[P.GENERAR_REPORTES]}><Reportes key="propios" /></Protegida>} />
          <Route path="/perfil" element={<Perfil />} />

          <Route path="/admin" element={<Protegida permisos={[P.ADMIN_PANEL]}><AdminPanel /></Protegida>} />
          <Route path="/admin/reportes" element={<Protegida permisos={[P.REPORTES_GLOBALES]}><Reportes key="global" global /></Protegida>} />
          <Route path="/admin/fuentes" element={<Protegida permisos={[P.GESTIONAR_FUENTES]}><Fuentes /></Protegida>} />
          <Route path="/admin/usuarios" element={<Protegida permisos={[P.GESTIONAR_USUARIOS]}><Usuarios /></Protegida>} />
          <Route path="/admin/usuarios/:id" element={<Protegida permisos={[P.GESTIONAR_USUARIOS]}><UsuarioDetalle /></Protegida>} />
          <Route path="/admin/roles" element={<Protegida permisos={[P.GESTIONAR_ROLES]}><Roles /></Protegida>} />
          <Route path="/admin/permisos" element={<Protegida permisos={[P.GESTIONAR_ROLES]}><Permisos /></Protegida>} />
          {/* Las tres rutas usan el mismo componente: la key distinta obliga a React a crear uno nuevo al
              cambiar de sección. Sin ella reutiliza el anterior, con su lista, búsqueda y página, y mezcla
              filas de una sección con las de otra (lo mismo con Reportes propios / globales). */}
          <Route path="/admin/tipos-emision" element={<Protegida permisos={[P.GESTIONAR_CATALOGOS]}><Catalogos key="tipos-emision" tipo="tipos-emision" /></Protegida>} />
          <Route path="/admin/alcances" element={<Protegida permisos={[P.GESTIONAR_CATALOGOS]}><Catalogos key="alcances" tipo="alcances" /></Protegida>} />
          <Route path="/admin/unidades" element={<Protegida permisos={[P.GESTIONAR_CATALOGOS]}><Catalogos key="unidades" tipo="unidades" /></Protegida>} />
        </Route>

        <Route path="*" element={<Inicio />} />
      </Routes>
    </Suspense>
  )
}
