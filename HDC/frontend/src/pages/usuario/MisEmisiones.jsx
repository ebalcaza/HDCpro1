import { useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { registrosApi } from '../../api/servicios'
import { useUI } from '../../context/UIContext.jsx'
import { EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import TablaPaginada from '../../components/comunes/TablaPaginada.jsx'
import { SelectorMes } from '../../components/comunes/Selectores.jsx'
import { aMes, mesActual, numero, periodo, toneladas } from '../../utils/formato'
import { pdfRegistro } from '../../utils/pdf'

export default function MisEmisiones() {
  const ui = useUI()
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const idDomicilio = params.get('domicilio') || ''
  const [desde, setDesde] = useState('')
  const [hasta, setHasta] = useState('')
  const [origen, setOrigen] = useState('')
  const { datos, cargando, error, recargar } = useCarga(() => registrosApi.listar(), [])

  const domicilios = useMemo(() => [...new Map((datos || []).map((r) => [r.idDomicilio, r.domicilio])).entries()], [datos])

  const filtrados = useMemo(() => (datos || []).filter((r) => {
    const m = aMes(r.periodo)
    return (!idDomicilio || r.idDomicilio === Number(idDomicilio))
      && (!desde || m >= desde) && (!hasta || m <= hasta)
      && (!origen || (origen === 'propios' ? !r.esColaboracion : r.esColaboracion))
  }), [datos, idDomicilio, desde, hasta, origen])

  const totalFiltrado = filtrados.reduce((a, r) => a + r.totalKgCo2, 0)

  const eliminar = async (r) => {
    const ok = await ui.confirmar({
      titulo: '¿Eliminar este registro?',
      mensaje: `Se eliminará el registro de ${periodo(r.periodo, true)} (${numero(r.totalKgCo2)} kg CO₂) de ${r.domicilio}. Esta acción no se puede deshacer.`,
      tipo: 'peligro',
      textoConfirmar: 'Eliminar registro',
    })
    if (!ok) return
    try {
      await registrosApi.eliminar(r.idRegistro)
      ui.exito('El registro se eliminó y los totales se recalcularon')
      recargar()
    } catch (err) { ui.error(err) }
  }

  const descargar = async (r) => {
    try {
      pdfRegistro(await registrosApi.obtener(r.idRegistro))
      ui.exito('El PDF del registro se descargó', 'Descarga lista')
    } catch (err) { ui.error(err) }
  }

  const columnas = [
    { clave: 'periodo', titulo: 'Período', ordenable: true, render: (r) => <span className="fw-semibold text-capitalize">{periodo(r.periodo, true)}</span> },
    { clave: 'domicilio', titulo: 'Domicilio', ordenable: true },
    { clave: 'alcances', titulo: 'Alcances', valor: (r) => r.alcances.join(' '), render: (r) => (
      <div className="d-flex flex-wrap gap-1">{r.alcances.map((a) => <span key={a} className="badge-suave badge-gris">{a}</span>)}</div>
    ) },
    { clave: 'usuarioCarga', titulo: 'Cargado por', ordenable: true, render: (r) => (
      <span>{r.usuarioCarga}{r.esColaboracion && <span className="badge-suave badge-azul ms-1">Colaboración</span>}</span>
    ) },
    { clave: 'cantidadDetalles', titulo: 'Consumos', numerico: true, ordenable: true },
    { clave: 'totalKgCo2', titulo: 'kg CO₂', numerico: true, ordenable: true, render: (r) => <b>{numero(r.totalKgCo2)}</b> },
  ]

  return (
    <>
      <EncabezadoPagina titulo="Mis emisiones" descripcion="Registros mensuales de consumo de tus domicilios y de tus colaboraciones." icono="bi-cloud-haze2">
        <Link to="/emisiones/nuevo" className="btn btn-primary"><i className="bi bi-plus-lg me-1" />Nuevo registro</Link>
      </EncabezadoPagina>

      <div className="tarjeta tarjeta-cuerpo mb-3 d-flex flex-wrap gap-3 align-items-end">
        <div style={{ minWidth: 220 }}>
          <label className="form-label">Domicilio</label>
          <select className="form-select" value={idDomicilio} onChange={(e) => setParams(e.target.value ? { domicilio: e.target.value } : {})}>
            <option value="">Todos</option>
            {domicilios.map(([id, n]) => <option key={id} value={id}>{n}</option>)}
          </select>
        </div>
        <div><label className="form-label">Desde</label><SelectorMes valor={desde} permitirVacio max={hasta || mesActual()} onChange={setDesde} /></div>
        <div><label className="form-label">Hasta</label><SelectorMes valor={hasta} permitirVacio max={mesActual()} onChange={setHasta} /></div>
        <div>
          <label className="form-label">Origen</label>
          <select className="form-select" value={origen} onChange={(e) => setOrigen(e.target.value)}>
            <option value="">Todos</option>
            <option value="propios">Mis domicilios</option>
            <option value="colaboracion">Colaboraciones</option>
          </select>
        </div>
        <div className="ms-auto text-end">
          <div className="dato-etiqueta">Total filtrado</div>
          <div className="fs-5 fw-bold">{toneladas(totalFiltrado)} <small className="text-muted">t CO₂</small></div>
        </div>
      </div>

      {error ? <ErrorCarga error={error} onReintentar={recargar} /> : (
        <TablaPaginada
          columnas={columnas}
          datos={filtrados}
          cargando={cargando}
          idFila={(r) => r.idRegistro}
          textoBusqueda="Buscar por domicilio, período, alcance..."
          vacio="No hay registros de emisiones"
          iconoVacio="bi-cloud"
          alClickFila={(r) => navigate(`/emisiones/${r.idRegistro}`)}
          acciones={(r) => (
            <>
              <button type="button" className="btn btn-icono" title="Ver detalle" aria-label="Ver detalle" onClick={() => navigate(`/emisiones/${r.idRegistro}`)}><i className="bi bi-eye" /></button>
              <button type="button" className="btn btn-icono" title="Descargar PDF" aria-label="Descargar PDF" onClick={() => descargar(r)}><i className="bi bi-file-earmark-pdf" /></button>
              {r.puedeEditar && <>
                <button type="button" className="btn btn-icono" title="Editar" aria-label="Editar" onClick={() => navigate(`/emisiones/${r.idRegistro}/editar`)}><i className="bi bi-pencil-square" /></button>
                <button type="button" className="btn btn-icono peligro" title="Eliminar" aria-label="Eliminar" onClick={() => eliminar(r)}><i className="bi bi-trash3" /></button>
              </>}
            </>
          )}
        />
      )}
    </>
  )
}
