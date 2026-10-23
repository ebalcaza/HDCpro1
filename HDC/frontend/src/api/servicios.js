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
}

/*
 * Cache de la sesión para catálogos y geografía (datos que casi nunca cambian).
 * Se guarda la PROMESA de cada GET: si dos pantallas piden lo mismo a la vez, viaja una sola petición.
 * Vive en memoria: se pierde al recargar la página (F5), así que nunca queda vieja mucho tiempo.
 * Cualquier alta/edición/baja de un catálogo hecha desde este navegador la vacía.
 */
const cacheSesion = new Map()

const conCache = (clave, pedir) => {
  if (!cacheSesion.has(clave)) {
    // Si la petición falla se borra, para poder reintentar después
    cacheSesion.set(clave, pedir().catch((err) => { cacheSesion.delete(clave); throw err }))
  }
  return cacheSesion.get(clave)
}

/** Borra de la cache las claves que empiezan con el prefijo (ej. 'catalogos:' o 'geo:') */
const invalidarCache = (prefijo) => {
  for (const clave of [...cacheSesion.keys()]) if (clave.startsWith(prefijo)) cacheSesion.delete(clave)
}

/** Ejecuta una modificación y, si salió bien, vacía la cache de catálogos */
const yRefrescarCatalogos = (promesa) => promesa.then((r) => { invalidarCache('catalogos:'); return r })

export const geoApi = {
  provincias: () => conCache('geo:provincias', () => api.get('/geo/provincias')),
  distritos: (idProvincia) => conCache(`geo:distritos:${idProvincia}`, () => api.get(`/geo/provincias/${idProvincia}/distritos`)),
  ciudades: (idDistrito) => conCache(`geo:ciudades:${idDistrito}`, () => api.get(`/geo/distritos/${idDistrito}/ciudades`)),
}

/** CRUD genérico de catálogos: alcances, unidades, tipos-emision, fuentes */
export const catalogoApi = (recurso) => ({
  // La clave incluye los parámetros (ej. fuentes con y sin bajas son listas distintas)
  listar: (params) => conCache(`catalogos:${recurso}:${JSON.stringify(params || {})}`, () => api.get(`/catalogos/${recurso}`, { params })),
  crear: (datos) => yRefrescarCatalogos(api.post(`/catalogos/${recurso}`, datos)),
  actualizar: (id, datos) => yRefrescarCatalogos(api.put(`/catalogos/${recurso}/${id}`, datos)),
  eliminar: (id) => yRefrescarCatalogos(api.delete(`/catalogos/${recurso}/${id}`)),
})
export const fuentesApi = {
  ...catalogoApi('fuentes'),
  reactivar: (id) => yRefrescarCatalogos(api.post(`/catalogos/fuentes/${id}/reactivar`)),
}
export const sincronizarGeoref = () => api.post('/catalogos/georef/sincronizar').then((r) => { invalidarCache('geo:'); return r })

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
  /** Página de usuarios: params = { pagina, tamanio, orden, asc, busqueda, idRol, estado } → { items, total } */
  listar: (params) => api.get('/usuarios', { params }),
  /** { total, activos, roles } para el panel admin y el filtro por tipo */
  resumen: () => api.get('/usuarios/resumen'),
  obtener: (id) => api.get(`/usuarios/${id}`),
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
  /** Página de registros: params = { pagina, tamanio, orden, asc, busqueda, idDomicilio, desde, hasta } → { items, total, totalKgCo2 } */
  listar: (params) => api.get('/registros', { params }),
  /** Domicilios con registros visibles (para el filtro de Mis emisiones) */
  domicilios: () => api.get('/registros/domicilios'),
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
