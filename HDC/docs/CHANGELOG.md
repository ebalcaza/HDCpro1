# Historial de cambios (CHANGELOG) — Huella de Carbono (HDC)

Registro de todos los cambios del sistema, **del más nuevo al más viejo**. Cada entrada indica qué se
cambió, por qué y qué archivos se tocaron.

> **Cómo agregar una entrada:** copiá la plantilla del final, pegala arriba de la entrada más reciente y
> completala. No crees archivos sueltos de cambios: todo va acá (ver
> [Guía del desarrollador § 11](GUIA_DESARROLLADOR.md#11-cómo-mantener-esta-documentación)).

---

## [2.12] — 03/11/2026 — Km totales por medio y un registro por mes en la colaboración

**Por qué:** `textos/fix13.txt` pide cargar más de una forma de traslado en un mes, con km y días por semana entre 1 y 7, contemplar varios
colectivos para llegar e impedir más de un registro del mismo mes en una colaboración.

**Decisiones tomadas:**
- **Km totales por medio:** quien toma 3 colectivos carga una sola fila con el total de km de los tres. Se eligió frente a repetir el medio en varias filas porque es más simple de completar y no cambia la base de datos. Si cada línea se usa distintos días, hay que promediar.
- **Días por semana entre 1 y 7:** antes el mínimo era 0,5. Lo ya cargado no cambia, pero una carga con menos de 1 día se tiene que corregir al editarla.
- **Un registro de consumos por mes en la colaboración:** quien colabora ya no puede crear un segundo registro de consumos de ese mes, ni mover otro a un mes ocupado; edita el existente. Los traslados ya tenían esta regla desde la v2.11. Corregir un registro sin cambiarlo de mes sigue permitido, por los datos anteriores.
- **Consumos y traslados se controlan por separado:** en una colaboración que habilita ambos, un mes puede tener un registro de cada clase, porque un traslado no se puede agregar a un registro de consumos.
- **El titular conserva su forma de cargar.**

| Archivo | Cambio |
|---|---|
| `backend/HDC.Entity/Dtos/EmisionDtos.cs`, `frontend/src/pages/usuario/TrasladoForm.jsx` | Días entre 1 y 7; textos de ayuda sobre el total de km por medio |
| `backend/HDC.BLL/Servicios/TrasladoServicio.cs` | Mensaje al repetir un medio y explicación de la regla |
| `backend/HDC.BLL/Servicios/RegistroServicio.cs`, `backend/HDC.DAL/Repositorios/EmisionRepositorios.cs` | Un registro de consumos por mes para quien colabora |

Sin cambios en la base de datos. Documentos actualizados: MANUAL_USUARIO (§ 5), DOCUMENTACION_TECNICA (traslados), GUIA_PRUEBAS (§ 3.1) y README.

---

## [2.11] — 03/11/2026 — Eliminar convocatorias, un traslado por mes y detalle de "Donde colaboro"

**Por qué:** `textos/fix12.txt` pide eliminar una convocatoria por completo, impedir que se vuelva a grabar un traslado ya cargado, filtrar
**Mis emisiones** por origen, ver todo el detalle en **Donde colaboro** y que **Colaboraciones** abra en **Convocatorias**.

**Decisiones tomadas:**
- **Eliminar borra todo lo que nació de la convocatoria,** en cualquier estado: miembros, roles, cargas de traslado y registros. Esos kg dejan de sumar a la huella del domicilio. Reemplaza la regla anterior, que solo permitía eliminarla sin miembros. La confirmación indica cuántas personas y kg se borran. **Finalizar** sigue siendo la opción para conservar lo cargado.
- **Quien colabora declara un solo traslado por mes en cada convocatoria,** con cualquier medio. Reemplaza el criterio anterior, que solo rechazaba repetir el mismo medio. Para cambiar un mes o sumar un medio, edita la carga. El servidor también lo valida.
- **El formulario se adapta a lo cargado:** sin meses disponibles se bloquea y ofrece **Editar**. Con algunos cargados, no permite **todo el período** y solo lista los meses que faltan.
- **El titular conserva la regla anterior** (no repetir el mismo medio en un mes), porque puede declarar varios grupos con medios distintos en un mismo mes.
- **Excepción para datos existentes:** una edición que no suma meses no se rechaza, para no trabar a quien ya tenía dos cargas con distinto medio en un mismo mes.
- **Origen en Mis emisiones:** usa el mismo criterio que la etiqueta "Colaboración": cargó alguien que no es titular del domicilio.
- **Meses que faltan:** un mes cuenta como cargado si tiene al menos un registro de esa colaboración.
- **Al enviar una invitación se pasa a su pestaña,** porque la sección ahora abre en **Convocatorias** y no se vería.

**Corrección relacionada:** `BotonCarga` dejaba el botón activo mientras guardaba si recibía `disabled={false}`, lo que permitía enviar dos veces.

| Archivo | Cambio |
|---|---|
| `backend/HDC.DAL/Repositorios/ConvocatoriaRepositorio.cs`, `backend/HDC.BLL/Servicios/ConvocatoriaServicio.cs` | Eliminación completa en una transacción y recálculo del total del domicilio |
| `backend/HDC.DAL/Repositorios/CargaTrasladoRepositorio.cs`, `backend/HDC.BLL/Servicios/TrasladoServicio.cs` | Un traslado por mes para quien colabora; `opciones` devuelve sus cargas |
| `backend/HDC.DAL/Repositorios/EmisionRepositorios.cs`, `backend/HDC.BLL/Servicios/ColaboracionServicio.cs`, `backend/HDC.API/Controllers/EmisionControllers.cs` | Filtro `origen` y `GET colaboraciones/{id}/detalle` |
| `backend/HDC.Entity/Dtos/` | `ColaboracionDetalleDto`, `RegistroColaboracionDto`, `CargaPropiaDto` y `FiltroRegistros.Origen` |
| `frontend/src/pages/usuario/Colaboraciones.jsx` | Pestaña inicial, tabla breve y ventana de detalle |
| `frontend/src/pages/usuario/Convocatorias.jsx` | Botón **Eliminar** en todas las convocatorias y confirmación con lo que se borra |
| `frontend/src/pages/usuario/TrasladoForm.jsx` | Bloqueo de lo ya cargado y enlaces para editar |
| `frontend/src/pages/usuario/MisEmisiones.jsx`, `frontend/src/api/servicios.js` | Filtro **Origen** y llamada al detalle |
| `frontend/src/components/comunes/Basicos.jsx` | Corrección de `BotonCarga` |

Sin cambios en la base de datos. Documentos actualizados: MANUAL_USUARIO (§ 5, 6 y 9), DOCUMENTACION_TECNICA (API, traslados y § 11), GUIA_PRUEBAS (§ 3.1) y README.

---

## [2.10] — 03/11/2026 — Botón Cargar por convocatoria y aportes separados

**Por qué:** `textos/fix11.txt` pide tres cosas: que **Cargar** no vuelva a pedir la convocatoria ni el rol, que sumarse a otra convocatoria del
mismo titular no muestre aportes que la persona no cargó y que **Traslados por rol** se actualice.

**Causas encontradas:**
- **El botón no enviaba la convocatoria,** solo el domicilio.
- **No se creaban registros de más.** Los aportes se buscaban por persona y domicilio, así que lo cargado en una convocatoria aparecía en todas
  las del mismo titular.
- **La tabla por rol contaba solo a quien ya había cargado,** y una carga hecha en otra convocatoria sumaba allí. Además, no se recargaba con la pantalla abierta.

**Decisiones tomadas:**
- **Cada registro guarda su convocatoria** (`registros.id_convocatoria`). Los ya existentes se completan una sola vez, al migrar.
- **Si llega por Cargar, el rol se muestra fijo** con la opción **Cambiar**. Los miembros que nunca eligieron rol lo eligen en su primera carga.
- **Colaboradores por rol:** quienes lo eligieron al sumarse y están activos, más quienes cargaron con él. Reemplaza el criterio de la v2.7.
- **Se puede cargar una convocatoria cuyo período ya terminó,** como se definió en la v2.8. Antes el botón desaparecía.
- **Cada convocatoria es independiente:** el dueño decide sus períodos, también de años ya terminados, y una persona declara su traslado en cada una. El mismo medio y mes solo se rechaza dentro de la misma convocatoria. Si dos convocatorias de un domicilio se superponen, el total del domicilio suma ambas.
- **El dashboard no cambia:** "Aportes de colaboradores" sigue sumando todo lo cargado en el domicilio.

| Archivo | Cambio |
|---|---|
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql` | Columna `registros.id_convocatoria`, migración e índice |
| `backend/HDC.DAL/Repositorios/ConvocatoriaRepositorio.cs`, `EmisionRepositorios.cs` | Aportes por convocatoria y resultados por rol |
| `backend/HDC.BLL/Servicios/RegistroServicio.cs`, `TrasladoServicio.cs`, `ColaboracionServicio.cs`, `DatosPruebaServicio.cs` | Convocatoria de cada aporte y carga de períodos terminados |
| `backend/HDC.Entity/`, `backend/HDC.DAL/HdcDbContext.cs` | Entidad, DTO y mapeo |
| `frontend/src/pages/usuario/Colaboraciones.jsx`, `RegistroForm.jsx`, `TrasladoForm.jsx` | **Cargar** lleva la convocatoria; domicilio, convocatoria y rol quedan definidos |
| `frontend/src/pages/usuario/ConvocatoriaDetalle.jsx` | Actualización al volver a la pestaña y explicación de la tabla por rol |

Documentos actualizados: MANUAL_USUARIO (§ 5 y 9), DOCUMENTACION_TECNICA (convocatorias), GUIA_PRUEBAS (§ 3.1) y README.

**Verificación:**

| Prueba | Resultado |
|---|---|
| Compilación de la API y del frontend | Correctas |
| Migración en una copia de la base local, ejecutada dos veces | 465 registros y kg sin cambios; el caso reportado queda con 13 registros en su convocatoria y 0 en las otras dos |
| 25 comprobaciones de fix11, en esa copia y en una base nueva | Todas correctas |
| 58 comprobaciones de fix10, en la base nueva | Todas correctas |
| 9 comprobaciones de convocatorias independientes, en la copia | Todas correctas: mismo medio y mes permitido entre convocatorias y rechazado dentro de una, y convocatoria de un año terminado |
| Datos de prueba generados en la base nueva | Sin errores; totales de domicilios correctos y aportes asociados a su convocatoria |

**Falta probar:** las pantallas en el navegador (ver [Guía de pruebas § 3.1](GUIA_PRUEBAS.md)).

---

## [2.9] — 03/11/2026 — Rol al sumarse, factores de viajes, domicilio opcional y baja inmediata

**Por qué:** `textos/fix10.txt` pide seis cambios: elegir el rol al sumarse a una convocatoria, deducir los factores de viajes de las
fuentes existentes, ordenar el formulario de medios de transporte, no exigir domicilio al registrarse, no aceptar 0 en los consumos y
quitar el acceso al instante cuando el administrador da de baja una cuenta.

**Decisiones tomadas:**
- **Los viajes dejan de tener factor 0.** Esto reemplaza la decisión de la v2.6, tomada por `fix7`.
- **Hace falta un consumo por km.** `fix10` pide usar el factor del combustible y la distancia, pero ese factor está en kg CO₂e por litro:
  multiplicarlo directamente por km daría 2,38 kg por km en colectivo, unas 25 veces más que el factor de traslados del catálogo.
  Por eso `factor por km = factor del combustible o la electricidad × consumo por km`.
- **Los consumos por km no son datos nuevos:** se calcularon a partir de los factores de traslados del catálogo, así que un km en colectivo
  emite lo mismo como traslado que como viaje. Falta que los valide un especialista. Se editan desde el panel.
- **El avión sigue en 0:** el catálogo no tiene un combustible de aviación del que partir.
- **Los traslados de colaboradores no cambian:** siguen con sus propios factores por km.
- **Lo ya cargado conserva su factor:** los viajes existentes siguen en 0 kg. Cambian solo las cargas nuevas.
- **El rol se elige al sumarse si la convocatoria tiene roles.** Se propone en cada traslado y el último usado queda guardado. Quien ya
  había cargado un traslado toma el rol de su última carga. Quien ya era miembro y nunca cargó lo elige en su primera carga.
- **Baja inmediata:** se consulta la base en cada pedido autenticado, por clave primaria. Se prefirió a un caché, que dejaría una demora.
  Una pantalla abierta muestra el aviso en su siguiente pedido al servidor.

| Archivo | Cambio |
|---|---|
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql`, `scripts/cargar_catalogo_definitivo.sql` | Columnas `id_fuente_base`, `consumo_por_km` e `id_convocatoria_rol`, factores calculados y rol tomado de la última carga |
| `backend/HDC.API/Program.cs`, `backend/HDC.BLL/Servicios/AuthServicio.cs`, `backend/HDC.DAL/Repositorios/SeguridadRepositorios.cs` | Comprobación de cuenta activa en cada pedido y domicilio opcional |
| `backend/HDC.BLL/Servicios/CatalogoServicios.cs`, `backend/HDC.DAL/Repositorios/CatalogoRepositorios.cs`, `backend/HDC.API/Controllers/AdminControllers.cs` | Cálculo y validación del factor, y baja de una fuente usada como base |
| `backend/HDC.BLL/Servicios/ConvocatoriaServicio.cs`, `TrasladoServicio.cs`, `backend/HDC.DAL/Repositorios/CargaTrasladoRepositorio.cs`, `backend/HDC.API/Controllers/ConvocatoriasController.cs` | Rol al sumarse, propuesto y recordado |
| `backend/HDC.Entity/`, `backend/HDC.DAL/HdcDbContext.cs`, `backend/HDC.BLL/Comun/Mapeos.cs`, `DatosPruebaServicio.cs` | Entidades, DTO, mapeos, km mayores a 0 y comentario de los datos de prueba |
| `frontend/src/pages/publicas/Registro.jsx` | Domicilio opcional |
| `frontend/src/pages/usuario/Convocatorias.jsx`, `Colaboraciones.jsx`, `frontend/src/api/servicios.js` | Rol al sumarse y aviso si no hay domicilios |
| `frontend/src/pages/usuario/TrasladoForm.jsx` | Rol propuesto y formulario del medio reordenado |
| `frontend/src/pages/usuario/RegistroForm.jsx` | Km en 0 rechazados y ayuda de viajes |
| `frontend/src/pages/admin/Fuentes.jsx` | Fuente base y consumo por km en los viajes |
| `frontend/src/api/cliente.js`, `frontend/src/context/AuthContext.jsx` | Aviso de cuenta deshabilitada |

Documentos actualizados: MANUAL_USUARIO (§ 1, 3, 5 y problemas frecuentes), DOCUMENTACION_TECNICA (§ 4, 6 y 9), GUIA_PRUEBAS (§ 3.1) y README.

**Verificación:**

| Prueba | Resultado |
|---|---|
| Compilación de la API y del frontend | Correctas |
| Script de inicio en una base nueva, ejecutado dos veces | Sin errores; 6 viajes calculados y avión en 0 |
| Script de inicio en una copia del respaldo previo a la v2.8, ejecutado dos veces | Registros y kg sin cambios; viajes ya cargados en factor 0; 50 de 62 miembros con rol tomado de su carga |
| Catálogo definitivo en esa copia | Mismos factores calculados |
| 58 comprobaciones de la API en una base temporal | Todas correctas: registro, rol, factores, ceros y baja inmediata. La del dashboard usó una ruta incorrecta y se repitió aparte: responde 200 sin domicilio |

**Falta probar:** las pantallas en el navegador, incluido el formulario reordenado (ver [Guía de pruebas § 3.1](GUIA_PRUEBAS.md)), y validar los consumos por km con un especialista.

---

## [2.8] — 03/11/2026 — Período definido por el organizador, varios medios y detalle de aportes

**Por qué:** `textos/fix9.txt` pide que en una convocatoria el período lo configure el organizador, que sea anual por defecto y
que el colaborador pueda cargar todo o por mes, con más ayuda. También pide varias formas de transportarse y que el organizador
vea el detalle completo de lo que cargaron los colaboradores.

**Decisiones tomadas:**
- **Las convocatorias se abren al final del período**, por ejemplo en diciembre para todo el año. Por eso los meses futuros no se
  generan ni se completan solos. Si el período sigue en curso, se carga hasta el mes actual y se puede editar después.
- **Las semanas de actividad las define el organizador**, porque conoce el calendario real, y así los resultados son comparables.
- **El titular y las invitaciones por email conservan la elección** de período mensual, semestral o anual. También pueden usar varios medios.
- **Una carga tiene varios medios:** cada mes genera un registro con un detalle por medio. No se limita la suma de días a 7, porque
  se pueden combinar medios en un mismo viaje.
- **Sin período de fin no hay traslados:** una convocatoria que habilita traslados necesita principio, fin y semanas. Si una convocatoria
  anterior no los tiene, sus colaboradores usan el formulario libre hasta que el organizador la edite.

| Archivo | Cambio |
|---|---|
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql` | Tabla `carga_traslado_medio`, columnas nuevas y migración de las cargas de la v2.6 |
| `scripts/cargar_catalogo_definitivo.sql`, `scripts/limpiar_datos_produccion.sql` | Adaptados a la tabla nueva |
| `backend/HDC.BLL/Servicios/TrasladoServicio.cs`, `backend/HDC.DAL/Repositorios/CargaTrasladoRepositorio.cs` | Período de la convocatoria, varios medios y meses cargados |
| `backend/HDC.BLL/Servicios/ConvocatoriaServicio.cs`, `backend/HDC.DAL/Repositorios/ConvocatoriaRepositorio.cs`, `backend/HDC.API/Controllers/ConvocatoriasController.cs` | Validación del período y las semanas, y `GET convocatorias/{id}/aportes` |
| `backend/HDC.BLL/Servicios/RegistroServicio.cs`, `backend/HDC.DAL/Repositorios/EmisionRepositorios.cs`, `backend/HDC.DAL/HdcDbContext.cs`, `backend/HDC.Entity/` | Entidades, DTO y mapeos |
| `backend/HDC.BLL/Servicios/DatosPruebaServicio.cs` | Datos de prueba con el período del organizador, carga total y mensual, y varios medios |
| `frontend/src/pages/usuario/TrasladoForm.jsx` | Período del organizador, opción de cargar todo o un mes, meses faltantes, varios medios y textos de ayuda |
| `frontend/src/pages/usuario/Convocatorias.jsx` | Año completo por defecto, otro período como última opción y semanas de actividad |
| `frontend/src/pages/usuario/ConvocatoriaDetalle.jsx` | Vista **Detalle de aportes** y botón **Ver aportes** |
| `frontend/src/pages/usuario/RegistroDetalle.jsx`, `frontend/src/utils/formato.js`, `frontend/src/api/servicios.js` | Datos de los medios y nueva llamada a la API |

Documentos actualizados: MANUAL_USUARIO (§ 5 y 9), GUIA_DESARROLLADOR (§ 3), DOCUMENTACION_TECNICA (§ 4, 5, 9 y 11), GUIA_PRUEBAS (§ 3.1) y README.

**Verificación:**

| Prueba | Resultado |
|---|---|
| Compilación de la API y del frontend | Correctas |
| Migración en una copia de la base local, ejecutada dos veces | 128 cargas pasan a 128 medios, 124 quedan asociadas a su convocatoria y se borran las columnas viejas |
| Catálogo y limpieza en esa copia | Sin errores; queda solo el administrador y 0 medios |
| 37 comprobaciones de la API en una base temporal | Todas correctas: período del organizador, carga total y mensual, varios medios, cálculos, solapamiento, edición, eliminación, aportes y permisos |

**Falta probar:** las pantallas nuevas en el navegador (ver [Guía de pruebas § 3.1](GUIA_PRUEBAS.md)).

---

## [2.7] — 03/11/2026 — Datos de prueba con el catálogo definitivo

**Por qué:** se pidieron datos de prueba actualizados con las fuentes nuevas. Los anteriores usaban solo unas pocas.

**Decisiones tomadas:**
- **Se mantienen las cuentas existentes** y se agrega una facultad de prueba, el caso que describe `fix8`, con los roles
  estudiantes, personal docente y personal no docente, cada uno con un período distinto.
- **Entre todos los escenarios se usan las 45 fuentes**, los 8 medios de transporte y los 3 períodos.
- **Los valores son inventados.** No representan a ninguna institución real.
- **La carga de un grupo no tiene rol**, porque los resultados por rol cuentan personas que cargaron, no los integrantes de un grupo.

| Archivo | Cambio |
|---|---|
| `backend/HDC.BLL/Servicios/DatosPruebaServicio.cs` | Facultad de prueba con la convocatoria FACU-2K26, más fuentes en los hogares y la planta, km de vehículos y viajes institucionales |
| `.env` local (no se versiona) | `DATOS_PRUEBA=true` |

Documentos actualizados: GUIA_DESARROLLADOR (§ 3), GUIA_PRUEBAS (§ 3.1) y README.

**Verificación** (en una base temporal, con el script de inicio y la geografía copiada):

| Prueba | Resultado |
|---|---|
| Generación | Sin errores: 67 usuarios y 354 registros |
| Cobertura del catálogo | 0 fuentes sin usar |
| Datos adicionales | 36 detalles con km y 7 viajes con fecha |
| Traslados | 8 cargas anuales, 17 semestrales y 101 mensuales; ninguna pasa del mes actual |
| Resultados por rol | Estudiantes: 16 personas; docentes: 8; no docentes: 6 |
| Totales de los domicilios | Coinciden con la suma de sus registros |

---

## [2.6] — 03/11/2026 — Catálogo definitivo de fuentes, traslados y roles de convocatoria

**Por qué:** `textos/fix7.txt` pide borrar los datos de prueba y cargar las fuentes definitivas, dejando la posibilidad de
editarlas o ampliarlas. `textos/fix8.txt` pide calcular el transporte con medio, distancia y frecuencia, en períodos
mensuales, semestrales o anuales, y que quien crea la convocatoria defina roles.

**Decisiones tomadas:**
- **Factores:** SNIGEI-AR 2025 donde existe, como pide el diseño metodológico de la UPE, y `fix7` para el resto. El detalle y
  los orígenes están en [Documentación técnica § 9](DOCUMENTACION_TECNICA.md#9-conceptos-del-dominio).
- **Diferencias con `fix7`:** los factores de combustibles, electricidad, refrigerantes, residuos sólidos y residuos
  peligrosos se reemplazaron por los del SNIGEI. Los residuos sólidos usan relleno con captura de metano (CEAMSE) y los
  peligrosos, incineración: si el destino real es otro, hay que ajustarlos desde el panel.
- **Grupos electrógenos en combustión fija:** como indica el documento de alcance de la UPE. `fix7` los ubicaba en móvil.
- **R-32 en lugar de R-30:** la lista de `fix7` pide R-32; su tabla de factores seguía con R-30.
- **Electricidad renovable con factor 0:** enfoque basado en el mercado. No se reportan los dos enfoques del GHG Protocol por separado.
- **Viajes institucionales con factor 0:** `fix7` pide guardarlos solo como dato.
- **Una carga de traslado genera un registro por mes:** el total se reparte en partes iguales, para no mostrar un pico falso.
  Solo se generan meses hasta el actual. Los meses de una carga se editan y eliminan juntos.
- **Colaborador y titular:** el colaborador carga solo su propio viaje. El titular puede cargar un grupo indicando la cantidad de personas.
- **Roles de convocatoria separados de los roles del sistema**, con resultados por rol en el detalle de la convocatoria.
- **Quedan afuera por ahora:** teletrabajo, porque el documento de la UPE no trae datos ni factor, y los indicadores por
  estudiante o por m². Una encuesta parcial tampoco se extrapola sola al total de la comunidad.

| Archivo | Cambio |
|---|---|
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql` | Tablas `carga_traslado` y `convocatoria_rol`, columnas nuevas, índices y catálogo definitivo para bases nuevas |
| `scripts/cargar_catalogo_definitivo.sql` | **Nuevo.** Actualiza el catálogo de una base existente |
| `scripts/limpiar_datos_produccion.sql` | Borra también las cargas de traslado y los roles |
| `backend/HDC.BLL/Servicios/TrasladoServicio.cs`, `backend/HDC.DAL/Repositorios/CargaTrasladoRepositorio.cs` | **Nuevos.** Cálculo, validaciones y gestión de los traslados |
| `backend/HDC.API/Controllers/EmisionControllers.cs` | Endpoints `api/traslados` |
| `backend/HDC.BLL/Servicios/RegistroServicio.cs` | Km opcionales de vehículos, fecha de viajes y bloqueo de la edición suelta de un mes de traslado |
| `backend/HDC.BLL/Servicios/ConvocatoriaServicio.cs`, `backend/HDC.DAL/Repositorios/ConvocatoriaRepositorio.cs` | Roles y resultados por rol |
| `backend/HDC.BLL/Servicios/DatosPruebaServicio.cs` | Datos de prueba adaptados al catálogo definitivo, con traslados y roles |
| `backend/HDC.Entity/`, `backend/HDC.DAL/HdcDbContext.cs`, `backend/HDC.BLL/Comun/Mapeos.cs`, `backend/HDC.BLL/Servicios/CatalogoServicios.cs`, `backend/HDC.BLL/DependencyInjection.cs` | Entidades, DTO, mapeos y registro de servicios |
| `frontend/src/pages/usuario/TrasladoForm.jsx` | **Nuevo.** Formulario de traslados |
| `frontend/src/pages/usuario/RegistroForm.jsx`, `RegistroDetalle.jsx`, `MisEmisiones.jsx` | Datos adicionales y derivación de los traslados a su formulario |
| `frontend/src/pages/usuario/Convocatorias.jsx`, `ConvocatoriaDetalle.jsx` | Alta de roles y resultados por rol |
| `frontend/src/App.jsx`, `frontend/src/api/servicios.js`, `frontend/src/utils/formato.js` | Rutas, llamadas a la API y períodos |
| `.env` local (no se versiona) | `DATOS_PRUEBA=false` |

Documentos actualizados: MANUAL_USUARIO (§ 1, 5 y 9), GUIA_DESARROLLADOR (§ 3 y 9), DOCUMENTACION_TECNICA (§ 4, 5, 9 y 11), GUIA_PRUEBAS (§ 3.1) y README.

**Verificación:**

| Prueba | Resultado |
|---|---|
| Compilación de la API y del frontend, y construcción de las imágenes Docker | Correctas. ESLint no corrió: el proyecto no tiene una configuración compatible |
| Script de inicio en una base nueva, ejecutado dos veces | Sin errores; 45 fuentes en 9 tipos |
| Catálogo en una copia de la base real, con datos viejos | 45 activas y 8 fuentes viejas de baja, sin perder historial |
| Limpieza y catálogo en esa copia | 45 activas, 0 de baja y catálogo idéntico al de una base nueva |
| 37 comprobaciones de la API en una base temporal con los datos de prueba | Todas correctas: cálculos, límite de meses, roles, solapamiento, permisos, edición, eliminación y totales |
| Borrado de un domicilio con traslados, en SQL dentro de una transacción revertida | Sus cargas se eliminan en cascada |
| Base local, tras respaldar | 1 usuario (administrador), 0 registros, 45 fuentes activas y 4.037 ciudades; login, catálogo y frontend responden |

**Falta probar:** las pantallas nuevas en el navegador (ver [Guía de pruebas § 3.1](GUIA_PRUEBAS.md)), con un especialista la
lista final de factores y el borrado de un domicilio con traslados desde la aplicación.

---

## [2.5] — 02/11/2026 — Guía para enviar emails reales desde el servidor

**Por qué:** se pidieron instrucciones paso a paso para quitar Mailpit y que los emails lleguen a casillas reales con el
proyecto alojado en un servidor. Es solo documentación: no cambia el código ni la configuración del proyecto.

| Archivo | Cambio |
|---|---|
| `docs/GUIA_EMAIL_PRODUCCION.md` | **Nuevo.** Proveedor, DNS, cambios en `.env` y `docker-compose.yml`, prueba, problemas frecuentes y cómo volver atrás |
| `docs/GUIA_DESARROLLADOR.md` | § 9: enlace a la guía nueva |
| `README.md` | Índice de documentación: guía nueva |

**Verificación:** los cambios indicados para `docker-compose.yml` se validaron con `docker compose config` en una copia.
Quedan los servicios `db`, `api` y `frontend`, y sin `SMTP_HOST` el proyecto no arranca y muestra el error. Falta probar
el recorrido en un servidor con un proveedor SMTP real.

---

## [2.4] — 02/11/2026 — Script para limpiar los datos antes de pasar a producción

**Por qué:** se pidió un script para poner en producción que borre todo menos la cuenta de administrador.

**Decisiones tomadas:**
- **Qué se conserva:** además de los administradores, los roles, permisos, catálogos y la geografía, porque el sistema
  los necesita para funcionar. Las fuentes de emisión actuales también quedan: si la tabla quedara vacía, el script de
  inicio volvería a cargar las de ejemplo.
- **Se borra también lo cargado por el administrador**, no solo lo de los demás usuarios.
- **El script va en `scripts/` y no en `backend/HDC.DAL/Scripts/`**, porque todo lo de esa carpeta se embebe en la DLL de la API.
- **Exige confirmar con el nombre de la base** y corre en una transacción: sin confirmación no borra nada, aunque el
  cliente de SQL siga ejecutando después del error. Tampoco borra si no hay un administrador activo.

| Archivo | Cambio |
|---|---|
| `scripts/limpiar_datos_produccion.sql` | **Nuevo.** Limpieza de los datos de uso, con las protecciones anteriores |
| `docs/GUIA_DESARROLLADOR.md` | § 9: pasos para ejecutarlo con respaldo previo |

**Verificación** (en la base de desarrollo, sin confirmar ningún borrado):

| Prueba | Resultado |
|---|---|
| Sin confirmación | Error; no borra nada |
| Confirmación con otro nombre de base | Error; no borra nada |
| Sin administrador activo | Error; no borra nada |
| Con confirmación, cambiando `COMMIT` por `ROLLBACK` | Queda 1 usuario (`admin@hdc.com`) y 0 filas en las tablas de uso; se conservan 3 roles, 12 permisos, 15 fuentes y 4.037 ciudades. Tras revertir siguen los 34 usuarios |

Falta probar el recorrido completo con `COMMIT`, el respaldo y su restauración.

---

## [2.3] — 02/11/2026 — Carga de consumos más clara y ajustes en colaboraciones

Pedido original: `textos/fix6.txt`.

**Por qué:** al cargar consumos, las fuentes quedaban ocultas detrás de un buscador y dos filtros. Se pidió verlas
directamente, simplificar lo que ve el colaborador, evitar que acumule filas vacías, quitar la opción de regenerar
el código y renombrar el botón "Colaboradores" de las convocatorias.

**Decisiones tomadas:**
- La lista visible y sin filtros vale para **todos** los usuarios. Lo que se le oculta al colaborador son los kg de
  CO₂ y el factor; el límite de una fila incompleta también se aplica solo al colaborador.
- Las fuentes aparecen **después de elegir el domicilio**, porque de él depende cuáles están disponibles.
- Se considera colaborador a quien eligió un domicilio de una colaboración, o a quien no tiene domicilios propios.
- Regenerar el código se quitó también de la API, para no dejar un endpoint sin uso. **Consecuencia:** si un código
  se difunde donde no debía, el titular puede cerrar la convocatoria, pero no cambiarle el código.
- El botón "Detalles" lleva ese nombre en todas las convocatorias y para todos los titulares, porque es el mismo botón.
- El colaborador sigue viendo los kg en otras pantallas (detalle del registro, "Mis emisiones", columna "Tu aporte"):
  el pedido se refería a la pantalla de carga.

**Qué se hizo:**

| Archivo | Cambio |
|---|---|
| `frontend/src/components/formularios/SelectorFuente.jsx` | **Nuevo.** Lista de fuentes siempre visible, se elige con un click. Deshabilita las fuentes usadas en otras filas y conserva, al editar, una fuente dada de baja |
| `frontend/src/components/formularios/BuscadorFuente.jsx` | **Eliminado:** su único uso era este formulario |
| `frontend/src/pages/usuario/RegistroForm.jsx` | Usa `SelectorFuente`; quita los filtros de alcance y tipo. Al colaborador le oculta factor, kg y total (también en el aviso de guardado) y le bloquea "Agregar consumo" si hay una fila incompleta. Al cambiar de domicilio se quitan las fuentes que dejan de estar disponibles |
| `frontend/src/styles/tema.css` | Estilos de la lista (`.lista-fuentes`, `.opcion-fuente`); se quitan los de las sugerencias |
| `frontend/src/pages/usuario/Colaboraciones.jsx` | "Donde colaboro" ya no muestra la columna "Puede cargar" (la pestaña del titular la conserva) |
| `frontend/src/pages/usuario/Convocatorias.jsx` | Botón "Colaboradores" → "Detalles"; se quita "Generar código nuevo" |
| `frontend/src/api/servicios.js` | Se quita `regenerarCodigo` |
| `backend/HDC.API/Controllers/ConvocatoriasController.cs`, `backend/HDC.BLL/Servicios/ConvocatoriaServicio.cs` | Se quitan el endpoint `POST convocatorias/{id}/codigo` y `RegenerarCodigoAsync` |

Documentos actualizados: MANUAL_USUARIO (§ cómo cargar un registro, § Colaboraciones, § 11), DOCUMENTACION_TECNICA
(§ API y § Convocatorias), GUIA_PRUEBAS (nueva § 3) y README (versión).

**Verificación realizada:**
- `vite build` y `dotnet build` de la API: sin errores.
- Navegador (Edge sin ventana) con las **respuestas de la API simuladas**: 28 comprobaciones correctas sobre las
  pantallas de colaborador y titular.
- Imágenes de Docker reconstruidas, con los datos de prueba reales: 21 comprobaciones de solo lectura correctas en el
  navegador, y `POST convocatorias/{id}/codigo` responde 404.
- **Pendiente:** guardar un registro real como colaborador y como titular (ver la
  [§ 3 de la Guía de pruebas](GUIA_PRUEBAS.md#3-pruebas-de-la-v23--carga-de-consumos)). En Docker no se guardó nada, para no modificar los datos.

---

## [2.2] — 31/10/2026 — Convocatorias: colaboración masiva por código

Pedido original: `textos/fix5.txt`.

**Por qué:** una organización puede tener 1.000, 2.000 o 3.000 colaboradores, e invitarlos uno por uno por
email no escala. Se pidió que el titular comparta un código para que las personas se sumen solas, y que pueda
editar qué y en qué períodos cargan, aceptar las colaboraciones de a una, todas o filtradas, y gestionarlas.

**Decisiones tomadas con el autor:**
- Disponible para **cualquier titular** (individuos y organizaciones), no solo organizaciones.
- Esquema de **convocatorias**: cada una tiene su código, domicilio, períodos y tipos de emisión. Un titular
  puede tener varias (por ejemplo, una por planta o por grupo).
- **Aprobación automática opcional** por convocatoria.
- El titular puede **dejar de recibir aportes de cualquier persona en cualquier momento** (dar de baja o
  rechazar, de a uno o en grupo). Quien fue dado de baja por el titular no puede volver a entrar con el código.
- No se puede "buscar perfiles" por nombre: el código solo identifica la convocatoria, para no exponer qué
  personas u organizaciones usan el sistema. Antes de sumarse, el usuario ve una vista previa.
- Las invitaciones por email se mantienen para casos puntuales.

**Qué se hizo:**

| Archivo | Cambio |
|---|---|
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql` | Tablas `convocatoria` y `convocatoria_tipo_emision`; columnas `colaboracion.id_convocatoria` e `id_usuario_baja` (`ADD COLUMN IF NOT EXISTS`); estado `SOLICITADA` (bloque `DO` que reemplaza la restricción de estados solo si hace falta, para bases de versiones anteriores); índices `(id_convocatoria, estado)` y `convocatoria(id_domicilio)` |
| `backend/HDC.Entity/Entities/Emisiones.cs` | Entidades `Convocatoria`, `ConvocatoriaTipoEmision`, constantes `EstadoConvocatoria`, estado `Solicitada`, `Colaboracion.IdConvocatoria/IdUsuarioBaja` y `IdsTiposHabilitados()` |
| `backend/HDC.Entity/Dtos/ColaboracionDtos.cs` | DTOs de convocatorias (alta, edición, estado, miembros, acciones masivas, vista previa del código); `ColaboracionDto` informa la convocatoria de origen |
| `backend/HDC.DAL/HdcDbContext.cs` | Mapeo de las tablas y columnas nuevas |
| `backend/HDC.DAL/Repositorios/ConvocatoriaRepositorio.cs` | **Nuevo.** Listados con conteos y aportes (`GROUP BY`), miembros paginados en SQL, filtros y acciones masivas con `ExecuteUpdate` |
| `backend/HDC.DAL/Repositorios/EmisionRepositorios.cs` | `ColaboracionRepositorio` incluye la convocatoria (sus tipos) y las "invitaciones enviadas" excluyen a los miembros de convocatorias |
| `backend/HDC.BLL/Servicios/ConvocatoriaServicio.cs` | **Nuevo.** Reglas de negocio: crear/editar (los cambios valen para todos los miembros), abrir/cerrar/finalizar, regenerar código, eliminar (solo sin miembros), sumarse con código, reglas de reingreso y acciones masivas |
| `backend/HDC.BLL/Servicios/ColaboracionServicio.cs` | Registra quién dio de baja (`IdUsuarioBaja`), permite cancelar una solicitud, impide editar individualmente una colaboración de convocatoria y usa los tipos de la convocatoria |
| `backend/HDC.BLL/Servicios/RegistroServicio.cs` | La validación de carga usa `IdsTiposHabilitados()` (tipos de la convocatoria si corresponde) |
| `backend/HDC.API/Controllers/ConvocatoriasController.cs` | **Nuevo.** Endpoints del titular y del colaborador |
| `backend/HDC.API/Infraestructura/LimitesPedidos.cs`, `Program.cs` | Límite de 20 intentos de código cada 15 min por usuario; `UseRateLimiter` pasa después de la autenticación para poder identificar al usuario |
| `backend/HDC.BLL/Servicios/DatosPruebaServicio.cs` | Convocatoria de prueba de EcoSur (código `ECSR-2K26`) con 30 empleados en distintos estados. Los bloques de datos de prueba pasan a ser idempotentes por separado |
| `frontend/src/pages/usuario/Convocatorias.jsx` | **Nuevo.** Formulario de convocatoria, modal "Sumarme con un código" con vista previa y pestaña con las convocatorias (código, copiar, conteos, acciones) |
| `frontend/src/pages/usuario/ConvocatoriaDetalle.jsx` | **Nueva** pantalla de miembros: paginado en el servidor, filtro por estado, buscador, selección con casillas, acciones por fila, sobre los seleccionados o sobre todos los filtrados |
| `frontend/src/pages/usuario/Colaboraciones.jsx` | Pestañas "Invitaciones por email", "Convocatorias" y "Donde colaboro"; botones "Sumarme con un código" y "Nueva convocatoria"; estado "Esperando aprobación" y "Cancelar solicitud" |
| `frontend/src/components/comunes/Selectores.jsx` | `SelectorTipos` se movió acá para reutilizarlo |
| `frontend/src/api/servicios.js`, `utils/formato.js`, `App.jsx` | `convocatoriasApi`, estados nuevos y ruta `/colaboraciones/convocatorias/:id` |
| `backend/HDC.BLL/HDC.BLL.csproj` | **Seguridad:** MailKit 4.9.0 → 4.18.2. La 4.9.0 (agregada en la v2.0) tiene una vulnerabilidad moderada ([GHSA-9j88-vvj5-vhgr](https://github.com/advisories/GHSA-9j88-vvj5-vhgr)). `dotnet list package --vulnerable` ya no reporta paquetes vulnerables. Probado en Docker: el email de recuperación de contraseña llega a Mailpit |

Documentos actualizados: MANUAL_USUARIO (§ 3 menú, § 9 Colaboraciones), DOCUMENTACION_TECNICA (§ 3.2, § 4, § 5,
§ 6, § 9, § 10 y nueva § 11 Convocatorias), GUIA_DESARROLLADOR (§ 3 datos de prueba). Nueva [GUIA_PRUEBAS](GUIA_PRUEBAS.md): resumen de lo hecho en v2.0–v2.2
y pruebas paso a paso en el navegador. Se completaron los comentarios de los métodos de `ConvocatoriaServicio`,
`ConvocatoriaRepositorio`, `AccionMiembrosRequest` y `Convocatorias.jsx` que no tenían.

**Verificación realizada** (API local contra la base de la v2.1 existente; luego imágenes de Docker):

| Prueba | Resultado |
|---|---|
| `dotnet build` y `npm run build` | Sin errores |
| Migración sobre la base existente (tablas, columnas, restricción de estados) y datos de prueba | OK |
| Listado de convocatorias con conteos (20 activos, 7 por aprobar, 3 bajas) y aportes | OK |
| Miembros paginados, filtro por estado, búsqueda y orden por aporte | OK |
| Aceptar 2 seleccionados / rechazar todos los filtrados / repetir sobre ya aceptados | 2 / 5 / 0 afectados |
| Dar de baja a todos los que coinciden con una búsqueda; reactivar a todos | 9 / 17 afectados |
| Otro usuario consulta los miembros | 403 |
| Vista previa con código en minúsculas y con espacio; código inexistente | OK / 404 |
| El titular intenta sumarse a su propia convocatoria | 400 |
| Sumarse (aprobación manual) / sumarse de nuevo | SOLICITADA / 409 |
| Rechazado por el titular intenta volver con el código | 403 |
| Se fue por su cuenta y vuelve | Vuelve a SOLICITADA |
| Convocatoria cerrada: sumarse | 400 |
| Aprobación automática: sumarse | ACEPTADA al instante |
| Cargar un tipo habilitado / no habilitado | 200 / 403 |
| El titular agrega un tipo a la convocatoria y el miembro lo carga | 200 (el cambio alcanza a todos) |
| Miembro dado de baja intenta cargar | 403 |
| Eliminar convocatoria con miembros | 409 |
| Finalizar: miembros dados de baja, el código deja de funcionar, no se puede reabrir | OK / 404 / 400 |
| 21 intentos de código en 15 minutos | El 21.º responde 429 |

Limitación conocida: los buscadores distinguen tildes ("lucia" no encuentra "Lucía"), igual que los demás
buscadores del sistema. Se podría resolver con la extensión `unaccent` de PostgreSQL.

---

## [2.1] — 31/10/2026 — Sección "Fuentes de emisión" para usuarios y corrección de catálogos del admin

### 2.1.1 Nueva sección "Fuentes de emisión" (Mi huella)

**Por qué:** el usuario solo veía las fuentes como una lista desplegable al cargar un registro, sin saber
qué era cada una, en qué unidad cargarla ni dónde encontrar el dato. Se pidió una sección para ver todas
las fuentes disponibles con más información.

**Qué se hizo:**
- Cada fuente tiene ahora una **descripción** (qué es y dónde encontrar el dato), editable por el
  administrador. Las 15 fuentes iniciales traen un texto de ejemplo.
- Nueva pantalla `/fuentes`: fórmula del cálculo, explicación de los alcances, fuentes agrupadas por
  alcance y tipo con unidad, factor y descripción; buscador sin distinguir tildes ni mayúsculas, filtro por
  alcance, **calculadora rápida** por fuente y botón **Cargar**, que abre el formulario de registro con la
  fuente (y el consumo) precargados.
- No hizo falta un endpoint nuevo: usa `GET /api/catalogos/fuentes`, `alcances` y `tipos-emision`
  (lectura permitida a cualquier usuario autenticado y cacheada).

| Archivo | Cambio |
|---|---|
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql` | Columna `fuente_emision.descripcion VARCHAR(500)`; `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` para bases creadas con la v2.0; `UPDATE` que completa la descripción de las fuentes iniciales **solo si nunca fueron editadas** (`fecha_modificacion IS NULL`) y no tienen texto, para no pisar cambios del administrador |
| `backend/HDC.Entity/Entities/Catalogos.cs` | Propiedad `FuenteEmision.Descripcion` |
| `backend/HDC.Entity/Dtos/CatalogoDtos.cs` | `FuenteEmisionDto.Descripcion` y `FuenteEmisionRequest.Descripcion` (máx. 500) |
| `backend/HDC.DAL/HdcDbContext.cs` | Mapeo de la columna `DESCRIPCION` |
| `backend/HDC.BLL/Comun/Mapeos.cs`, `Servicios/CatalogoServicios.cs` | Se incluye la descripción en el DTO y se guarda al crear o editar |
| `frontend/src/pages/usuario/FuentesEmision.jsx` | **Nueva** pantalla |
| `frontend/src/App.jsx`, `components/layout/Layout.jsx` | Ruta `/fuentes` y opción de menú (permisos `GESTIONAR_EMISIONES` o `COLABORAR`) |
| `frontend/src/pages/usuario/RegistroForm.jsx` | Acepta `?fuente=ID&consumo=N` para precargar la primera fila; si la fuente no existe se ignora |
| `frontend/src/pages/admin/Fuentes.jsx` | Campo "Descripción para los usuarios" en el formulario |

Documentos actualizados: MANUAL_USUARIO (§ 3 menú, § 5 "Antes de cargar", § 12 anexo admin),
DOCUMENTACION_TECNICA (§ 3.2 frontend, § 4 modelo de datos).

### 2.1.2 Corrección: listas mezcladas en Alcances / Unidades de medida / Tipos de emisión

**Problema reportado:** en el panel del administrador, al pasar de "Alcances" a "Unidades de medida" o
"Tipos de emisión", la tabla mostraba ítems de distintas secciones mezclados.

**Causa:** las tres rutas usan el mismo componente `Catalogos` y solo cambia la propiedad `tipo`. Como
ocupa la misma posición en el árbol, React **no lo vuelve a crear al navegar, lo reutiliza**:
1. hasta que llegan los datos nuevos, la tabla muestra la lista anterior con las columnas de la sección nueva;
2. las filas se identifican con el campo id de la sección nueva (por ejemplo `idUnidadMedida`), que las
   filas viejas no tienen: React se queda sin claves válidas y mezcla filas al actualizar la tabla;
3. la tabla conserva la búsqueda, la página y el orden de la sección anterior.

**Solución:** cada ruta le pasa una `key` distinta al componente (`key="alcances"`, `"unidades"`,
`"tipos-emision"`). Con una key distinta React desmonta el componente anterior y crea uno nuevo, con su
estado limpio. Lo mismo se aplicó a `Reportes` (`/reportes` y `/admin/reportes` usan el mismo componente).

| Archivo | Cambio |
|---|---|
| `frontend/src/App.jsx` | `key` en las rutas de `Catalogos` y `Reportes`, con comentario explicativo |

### 2.1.3 Verificación realizada

Sobre la base de la v2.0 ya existente (con datos), después de `docker compose up -d --build api frontend`:

| Prueba | Resultado |
|---|---|
| `dotnet build` y `npm run build` | Compilan sin errores |
| Arranque: el script agrega la columna `descripcion` a la base existente | OK: 15 de 15 fuentes con descripción |
| Un usuario (Juan) lista las fuentes | Recibe las 15 con su descripción |
| El admin edita la descripción de una fuente | Se guarda |
| Descripción de 501 caracteres | 400 "La descripción admite hasta 500 caracteres" |
| Reiniciar la API (el script se ejecuta de nuevo) | La descripción editada por el admin **no** se pisa |
| Ruta `/fuentes` servida por nginx, pantalla incluida en la versión publicada | OK |
| `key` de las rutas `Catalogos` y `Reportes` en la versión publicada | Presentes |

Pendiente de verificar a mano en el navegador: la vista de la sección "Fuentes de emisión" (diseño,
calculadora, botón Cargar) y la navegación Alcances → Unidades → Tipos de emisión en el panel del admin.

---

## [2.0] — 31/10/2026 — PostgreSQL, recuperación de contraseña, datos de prueba y documentación

Pedido original: `textos/fix4.txt`. Se hicieron cuatro cambios grandes, en este orden.

### 2.0.1 Migración de la base de datos de SQL Server a PostgreSQL

**Por qué:** SQL Server en Docker necesita ~2 GB de RAM y en equipos de 4 GB tardaba hasta 10 minutos en
arrancar o quedaba *unhealthy*. PostgreSQL usa ~50–100 MB, arranca en segundos, es libre y gratuito sin
límites de edición, y es un motor muy usado en producción. Se empezó con una **base nueva** (los datos de
SQL Server no se migraron; se reemplazan por datos de prueba, ver 2.0.3).

**Qué se hizo:**

| Archivo | Cambio |
|---|---|
| `backend/HDC.DAL/HDC.DAL.csproj` | `Microsoft.EntityFrameworkCore.SqlServer` → `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4 |
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql` | Reescrito para PostgreSQL manteniendo las mismas tablas, datos iniciales e índices (detalle abajo). Se agregó la tabla `recuperacion_contrasena` (ver 2.0.2) e índices para `distrito.id_provincia` y `ciudad.id_distrito` |
| `backend/HDC.DAL/InicializadorBaseDatos.cs` | Reescrito con Npgsql: se conecta a la base `postgres` para crear la base si no existe (consulta `pg_database`, porque PostgreSQL no tiene `CREATE DATABASE IF NOT EXISTS`) y ejecuta el script en una sola transacción. Ya no divide el script en lotes `GO` |
| `backend/HDC.DAL/HdcDbContext.cs` | Tipos de fecha `smalldatetime` → `timestamp(0) without time zone` y `datetime` → `timestamp(3) without time zone`. Nuevo método `AMinusculas` que pasa a minúscula los nombres de tablas y columnas (ver "nombres" abajo). Mapeo de `RecuperacionContrasena` |
| `backend/HDC.BLL/DependencyInjection.cs` | `UseSqlServer` → `UseNpgsql` (con reintentos ante cortes de conexión) |
| `backend/HDC.API/Infraestructura/ManejoErroresMiddleware.cs` | Los errores de SQL Server 547 (FK) y 2627/2601 (UNIQUE) pasan a los códigos de PostgreSQL `23503` y `23505` |
| `backend/HDC.DAL/Repositorios/ExtensionesConsulta.cs` | Nuevo `PatronContiene`: arma `%texto%` escapando `%`, `_` y `\` |
| `backend/HDC.DAL/Repositorios/EmisionRepositorios.cs`, `SeguridadRepositorios.cs` | Buscadores de "Mis emisiones" y "Usuarios": `Contains` → `EF.Functions.ILike` |
| `backend/HDC.DAL/Repositorios/CatalogoRepositorios.cs`, `SeguridadRepositorios.cs` | Detección de nombres duplicados (alcances, unidades, roles, permisos) sin distinguir mayúsculas (`ToLower()`) |
| `docker-compose.yml` | Servicio `db`: imagen `postgres:17-alpine`, volumen `pgdata`, healthcheck con `pg_isready` (5 s en vez de hasta 20 min), puerto publicado solo en `127.0.0.1`. Cadena de conexión de la API en formato Npgsql |
| `.env.example`, `.env` | `DB_SA_PASSWORD` → `DB_USUARIO` + `DB_CONTRASENA`; `DB_NAME` = `huellacarbono`; `DB_PORT` = `5433` |

**Traducción del script SQL Server → PostgreSQL:**

| SQL Server | PostgreSQL | Nota |
|---|---|---|
| `IF OBJECT_ID(N'X', N'U') IS NULL CREATE TABLE X` | `CREATE TABLE IF NOT EXISTS x` | |
| `BIGINT IDENTITY(1,1)` | `BIGINT GENERATED BY DEFAULT AS IDENTITY` | "BY DEFAULT" permite insertar ids fijos en los datos iniciales |
| `SET IDENTITY_INSERT X ON/OFF` | `setval(pg_get_serial_sequence(...), MAX(id))` | Después de insertar ids fijos se adelanta la secuencia |
| `NVARCHAR(n)` / `NVARCHAR(MAX)` | `VARCHAR(n)` / `TEXT` | En PostgreSQL todo texto es Unicode |
| `BIT` | `BOOLEAN` | |
| `DECIMAL(p,s)` | `NUMERIC(p,s)` | |
| `SMALLDATETIME` / `DATETIME` | `TIMESTAMP(0)` / `TIMESTAMP(3)` | Sin zona horaria, hora de Argentina |
| `DEFAULT (GETDATE())` | `DEFAULT LOCALTIMESTAMP(n)` | |
| `CHECK (DAY(PERIODO) = 1)` | `CHECK (EXTRACT(DAY FROM periodo) = 1)` | |
| `IF NOT EXISTS (SELECT 1 FROM X) INSERT ... VALUES` | `INSERT ... SELECT ... FROM (VALUES ...) WHERE NOT EXISTS (...)` | |
| `IF NOT EXISTS (SELECT 1 FROM sys.indexes ...) CREATE INDEX` | `CREATE INDEX IF NOT EXISTS` | `INCLUDE (...)` funciona igual |
| Lotes separados por `GO` | Sentencias separadas por `;` | |
| `DROP TABLE VEHICULO` (limpieza de bases viejas) | Eliminado | La base es nueva |

**Nombres en minúscula:** PostgreSQL convierte a minúscula todo nombre sin comillas, pero EF Core siempre
usa comillas (`"USUARIO"` ≠ `usuario`). El script crea todo en minúscula y `HdcDbContext.AMinusculas`
convierte los nombres del mapeo, que siguen escritos en MAYÚSCULA para leerse como el diagrama original.

**Diferencias de comportamiento resueltas:**
- En SQL Server `LIKE` y `=` no distinguían mayúsculas (intercalación por defecto); en PostgreSQL sí. Se usó
  `ILIKE` en los buscadores y `lower()` en los controles de duplicados, para que todo siga funcionando igual.
- Npgsql exige que las columnas `timestamp without time zone` reciban fechas locales; el código ya usaba
  `DateTime.Now`, así que no hubo que cambiar los servicios.

### 2.0.2 Recuperación de contraseña por email

**Por qué:** el usuario que olvida su contraseña no tenía forma de recuperarla. Se pidió un link por email
con vencimiento que lleve a un formulario para crear la contraseña nueva. Como el sistema va a producción,
se diseñó siguiendo prácticas de seguridad estándar (detalle en
[Documentación técnica § 7](DOCUMENTACION_TECNICA.md#7-recuperación-de-contraseña)).

| Archivo | Cambio |
|---|---|
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql` | Tabla `recuperacion_contrasena` (hash del token, creación, vencimiento, uso) + índice por usuario |
| `backend/HDC.Entity/Entities/Usuarios.cs` | Entidad `RecuperacionContrasena` |
| `backend/HDC.Entity/Dtos/AuthDtos.cs` | `SolicitarRecuperacionRequest` y `RestablecerContrasenaRequest` (mismas reglas de contraseña que el registro) |
| `backend/HDC.DAL/Repositorios/SeguridadRepositorios.cs` | `RecuperacionRepositorio` |
| `backend/HDC.BLL/Servicios/RecuperacionServicio.cs` | **Nuevo.** Genera el token (256 bits), guarda su SHA-256, valida y cambia la contraseña; plantillas de los emails |
| `backend/HDC.BLL/Servicios/EmailServicio.cs` | **Nuevo.** `ColaEmails` (cola en memoria) + `EnvioEmailsServicio` (envío SMTP en segundo plano con MailKit, 3 reintentos) |
| `backend/HDC.BLL/HDC.BLL.csproj` | Paquetes `MailKit` y `Microsoft.Extensions.Hosting.Abstractions` |
| `backend/HDC.BLL/DependencyInjection.cs` | Registro de repositorio, servicios, cola y servicio en segundo plano |
| `backend/HDC.API/Controllers/AuthController.cs` | `POST /api/auth/recuperar` y `POST /api/auth/restablecer` |
| `backend/HDC.API/Infraestructura/LimitesPedidos.cs` | **Nuevo.** Límite de 5 pedidos cada 15 minutos por IP (HTTP 429) y lectura de la IP real detrás de nginx (`X-Forwarded-For`) |
| `backend/HDC.API/Program.cs` | `AgregarLimitesPedidos`, `UseForwardedHeaders`, `UseRateLimiter` |
| `backend/HDC.API/appsettings.json` | Secciones `Email` y `Recuperacion` |
| `docker-compose.yml`, `.env.example` | Servicio `mailpit` (correo de prueba en http://localhost:8025) y variables `SMTP_*`, `EMAIL_*`, `URL_PUBLICA`, `RECUPERACION_MINUTOS` |
| `frontend/src/api/servicios.js` | `authApi.solicitarRecuperacion` y `authApi.restablecerContrasena` |
| `frontend/src/pages/publicas/OlvideContrasena.jsx` | **Nueva** pantalla: pedir el link |
| `frontend/src/pages/publicas/RestablecerContrasena.jsx` | **Nueva** pantalla: crear la nueva contraseña desde el link |
| `frontend/src/App.jsx` | Rutas públicas `/olvide-contrasena` y `/restablecer-contrasena` |
| `frontend/src/pages/publicas/Login.jsx` | Link "¿Olvidaste tu contraseña?" |

Proveedor de email: el código usa SMTP estándar, así que el proveedor se elige solo con configuración.
Para producción se recomienda un servicio transaccional (Brevo, Amazon SES, Mailgun…) o el SMTP
institucional, con remitente de dominio propio con SPF/DKIM (ver Guía del desarrollador § 9).

### 2.0.3 Datos de prueba

**Por qué:** al empezar con una base nueva hacen falta usuarios y consumos para ver y probar todas las pantallas.

| Archivo | Cambio |
|---|---|
| `backend/HDC.BLL/Servicios/DatosPruebaServicio.cs` | **Nuevo.** Crea 2 individuos y 1 organización (contraseña `Prueba2026`), un domicilio cada uno, 12 meses de consumos con variación por estación, una colaboración aceptada (Juan en la casa de María) con sus registros y una invitación pendiente (EcoSur → María). Idempotente |
| `backend/HDC.API/Program.cs` | Se ejecuta después de descargar la geografía (los domicilios necesitan una ciudad) |
| `backend/HDC.API/appsettings.json`, `docker-compose.yml`, `.env.example` | Opción `DatosPrueba:Habilitado` / variable `DATOS_PRUEBA` (falso por defecto; en el `.env` de desarrollo, verdadero) |

El administrador sigue creándose como antes, con `ADMIN_EMAIL` / `ADMIN_CONTRASENA`.
Cuentas y detalle: [Guía del desarrollador § 3](GUIA_DESARROLLADOR.md#3-usuarios-y-datos-de-prueba).

### 2.0.4 Documentación reorganizada

**Por qué:** para la entrega formal se pidió una documentación ordenada, sin muchos archivos sueltos, y
fácil de actualizar con cambios futuros.

| Antes | Ahora |
|---|---|
| `README.md` con instalación, desarrollo, arquitectura, dominio y problemas (305 líneas) | `README.md`: portada breve con inicio rápido e índice |
| `MANUAL_USUARIO.md` en la raíz | `docs/MANUAL_USUARIO.md` (+ sección "¿Olvidaste tu contraseña?" y preguntas frecuentes) |
| Instalación y desarrollo dentro del README | `docs/GUIA_DESARROLLADOR.md` (Docker, desarrollo local, variables, datos de prueba, Mailpit, cambios en la base, convenciones, producción, problemas frecuentes) |
| Arquitectura y dominio dentro del README | `docs/DOCUMENTACION_TECNICA.md` (tecnologías, arquitectura, modelo de datos, API, seguridad, recuperación, rendimiento, decisiones) |
| Notas `textos/CAMBIOS_*.md` sueltas | `docs/CHANGELOG.md` (este archivo), con un resumen de cada cambio anterior |

La carpeta `textos/` (consignas y notas de trabajo) no se modificó; su organización queda a cargo del autor.

### 2.0.5 Verificación realizada

Con `docker compose up -d --build` sobre una base vacía (las pruebas se hicieron contra la API a través de
nginx, como lo usa el navegador):

| Prueba | Resultado |
|---|---|
| `dotnet build HDC.sln` y `npm run build` | Compilan sin errores |
| Arranque: script SQL, administrador, georef (24 provincias / 529 distritos / 4.037 ciudades), datos de prueba | OK, sin errores en el log |
| Login de las 4 cuentas (admin + 3 de prueba) | OK |
| Dashboard, listado de registros, colaboraciones recibidas | OK, con los datos de prueba |
| Buscador sin distinguir mayúsculas (`COLECTIVO`, `GÓMEZ`) | OK (12 registros / 1 usuario) |
| Admin: usuarios, resumen, reporte global, fuentes | OK |
| Alta de unidad duplicada con otras mayúsculas (`KWH` vs `kWh`) | 409 "Ya existe una unidad con ese nombre" |
| Recuperación: email existente e inexistente | Misma respuesta; solo el existente recibe email en Mailpit |
| Token guardado en la base | Se guarda el hash (distinto del token); validez 30 min |
| Restablecer con el link | OK; la contraseña nueva funciona y la vieja da 401; llega email de confirmación |
| Reusar el mismo link | 400 "El link no es válido o ya venció" |
| Límite de pedidos | El 6.º pedido en 15 minutos responde 429 |
| Ruta `/restablecer-contrasena` servida por nginx | 200 |

No se probó con un proveedor SMTP real (solo Mailpit): al configurarlo para producción, repetir la prueba de
recuperación.

---

## [1.3] — 10/2026 — Corrección del valor de 1 ppm

- El proyecto usaba **1 ppm = 17.600 millones de t CO₂**; el valor correcto es **≈ 7.800 millones**
  (1 ppm ≈ 2,13 Gt C × 3,664). Con el valor viejo los ppm mostrados quedaban 2,26 veces más chicos.
- Archivos: `frontend/src/utils/formato.js` (`TONELADAS_POR_PPM` y nueva `EQUIVALENCIA_PPM`, único lugar
  donde se define), `Dashboard.jsx` y `utils/pdf.js` usan la constante; documentación corregida.
- Nota original: `textos/CAMBIOS_valor_ppm.md`.

## [1.2] — 10/2026 — Se quita "vehículos" y se unifica el script de base de datos

- La sección de vehículos ya no existía en la aplicación: se eliminó la tabla `VEHICULO` del script (y se
  borraba de bases existentes al arrancar). Nota: `textos/CAMBIOS_quitar_vehiculos.md`.
- Los scripts `01_esquema.sql`, `02_datos_iniciales.sql` y `03_indices.sql` se unificaron en un único
  script idempotente `HDC_base_datos.sql`, que la API ejecuta en cada arranque.
  Nota: `textos/CAMBIOS_script_unico_bd.md`.
- Healthcheck del contenedor de SQL Server más tolerante (hasta ~20 min) para equipos con poca RAM.
  Nota: `textos/CAMBIOS_healthcheck_db.md`. *(Reemplazado en 2.0 por PostgreSQL.)*

## [1.1] — 21/10/2026 — Mejoras de rendimiento

1. Paginado en el servidor en "Mis emisiones" y "Usuarios" (`{ items, total }`).
2. Totales del Dashboard calculados con SQL (`GROUP BY`) en lugar de en memoria.
3. Índices sobre las claves foráneas usadas en filtros, con columnas `INCLUDE`.
4. Cache en memoria de catálogos y geografía (`CacheCatalogos`).
5. Carga diferida del generador de PDF (`utils/pdfDiferido.js`).
6. Espera en el buscador (debounce), compresión Brotli/gzip y proyección liviana del listado de usuarios.

Nota original con el detalle de cada punto: `textos/CAMBIOS_rendimiento.md`.

## [1.0] — 09/10/2026 — Primera versión funcionando

Sistema completo: registro de individuos y organizaciones, domicilios con geografía de georef, carga de
consumos mensuales, dashboard, reportes PDF, colaboraciones y panel de administración (fuentes, usuarios,
roles, permisos y catálogos). Backend .NET 9 en 4 capas con SQL Server; frontend React; Docker Compose.

Cambios respecto del esquema de base de datos original de la consigna:

| Cambio | Motivo |
|---|---|
| PK autonuméricas (salvo provincia/distrito/ciudad, que usan el id de georef) | IDs generados por la base |
| `CIUDAD.NOMBRE` BIGINT → texto | Era un error de tipo |
| `FECHA_BAJA` admite NULL | Activo = sin fecha de baja |
| `REGISTROS` pasa a cabecera (domicilio + período + quién cargó) con `DETALLE_REGISTRO` (fuente, consumo, factor, kg CO₂) | Un registro mensual tiene varios consumos; el factor se congela |
| `ROL_PERMISO` (N:M) | Un permiso puede pertenecer a varios tipos de usuario |
| `INDIVIDUO` + DNI y género · `ORGANIZACION` + descripción y cantidad de miembros · `DOMICILIO` + block, manzana y teléfono | Datos del glosario |
| `COLABORACION` + `COLABORACION_TIPO_EMISION` (reemplaza `COLABORA`) | Colaboradores por tipo de emisión y período |
| `REPORTE` | Guardar combinaciones de filtros |
| Unidades `kWh` y `km` | Necesarias para electricidad y transporte |

---

## Plantilla para nuevas entradas

```markdown
## [X.Y] — DD/MM/AAAA — Título corto

**Por qué:** problema o pedido que motivó el cambio.

| Archivo | Cambio |
|---|---|
| `ruta/al/archivo` | Qué se cambió |

Documentos actualizados: (MANUAL_USUARIO / GUIA_DESARROLLADOR / DOCUMENTACION_TECNICA, sección).
```

Versionado: se sube el número mayor (2.0 → 3.0) con cambios que obligan a reinstalar o migrar datos, y el
menor (2.0 → 2.1) con funcionalidades o correcciones compatibles.
