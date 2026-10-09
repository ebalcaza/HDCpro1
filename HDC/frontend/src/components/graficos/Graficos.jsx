import { useEffect, useMemo, useRef, useState } from 'react'
import {
  Chart as ChartJS, ArcElement, CategoryScale, LinearScale, PointElement, LineElement,
  BarElement, Filler, Tooltip, Legend,
} from 'chart.js'
import { Doughnut, Line, Bar } from 'react-chartjs-2'
import { usePreferencias } from '../../context/PreferenciasContext.jsx'
import { colorPorId, SERIES, tintaGrafico } from '../../utils/colores'
import { numero, periodo, porcentaje, toneladas } from '../../utils/formato'

ChartJS.register(ArcElement, CategoryScale, LinearScale, PointElement, LineElement, BarElement, Filler, Tooltip, Legend)
ChartJS.defaults.font.family = "'Nunito Sans', system-ui, sans-serif"

/** Detecta cuándo el gráfico entra en pantalla para activar el halo */
function useEnVista() {
  const ref = useRef(null)
  const [visible, setVisible] = useState(false)
  useEffect(() => {
    if (!ref.current || !('IntersectionObserver' in window)) { setVisible(true); return }
    const obs = new IntersectionObserver(([e]) => setVisible(e.isIntersecting), { threshold: 0.4 })
    obs.observe(ref.current)
    return () => obs.disconnect()
  }, [])
  return [ref, visible]
}

/**
 * Dona por categoría (alcances, tipos...). Sin tooltip: la leyenda muestra el nombre del
 * parámetro, el porcentaje y los kg CO2. Animación de "llenado" al cargar y halo cuando está en vista.
 * items: [{ id, nombre, kgCo2, porcentaje, grupo? }]
 */
export function GraficoDona({ items, ordenIds, tituloCentro = 'Total', mostrarLeyenda = true, alto = 210, refGrafico, maxLeyenda }) {
  const { tema, animaciones } = usePreferencias()
  const [ref, enVista] = useEnVista()
  const tinta = tintaGrafico(tema)
  const ids = ordenIds || items.map((i) => i.id)
  const colores = items.map((i) => colorPorId(i.id, tema, ids))
  const total = items.reduce((a, i) => a + Number(i.kgCo2), 0)

  const data = {
    labels: items.map((i) => i.nombre),
    datasets: [{
      data: items.map((i) => Number(i.kgCo2)),
      backgroundColor: colores,
      borderColor: tinta.superficie,
      borderWidth: 2, // separación de 2px entre segmentos
      borderRadius: 4,
      hoverOffset: 0,
    }],
  }

  const opciones = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '68%',
    animation: animaciones ? { animateRotate: true, animateScale: false, duration: 1300, easing: 'easeOutQuart' } : false,
    plugins: { legend: { display: false }, tooltip: { enabled: false } },
    events: [],
  }

  const leyenda = maxLeyenda ? items.slice(0, maxLeyenda) : items

  return (
    <div className="d-flex flex-wrap align-items-center gap-4">
      <div ref={ref} className={`grafico-dona ${enVista && animaciones ? 'activo' : ''}`} style={{ width: alto, height: alto, flexShrink: 0 }}>
        <Doughnut ref={refGrafico} data={data} options={opciones} aria-label={`Gráfico de dona: ${items.map((i) => `${i.nombre} ${i.porcentaje}%`).join(', ')}`} role="img" />
        <div className="dona-centro">
          <div>
            <div className="valor">{toneladas(total)}</div>
            <div className="etiqueta">t CO₂ · {tituloCentro}</div>
          </div>
        </div>
      </div>
      {mostrarLeyenda && (
        <ul className="leyenda flex-grow-1" style={{ minWidth: 200 }}>
          {leyenda.map((i, idx) => (
            <li key={i.id}>
              <span className="punto-serie" style={{ background: colores[idx] }} aria-hidden="true" />
              <span className="nombre text-truncate" title={i.nombre}>
                {i.nombre}
                {i.grupo && <small>{i.grupo}</small>}
              </span>
              <span className="pct">{porcentaje(i.porcentaje)}</span>
              <span className="kg">{numero(i.kgCo2)} kg</span>
            </li>
          ))}
          {maxLeyenda && items.length > maxLeyenda && (
            <li className="text-muted small">+ {items.length - maxLeyenda} más</li>
          )}
        </ul>
      )}
    </div>
  )
}

/**
 * Evolución mensual (curvas). Línea del total + una por alcance.
 * Crosshair con tooltip agrupado por mes.
 */
export function GraficoEvolucion({ puntos, alto = 300, mostrarAlcances = true, refGrafico }) {
  const { tema, animaciones } = usePreferencias()
  const tinta = tintaGrafico(tema)

  const alcances = useMemo(() => {
    const s = new Set()
    puntos.forEach((p) => Object.keys(p.porAlcance || {}).forEach((a) => s.add(a)))
    return [...s].sort()
  }, [puntos])

  const paleta = SERIES[tema] || SERIES.light
  const colorTotal = tema === 'dark' ? '#e2ebe5' : '#33413a'

  const datasets = [
    {
      label: 'Total',
      data: puntos.map((p) => Number(p.kgCo2)),
      borderColor: colorTotal,
      backgroundColor: (ctx) => {
        const { chart } = ctx
        if (!chart.chartArea) return 'transparent'
        const g = chart.ctx.createLinearGradient(0, chart.chartArea.top, 0, chart.chartArea.bottom)
        g.addColorStop(0, tema === 'dark' ? 'rgba(95,194,138,0.22)' : 'rgba(46,139,87,0.16)')
        g.addColorStop(1, 'rgba(46,139,87,0)')
        return g
      },
      fill: true,
      tension: 0.35,
      borderWidth: 2,
      pointRadius: puntos.length > 18 ? 0 : 4,
      pointHoverRadius: 6,
      pointBackgroundColor: colorTotal,
      pointBorderColor: tinta.superficie,
      pointBorderWidth: 2,
    },
    ...(mostrarAlcances ? alcances.map((a, i) => ({
      label: a,
      data: puntos.map((p) => Number(p.porAlcance?.[a] || 0)),
      borderColor: paleta[i % paleta.length],
      backgroundColor: paleta[i % paleta.length],
      borderDash: [5, 4],
      tension: 0.35,
      borderWidth: 2,
      pointRadius: 0,
      pointHoverRadius: 5,
      fill: false,
    })) : []),
  ]

  const opciones = {
    responsive: true,
    maintainAspectRatio: false,
    animation: animaciones ? { duration: 900 } : false,
    interaction: { mode: 'index', intersect: false },
    plugins: {
      legend: {
        display: datasets.length > 1,
        position: 'bottom',
        labels: { color: tinta.texto, usePointStyle: true, pointStyle: 'line', boxWidth: 24, padding: 16 },
      },
      tooltip: {
        backgroundColor: tinta.superficie,
        titleColor: tinta.texto,
        bodyColor: tinta.texto,
        borderColor: tema === 'dark' ? 'rgba(220,240,230,0.2)' : 'rgba(55,80,68,0.2)',
        borderWidth: 1,
        padding: 10,
        callbacks: {
          title: (it) => periodo(puntos[it[0].dataIndex].periodo, true),
          label: (it) => ` ${it.dataset.label}: ${numero(it.parsed.y)} kg CO₂`,
        },
      },
    },
    scales: {
      x: {
        grid: { display: false },
        ticks: { color: tinta.suave, callback: (_, i) => periodo(puntos[i]?.periodo), maxRotation: 0, autoSkipPadding: 14 },
        border: { color: tinta.grilla },
      },
      y: {
        beginAtZero: true,
        grid: { color: tinta.grilla },
        border: { display: false },
        ticks: { color: tinta.suave, callback: (v) => `${numero(v, 0)} kg` },
      },
    },
  }

  return (
    <div style={{ height: alto }}>
      <Line ref={refGrafico} data={{ labels: puntos.map((p) => p.periodo), datasets }} options={opciones} role="img"
        aria-label="Evolución mensual de las emisiones en kg de CO2" />
    </div>
  )
}

/** Barras horizontales para rankings (fuentes, zonas, usuarios). Un solo color: es magnitud, no identidad. */
export function GraficoBarras({ items, alto, refGrafico, color }) {
  const { tema, animaciones } = usePreferencias()
  const tinta = tintaGrafico(tema)
  // El canvas no entiende var(--x): se resuelve la variable CSS al color real del tema actual
  const c = (color?.startsWith('var(')
    ? getComputedStyle(document.documentElement).getPropertyValue(color.slice(4, -1)).trim()
    : color) || (SERIES[tema] || SERIES.light)[1]

  const data = {
    labels: items.map((i) => i.nombre),
    datasets: [{
      data: items.map((i) => Number(i.kgCo2)),
      backgroundColor: c,
      borderRadius: 4,
      borderSkipped: 'start',
      barThickness: 16,
    }],
  }

  const opciones = {
    indexAxis: 'y',
    responsive: true,
    maintainAspectRatio: false,
    animation: animaciones ? { duration: 800 } : false,
    plugins: {
      legend: { display: false },
      tooltip: {
        backgroundColor: tinta.superficie, titleColor: tinta.texto, bodyColor: tinta.texto,
        borderColor: tema === 'dark' ? 'rgba(220,240,230,0.2)' : 'rgba(55,80,68,0.2)', borderWidth: 1,
        callbacks: { label: (it) => ` ${numero(it.parsed.x)} kg CO₂ (${porcentaje(items[it.dataIndex].porcentaje)})` },
      },
    },
    scales: {
      x: { beginAtZero: true, grid: { color: tinta.grilla }, border: { display: false }, ticks: { color: tinta.suave, callback: (v) => `${numero(v, 0)}` } },
      y: { grid: { display: false }, ticks: { color: tinta.texto, callback: (_, i) => { const n = items[i]?.nombre || ''; return n.length > 28 ? `${n.slice(0, 27)}…` : n } } },
    },
  }

  return (
    <div style={{ height: alto || Math.max(140, items.length * 30 + 40) }}>
      <Bar ref={refGrafico} data={data} options={opciones} role="img" aria-label="Ranking de emisiones en kg de CO2" />
    </div>
  )
}

/** Lista con barras de progreso (vista compacta de rankings en las tarjetas) */
export function ListaProgreso({ items, max = 5, colorFijo }) {
  const { tema } = usePreferencias()
  const c = colorFijo || (SERIES[tema] || SERIES.light)[1]
  return (
    <div className="d-flex flex-column gap-3">
      {items.slice(0, max).map((i) => (
        <div key={i.id}>
          <div className="d-flex justify-content-between gap-2 small mb-1">
            <span className="fw-semibold text-truncate" title={i.nombre}>{i.nombre}</span>
            <span className="fw-bold text-nowrap">{porcentaje(i.porcentaje)}</span>
          </div>
          <div className="barra-progreso"><div style={{ width: `${Math.max(2, i.porcentaje)}%`, background: c }} /></div>
        </div>
      ))}
    </div>
  )
}
