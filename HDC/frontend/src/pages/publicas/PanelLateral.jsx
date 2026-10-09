import MenuAccesibilidad from '../../components/layout/MenuAccesibilidad.jsx'
import { usePreferencias } from '../../context/PreferenciasContext.jsx'

export default function PanelLateral() {
  const { tema, alternarTema } = usePreferencias()
  return (
    <section className="auth-lateral">
      <div className="d-flex align-items-center justify-content-between">
        <div className="d-flex align-items-center gap-2 fw-bold fs-5">
          <span className="marca-icono" style={{ background: 'rgba(255,255,255,0.18)' }}><i className="bi bi-leaf" /></span>
          Huella de Carbono
        </div>
        <div className="d-flex gap-1" data-bs-theme="dark">
          <button type="button" className="btn btn-icono text-white" onClick={alternarTema} aria-label="Cambiar tema">
            <i className={`bi ${tema === 'dark' ? 'bi-sun' : 'bi-moon-stars'}`} />
          </button>
          {/* <MenuAccesibilidad /> */}
        </div>
      </div>
      <div className="ocultar-movil my-5">
        <h1 className="display-6 fw-bold mb-3">Medí, compará y reducí tus emisiones de CO₂</h1>
        <p className="fs-5 opacity-75 mb-4">
          Cargá tus consumos mensuales y conocé cuánto aporta cada domicilio, cada fuente y cada alcance a tu huella de carbono.
        </p>
        <div className="d-flex flex-column gap-3">
          {[
            ['bi-graph-up-arrow', 'Estadísticas y evolución mes a mes'],
            ['bi-people', 'Sumá colaboradores que aporten sus traslados'],
            ['bi-file-earmark-pdf', 'Reportes descargables en PDF'],
          ].map(([i, t]) => (
            <div key={t} className="d-flex align-items-center gap-3">
              <span className="d-grid rounded-3" style={{ width: 38, height: 38, placeItems: 'center', background: 'rgba(255,255,255,0.16)' }}><i className={`bi ${i}`} /></span>
              <span className="fw-semibold">{t}</span>
            </div>
          ))}
        </div>
      </div>
      <small className="ocultar-movil opacity-75">Las emisiones se calculan como consumo × factor de emisión (kg CO₂ por unidad).</small>
    </section>
  )
}
