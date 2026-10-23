import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { domiciliosApi } from '../../api/servicios'
import { useUI } from '../../context/UIContext.jsx'
import { BotonCarga, Dato, EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import { useFormulario } from '../../components/comunes/Formulario.jsx'
import Modal from '../../components/comunes/Modal.jsx'
import TablaPaginada from '../../components/comunes/TablaPaginada.jsx'
import CamposDomicilio, { domicilioARequest, domicilioVacio, validarDomicilio } from '../../components/formularios/CamposDomicilio.jsx'
import { fecha, toneladas } from '../../utils/formato'

function FormularioDomicilio({ abierto, domicilio, onCerrar, onGuardado }) {
  const ui = useUI()
  const inicial = domicilio ? {
    ...domicilioVacio,
    ...Object.fromEntries(Object.entries(domicilio).map(([k, v]) => [k, v ?? ''])),
  } : domicilioVacio
  const f = useFormulario(inicial, validarDomicilio)

  const guardar = async (e) => {
    e.preventDefault()
    if (!f.validarTodo()) return ui.error('Revisá los campos marcados en rojo', 'Datos incompletos')
    f.setEnviando(true)
    try {
      const datos = domicilioARequest(f.valores)
      const r = domicilio ? await domiciliosApi.actualizar(domicilio.idDomicilio, datos) : await domiciliosApi.crear(datos)
      ui.exito(domicilio ? 'Los cambios del domicilio se guardaron' : 'El domicilio se agregó a tu perfil')
      onGuardado(r)
    } catch (err) {
      ui.error(err)
      f.aplicarErroresServidor(err.errores)
    } finally {
      f.setEnviando(false)
    }
  }

  return (
    <Modal abierto={abierto} onCerrar={onCerrar} titulo={domicilio ? 'Editar domicilio' : 'Nuevo domicilio'} tamano="lg"
      pie={<>
        <button type="button" className="btn btn-outline-secondary" onClick={onCerrar}>Cancelar</button>
        <BotonCarga cargando={f.enviando} form="form-domicilio">Guardar</BotonCarga>
      </>}>
      <form id="form-domicilio" noValidate onSubmit={guardar}>
        <CamposDomicilio f={f} />
      </form>
    </Modal>
  )
}

function DetalleDomicilio({ d, onCerrar, onEditar }) {
  const navigate = useNavigate()
  if (!d) return null
  return (
    <Modal abierto={!!d} onCerrar={onCerrar} titulo="Datos del domicilio" tamano="lg"
      pie={<>
        <button type="button" className="btn btn-outline-secondary" onClick={() => navigate(`/emisiones?domicilio=${d.idDomicilio}`)}>
          <i className="bi bi-cloud-haze2 me-1" />Ver registros
        </button>
        {!d.fechaBaja && <button type="button" className="btn btn-primary" onClick={onEditar}><i className="bi bi-pencil me-1" />Editar</button>}
      </>}>
      <div className="row g-3">
        <Dato className="col-md-6" etiqueta="Calle y altura">{d.calle} {d.numero}</Dato>
        <Dato className="col-6 col-md-3" etiqueta="Piso">{d.piso}</Dato>
        <Dato className="col-6 col-md-3" etiqueta="Departamento">{d.departamento}</Dato>
        <Dato className="col-6 col-md-3" etiqueta="Block">{d.block}</Dato>
        <Dato className="col-6 col-md-3" etiqueta="Manzana">{d.manzana}</Dato>
        <Dato className="col-6 col-md-3" etiqueta="Código postal">{d.codigoPostal}</Dato>
        <Dato className="col-6 col-md-3" etiqueta="Teléfono">{d.telefono}</Dato>
        <Dato className="col-md-4" etiqueta="Ciudad">{d.ciudad}</Dato>
        <Dato className="col-md-4" etiqueta="Distrito">{d.distrito}</Dato>
        <Dato className="col-md-4" etiqueta="Provincia">{d.provincia}</Dato>
        <Dato className="col-6 col-md-4" etiqueta="Total emitido">{toneladas(d.totalKgCo2)} t CO₂</Dato>
        <Dato className="col-6 col-md-4" etiqueta="Registros">{d.cantidadRegistros}</Dato>
        <Dato className="col-6 col-md-4" etiqueta="Colaboradores activos">{d.cantidadColaboradores}</Dato>
        <Dato className="col-6 col-md-4" etiqueta="Alta">{fecha(d.fechaAlta)}</Dato>
        {d.fechaBaja && <Dato className="col-6 col-md-4" etiqueta="Baja">{fecha(d.fechaBaja)}</Dato>}
      </div>
    </Modal>
  )
}

export default function Domicilios() {
  const ui = useUI()
  const [verBajas, setVerBajas] = useState(false)
  const { datos, cargando, error, recargar } = useCarga(() => domiciliosApi.listar(verBajas), [verBajas])
  const [form, setForm] = useState({ abierto: false, domicilio: null, clave: 0 })
  const [detalle, setDetalle] = useState(null)
  const [resaltado, setResaltado] = useState(null)

  const abrirForm = (domicilio = null) => setForm((f) => ({ abierto: true, domicilio, clave: f.clave + 1 }))

  const eliminar = async (d) => {
    const opcion = await ui.confirmar({
      titulo: `¿Eliminar "${d.descripcion}"?`,
      mensaje: d.cantidadRegistros > 0
        ? `Este domicilio tiene ${d.cantidadRegistros} registro(s) de emisiones. ¿Qué querés hacer con ellos?`
        : 'Elegí cómo eliminar el domicilio.',
      tipo: 'peligro',
      textoConfirmar: 'Eliminar',
      opciones: [
        { valor: 'conservar', titulo: 'Dar de baja y conservar el historial', descripcion: 'Las emisiones siguen contando en tus estadísticas y reportes.' },
        { valor: 'borrar', titulo: 'Eliminar todo definitivamente', descripcion: 'Se borran el domicilio, sus registros y colaboraciones. No se puede deshacer.' },
      ],
    })
    if (!opcion) return
    try {
      const r = await domiciliosApi.eliminar(d.idDomicilio, opcion === 'borrar')
      ui.exito(r.mensaje)
      recargar()
    } catch (err) { ui.error(err) }
  }

  const columnas = [
    { clave: 'descripcion', titulo: 'Domicilio', ordenable: true, render: (d) => (
      <div>
        <div className="fw-semibold">{d.calle} {d.numero}{d.piso ? `, piso ${d.piso}` : ''}{d.departamento ? ` ${d.departamento}` : ''}</div>
        <div className="small text-muted">CP {d.codigoPostal}</div>
      </div>
    ) },
    { clave: 'ciudad', titulo: 'Ubicación', ordenable: true, valor: (d) => `${d.ciudad} ${d.provincia}`, render: (d) => (
      <div><div>{d.ciudad}</div><div className="small text-muted">{d.provincia}</div></div>
    ) },
    { clave: 'cantidadRegistros', titulo: 'Registros', ordenable: true, numerico: true },
    { clave: 'totalKgCo2', titulo: 't CO₂', ordenable: true, numerico: true, render: (d) => <b>{toneladas(d.totalKgCo2)}</b> },
    { clave: 'estado', titulo: 'Estado', valor: (d) => (d.fechaBaja ? 'Baja' : 'Activo'), render: (d) => (
      d.fechaBaja ? <span className="badge-suave badge-gris">Baja</span> : <span className="badge-suave badge-verde">Activo</span>
    ) },
  ]

  return (
    <>
      <EncabezadoPagina titulo="Domicilios" descripcion="Los lugares donde medís tus emisiones." icono="bi-house-door">
        <button type="button" className="btn btn-primary" onClick={() => abrirForm()}><i className="bi bi-plus-lg me-1" />Nuevo domicilio</button>
      </EncabezadoPagina>

      {error ? <ErrorCarga error={error} onReintentar={recargar} /> : (
        <TablaPaginada
          columnas={columnas}
          datos={datos}
          cargando={cargando}
          idFila={(d) => d.idDomicilio}
          resaltarId={resaltado}
          textoBusqueda="Buscar por calle, ciudad..."
          vacio="No tenés domicilios cargados"
          iconoVacio="bi-house"
          alClickFila={setDetalle}
          barra={
            <div className="form-check form-switch m-0 align-self-center">
              <input className="form-check-input" type="checkbox" id="ver-bajas" checked={verBajas} onChange={(e) => setVerBajas(e.target.checked)} />
              <label className="form-check-label small" htmlFor="ver-bajas">Mostrar dados de baja</label>
            </div>
          }
          acciones={(d) => (
            <>
              <button type="button" className="btn btn-icono" title="Ver" aria-label="Ver domicilio" onClick={() => setDetalle(d)}><i className="bi bi-eye" /></button>
              {!d.fechaBaja && <>
                <button type="button" className="btn btn-icono" title="Editar" aria-label="Editar domicilio" onClick={() => abrirForm(d)}><i className="bi bi-pencil-square" /></button>
                <button type="button" className="btn btn-icono peligro" title="Eliminar" aria-label="Eliminar domicilio" onClick={() => eliminar(d)}><i className="bi bi-trash3" /></button>
              </>}
            </>
          )}
        />
      )}

      <FormularioDomicilio
        key={form.clave}
        abierto={form.abierto}
        domicilio={form.domicilio}
        onCerrar={() => setForm((f) => ({ ...f, abierto: false }))}
        onGuardado={(d) => { setForm((f) => ({ ...f, abierto: false })); setResaltado(d.idDomicilio); recargar() }}
      />
      <DetalleDomicilio d={detalle} onCerrar={() => setDetalle(null)} onEditar={() => { const d = detalle; setDetalle(null); abrirForm(d) }} />
    </>
  )
}
