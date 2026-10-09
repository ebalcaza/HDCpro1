import { Campo, errores, reglas } from '../comunes/Formulario.jsx'

export const vehiculoVacio = { tipoVehiculo: '', marca: '', modelo: '', patente: '', tipoCombustible: '', tipoUso: '' }

export const validarVehiculo = (v) => errores({
  tipoVehiculo: reglas.requerido(v.tipoVehiculo, 'Elegí el tipo de vehículo'),
  marca: reglas.requerido(v.marca, 'La marca es obligatoria'),
  modelo: reglas.requerido(v.modelo, 'El modelo es obligatorio'),
  patente: reglas.requerido(v.patente, 'La patente es obligatoria') || reglas.patente(v.patente),
  tipoCombustible: reglas.requerido(v.tipoCombustible, 'Elegí el combustible'),
  tipoUso: reglas.requerido(v.tipoUso, 'Elegí el tipo de uso'),
})

const TIPOS = ['Auto', 'Motocicleta', 'Camioneta', 'Camión']
const COMBUSTIBLES = ['Nafta', 'Gasoil', 'GNC']
const USOS = ['Personal', 'Institucional']

/** Campos del vehículo. `f` es un useFormulario; `prefijo` permite anidarlo (ej: "vehiculo."). */
export default function CamposVehiculo({ f }) {
  const t = f.tocados
  const sel = (nombre) => ({ ...f.campo(nombre), className: f.claseValidacion(nombre, 'form-select') })
  return (
    <div className="row g-3">
      <Campo className="col-md-4" etiqueta="Tipo de vehículo" requerido error={f.errores.tipoVehiculo} tocado={t.tipoVehiculo}>
        <select {...sel('tipoVehiculo')}><option value="">Seleccioná</option>{TIPOS.map((x) => <option key={x}>{x}</option>)}</select>
      </Campo>
      <Campo className="col-md-4" etiqueta="Marca" requerido error={f.errores.marca} tocado={t.marca}>
        <input {...f.campo('marca')} maxLength={100} />
      </Campo>
      <Campo className="col-md-4" etiqueta="Modelo" requerido error={f.errores.modelo} tocado={t.modelo}>
        <input {...f.campo('modelo')} maxLength={100} />
      </Campo>
      <Campo className="col-md-4" etiqueta="Patente" requerido error={f.errores.patente} tocado={t.patente}>
        <input {...f.campo('patente')} maxLength={10} style={{ textTransform: 'uppercase' }} placeholder="AB123CD" />
      </Campo>
      <Campo className="col-md-4" etiqueta="Combustible" requerido error={f.errores.tipoCombustible} tocado={t.tipoCombustible}>
        <select {...sel('tipoCombustible')}><option value="">Seleccioná</option>{COMBUSTIBLES.map((x) => <option key={x}>{x}</option>)}</select>
      </Campo>
      <Campo className="col-md-4" etiqueta="Tipo de uso" requerido error={f.errores.tipoUso} tocado={t.tipoUso}>
        <select {...sel('tipoUso')}><option value="">Seleccioná</option>{USOS.map((x) => <option key={x}>{x}</option>)}</select>
      </Campo>
    </div>
  )
}
