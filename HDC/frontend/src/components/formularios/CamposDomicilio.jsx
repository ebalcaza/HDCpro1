import { Campo, errores, LARGOS, reglas } from '../comunes/Formulario.jsx'
import { SelectorGeo } from '../comunes/Selectores.jsx'
import CampoTelefono, { completarCaracteristica, telefonoARequest } from './CampoTelefono.jsx'

export const domicilioVacio = {
  calle: '', numero: '', departamento: '', piso: '', block: '', manzana: '',
  idProvincia: '', idDistrito: '', idCiudad: '', codigoPostal: '', telefono: '', idsFuentes: [],
}

export const validarDomicilio = (v) => errores({
  // Calle: obligatoria, solo letras/números/espacios simples y hasta el largo de DOMICILIO.CALLE
  calle: reglas.requerido(v.calle, 'La calle es obligatoria') || reglas.calle(v.calle) || reglas.maximo(v.calle, LARGOS.calle),
  numero: reglas.requerido(v.numero, 'La altura es obligatoria') || (Number(v.numero) < 0 || !Number.isInteger(Number(v.numero)) ? 'Altura inválida' : null),
  idCiudad: !v.idCiudad ? 'Seleccioná provincia, distrito y ciudad' : null,
  codigoPostal: reglas.requerido(v.codigoPostal, 'El código postal es obligatorio'),
  telefono: reglas.telefono(v.telefono),
  // Agregá acá otras validaciones del domicilio, ej: piso: reglas.miRegla(v.piso),
})

export const domicilioARequest = (v) => ({
  calle: v.calle.trim(),
  numero: Number(v.numero),
  departamento: v.departamento || null,
  piso: v.piso || null,
  block: v.block || null,
  manzana: v.manzana || null,
  idCiudad: Number(v.idCiudad),
  codigoPostal: v.codigoPostal.trim(),
  telefono: telefonoARequest(v.telefono),
  idsFuentes: v.idsFuentes || [],
})

/**
 * Campos del domicilio para usar dentro de un useFormulario (registro y ABM de domicilios).
 */
export default function CamposDomicilio({ f }) {
  const t = f.tocados

  const cambiarGeo = (g) => {
    // Al elegir otra provincia se precarga la característica del teléfono según la zona (si todavía no tenía)
    if (g.idProvincia && g.idProvincia !== f.valores.idProvincia) {
      f.cambiar('telefono', completarCaracteristica(f.valores.telefono, g.idProvincia))
    }
    f.cambiar('idProvincia', g.idProvincia); f.cambiar('idDistrito', g.idDistrito); f.cambiar('idCiudad', g.idCiudad)
  }

  return (
    <div className="row g-3">
      <Campo className="col-md-6" etiqueta="Calle" requerido error={f.errores.calle} tocado={t.calle}>
        <input {...f.campo('calle')} maxLength={LARGOS.calle} />
      </Campo>
      <Campo className="col-6 col-md-3" etiqueta="Altura" requerido error={f.errores.numero} tocado={t.numero}>
        <input type="number" min="0" inputMode="numeric" {...f.campo('numero')} />
      </Campo>
      <Campo className="col-6 col-md-3" etiqueta="Código postal" requerido error={f.errores.codigoPostal} tocado={t.codigoPostal}>
        <input {...f.campo('codigoPostal')} maxLength={15} />
      </Campo>
      <Campo className="col-6 col-md-3" etiqueta="Piso" exito={null}><input {...f.campo('piso')} maxLength={20} /></Campo>
      <Campo className="col-6 col-md-3" etiqueta="Departamento" exito={null}><input {...f.campo('departamento')} maxLength={20} /></Campo>
      <Campo className="col-6 col-md-3" etiqueta="Block" exito={null}><input {...f.campo('block')} maxLength={20} /></Campo>
      <Campo className="col-6 col-md-3" etiqueta="Manzana" exito={null}><input {...f.campo('manzana')} maxLength={20} /></Campo>
      <SelectorGeo
        valor={{ idProvincia: f.valores.idProvincia, idDistrito: f.valores.idDistrito, idCiudad: f.valores.idCiudad }}
        onChange={cambiarGeo}
        tocado={t.idCiudad}
        error={f.errores.idCiudad}
      />
      <CampoTelefono f={f} etiqueta="Teléfono del domicilio" />
    </div>
  )
}
