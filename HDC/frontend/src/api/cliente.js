import axios from 'axios'

const CLAVE_TOKEN = 'hdc-token'

export const almacenToken = {
  obtener: () => { try { return localStorage.getItem(CLAVE_TOKEN) } catch { return null } },
  guardar: (t) => { try { localStorage.setItem(CLAVE_TOKEN, t) } catch { /* sin almacenamiento */ } },
  borrar: () => { try { localStorage.removeItem(CLAVE_TOKEN) } catch { /* sin almacenamiento */ } },
}

const cliente = axios.create({ baseURL: '/api', timeout: 60000 })

cliente.interceptors.request.use((config) => {
  const token = almacenToken.obtener()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

let alExpirarSesion = null
export const registrarExpiracion = (fn) => { alExpirarSesion = fn }

cliente.interceptors.response.use(
  (r) => r.data,
  (error) => {
    const status = error.response?.status
    const url = error.config?.url || ''
    if (status === 401 && !url.includes('/auth/login')) alExpirarSesion?.()
    const data = error.response?.data
    const err = new Error(
      data?.mensaje ||
      (status === 403 ? 'No tenés permiso para realizar esta acción' :
        status ? `Error del servidor (${status})` : 'No se pudo conectar con el servidor'),
    )
    err.status = status
    err.errores = data?.errores || null
    return Promise.reject(err)
  },
)

export default cliente
