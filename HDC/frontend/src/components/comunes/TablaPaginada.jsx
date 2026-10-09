import { useEffect, useMemo, useState } from 'react'
import { EsqueletoTabla } from './Esqueleto.jsx'

const TAMANIOS = [10, 20, 50, 100]

const normalizar = (s) => String(s ?? '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase()

/**
 * Tabla con búsqueda, orden y paginación editable (10/20/50/100).
 * La columna "#" es un número correlativo que arranca en 1 (no es el id de la base).
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
}) {
  const [pagina, setPagina] = useState(1)
  const [tamanio, setTamanio] = useState(tamanioInicial)
  const [texto, setTexto] = useState('')
  const [orden, setOrden] = useState(null)

  const filtrados = useMemo(() => {
    let lista = datos || []
    if (texto.trim()) {
      const t = normalizar(texto)
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
  }, [datos, texto, orden, columnas])

  const totalPaginas = Math.max(1, Math.ceil(filtrados.length / tamanio))
  useEffect(() => { if (pagina > totalPaginas) setPagina(totalPaginas) }, [pagina, totalPaginas])
  useEffect(() => { setPagina(1) }, [texto, tamanio])

  const inicio = (pagina - 1) * tamanio
  const visibles = filtrados.slice(inicio, inicio + tamanio)

  const ordenarPor = (c) => {
    if (!c.ordenable) return
    setOrden((o) => (o?.clave === c.clave ? { clave: c.clave, asc: !o.asc } : { clave: c.clave, asc: true }))
  }

  const paginas = useMemo(() => {
    const res = []
    const desde = Math.max(1, pagina - 2), hasta = Math.min(totalPaginas, pagina + 2)
    for (let i = desde; i <= hasta; i++) res.push(i)
    return res
  }, [pagina, totalPaginas])

  return (
    <div className="tarjeta">
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

      {cargando ? (
        <div className="p-3"><EsqueletoTabla filas={Math.min(tamanio, 6)} columnas={Math.min(columnas.length + 1, 6)} /></div>
      ) : filtrados.length === 0 ? (
        <div className="vacio"><i className={`bi ${iconoVacio}`} />{texto ? 'No hay resultados para la búsqueda' : vacio}</div>
      ) : (
        <div className="tabla-contenedor">
          <table className="table tabla-hdc align-middle">
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

      {!cargando && filtrados.length > 0 && (
        <div className="paginacion">
          <div className="d-flex align-items-center gap-2 small texto-2">
            <span>Mostrar</span>
            <select className="form-select form-select-sm" value={tamanio} onChange={(e) => setTamanio(Number(e.target.value))} aria-label="Registros por página">
              {TAMANIOS.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
            <span>de {filtrados.length} {filtrados.length === 1 ? 'registro' : 'registros'}</span>
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
