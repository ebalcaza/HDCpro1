import { createContext, useContext, useEffect, useMemo, useState } from 'react'

const CLAVE = 'hdc-preferencias'
const PreferenciasContext = createContext(null)

const leer = () => {
  try { return JSON.parse(localStorage.getItem(CLAVE) || '{}') } catch { return {} }
}

export const ESCALAS = [
  { valor: 0.9, texto: 'Pequeña' },
  { valor: 1, texto: 'Normal' },
  { valor: 1.1, texto: 'Grande' },
  { valor: 1.25, texto: 'Muy grande' },
  { valor: 1.4, texto: 'Máxima' },
]

export function PreferenciasProvider({ children }) {
  const inicial = leer()
  const [tema, setTema] = useState(
    inicial.tema || (window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'))
  const [escala, setEscala] = useState(inicial.escala || 1)
  const [animaciones, setAnimaciones] = useState(inicial.animaciones ?? true)
  const [altoContraste, setAltoContraste] = useState(inicial.altoContraste ?? false)

  useEffect(() => {
    const html = document.documentElement
    html.setAttribute('data-bs-theme', tema)
    html.style.setProperty('--escala-fuente', escala)
    html.classList.toggle('sin-animaciones', !animaciones)
    html.classList.toggle('alto-contraste', altoContraste)
    try { localStorage.setItem(CLAVE, JSON.stringify({ tema, escala, animaciones, altoContraste })) } catch { /* sin almacenamiento */ }
  }, [tema, escala, animaciones, altoContraste])

  const valor = useMemo(() => ({
    tema, alternarTema: () => setTema((t) => (t === 'dark' ? 'light' : 'dark')),
    escala, setEscala,
    animaciones, setAnimaciones,
    altoContraste, setAltoContraste,
    restablecer: () => { setEscala(1); setAnimaciones(true); setAltoContraste(false) },
  }), [tema, escala, animaciones, altoContraste])

  return <PreferenciasContext.Provider value={valor}>{children}</PreferenciasContext.Provider>
}

export const usePreferencias = () => useContext(PreferenciasContext)
