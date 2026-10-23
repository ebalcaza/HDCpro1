import { useEffect, useMemo, useRef, useState } from 'react'
import { EsqueletoTabla } from './Esqueleto.jsx'

// Deben coincidir con ConsultaPagina.TamaniosPermitidos del backend
const TAMANIOS = [10, 20, 50, 100]

// Milisegundos que se espera después de la última tecla antes de buscar (debounce):
// así no se filtra / no se llama a la API en cada letra que se escribe
const ESPERA_BUSQUEDA = 250

const normalizar = (s) => String(s ?? '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase()

/**
 * Convierte el estado de la tabla en los parámetros que espera la API paginada
 * (ConsultaPagina en el backend: pagina, tamanio, orden, asc, busqueda).
 */
export const consultaAParams = (c) => ({
  pagina: c.pagina,
  tamanio: c.tamanio,
  orden: c.orden?.clave,
  asc: c.orden ? c.orden.asc : undefined,
  busqueda: c.texto.trim() || undefined,
})

/**
 * Tabla con búsqueda, orden y paginación editable (10/20/50/100).
 * La columna "#" es un número correlativo que arranca en 1 (no es el id de la base).
 * plana: sin tarjeta propia (para usarla dentro de otra tarjeta). compacta: filas más bajas.
 * altoMaximo: alto en px a partir del cual la tabla scrollea (la cabecera queda fija).
 *
 * Dos modos de trabajo:
 * - Local (por defecto): recibe TODAS las filas en `datos` y busca/ordena/pagina en el navegador.
 *   Sirve para listas chicas (catálogos, roles, tablas de los gráficos).
 * - Servidor (si se pasa `onConsulta`): `datos` es solo la página actual y `total` la cantidad de filas.
 *   Cada vez que cambia la página, el tamaño, el orden o la búsqueda, la tabla llama a
 *   onConsulta({ pagina, tamanio, orden, texto }) y el padre pide esa página a la API.
 *   `claveFiltros`: valor que cambia cuando cambian filtros externos (ej. domicilio o fechas);
 *   al cambiar, la tabla vuelve a la página 1.
 */
export default function TablaPaginada({
  columnas,
  datos,
  cargando,
  idFila,
  resaltarId,
  busqueda = true,
  textoBusqueda = 'Buscar...',
  vacio = 'No hay datos para mostrar',
  iconoVacio = 'bi-inbox',
  acciones,
  alClickFila,
  barra,
  tamanioInicial = 10,
  plana = false,
  compacta = false,
  altoMaximo,
  onConsulta,
  total,
  claveFiltros,
}) {
  const modoServidor = !!onConsulta

  // Todo lo que define "qué filas mostrar" vive en un solo objeto: así un cambio (ej. de tamaño)
  // que también vuelve a la página 1 produce UNA sola consulta y no dos seguidas.
  const [consulta, setConsulta] = useState({ pagina: 1, tamanio: tamanioInicial, orden: null, texto: '' })
  const { pagina, tamanio, orden } = consulta
  const cambiar = (cambios) => setConsulta((c) => ({ ...c, ...cambios }))

  // Lo que el usuario va escribiendo; recién pasa a `consulta.texto` cuando deja de tipear (debounce)
  const [texto, setTexto] = useState('')
  useEffect(() => {
    const t = setTimeout(() => setConsulta((c) => (c.texto === texto ? c : { ...c, texto, pagina: 1 })), ESPERA_BUSQUEDA)
    return () => clearTimeout(t)
  }, [texto])

  // Si cambian los filtros externos se vuelve a la página 1 (se saltea el primer render)
  const primerRender = useRef(true)
  useEffect(() => {
    if (primerRender.current) { primerRender.current = false; return }
    setConsulta((c) => ({ ...c, pagina: 1 }))
  }, [claveFiltros])

  // Modo servidor: avisar al padre cada vez que cambia la consulta para que pida esa página a la API
  useEffect(() => {
    onConsulta?.(consulta)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [consulta])

  // Modo local: buscar y ordenar en el navegador
  const filtrados = useMemo(() => {
    let lista = datos || []
    if (modoServidor) return lista // el servidor ya filtró y ordenó
    if (consulta.texto.trim()) {
      const t = normalizar(consulta.texto)
      lista = lista.filter((fila) => columnas.some((c) => normalizar(c.valor ? c.valor(fila) : fila[c.clave]).includes(t)))
    }
    if (orden) {
      const col = columnas.find((c) => c.clave === orden.clave)
      const obtener = col?.valor || ((f) => f[orden.clave])
      lista = [...lista].sort((a, b) => {
        const va = obtener(a), vb = obtener(b)
        const r = typeof va === 'number' && typeof vb === 'number' ? va - vb : String(va ?? '').localeCompare(String(vb ?? ''), 'es')
        return orden.asc ? r : -r
      })
    }
    return lista
  }, [datos, consulta.texto, orden, columnas, modoServidor])

  // Cantidad total de filas: en modo servidor la informa la API; en modo local se cuentan
  const cantidad = modoServidor ? (total ?? 0) : filtrados.length
  const totalPaginas = Math.max(1, Math.ceil(cantidad / tamanio))
  // Si se borró la última fila de la última página, retroceder una página
  useEffect(() => { if (pagina > totalPaginas && !cargando) cambiar({ pagina: totalPaginas }) }, [pagina, totalPaginas, cargando])

  const inicio = (pagina - 1) * tamanio
  // En modo servidor `datos` ya es la página; en modo local se recorta acá
  const visibles = modoServidor ? filtrados : filtrados.slice(inicio, inicio + tamanio)
  // Mostrar el esqueleto solo en la primera carga; en las siguientes se mantiene la página anterior atenuada
  const cargaInicial = cargando && !(datos?.length)

  const setPagina = (p) => cambiar({ pagina: typeof p === 'function' ? p(pagina) : p })

  const ordenarPor = (c) => {
    if (!c.ordenable) return
    const nuevo = orden?.clave === c.clave ? { clave: c.clave, asc: !orden.asc } : { clave: c.clave, asc: true }
    cambiar({ orden: nuevo, pagina: 1 })
  }

  const paginas = useMemo(() => {
    const res = []
    const desde = Math.max(1, pagina - 2), hasta = Math.min(totalPaginas, pagina + 2)
    for (let i = desde; i <= hasta; i++) res.push(i)
    return res
  }, [pagina, totalPaginas])

  return (
    <div className={plana ? '' : 'tarjeta'}>
      {(busqueda || barra) && (
        <div className="d-flex flex-wrap gap-2 align-items-center p-3 pb-2">
          {busqueda && (
            <div className="position-relative flex-grow-1" style={{ maxWidth: 360 }}>
              <i className="bi bi-search position-absolute text-muted" style={{ left: 12, top: '50%', transform: 'translateY(-50%)' }} />
              <input
                type="search"
                className="form-control ps-5"
                placeholder={textoBusqueda}
                value={texto}
                onChange={(e) => setTexto(e.target.value)}
                aria-label="Buscar en la tabla"
              />
            </div>
          )}
          <div className="ms-auto d-flex flex-wrap gap-2">{barra}</div>
        </div>
      )}

      {cargaInicial ? (
        <div className="p-3"><EsqueletoTabla filas={Math.min(tamanio, 6)} columnas={Math.min(columnas.length + 1, 6)} /></div>
      ) : filtrados.length === 0 ? (
        <div className="vacio"><i className={`bi ${iconoVacio}`} />{consulta.texto ? 'No hay resultados para la búsqueda' : vacio}</div>
      ) : (
        // "actualizando": mientras llega la página nueva se ve la anterior atenuada (sin saltos de altura)
        <div className={`tabla-contenedor ${altoMaximo ? 'tabla-scroll' : ''} ${cargando ? 'actualizando' : ''}`}
          style={altoMaximo ? { maxHeight: altoMaximo } : undefined} aria-busy={cargando || undefined}>
          <table className={`table tabla-hdc align-middle ${compacta ? 'table-sm mb-0' : ''}`}>
            <thead>
              <tr>
                <th className="col-num">#</th>
                {columnas.map((c) => (
                  <th
                    key={c.clave}
                    className={`${c.ordenable ? 'ordenable' : ''} ${c.numerico ? 'text-end' : ''} ${c.claseCabecera || ''}`}
                    onClick={() => ordenarPor(c)}
                    aria-sort={orden?.clave === c.clave ? (orden.asc ? 'ascending' : 'descending') : undefined}
                  >
                    {c.titulo}
                    {c.ordenable && (
                      <i className={`bi ms-1 small ${orden?.clave === c.clave ? (orden.asc ? 'bi-arrow-up' : 'bi-arrow-down') : 'bi-arrow-down-up opacity-50'}`} />
                    )}
                  </th>
                ))}
                {acciones && <th className="text-end">Acciones</th>}
              </tr>
            </thead>
            <tbody>
              {visibles.map((fila, i) => {
                const id = idFila ? idFila(fila) : i
                return (
                  <tr
                    key={id}
                    className={`${resaltarId != null && resaltarId === id ? 'fila-nueva' : ''} ${alClickFila ? 'cursor-pointer' : ''}`}
                    onClick={alClickFila ? () => alClickFila(fila) : undefined}
                  >
                    <td className="col-num">{inicio + i + 1}</td>
                    {columnas.map((c) => (
                      <td key={c.clave} className={`${c.numerico ? 'num' : ''} ${c.clase || ''}`}>
                        {c.render ? c.render(fila) : fila[c.clave]}
                      </td>
                    ))}
                    {acciones && (
                      <td className="text-end text-nowrap" onClick={(e) => e.stopPropagation()}>{acciones(fila)}</td>
                    )}
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {!cargaInicial && filtrados.length > 0 && (
        <div className="paginacion">
          <div className="d-flex align-items-center gap-2 small texto-2">
            <span>Mostrar</span>
            {/* Cambiar el tamaño vuelve a la página 1 en la misma actualización */}
            <select className="form-select form-select-sm" value={tamanio} onChange={(e) => cambiar({ tamanio: Number(e.target.value), pagina: 1 })} aria-label="Registros por página">
              {TAMANIOS.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
            <span>de {cantidad} {cantidad === 1 ? 'registro' : 'registros'}</span>
          </div>
          <nav aria-label="Paginación">
            <ul className="pagination pagination-sm">
              <li className={`page-item ${pagina === 1 ? 'disabled' : ''}`}>
                <button type="button" className="page-link" onClick={() => setPagina(1)} aria-label="Primera"><i className="bi bi-chevron-double-left" /></button>
              </li>
              <li className={`page-item ${pagina === 1 ? 'disabled' : ''}`}>
                <button type="button" className="page-link" onClick={() => setPagina((p) => p - 1)} aria-label="Anterior"><i className="bi bi-chevron-left" /></button>
              </li>
              {paginas.map((p) => (
                <li key={p} className={`page-item ${p === pagina ? 'active' : ''}`}>
                  <button type="button" className="page-link" onClick={() => setPagina(p)}>{p}</button>
                </li>
              ))}
              <li className={`page-item ${pagina === totalPaginas ? 'disabled' : ''}`}>
                <button type="button" className="page-link" onClick={() => setPagina((p) => p + 1)} aria-label="Siguiente"><i className="bi bi-chevron-right" /></button>
              </li>
              <li className={`page-item ${pagina === totalPaginas ? 'disabled' : ''}`}>
                <button type="button" className="page-link" onClick={() => setPagina(totalPaginas)} aria-label="Última"><i className="bi bi-chevron-double-right" /></button>
              </li>
            </ul>
          </nav>
        </div>
      )}
    </div>
  )
}
