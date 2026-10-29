# Cambios: corrección del valor de 1 ppm

## Problema

El proyecto usaba **1 ppm = 17.600 millones de t CO₂**. El valor correcto es
**1 ppm ≈ 7.800 millones de t CO₂**: 1 ppm de CO₂ atmosférico equivale a unas 2,13 Gt de
carbono, y 2,13 × 3,664 (relación de masa CO₂/C) ≈ 7,8 Gt de CO₂.
`textos/especificacionEstilos.txt` ya traía el valor correcto, así que el proyecto se
contradecía. Además, el número estaba escrito a mano en varios lugares.

Con el valor viejo, los ppm que mostraba la app quedaban **2,26 veces más chicos** de lo que
corresponde.

## Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `frontend/src/utils/formato.js` | `TONELADAS_POR_PPM` pasa de `17_600_000_000` a `7_800_000_000`. Se agregó la constante `EQUIVALENCIA_PPM` con el texto de referencia. Es el **único lugar** donde se define el valor |
| `frontend/src/pages/usuario/Dashboard.jsx` | El tooltip del total usa `EQUIVALENCIA_PPM` en lugar del texto escrito a mano |
| `frontend/src/utils/pdf.js` | La línea "Equivalente atmosférico" usa `EQUIVALENCIA_PPM`; el "₂" se pasa a "2" porque las fuentes estándar de jsPDF no lo dibujan |
| `README.md`, `textos/README.md`, `MANUAL_USUARIO.md` | Se corrigió el valor en la documentación |

Las otras pantallas que muestran ppm (`Reportes.jsx` y `AdminPanel.jsx`) usan la función `ppm()`
de `formato.js`, así que el cambio de la constante ya las corrige.
El backend no calcula ppm.

## Qué NO se tocó

- `textos/instruccionesClaude.txt` (línea 220) sigue diciendo 17.600. Es la consigna original
  y se dejó como registro histórico. De ahí salió el valor equivocado.

## Para cambiar el valor en el futuro

Modificar solo `TONELADAS_POR_PPM` y `EQUIVALENCIA_PPM` en `frontend/src/utils/formato.js`.
