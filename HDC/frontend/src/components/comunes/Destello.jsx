import { useEffect, useRef, useState } from 'react'

/**
 * Envuelve un elemento y le aplica un destello sutil (borde inferior que titila)
 * cada vez que cambia `valor`. No se activa en el primer render.
 */
export default function Destello({ valor, as: Tag = 'div', className = '', children, ...props }) {
  const previo = useRef(valor)
  const [activo, setActivo] = useState(false)

  useEffect(() => {
    const serial = JSON.stringify(valor)
    if (previo.current !== undefined && JSON.stringify(previo.current) !== serial) {
      setActivo(false)
      const r = requestAnimationFrame(() => setActivo(true))
      const t = setTimeout(() => setActivo(false), 1500)
      previo.current = valor
      return () => { cancelAnimationFrame(r); clearTimeout(t) }
    }
    previo.current = valor
  }, [valor])

  return <Tag className={`${className} ${activo ? 'destello' : ''}`} {...props}>{children}</Tag>
}

/** Número que "sube" con una animación cuando cambia. */
export function ValorAnimado({ valor, children }) {
  return <span key={String(valor)} className="valor-cambio">{children ?? valor}</span>
}
