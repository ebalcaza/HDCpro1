# Manual de usuario — Huella de Carbono (HDC)

> **Última actualización:** 03/11/2026 · Versión del sistema: 2.12 (ver [CHANGELOG](CHANGELOG.md))

Este manual explica cómo usar la aplicación **Huella de Carbono** para medir cuánto CO₂ generan tus
consumos de todos los meses, ver cómo evoluciona tu huella y descargar reportes.

> Este manual es para quien **usa** la aplicación desde el navegador.
> Para instalarla o trabajar en el código, ver la [Guía del desarrollador](GUIA_DESARROLLADOR.md).

## Índice
1. [¿Qué es la huella de carbono?](#1-qué-es-la-huella-de-carbono)
2. [Crear tu cuenta](#2-crear-tu-cuenta)
3. [Iniciar sesión y moverte por la aplicación](#3-iniciar-sesión-y-moverte-por-la-aplicación)
4. [Domicilios](#4-domicilios)
5. [Cargar tus consumos del mes](#5-cargar-tus-consumos-del-mes)
6. [Mis emisiones: ver, editar y descargar registros](#6-mis-emisiones-ver-editar-y-descargar-registros)
7. [Dashboard: entender tu huella](#7-dashboard-entender-tu-huella)
8. [Reportes en PDF](#8-reportes-en-pdf)
9. [Colaboraciones](#9-colaboraciones)
10. [Mi perfil y opciones de usabilidad](#10-mi-perfil-y-opciones-de-usabilidad)
11. [Preguntas frecuentes](#11-preguntas-frecuentes)
12. [Anexo: para el administrador](#12-anexo-para-el-administrador)

---

## 1. ¿Qué es la huella de carbono?

Es la cantidad de **dióxido de carbono (CO₂)** que se libera a la atmósfera por las actividades de una
persona u organización: calefaccionarse con gas, usar electricidad, viajar en auto o colectivo, generar
residuos, consumir agua, etc.

La aplicación la calcula así:

```
kg de CO₂ = consumo × factor de emisión
```

- El **consumo** lo cargás vos, con el dato de tu factura o tu estimación (por ejemplo, 120 m³ de gas).
- El **factor de emisión** ya está cargado en el sistema para cada fuente (por ejemplo, 1 m³ de gas natural
  ≈ 1,95 kg de CO₂).

Ejemplo: 120 m³ de gas × 1,95 = **234 kg de CO₂**.

Los totales se muestran en **toneladas de CO₂** (1 t = 1.000 kg). En algunas pantallas también vas a ver el
equivalente en **ppm** (partes por millón de CO₂ en la atmósfera): es un número muy chico, que sirve para dimensionar
tu aporte al total del planeta (1 ppm ≈ 7.800 millones de toneladas).

### Los tres alcances
Las emisiones se agrupan en tres **alcances**, según un estándar internacional (GHG Protocol):

| Alcance       | Qué es                                          | Ejemplos                                             |
|---------------|-------------------------------------------------|------------------------------------------------------|
| **Alcance 1** | Lo que quemás o emitís vos directamente         | Gas natural, garrafa, nafta o GNC de tu vehículo     |
| **Alcance 2** | La energía que comprás                          | Electricidad de red                                  |
| **Alcance 3** | Otras emisiones indirectas                      | Colectivo, tren, avión, residuos, agua de red        |

### ¿Qué datos necesito tener a mano?
| Consumo                         | Dónde lo encontrás                              | Unidad |
|---------------------------------|-------------------------------------------------|--------|
| Gas natural de red              | Factura del proveedor                           | m³     |
| Gas envasado (GLP), si corresponde | Facturas o registros de compra               | kg o L |
| Gasoil de grupos electrógenos   | Registro de mantenimiento o compras             | L      |
| Nafta / gasoil de vehículos     | Tarjeta de combustible o comprobantes; los km son opcionales | L |
| Refrigerantes de aires y heladeras | Empresa de mantenimiento, cuando hay una recarga | kg   |
| Electricidad (de la red o renovable) | Factura del proveedor eléctrico            | kWh    |
| Traslado de personas            | Medio de transporte, distancia de ida y vuelta y días por semana | km |
| Viajes institucionales          | Rendiciones: distancia, medio y fecha | km |
| Residuos y reciclables          | Empresa recolectora, registros internos o manifiestos | kg o L |
| Agua potable                    | Factura del proveedor                           | m³     |
| Papel, tóner y cartuchos        | Compras                                         | resma o unidad |
| Insumos de oficina (opcional)   | Compras                                         | kg     |

> Si la factura es **bimestral**, dividí el consumo en los dos meses y aclaralo en *Observaciones*.

---

## 2. Crear tu cuenta

1. Abrí la aplicación en el navegador y hacé click en **Registrate**.
2. El registro tiene **3 pasos**. Los campos con asterisco (\*) son obligatorios.

**Paso 1 – Cuenta**
- Elegí el tipo de cuenta:
  - **Individuo:** para medir la huella de tu hogar.
  - **Organización:** para empresas e instituciones.
- Email, teléfono (opcional) y contraseña.
  La contraseña debe tener **al menos 8 caracteres, con letras y números, sin espacios**.

**Paso 2 – Perfil**
- *Individuo:* nombres, apellidos, DNI y, si querés, fecha de nacimiento y género.
- *Organización:* razón social, CUIT, área, y opcionalmente cantidad de miembros y descripción.

**Paso 3 – Domicilio (opcional)**
- Si solo vas a colaborar con otra persona u organización, no lo necesitás: tocá **Crear cuenta** directamente.
  Podés agregarlo después desde **Domicilios**.
- Para medir tus propias emisiones, activá **Agregar un domicilio ahora**. Es el primer lugar donde vas a medir
  (tu casa, tu oficina...).
- Elegí **provincia, distrito y ciudad** de las listas, y completá calle, altura y código postal.
  Piso, departamento, block, manzana y teléfono son opcionales.

3. Click en **Crear cuenta**. Entrás directamente a la aplicación.

Si algún dato está mal, el campo se marca en rojo con una explicación. Podés volver con **Atrás** sin perder lo cargado.

---

## 3. Iniciar sesión y moverte por la aplicación

- **Ingresar:** en la pantalla de inicio escribí tu email y contraseña, y hacé click en **Ingresar**.
  El ícono del ojo muestra u oculta la contraseña.
- La sesión dura un tiempo limitado (por defecto 2 horas). Cuando vence, tenés que volver a ingresar.

### ¿Olvidaste tu contraseña?
1. En la pantalla de inicio de sesión hacé click en **¿Olvidaste tu contraseña?**.
2. Escribí el **email de tu cuenta** y hacé click en **Enviar link**.
3. Revisá tu correo (también la carpeta de **spam**). Vas a recibir un email de *Huella de Carbono*
   con el botón **Crear nueva contraseña**.
4. El botón abre la aplicación: escribí la contraseña nueva dos veces y hacé click en **Guardar contraseña**.
5. Ingresá con tu email y la contraseña nueva. También te llega un email avisando que la contraseña cambió.

Para tener en cuenta:
- El link **vence a los 30 minutos** y sirve **una sola vez**. Si venció, pedí otro desde el paso 1.
- Si pedís varios links, **solo funciona el último**.
- Por seguridad, la pantalla siempre muestra el mismo mensaje, aunque el email no esté registrado.
- Si recibís el email sin haberlo pedido, ignoralo: tu contraseña no cambia.
- Por protección hay un límite de pedidos: después de 5 intentos seguidos hay que esperar 15 minutos.

### El menú
A la izquierda (en el celular, con el botón ☰ arriba) tenés el menú **Mi huella**:

| Opción             | Para qué sirve                                                            |
|--------------------|---------------------------------------------------------------------------|
| **Dashboard**      | Resumen de tu huella con gráficos                                         |
| **Mis emisiones**  | Lista de tus registros mensuales; desde acá cargás uno nuevo              |
| **Fuentes de emisión** | Todo lo que podés cargar: unidad, factor, dónde encontrar el dato y una calculadora rápida |
| **Domicilios**     | Los lugares donde medís                                                   |
| **Colaboraciones** | Invitar a otros a cargar datos (por email o con una **convocatoria** con código), sumarte con un código o aceptar invitaciones. Un número indica invitaciones pendientes |
| **Reportes**       | Reportes filtrados y descargables en PDF                                  |
| **Mi perfil**      | Tus datos y cambio de contraseña                                          |

Arriba a la derecha:
- 🌙 / ☀️ : modo oscuro / claro.
- ♿ : opciones de usabilidad (tamaño de letra, animaciones, contraste). Ver [sección 10](#10-mi-perfil-y-opciones-de-usabilidad).
- Tu nombre: acceso a **Mi perfil** y **Cerrar sesión**.

---

## 4. Domicilios

Cada registro de consumos pertenece a un **domicilio**. Si tenés casa y oficina, o varias sucursales,
cargá un domicilio por cada una para ver la huella de cada lugar por separado.

En **Domicilios** ves la lista con su ubicación, cantidad de registros, toneladas de CO₂ y estado.

- **Nuevo domicilio:** botón arriba a la derecha. Completá los mismos datos que en el registro y **Guardar**.
- **Ver** (👁 o click en la fila): muestra todos los datos, el total emitido, la cantidad de registros y de
  colaboradores. Desde ahí, **Ver registros** te lleva a sus emisiones.
- **Editar** (✏️): modifica los datos.
- **Eliminar** (🗑): te deja elegir entre:
  - **Dar de baja y conservar el historial:** el domicilio deja de usarse, pero sus emisiones siguen contando
    en tus estadísticas y reportes. *(Recomendado.)*
  - **Eliminar todo definitivamente:** borra el domicilio, sus registros y colaboraciones. **No se puede deshacer.**
- **Mostrar dados de baja:** interruptor para ver también los domicilios dados de baja.

El buscador filtra por calle o ciudad, y podés ordenar haciendo click en el título de cada columna.

---

## 5. Cargar tus consumos del mes

Es la tarea principal: **una vez por mes**, cargá los consumos de cada domicilio.

### Antes de cargar: la sección "Fuentes de emisión"
En el menú **Mi huella → Fuentes de emisión** está la lista de **todo lo que podés cargar**, para saber
qué elegir y dónde buscar cada dato:

- Arriba se explica **cómo se calcula** la huella (`consumo × factor`) y qué incluye cada **alcance**.
- Las fuentes están agrupadas por **alcance** y por **tipo de emisión**. Cada tarjeta muestra:
  - la **unidad** en la que se carga (m³, kWh, L, km, kg, resma, unidad);
  - el **factor**: cuántos kg de CO₂ genera cada unidad (por ejemplo, 1 kWh = 0,286 kg CO₂);
  - una **descripción**: qué es esa fuente, dónde encontrar el dato y de dónde sale el factor.
- **Buscador:** escribí una palabra ("luz", "garrafa", "colectivo", "kWh"). No hace falta poner tildes.
  También podés filtrar por alcance.
- **Calculadora rápida:** escribí un consumo en una tarjeta y ves al instante cuántos kg de CO₂ representa.
  No guarda nada: sirve para probar.
- **Cargar:** abre el formulario de carga con esa fuente ya elegida (y el consumo, si lo escribiste en la calculadora).
  Solo falta elegir el domicilio y el mes.

> Si la fuente que necesitás no está en la lista, pedile al administrador que la agregue.

### Cómo cargar un registro

1. Andá a **Mis emisiones → Nuevo registro** (o el botón **Nuevo registro** del Dashboard).
2. Elegí el **Domicilio** y el **Período** (mes y año). Se puede cargar hasta el mes en curso, sin meses futuros.
3. En **Consumos**, completá una fila por cada fuente:
   1. En **Fuente de emisión**, hacé click en una de las fuentes de la lista. Solo aparecen las disponibles para
      ese domicilio, por eso primero hay que elegir el domicilio. Si son muchas, la lista se desplaza por dentro.
   2. Escribí el **Consumo**. La unidad aparece a la derecha.
      - Los puntos de miles se ponen solos: escribís `20000` y ves `20.000`.
      - Los decimales van con **coma**: `150,5`.
      - Ningún campo acepta 0: un consumo tiene que ser mayor a 0.
      - En **vehículos** cargás los litros y, si querés, los **kilómetros recorridos**. Los km no cambian el cálculo.
        Si no los sabés, dejalos vacíos.
      - En **viajes institucionales** cargás la distancia en km y la **fecha**. Las emisiones se calculan con el
        combustible o la electricidad de cada medio. El avión se guarda como dato y no suma emisiones.
        Para varios viajes con el mismo medio en un mes, usá otro registro. Si colaborás, tenés un solo registro de consumos por mes:
        sumá las distancias en esa fila y editá el registro si ya lo guardaste.
   3. Debajo ves el factor usado y los **kg de CO₂** de esa fila, calculados al instante.
4. **Agregar consumo** suma otra fila. La ✕ de cada fila la quita.
   Una fuente ya elegida aparece deshabilitada en las otras filas: si tenés dos facturas de luz del mismo mes,
   sumalas en una sola.
5. A la derecha ves el **Total estimado** del mes en toneladas y kg de CO₂.
   Podés escribir **Observaciones** (por ejemplo, "factura bimestral dividida en dos meses").
6. Click en **Guardar registro**. Se abre el detalle del registro recién guardado.

> **Si cargás como colaborador:** ves solo las fuentes que te habilitaron y no se muestran el factor, los kg de CO₂
> ni el total estimado. Además, para **Agregar consumo** primero tenés que completar la fuente y el consumo de las
> filas que ya están.

### Cómo cargar un traslado
El traslado indica cómo viaja una persona hasta el domicilio. Tiene su propio formulario: se llega con el botón
**Cargar** de **Colaboraciones → Donde colaboro** si solo te habilitaron traslados, o con **Cargar traslado** en
**Nuevo registro**.

1. Con **Cargar**, desde **Colaboraciones → Donde colaboro**, el domicilio, la convocatoria y **tu rol** ya vienen definidos:
   el rol lo indicás una vez, al sumarte con el código. Si cambió, tocá **Cambiar** y queda guardado para las próximas cargas.
   Si entrás desde otro lugar, elegí el **Domicilio** y, si participás en varias, la **convocatoria**.
   También podés cargar una convocatoria cuyo período ya terminó: suelen abrirse al final, para declarar el año transcurrido.
2. Revisá el **Período**:
   - **En una convocatoria** lo define el organizador, y el formulario te muestra sus meses y semanas. Vos elegís si
     cargás **todo el período de una vez** o **un mes por vez**, si tus viajes cambiaron. Si cargás por mes, el
     formulario te indica cuáles te faltan.
   - **Si sos titular o te invitaron por email**, elegí mensual, semestral o anual, su primer mes y las semanas con
     asistencia. El sistema propone 4, 26 o 52; restá las de receso o vacaciones.
3. En **Medios de transporte**, completá una fila por cada uno: **medio**, **distancia por día, ida y vuelta**, y
   **días por semana**. Con **Agregar otro medio** sumás más. Por ejemplo:
   - Si venís 3 días en colectivo y 2 en bicicleta, cargá dos filas.
   - Si en un mismo viaje combinás colectivo y tren, cargá cada uno con los mismos días.
   - Si tomás 3 colectivos para llegar, cargá una sola fila con el total de km de los tres: cada medio va una vez.
   - Los días por semana van de 1 a 7.
4. **Guardar traslado**.

El cálculo es: `distancia × días por semana × semanas × factor del medio`. El total se reparte en partes iguales
entre los meses del período, para que los gráficos muestren una evolución pareja. Los meses que todavía no empezaron no
se cargan: si el período sigue en curso, podés editar la carga cuando pasen.

Por ejemplo, con 32 semanas en un período de 12 meses, 20 km por día y 3 días por semana en colectivo son
`20 × 3 × 32 = 1.920 km`: 160 km por mes.

Los meses de una carga se **editan y eliminan juntos**. Como colaborador, declarás un solo traslado por mes en cada convocatoria: si ya cargaste
todo, el formulario se bloquea y te ofrece **Editar**; si cargaste algunos meses, solo podés elegir los que faltan. Para cambiar un mes o sumar un medio, editá esa carga.
El titular no puede repetir el mismo medio en un mes, pero sí declarar otros grupos con medios distintos.
El titular del domicilio también puede cargar un grupo con los mismos hábitos, indicando la **cantidad de personas**.

### Ejemplo de un mes en un hogar
| Fuente                     | Consumo  | Factor           | kg CO₂ |
|----------------------------|----------|------------------|--------|
| Gas natural de red         | 120 m³   | 1,9568 kg/m³     | 234,8  |
| Electricidad de la red     | 250 kWh  | 0,2860 kg/kWh    | 71,5   |
| Nafta para vehículos       | 60 L     | 2,0209 kg/L      | 121,3  |
| **Total**                  |          |                  | **427,6 kg ≈ 0,43 t** |

> Los factores son valores de referencia que define el administrador y pueden cambiar con el tiempo.
> Cada registro **guarda el factor con el que se cargó**, así que los cambios posteriores no modifican tus meses anteriores.

---

## 6. Mis emisiones: ver, editar y descargar registros

En **Mis emisiones** está la lista de todos tus registros mensuales (de tus domicilios y de los que cargaste
como colaborador).

- **Filtros:** domicilio, **Origen** (*Emisiones propias*, cargadas por el titular del domicilio, o *Colaboraciones*, cargadas por otra persona),
  mes **Desde** y **Hasta**, y buscador (calle, ciudad, quién cargó, alcance o fuente).
  Arriba se muestra el **Total filtrado** en t CO₂.
- Ordená haciendo click en el título de una columna y elegí cuántas filas ver por página.
- Acciones de cada registro:
  - 👁 **Ver detalle:** consumos, factor, kg de cada fuente, distribución por alcance, quién lo cargó y cuándo.
  - ✏️ **Editar:** podés cambiar el período, los consumos y las observaciones (el domicilio no se puede cambiar).
    Las fuentes que ya estaban conservan su factor original.
  - 📄 **Descargar PDF** del registro.
  - 🗑 **Eliminar:** pide confirmación. Los totales se recalculan solos. **No se puede deshacer.**

Solo pueden editar o eliminar un registro el titular del domicilio o quien lo cargó.

---

## 7. Dashboard: entender tu huella

El **Dashboard** resume tu huella. Arriba podés filtrar por **domicilio** y por rango de meses (**Desde / Hasta**).

| Bloque                           | Qué muestra                                                                      |
|----------------------------------|----------------------------------------------------------------------------------|
| **Emisiones totales**            | Toneladas de CO₂ acumuladas (y su equivalente en ppm)                            |
| **Mes corriente**                | Lo emitido este mes comparado con el mes anterior                                |
| **Alcance principal**            | El alcance que más aporta a tu huella                                            |
| **Emisiones por alcance**        | Gráfico de dona con el peso de cada alcance                                      |
| **Por tipo de emisión**          | Tabla con lo emitido por cada tipo (combustión, electricidad, transporte...)     |
| **Evolución de las emisiones**   | kg de CO₂ por mes, total y por alcance                                           |
| **Fuentes que más emiten**       | Ranking de las fuentes con más CO₂                                               |
| **Lectura rápida**               | Promedio mensual y mes con más emisiones                                         |
| **Aportes de colaboradores**     | Lo que cargaron otras personas en tus domicilios                                 |

### Cómo usarlo para reducir tu huella
1. Mirá **Fuentes que más emiten**: ahí está lo que más conviene atacar.
2. Usá **Evolución** para ver si hay estacionalidad (por ejemplo, el gas en invierno) y comparar el mismo mes entre años.
3. Si cambiás un hábito (bajar la calefacción, usar más transporte público), seguí los meses siguientes
   para ver el efecto.

Si todavía no cargaste nada, el Dashboard te ofrece **Cargar mi primer registro**.

---

## 8. Reportes en PDF

En **Reportes** armás un informe de tus emisiones y lo descargás en PDF.

1. Elegí los **Filtros** (todos son opcionales y se pueden combinar):
   - Período **Desde / Hasta**.
   - **Alcances**, **Tipos de emisión**, **Fuentes** y **Domicilios**
     (en las listas, **Ctrl + click** para elegir varios).
2. Click en **Generar reporte**. Vas a ver:
   total, promedio mensual, cantidad de registros, y gráficos **por alcance**, **por fuente**, **por domicilio**
   y **evolución por período**, más el **Detalle de registros**.
3. **Descargar PDF** genera el informe completo para guardarlo, imprimirlo o enviarlo.
4. **Limpiar** borra los filtros.

### Reportes guardados
Si usás seguido la misma combinación de filtros (por ejemplo, "Alcance 2 del último año"):
1. Elegí los filtros y hacé click en **Guardar filtros**.
2. Poné un nombre (y si querés, una descripción) y **Guardar**.
3. Aparece en **Reportes guardados**: un click y se genera de nuevo con los datos actualizados.
   Eliminar un reporte guardado borra solo los filtros, no tus emisiones.

---

## 9. Colaboraciones

Sirve para que **otras personas carguen datos en tu domicilio**. Por ejemplo, una organización invita a sus
empleados a cargar los kilómetros que hacen para llegar a la oficina. Esas emisiones **suman a la huella del
domicilio del titular**.

Hay dos formas de sumar colaboradores:

| Forma | Conviene para | Cómo funciona |
|---|---|---|
| **Invitación por email** | Pocas personas (un contador, un familiar) | Invitás a cada una con su email y ella acepta |
| **Convocatoria con código** | Muchas personas (todo el personal, cientos o miles) | Compartís un código; cada uno se suma solo y vos aprobás o das de baja |

### Convocatorias: sumar a muchas personas con un código (titular)
1. **Colaboraciones → Nueva convocatoria**.
2. Completá:
   - **Domicilio** donde van a cargar y un **Nombre** (por ejemplo, "Transporte del personal 2026").
   - **Instrucciones** (opcional): qué tienen que cargar.
   - **Información que pueden cargar:** los tipos de emisión habilitados.
   - **Período que se va a cargar:** lo definís vos y los colaboradores no lo pueden cambiar. Lo recomendado es **un año
     completo**; **otro período** queda como última opción. Abrí la convocatoria al final del período, por ejemplo en
     diciembre, para que puedan cargar todo el año, porque los meses que no empezaron no se cargan.
   - **Semanas de actividad del período:** aparece si habilitás *Traslado de colaboradores*. Restá recesos y vacaciones;
     por ejemplo, 32 en una facultad. Los traslados necesitan período con principio y fin.
   - **Roles de los colaboradores (opcional):** aparece si habilitás *Traslado de colaboradores*. Por ejemplo, en una
     facultad: estudiantes, personal docente y personal no docente. Cada colaborador elige el suyo al cargar su
     traslado, y en **Detalles** ves cuántas personas y cuántos kg de CO₂ corresponden a cada rol. Un rol ya elegido
     en un traslado no se puede quitar.
   - **Aprobación automática:** si está activada, quien escribe el código queda aceptado al instante. Si no,
     queda *esperando aprobación* hasta que lo aceptes.
3. **Crear y obtener código**. El sistema genera un código como **`ECSR-2K26`**. Usá **Copiar** y compartilo
   por email, WhatsApp, la intranet, un cartel…

En la pestaña **Convocatorias** ves cada una con su código y cuántas personas hay activas, por aprobar y dadas
de baja. Desde ahí podés:
- **Editar**: los cambios de períodos y tipos se aplican **a todos** sus colaboradores a la vez.
- **Cerrar**: nadie más puede sumarse, pero los activos siguen cargando. Se puede volver a **Abrir**.
- **Finalizar** (menú ⋮): nadie carga más y el código deja de funcionar. No se puede deshacer; lo cargado se conserva.
- **Eliminar**: borra la convocatoria **por completo**, en cualquier estado, con sus colaboradores y todo lo que cargaron en ella. Esos kg dejan de
  sumar a la huella del domicilio y los registros desaparecen de **Mis emisiones** de cada persona. No se puede deshacer; para conservar lo cargado, usá **Finalizar**.

**Ver lo que cargó cada colaborador:** en **Detalles**, elegí **Detalle de aportes** para ver una fila por persona, fuente y mes,
con su rol, los datos declarados y los kg de CO₂. Cada convocatoria muestra solo lo cargado en ella, aunque la persona participe en otras del mismo domicilio. Podés buscar y ordenar. El botón **Ver aportes** de un colaborador muestra solo lo suyo.

**Gestionar los colaboradores de una convocatoria:** botón **Detalles**.
- Filtrá por estado (*Por aprobar*, *Activos*, *Dados de baja*, *Rechazados*) y buscá por nombre, DNI o email.
- Cada fila tiene sus botones: **Aceptar** o **Rechazar** (solicitudes), **Dar de baja** (activos) y **Reactivar**.
- **De a varios:** marcá las casillas (la del título marca toda la página) y usá la barra que aparece arriba.
- **Todos de una vez:** **Aplicar a los N filtrados** actúa sobre todos los que cumplen el filtro, aunque estén
  en otras páginas. Por ejemplo: filtro *Por aprobar* → *Aplicar a los 250 filtrados* → *Aceptar*.

> **Dejar de recibir aportes de alguien:** usá **Dar de baja** (o **Rechazar** si todavía no lo aprobaste).
> Esa persona no puede cargar más consumos **ni volver a sumarse con el código**. Lo que ya cargó se conserva.
> Si cambiás de opinión, usá **Reactivar**.

### Invitar a un colaborador por email (titular)
1. **Colaboraciones → Invitar por email**.
2. Completá:
   - **Domicilio** donde va a colaborar.
   - **Email del colaborador** (tiene que tener una cuenta en la aplicación).
   - **Desde** y **Hasta (opcional)**: los meses en que puede cargar.
   - **Información que puede cargar:** los tipos de emisión habilitados (por ejemplo, solo *Traslado de colaboradores*).
   - **Mensaje (opcional)**, por ejemplo: "Cargá cómo venís a la oficina".
3. **Enviar invitación**. Queda *pendiente* hasta que la otra persona responda.

En **Invitaciones por email** ves cada colaboración, su estado y cuánto CO₂ aportó. Podés **Editar** (períodos y tipos)
o **Finalizar** la colaboración. Al finalizar, los registros ya cargados se conservan.

### Sumarte a una convocatoria con un código (colaborador)
1. **Colaboraciones → Sumarme con un código**.
2. Escribí el código que te pasaron (da igual con o sin guion, en mayúsculas o minúsculas) y **Buscar**.
3. Revisá a quién pertenece, los períodos y qué vas a poder cargar. Si es correcto, **Sumarme**.
4. Según cómo esté configurada, quedás **aceptado** al instante o **esperando aprobación** del titular.
   Lo ves en la pestaña **Donde colaboro**; mientras esperás, podés **Cancelar solicitud**.
5. Una vez aceptado, cargás los datos habilitados. Para el transporte, seguí [Cómo cargar un traslado](#cómo-cargar-un-traslado).

Si el código "no existe", revisá que esté bien escrito o consultá al titular (puede haber finalizado la
convocatoria). Por seguridad, después de 20 intentos en 15 minutos hay que esperar.

### Ver el detalle de donde colaborás (colaborador)
En **Donde colaboro**, tocá una fila o el ícono 👁. Se abre una ventana con todos los datos de la convocatoria o invitación, los **meses que te faltan**,
los que todavía no empezaron y los **registros que ya guardaste**, con botones para verlos o editarlos. Un mes cuenta como cargado si guardaste al menos un registro en él.

### Responder una invitación por email (colaborador)
1. Cuando te invitan, aparece un número junto a **Colaboraciones** en el menú.
2. Entrá, revisá quién te invitó y qué podés cargar, y elegí **Aceptar** o **Rechazar**.
3. Si aceptás, en **Nuevo registro** vas a ver ese domicilio en el grupo **Como colaborador**.
   Un aviso te indica qué tipos de emisión podés usar, y la lista de fuentes se limita a esos tipos.
4. Podés **Dejar de colaborar** en cualquier momento.

---

## 10. Mi perfil y opciones de usabilidad

### Mi perfil
- Muestra tus datos, la fecha de alta, tus domicilios activos y tu total emitido.
- **Editar perfil:** cambiá tus datos personales o de la organización.
- **Cambiar contraseña:** pedí la contraseña actual y la nueva (dos veces).

### Usabilidad (ícono ♿ arriba a la derecha)
- **Tamaño de la letra:** A− / A+ o elegilo de la lista. Se aplica a todas las pantallas.
- **Modo oscuro.**
- **Animaciones:** desactivalas si te molestan o si la PC es lenta.
- **Bordes de alto contraste:** facilitan distinguir los campos y botones.
- **Restablecer:** vuelve a la configuración inicial.

Estas preferencias se recuerdan en el navegador.

---

## 11. Preguntas frecuentes

**No encuentro mi ciudad al cargar el domicilio.**
Elegí primero la provincia y después el distrito: la lista de ciudades depende de ellos. Si las listas están
vacías, avisale al administrador (tiene que cargar la geografía).

**La fuente que quiero no aparece en la lista.**
Primero elegí el domicilio: la lista depende de él. Si cargás como colaborador, solo ves las fuentes de los tipos
que te habilitaron. Si la fuente no existe, pedile al administrador que la agregue.

**Me dice "Fuente repetida".**
La misma fuente quedó en dos filas. Sumá los consumos en una sola.

**Me dice "No puede ser futuro".**
Solo se pueden cargar meses que ya empezaron (hasta el mes actual).

**¿Puedo corregir un mes que ya cargué?**
Sí: **Mis emisiones → ✏️ Editar**.

**Cambiaron un factor de emisión. ¿Se modifican mis meses anteriores?**
No. Cada registro conserva el factor con el que se cargó.

**¿Qué pasa con mis datos si doy de baja un domicilio?**
Si elegís *Dar de baja y conservar el historial*, sus emisiones siguen en tus estadísticas y reportes.

**La aplicación me sacó de la sesión.**
La sesión vence a las 2 horas (o lo que haya configurado el administrador). Volvé a ingresar.
Si el aviso dice que tu cuenta fue deshabilitada, el administrador le quitó el acceso: contactalo.

**Pedí recuperar la contraseña y no me llega el email.**
Revisá la carpeta de spam y que el email sea el de tu cuenta. Esperá unos minutos y pedilo de nuevo. Si tu
cuenta fue deshabilitada por el administrador, no se envía el link: contactalo.

**El link para crear la contraseña dice "no es válido o ya venció".**
Pasaron más de 30 minutos, ya lo usaste o pediste otro después. Pedí un link nuevo.

---

## 12. Anexo: para el administrador

El administrador ingresa con el usuario creado en la instalación y ve el menú **Administración**:

| Opción                  | Para qué sirve                                                                              |
|-------------------------|---------------------------------------------------------------------------------------------|
| **Panel general**       | Resumen del sistema y botón **Cargar geografía (georef)** para descargar provincias y ciudades |
| **Reportes y métricas** | Reportes de todo el sistema: por período, tipo de usuario, zona geográfica y categorías     |
| **Fuentes de emisión**  | Alta, edición y baja de fuentes, de su **factor de emisión** y de la **descripción** que ven los usuarios en *Mi huella → Fuentes de emisión* |
| **Usuarios**            | Ver usuarios registrados, filtrarlos y consultar su información                             |
| **Tipos de usuario**    | Crear roles nuevos además de Administrador, Individuo y Organización                       |
| **Permisos**            | Qué puede hacer cada tipo de usuario                                                        |
| **Tipos de emisión, Alcances, Unidades de medida** | Catálogos que usan las fuentes                                   |

Recomendaciones:
- Revisá los factores de emisión iniciales contra una fuente oficial: vienen como **valores de referencia**.
- Al cambiar un factor, solo se aplica a los consumos que se carguen desde ese momento; los registros existentes conservan el factor original (también al editarlos).
- Los cambios de permisos se aplican cuando el usuario vuelve a iniciar sesión.
