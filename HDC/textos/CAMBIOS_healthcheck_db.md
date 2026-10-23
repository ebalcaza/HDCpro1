# Cambio: healthcheck del contenedor `db` más tolerante

## Problema
Al ejecutar `docker compose up -d --build` aparecía:

```
✘ Container hdc-db-1  Error  dependency db failed to start
dependency failed to start: container hdc-db-1 is unhealthy
```

## Causa
SQL Server no falla, pero en un equipo con poca RAM (~4 GB) tarda unos 10 minutos en
terminar de arrancar. El healthcheck anterior esperaba como máximo unos 6 minutos
(`start_period: 60s` + `retries: 30` x `interval: 10s`). Cuando se agotaba ese tiempo, Docker
marcaba el contenedor como *unhealthy* y, por `depends_on: condition: service_healthy`,
no levantaba la API.

## Cambio (`docker-compose.yml`, servicio `db`)
| Parámetro      | Antes | Ahora |
|----------------|-------|-------|
| `interval`     | 10s   | 15s   |
| `timeout`      | 20s   | 30s   |
| `retries`      | 30    | 60    |
| `start_period` | 60s   | 300s  |

Margen total: ~20 minutos. En una PC rápida no cambia nada: el healthcheck pasa apenas
SQL Server responde.

## Recomendaciones
- Antes de levantar los contenedores, cerrar las aplicaciones pesadas (navegador, IDE).
- Si el `db` ya quedó *healthy*, basta con `docker compose up -d` (sin `--build`) para levantar el resto.
