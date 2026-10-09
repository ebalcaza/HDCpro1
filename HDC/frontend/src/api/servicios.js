import api from './cliente'

export const authApi = {
  login: (datos) => api.post('/auth/login', datos),
  registrarIndividuo: (datos) => api.post('/auth/registro/individuo', datos),
  registrarOrganizacion: (datos) => api.post('/auth/registro/organizacion', datos),
  sesion: () => api.get('/auth/sesion'),
}

export const perfilApi = {
  obtener: () => api.get('/perfil'),
  actualizar: (datos) => api.put('/perfil', datos),
  cambiarContrasena: (datos) => api.put('/perfil/contrasena', datos),
  agregarVehiculo: (datos) => api.post('/perfil/vehiculos', datos),
  actualizarVehiculo: (id, datos) => api.put(`/perfil/vehiculos/${id}`, datos),
  eliminarVehiculo: (id) => api.delete(`/perfil/vehiculos/${id}`),
}

export const geoApi = {
  provincias: () => api.get('/geo/provincias'),
  distritos: (idProvincia) => api.get(`/geo/provincias/${idProvincia}/distritos`),
  ciudades: (idDistrito) => api.get(`/geo/distritos/${idDistrito}/ciudades`),
}

/** CRUD genérico de catálogos: alcances, unidades, tipos-emision, fuentes */
export const catalogoApi = (recurso) => ({
  listar: (params) => api.get(`/catalogos/${recurso}`, { params }),
  crear: (datos) => api.post(`/catalogos/${recurso}`, datos),
  actualizar: (id, datos) => api.put(`/catalogos/${recurso}/${id}`, datos),
  eliminar: (id) => api.delete(`/catalogos/${recurso}/${id}`),
})
export const fuentesApi = {
  ...catalogoApi('fuentes'),
  reactivar: (id) => api.post(`/catalogos/fuentes/${id}/reactivar`),
}
export const sincronizarGeoref = () => api.post('/catalogos/georef/sincronizar')

export const rolesApi = {
  listar: () => api.get('/roles'),
  crear: (d) => api.post('/roles', d),
  actualizar: (id, d) => api.put(`/roles/${id}`, d),
  eliminar: (id) => api.delete(`/roles/${id}`),
}

export const permisosApi = {
  listar: () => api.get('/permisos'),
  crear: (d) => api.post('/permisos', d),
  actualizar: (id, d) => api.put(`/permisos/${id}`, d),
  eliminar: (id) => api.delete(`/permisos/${id}`),
}

export const usuariosApi = {
  listar: () => api.get('/usuarios'),
  obtener: (id) => api.get(`/usuarios/${id}`),
  cambiarRol: (id, idRol) => api.put(`/usuarios/${id}/rol`, { idRol }),
  cambiarEstado: (id, activo) => api.put(`/usuarios/${id}/estado`, { activo }),
}

export const domiciliosApi = {
  listar: (incluirBajas = false) => api.get('/domicilios', { params: { incluirBajas } }),
  obtener: (id) => api.get(`/domicilios/${id}`),
  crear: (d) => api.post('/domicilios', d),
  actualizar: (id, d) => api.put(`/domicilios/${id}`, d),
  eliminar: (id, eliminarRegistros) => api.delete(`/domicilios/${id}`, { params: { eliminarRegistros } }),
}

export const registrosApi = {
  listar: (idDomicilio) => api.get('/registros', { params: { idDomicilio } }),
  obtener: (id) => api.get(`/registros/${id}`),
  crear: (d) => api.post('/registros', d),
  actualizar: (id, d) => api.put(`/registros/${id}`, d),
  eliminar: (id) => api.delete(`/registros/${id}`),
}

export const colaboracionesApi = {
  enviadas: () => api.get('/colaboraciones/enviadas'),
  recibidas: () => api.get('/colaboraciones/recibidas'),
  invitar: (d) => api.post('/colaboraciones', d),
  actualizar: (id, d) => api.put(`/colaboraciones/${id}`, d),
  aceptar: (id) => api.post(`/colaboraciones/${id}/aceptar`),
  rechazar: (id) => api.post(`/colaboraciones/${id}/rechazar`),
  revocar: (id) => api.post(`/colaboraciones/${id}/revocar`),
}

export const dashboardApi = {
  obtener: (params) => api.get('/dashboard', { params }),
}

export const reportesApi = {
  generar: (filtros, global) => api.post(global ? '/reportes/global/generar' : '/reportes/generar', filtros),
  guardados: (global) => api.get('/reportes/guardados', { params: { global } }),
  guardar: (d) => api.post('/reportes/guardados', d),
  eliminar: (id) => api.delete(`/reportes/guardados/${id}`),
}
