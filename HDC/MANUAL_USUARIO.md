# Manual de usuario — Huella de Carbono (HDC)

Este manual explica cómo usar la aplicación **Huella de Carbono** para medir cuánto CO₂ generan tus
consumos de todos los meses, ver cómo evoluciona tu huella y descargar reportes.

> Para instalar o poner en marcha la aplicación, ver el [README](README.md).
> Este manual es para quien **usa** la aplicación desde el navegador.

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
tu aporte al total del planeta (1 ppm ≈ 17.600 millones de toneladas).

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
| Gas natural de red              | Factura de gas                                  | m³     |
| Garrafa (GLP)                   | Kilos de la garrafa (10, 15 o 45 kg)            | kg     |
| Electricidad                    | Factura de luz                                  | kWh    |
| Nafta / gasoil del vehículo     | Tickets de carga de combustible                 | L      |
| GNC del vehículo                | Tickets de carga                                | m³     |
| Auto, moto, colectivo, tren, avión | Kilómetros recorridos en el mes (estimados)  | km     |
| Residuos                        | Estimación de kilos de basura                   | kg     |
| Agua de red                     | Factura de agua                                 | m³     |

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

**Paso 3 – Domicilio**
- El primer lugar donde vas a medir tus emisiones (tu casa, tu oficina...). Después podés agregar más.
- Elegí **provincia, distrito y ciudad** de las listas, y completá calle, altura y código postal.
  Piso, departamento, block, manzana y teléfono son opcionales.

3. Click en **Crear cuenta**. Entrás directamente a la aplicación.

Si algún dato está mal, el campo se marca en rojo con una explicación. Podés volver con **Atrás** sin perder lo cargado.

---

## 3. Iniciar sesión y moverte por la aplicación

- **Ingresar:** en la pantalla de inicio escribí tu email y contraseña, y hacé click en **Ingresar**.
  El ícono del ojo muestra u oculta la contraseña.
- La sesión dura un tiempo limitado (por defecto 2 horas). Cuando vence, tenés que volver a ingresar.

### El menú
A la izquierda (en el celular, con el botón ☰ arriba) tenés el menú **Mi huella**:

| Opción             | Para qué sirve                                                            |
|--------------------|---------------------------------------------------------------------------|
| **Dashboard**      | Resumen de tu huella con gráficos                                         |
| **Mis emisiones**  | Lista de tus registros mensuales; desde acá cargás uno nuevo              |
| **Domicilios**     | Los lugares donde medís                                                   |
| **Colaboraciones** | Invitar a otros a cargar datos, o aceptar invitaciones. Un número indica invitaciones pendientes |
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

1. Andá a **Mis emisiones → Nuevo registro** (o el botón **Nuevo registro** del Dashboard).
2. Elegí el **Domicilio** y el **Período** (mes y año). No se pueden cargar meses futuros.
3. En **Consumos**, completá una fila por cada fuente:
   1. *(Opcional)* Elegí **Alcance** y/o **Tipo de emisión** para achicar la lista de fuentes.
   2. En **Fuente de emisión**, escribí parte del nombre (por ejemplo "gas", "luz", "colectivo") y
      **elegí una de las sugerencias**.
   3. Escribí el **Consumo**. La unidad (m³, kWh, L, km, kg) aparece a la derecha.
      - Los puntos de miles se ponen solos: escribís `20000` y ves `20.000`.
      - Los decimales van con **coma**: `150,5`.
   4. Debajo ves el factor usado y los **kg de CO₂** de esa fila, calculados al instante.
4. **Agregar consumo** suma otra fila. La ✕ de cada fila la quita.
   No repitas la misma fuente en dos filas: si tenés dos facturas de luz del mismo mes, sumalas en una sola.
5. A la derecha ves el **Total estimado** del mes en toneladas y kg de CO₂.
   Podés escribir **Observaciones** (por ejemplo, "factura bimestral dividida en dos meses").
6. Click en **Guardar registro**. Se abre el detalle del registro recién guardado.

### Ejemplo de un mes en un hogar
| Fuente                     | Consumo  | Factor          | kg CO₂ |
|----------------------------|----------|-----------------|--------|
| Gas natural de red         | 120 m³   | 1,95 kg/m³      | 234,0  |
| Energía eléctrica de red   | 250 kWh  | 0,40 kg/kWh     | 100,0  |
| Nafta - vehículo propio    | 60 L     | 2,27 kg/L       | 136,2  |
| Colectivo urbano           | 200 km   | 0,09 kg/km      | 18,0   |
| **Total**                  |          |                 | **488,2 kg ≈ 0,49 t** |

> Los factores son valores de referencia que define el administrador y pueden cambiar con el tiempo.
> Cada registro **guarda el factor con el que se cargó**, así que los cambios posteriores no modifican tus meses anteriores.

---

## 6. Mis emisiones: ver, editar y descargar registros

En **Mis emisiones** está la lista de todos tus registros mensuales (de tus domicilios y de los que cargaste
como colaborador).

- **Filtros:** domicilio, mes **Desde** y **Hasta**, y buscador (calle, ciudad, quién cargó, alcance o fuente).
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

### Invitar a un colaborador (titular)
1. **Colaboraciones → Invitar colaborador**.
2. Completá:
   - **Domicilio** donde va a colaborar.
   - **Email del colaborador** (tiene que tener una cuenta en la aplicación).
   - **Desde** y **Hasta (opcional)**: los meses en que puede cargar.
   - **Información que puede cargar:** los tipos de emisión habilitados (por ejemplo, solo *Transporte de colaboradores*).
   - **Mensaje (opcional)**, por ejemplo: "Cargá los km que hacés para venir a la oficina cada mes".
3. **Enviar invitación**. Queda *pendiente* hasta que la otra persona responda.

En **Mis colaboradores** ves cada colaboración, su estado y cuánto CO₂ aportó. Podés **Editar** (períodos y tipos)
o **Finalizar** la colaboración. Al finalizar, los registros ya cargados se conservan.

### Responder una invitación (colaborador)
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
Revisá que el filtro de *Alcance* o *Tipo de emisión* de esa fila esté en "Todos". Si cargás como colaborador,
solo ves las fuentes de los tipos que te habilitaron. Si la fuente no existe, pedile al administrador que la agregue.

**Me dice "Fuente repetida".**
Usaste la misma fuente en dos filas. Sumá los consumos en una sola.

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

---

## 12. Anexo: para el administrador

El administrador ingresa con el usuario creado en la instalación y ve el menú **Administración**:

| Opción                  | Para qué sirve                                                                              |
|-------------------------|---------------------------------------------------------------------------------------------|
| **Panel general**       | Resumen del sistema y botón **Cargar geografía (georef)** para descargar provincias y ciudades |
| **Reportes y métricas** | Reportes de todo el sistema: por período, tipo de usuario, zona geográfica y categorías     |
| **Fuentes de emisión**  | Alta, edición y baja de fuentes y de su **factor de emisión**                               |
| **Usuarios**            | Ver usuarios registrados, filtrarlos y consultar su información                             |
| **Tipos de usuario**    | Crear roles nuevos además de Administrador, Individuo y Organización                       |
| **Permisos**            | Qué puede hacer cada tipo de usuario                                                        |
| **Tipos de emisión, Alcances, Unidades de medida** | Catálogos que usan las fuentes                                   |

Recomendaciones:
- Revisá los factores de emisión iniciales contra una fuente oficial: vienen como **valores de referencia**.
- Al cambiar un factor, solo se aplica a los consumos que se carguen desde ese momento; los registros existentes conservan el factor original (también al editarlos).
- Los cambios de permisos se aplican cuando el usuario vuelve a iniciar sesión.
