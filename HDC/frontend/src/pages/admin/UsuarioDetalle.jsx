import { Link, useParams } from 'react-router-dom'
import { usuariosApi } from '../../api/servicios'
import { Dato, EncabezadoPagina, ErrorCarga, useCarga } from '../../components/comunes/Basicos.jsx'
import { EsqueletoTarjeta } from '../../components/comunes/Esqueleto.jsx'
import Destello from '../../components/comunes/Destello.jsx'
import TablaPaginada from '../../components/comunes/TablaPaginada.jsx'
import { fecha, toneladas } from '../../utils/formato'

/** RFA06: más información del usuario seleccionado */
export default function UsuarioDetalle() {
  const { id } = useParams()
  const { datos, cargando, error, recargar } = useCarga(() => usuariosApi.obtener(id), [id])

  if (error) return <ErrorCarga error={error} onReintentar={recargar} />
  if (cargando || !datos) return <><EncabezadoPagina titulo="Usuario" icono="bi-person" /><EsqueletoTarjeta alto={260} /></>

  const p = datos.perfil

  return (
    <>
      <EncabezadoPagina titulo={p.nombreMostrar} descripcion={p.email} icono="bi-person-vcard">
        <Link to="/admin/usuarios" className="btn btn-outline-secondary"><i className="bi bi-arrow-left me-1" />Volver</Link>
      </EncabezadoPagina>
      <div className="row g-4">
        <div className="col-lg-4 d-flex flex-column gap-4">
          <Destello valor={p.rol} className="tarjeta tarjeta-cuerpo d-flex flex-column gap-3">
            <Dato etiqueta="Tipo de usuario"><span className="badge-suave badge-verde">{p.rol}</span></Dato>
            <Dato etiqueta="Estado">{p.fechaBaja ? <span className="badge-suave badge-rojo">Deshabilitado desde {fecha(p.fechaBaja)}</span> : <span className="badge-suave badge-verde">Activo</span>}</Dato>
            <Dato etiqueta="Alta">{fecha(p.fechaAlta)}</Dato>
            <Dato etiqueta="Teléfono">{p.telefono}</Dato>
            <Dato etiqueta="Total emitido">{toneladas(p.totalKgCo2)} t CO₂</Dato>
            <Dato etiqueta="Registros de emisiones">{datos.cantidadRegistros}</Dato>
          </Destello>
        </div>
        <div className="col-lg-8 d-flex flex-column gap-4">
          <div className="tarjeta tarjeta-cuerpo">
            <h2 className="tarjeta-titulo mb-3">{p.organizacion ? 'Datos de la organización' : p.individuo ? 'Datos personales' : 'Perfil'}</h2>
            {p.individuo && (
              <div className="row g-3">
                <Dato className="col-md-6" etiqueta="Nombres">{p.individuo.nombres}</Dato>
                <Dato className="col-md-6" etiqueta="Apellidos">{p.individuo.apellidos}</Dato>
                <Dato className="col-md-4" etiqueta="DNI">{p.individuo.dni}</Dato>
                <Dato className="col-md-4" etiqueta="Nacimiento">{p.individuo.fechaNacimiento ? fecha(`${p.individuo.fechaNacimiento}T00:00`) : null}</Dato>
                <Dato className="col-md-4" etiqueta="Género">{p.individuo.genero}</Dato>
              </div>
            )}
            {p.organizacion && (
              <div className="row g-3">
                <Dato className="col-md-8" etiqueta="Razón social">{p.organizacion.razonSocial}</Dato>
                <Dato className="col-md-4" etiqueta="CUIT">{p.organizacion.cuit}</Dato>
                <Dato className="col-md-8" etiqueta="Área">{p.organizacion.area}</Dato>
                <Dato className="col-md-4" etiqueta="Miembros">{p.organizacion.cantidadMiembros}</Dato>
                <Dato className="col-12" etiqueta="Descripción">{p.organizacion.descripcion}</Dato>
              </div>
            )}
            {!p.tipoPerfil && <p className="text-muted mb-0">Cuenta sin perfil de individuo u organización.</p>}
          </div>
          <div>
            <h2 className="tarjeta-titulo mb-2">Domicilios</h2>
            <TablaPaginada busqueda={false} datos={datos.domicilios} idFila={(d) => d.idDomicilio} vacio="Sin domicilios"
              columnas={[
                { clave: 'descripcion', titulo: 'Domicilio', render: (d) => <div>{d.calle} {d.numero}<div className="small text-muted">{d.ciudad}, {d.provincia}</div></div> },
                { clave: 'cantidadRegistros', titulo: 'Registros', numerico: true },
                { clave: 'totalKgCo2', titulo: 't CO₂', numerico: true, render: (d) => toneladas(d.totalKgCo2) },
                { clave: 'estado', titulo: 'Estado', render: (d) => (d.fechaBaja ? <span className="badge-suave badge-gris">Baja</span> : <span className="badge-suave badge-verde">Activo</span>) },
              ]} />
          </div>
        </div>
      </div>
    </>
  )
}
