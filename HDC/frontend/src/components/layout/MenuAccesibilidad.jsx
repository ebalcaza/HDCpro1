import { ESCALAS, usePreferencias } from '../../context/PreferenciasContext.jsx'

/** Opciones de usabilidad: tamaño de fuente, tema, animaciones y contraste. */
export default function MenuAccesibilidad({ alineacion = 'dropdown-menu-end' }) {
  const p = usePreferencias()
  const idx = ESCALAS.findIndex((e) => e.valor === p.escala)

  return (
    <div className="dropdown">
      <button type="button" className="btn btn-icono" data-bs-toggle="dropdown" data-bs-auto-close="outside"
        aria-expanded="false" aria-label="Opciones de usabilidad" title="Usabilidad">
        <i className="bi bi-universal-access-circle" />
      </button>
      <div className={`dropdown-menu ${alineacion} p-3`} style={{ width: 290 }}>
        <div className="fw-bold mb-2"><i className="bi bi-universal-access me-2" />Usabilidad</div>

        <div className="dato-etiqueta mb-1">Tamaño de la letra</div>
        <div className="d-flex align-items-center gap-2 mb-1">
          <button type="button" className="btn btn-outline-secondary btn-sm" aria-label="Achicar letra"
            disabled={idx <= 0} onClick={() => p.setEscala(ESCALAS[idx - 1].valor)}>A−</button>
          <select className="form-select form-select-sm" value={p.escala} onChange={(e) => p.setEscala(Number(e.target.value))} aria-label="Tamaño de letra">
            {ESCALAS.map((e) => <option key={e.valor} value={e.valor}>{e.texto} ({Math.round(e.valor * 100)}%)</option>)}
          </select>
          <button type="button" className="btn btn-outline-secondary btn-sm" aria-label="Agrandar letra"
            disabled={idx >= ESCALAS.length - 1} onClick={() => p.setEscala(ESCALAS[idx + 1].valor)}>A+</button>
        </div>
        <div className="form-text mb-3">Se aplica a todas las pantallas.</div>

        <div className="form-check form-switch mb-2">
          <input className="form-check-input" type="checkbox" id="pref-oscuro" checked={p.tema === 'dark'} onChange={p.alternarTema} />
          <label className="form-check-label" htmlFor="pref-oscuro">Modo oscuro</label>
        </div>
        <div className="form-check form-switch mb-2">
          <input className="form-check-input" type="checkbox" id="pref-anim" checked={p.animaciones} onChange={(e) => p.setAnimaciones(e.target.checked)} />
          <label className="form-check-label" htmlFor="pref-anim">Animaciones</label>
        </div>
        <div className="form-check form-switch mb-3">
          <input className="form-check-input" type="checkbox" id="pref-contraste" checked={p.altoContraste} onChange={(e) => p.setAltoContraste(e.target.checked)} />
          <label className="form-check-label" htmlFor="pref-contraste">Bordes de alto contraste</label>
        </div>
        <button type="button" className="btn btn-outline-secondary btn-sm w-100" onClick={p.restablecer}>
          <i className="bi bi-arrow-counterclockwise me-1" />Restablecer
        </button>
      </div>
    </div>
  )
}
