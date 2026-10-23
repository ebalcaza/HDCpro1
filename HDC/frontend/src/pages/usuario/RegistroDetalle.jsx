import { useMemo } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { registrosApi } from '../../api/servicios'
import { useUI } from '../../context/UIContext.jsx'
import { Dato, EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import { EsqueletoTarjeta } from '../../components/comunes/Esqueleto.jsx'
import { GraficoDona } from '../../components/graficos/Graficos.jsx'
import TablaPaginada from '../../components/comunes/TablaPaginada.jsx'
import { fechaHora, numero, periodo, porcentaje, toneladas } from '../../utils/formato'
import { descargarPdfRegistro } from '../../utils/pdfDiferido'

/** RFR06: detalle de un registro, con descarga en PDF (RFR07) */
export default function RegistroDetalle() {
  const { id } = useParams()
  const navigate = useNavigate()
  const ui = useUI()
  const { datos: r, cargando, error, recargar } = useCarga(() => registrosApi.obtener(id), [id])

  const porAlcance = useMemo(() => {
    if (!r) return []
    const m = new Map()
    r.detalles.forEach((d) => {
      const x = m.get(d.idAlcance) || { id: d.idAlcance, nombre: d.alcance, kgCo2: 0 }
      x.kgCo2 += d.kgCo2
      m.set(d.idAlcance, x)
    })
    return [...m.values()].map((x) => ({ ...x, porcentaje: r.totalKgCo2 ? (x.kgCo2 / r.totalKgCo2) * 100 : 0 })).sort((a, b) => b.kgCo2 - a.kgCo2)
  }, [r])

  const eliminar = async () => {
    const ok = await ui.confirmar({ titulo: '¿Eliminar este registro?', mensaje: 'Esta acción no se puede deshacer.', tipo: 'peligro', textoConfirmar: 'Eliminar' })
    if (!ok) return
    try { await registrosApi.eliminar(id); ui.exito('Registro eliminado'); navigate('/emisiones') } catch (err) { ui.error(err) }
  }

  if (error) return <ErrorCarga error={error} onReintentar={recargar} />
  if (cargando || !r) return <><EncabezadoPagina titulo="Detalle del registro" icono="bi-receipt" /><EsqueletoTarjeta alto={300} /></>

  return (
    <>
      <EncabezadoPagina titulo={`Registro de ${periodo(r.periodo, true)}`} descripcion={`${r.domicilio}, ${r.ciudad}`} icono="bi-receipt">
        <Link to="/emisiones" className="btn btn-outline-secondary"><i className="bi bi-arrow-left me-1" />Volver</Link>
        <button type="button" className="btn btn-outline-secondary" onClick={async () => {
          // El generador de PDF se descarga recién al hacer click (ver utils/pdfDiferido.js)
          try { await descargarPdfRegistro(r); ui.exito('El PDF se descargó', 'Descarga lista') } catch (err) { ui.error(err) }
        }}>
          <i className="bi bi-file-earmark-pdf me-1" />Descargar PDF
        </button>
        {r.puedeEditar && <>
          <Link to={`/emisiones/${r.idRegistro}/editar`} className="btn btn-primary"><i className="bi bi-pencil me-1" />Editar</Link>
          <button type="button" className="btn btn-outline-secondary text-danger" onClick={eliminar} aria-label="Eliminar"><i className="bi bi-trash3" /></button>
        </>}
      </EncabezadoPagina>

      <div className="row g-4">
        <div className="col-lg-4">
          <div className="tarjeta kpi kpi-verde mb-4">
            <div className="kpi-icono"><i className="bi bi-cloud-haze2" /></div>
            <div className="kpi-etiqueta">Total del registro</div>
            <div className="kpi-valor">{toneladas(r.totalKgCo2)}<span className="kpi-unidad">t CO₂</span></div>
            <div className="small texto-2">{numero(r.totalKgCo2)} kg CO₂</div>
          </div>
          <div className="tarjeta tarjeta-cuerpo d-flex flex-column gap-3">
            <Dato etiqueta="Titular">{r.titular}</Dato>
            <Dato etiqueta="Domicilio">{r.domicilio}, {r.ciudad}, {r.provincia}</Dato>
            <Dato etiqueta="Cargado por">{r.usuarioCarga} {r.esColaboracion && <span className="badge-suave badge-azul ms-1">Colaborador</span>}</Dato>
            <Dato etiqueta="Creado">{fechaHora(r.fechaCreacion)}</Dato>
            <Dato etiqueta="Última modificación">{fechaHora(r.fechaUltimaModificacion)}</Dato>
            {r.observaciones && <Dato etiqueta="Observaciones">{r.observaciones}</Dato>}
          </div>
        </div>
        <div className="col-lg-8 d-flex flex-column gap-4">
          <div className="tarjeta tarjeta-cuerpo">
            <h2 className="tarjeta-titulo mb-3">Distribución por alcance</h2>
            <GraficoDona items={porAlcance} ordenIds={porAlcance.map((a) => a.id).sort((a, b) => a - b)} tituloCentro="Registro" alto={180} />
          </div>
          <div className="tarjeta">
            <div className="p-3 pb-0"><h2 className="tarjeta-titulo">Consumos ({r.detalles.length})</h2></div>
            <TablaPaginada plana busqueda={false} idFila={(d) => d.idDetalle} datos={r.detalles} columnas={[
              { clave: 'fuente', titulo: 'Fuente', ordenable: true, clase: 'fw-semibold' },
              { clave: 'tipoEmision', titulo: 'Tipo / Alcance', ordenable: true, render: (d) => <><div>{d.tipoEmision}</div><div className="small text-muted">{d.alcance}</div></> },
              { clave: 'consumo', titulo: 'Consumo', ordenable: true, numerico: true, render: (d) => `${numero(d.consumo, 4)} ${d.unidadMedida}` },
              { clave: 'valorFactorEmision', titulo: 'Factor', numerico: true, clase: 'text-muted', render: (d) => numero(d.valorFactorEmision, 6) },
              { clave: 'kgCo2', titulo: 'kg CO₂', ordenable: true, numerico: true, clase: 'fw-bold', render: (d) => numero(d.kgCo2) },
              { clave: 'porcentaje', titulo: '%', numerico: true, render: (d) => porcentaje(r.totalKgCo2 ? (d.kgCo2 / r.totalKgCo2) * 100 : 0) },
            ]} />
          </div>
        </div>
      </div>
    </>
  )
}
