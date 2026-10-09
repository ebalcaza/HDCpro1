/**
 * Series de gráficos. Orden fijo (nunca se recicla) y validado para contraste,
 * daltonismo y separación en modo claro y oscuro. Tienen que coincidir con --serie-N de tema.css.
 */
export const SERIES = {
  light: ['#2e8b57', '#2f6db5', '#b4602a', '#6d5bb0', '#a87a06', '#00899c'],
  dark: ['#3fa36c', '#4f8ad6', '#cf7840', '#8b7bd6', '#b98a1c', '#2aa4ac'],
}

export const OTROS = { light: '#8a948f', dark: '#7d8a84' }

/** El color sigue a la entidad (id), no al ranking: un filtro no repinta las series. */
export const colorPorId = (id, tema, ids) => {
  const paleta = SERIES[tema] || SERIES.light
  const idx = ids ? ids.indexOf(id) : Number(id) - 1
  return idx >= 0 && idx < paleta.length ? paleta[idx] : OTROS[tema] || OTROS.light
}

export const tintaGrafico = (tema) => (tema === 'dark'
  ? { texto: '#c8d3cd', suave: '#a2afa8', grilla: 'rgba(220,240,230,0.08)', superficie: '#26322d' }
  : { texto: '#4a5650', suave: '#6b7671', grilla: 'rgba(55,80,68,0.08)', superficie: '#fbfcfa' })
