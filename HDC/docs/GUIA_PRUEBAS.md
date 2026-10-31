# Guía de pruebas — Huella de Carbono (HDC) v2.2

Resumen de lo que se hizo en las últimas versiones y **cómo probarlo paso a paso** en el navegador.
El detalle de cada cambio (archivos, motivos) está en el [CHANGELOG](CHANGELOG.md).

---

## Índice

1. [Qué se hizo](#1-qué-se-hizo)
2. [Antes de empezar](#2-antes-de-empezar)
3. [Pruebas de la v2.2 — Convocatorias](#3-pruebas-de-la-v22--convocatorias)
4. [Pruebas de la v2.1 — Fuentes de emisión y administrador](#4-pruebas-de-la-v21--fuentes-de-emisión-y-administrador)
5. [Pruebas de la v2.0 — Recuperar contraseña](#5-pruebas-de-la-v20--recuperar-contraseña)
6. [Volver los datos de prueba al estado inicial](#6-volver-los-datos-de-prueba-al-estado-inicial)
7. [Limitaciones conocidas](#7-limitaciones-conocidas)

---

## 1. Qué se hizo

### v2.2 — Convocatorias: colaboración masiva por código

Para organizaciones (o individuos) que necesitan que **muchas personas** carguen datos, por ejemplo el
transporte de todo el personal. Invitar a miles de personas una por una por email no escala.

- El titular crea una **convocatoria** para uno de sus domicilios: nombre, períodos, tipos de emisión que se
  pueden cargar y si la **aprobación es automática** o manual.
- El sistema genera un **código** (por ejemplo `ECSR-2K26`) que el titular comparte como quiera
  (email interno, cartel, intranet).
- El colaborador entra a **Colaboraciones → Sumarme con un código**, ve a quién pertenece la convocatoria y
  qué va a poder cargar, y confirma.
- El titular gestiona a los miembros en una pantalla pensada para miles de personas: paginada, con filtro por
  estado y buscador por nombre, DNI o email. Puede **aceptar, rechazar, dar de baja o reactivar** de a uno,
  a los seleccionados o a **todos los que cumplen el filtro**.
- El titular puede **dejar de recibir aportes de quien quiera, en cualquier momento**. Quien fue dado de baja
  o rechazado por el titular **no puede volver a entrar** con el código; solo el titular puede reactivarlo.
- Si se editan los períodos o tipos de la convocatoria, el cambio vale para todos sus miembros.
- Estados de una convocatoria: **Abierta** (entra gente), **Cerrada** (no entra nadie nuevo, los activos siguen
  cargando; se puede reabrir) y **Finalizada** (nadie carga más; no se reabre).
- Seguridad: máximo 20 intentos de código cada 15 minutos por usuario, para que no se puedan adivinar.
- La librería de email (MailKit) se actualizó a la 4.18.2 por una vulnerabilidad de la versión anterior.

### v2.1 — Fuentes de emisión y arreglo del administrador

- Nueva sección **Fuentes de emisión** en el menú del usuario: muestra todas las fuentes que se pueden cargar,
  con una descripción, agrupadas por alcance y tipo.
- Arreglo: en el administrador, al pasar de **Alcances** a **Unidades** o **Tipos de emisión**, la lista mezclaba
  elementos de distintas secciones.

### v2.0 — Pase a producción

- Base de datos migrada a **PostgreSQL**.
- **Recuperación de contraseña por email**, con un link que vence.
- Manual de usuario, documentación técnica y guía del desarrollador.
- Datos iniciales, cuenta de administrador y datos de prueba.

---

## 2. Antes de empezar

1. Levantá todo (si no está corriendo) desde la carpeta `HDC`:

   ```
   docker compose up -d
   ```

   Si cambiaste código, recompilá de a una imagen para no quedarte sin memoria:

   ```
   docker compose build api
   docker compose build frontend
   docker compose up -d --force-recreate api frontend
   ```

2. Direcciones:

   | Qué | Dirección |
   |---|---|
   | Sistema | http://localhost:8081 |
   | Emails de prueba (Mailpit) | http://localhost:8025 |
   | API | http://localhost:5000 |

3. Cuentas de prueba (todas con contraseña `Prueba2026`, salvo el administrador):

   | Cuenta | Para qué sirve en las pruebas |
   |---|---|
   | `contacto@ecosur.prueba.com` | **Titular** de la convocatoria `ECSR-2K26` |
   | `maria.gomez@prueba.com` | Persona **que se suma** con el código |
   | `juan.perez@prueba.com` | Segunda persona, para probar aprobación automática |
   | `empleado01@ecosur.prueba.com` … `empleado20@…` | Miembros **activos** (ya cargaron consumos) |
   | `empleado21@ecosur.prueba.com` … `empleado27@…` | Miembros **esperando aprobación** |
   | `empleado28@ecosur.prueba.com` … `empleado30@…` | Miembros **dados de baja** por EcoSur |
   | `admin@hdc.com` | Administrador (contraseña: `ADMIN_CONTRASENA` del `.env`) |

> Conviene usar **dos navegadores** (o una ventana normal y una de incógnito) para tener al titular y al
> colaborador abiertos a la vez.

---

## 3. Pruebas de la v2.2 — Convocatorias

Marcá cada prueba a medida que la hacés. La columna **Esperado** dice lo que tiene que pasar.

### 3.1 Ver la convocatoria (titular)

Entrá como **EcoSur** → **Colaboraciones** → pestaña **Convocatorias**.

| ✔ | Paso | Esperado |
|---|---|---|
| ☐ | Mirar la tarjeta "Transporte del personal 2026" | Muestra el código `ECSR-2K26`, estado **Abierta**, 20 activos, 7 por aprobar, 3 bajas y los kg CO₂ aportados |
| ☐ | Tocar el botón de copiar al lado del código | Aviso "Código copiado"; al pegarlo aparece `ECSR-2K26` |
| ☐ | Mirar la pestaña **Convocatorias** | Tiene un número (7) con las solicitudes por aprobar |

### 3.2 Gestionar a los miembros (titular)

En la tarjeta, tocá **Colaboradores**.

| ✔ | Paso | Esperado |
|---|---|---|
| ☐ | Mirar la lista | 30 personas, paginadas, con estado, fecha en que se sumaron y aporte en kg CO₂ |
| ☐ | Filtro **Por aprobar** | Quedan 7 (empleados 21 a 27) |
| ☐ | Filtro **Dados de baja** | Quedan 3, marcados "(por vos)" |
| ☐ | Buscar `empleado05` o un apellido | Solo aparecen los que coinciden |
| ☐ | Ordenar por **Aporte kg CO₂** | Se ordena de mayor a menor (o al revés) |
| ☐ | En **Por aprobar**, marcar 2 casillas → **Aceptar** → confirmar | Aviso "Se aceptaron 2 colaboradores"; quedan 5 por aprobar |
| ☐ | En **Por aprobar**, **Aplicar a los 5 filtrados → Rechazar** | Se rechazan los 5 aunque no los hayas marcado |
| ☐ | En **Activos**, dar de baja a uno con el botón de su fila | Pasa a "Dado de baja (por vos)" |
| ☐ | En **Dados de baja**, **Reactivar** a esa persona | Vuelve a **Activos** |
| ☐ | Marcar a un activo y elegir **Aceptar** | Aviso de que no se modificó a nadie (ya estaba aceptado) |

### 3.3 Sumarse con el código (colaborador)

Entrá como **María** → **Colaboraciones** → **Sumarme con un código**.

| ✔ | Paso | Esperado |
|---|---|---|
| ☐ | Escribir `XXXX-0000` | Mensaje de código inexistente |
| ☐ | Escribir `ecsr 2k26` (en minúsculas y con espacio) | Lo acepta igual: se ignoran mayúsculas, guiones y espacios |
| ☐ | Mirar la vista previa | Muestra EcoSur S.A., el domicilio, los períodos y el tipo "Transporte de colaboradores" |
| ☐ | Confirmar | Aparece en **Donde colaboro** como **Esperando aprobación**, con la etiqueta de la convocatoria |
| ☐ | Volver a ingresar el mismo código | Mensaje "Ya formás parte de esta convocatoria" |
| ☐ | Como **EcoSur**, aceptar a María en **Por aprobar** | — |
| ☐ | Como **María**, ir a **Mis emisiones → Nuevo registro** | Puede elegir el domicilio de EcoSur y **solo** el tipo "Transporte de colaboradores" |

### 3.4 Reglas para volver a entrar

| ✔ | Paso | Esperado |
|---|---|---|
| ☐ | Como **EcoSur**, dar de baja a María | — |
| ☐ | Como **María**, intentar sumarse otra vez con `ECSR-2K26` | Se lo impide: fue dada de baja por el titular |
| ☐ | Como **EcoSur**, reactivar a María | María vuelve a poder cargar |
| ☐ | Como **María**, en **Donde colaboro**, **Finalizar** su colaboración | Queda finalizada por ella |
| ☐ | Como **María**, sumarse de nuevo con el código | **Sí** puede: se había ido por su cuenta. Vuelve a "Esperando aprobación" |
| ☐ | Como **María**, tocar **Cancelar solicitud** | La solicitud se cancela |

### 3.5 Crear y editar una convocatoria

Como **EcoSur** → **Nueva convocatoria**.

| ✔ | Paso | Esperado |
|---|---|---|
| ☐ | Crear "Prueba automática" con **aprobación automática**, un período y un tipo de emisión | Aparece una tarjeta nueva con su propio código |
| ☐ | Como **Juan**, sumarse con ese código | Queda **Aceptada** al instante, sin esperar al titular |
| ☐ | Como **EcoSur**, **Editar** la convocatoria y agregar otro tipo de emisión | Juan ahora también puede cargar ese tipo |
| ☐ | **Cerrar** la convocatoria | Juan sigue pudiendo cargar; otra persona no puede sumarse con el código |
| ☐ | **Abrir** de nuevo | El código vuelve a funcionar |
| ☐ | **Regenerar código** | El código cambia; el anterior deja de funcionar |
| ☐ | **Finalizar** | Juan ya no puede cargar; la convocatoria no se puede reabrir |
| ☐ | **Eliminar** una convocatoria que tiene miembros | No se permite (para no perder el historial) |
| ☐ | Crear otra sin que nadie se sume y **Eliminarla** | Se elimina |

### 3.6 Lo que no tiene que haber cambiado

| ✔ | Paso | Esperado |
|---|---|---|
| ☐ | Como **EcoSur**, pestaña **Invitaciones por email** | Sigue mostrando la invitación a María, **sin** los 30 empleados de la convocatoria |
| ☐ | **Invitar por email** a un usuario | Funciona como antes |
| ☐ | **Dashboard** de EcoSur | Los aportes de la convocatoria se suman en "Aportes de colaboradores" |

---

## 4. Pruebas de la v2.1 — Fuentes de emisión y administrador

| ✔ | Paso | Esperado |
|---|---|---|
| ☐ | Como **María**, menú **Fuentes de emisión** | Lista de todas las fuentes, agrupadas, con su descripción |
| ☐ | Desde una fuente, ir a cargar un registro | El formulario se abre con esa fuente ya elegida |
| ☐ | Como **admin**, ir a **Alcances** → **Unidades de medida** → **Tipos de emisión** | Cada lista muestra solo lo suyo, sin mezclar |
| ☐ | Como **admin**, editar una fuente y cambiar su descripción | El usuario ve la descripción nueva en **Fuentes de emisión** |

---

## 5. Pruebas de la v2.0 — Recuperar contraseña

| ✔ | Paso | Esperado |
|---|---|---|
| ☐ | En el login, **¿Olvidaste tu contraseña?** → `maria.gomez@prueba.com` | Mensaje de que se envió un link (el mismo mensaje aparece con un email que no existe, por seguridad) |
| ☐ | Abrir http://localhost:8025 | Llegó el email "Recuperá tu contraseña - Huella de Carbono" |
| ☐ | Abrir el link y poner una contraseña nueva | Se puede entrar con la contraseña nueva |
| ☐ | Volver a abrir el mismo link | Ya no funciona (se usa una sola vez) |
| ☐ | Pedir el link más de 5 veces seguidas | Aviso de demasiados intentos; hay que esperar 15 minutos |

> Si cambiás la contraseña de María, después volvé a ponerle `Prueba2026` para que el resto de las pruebas
> siga coincidiendo con esta guía.

---

## 6. Volver los datos de prueba al estado inicial

Las pruebas modifican los datos (aceptar, dar de baja, etc.). Para empezar de cero, desde la carpeta `HDC`:

```
docker compose down -v
docker compose up -d
```

> ⚠️ `down -v` **borra toda la base de datos**. Usalo solo en desarrollo, nunca en producción.

Al arrancar, la API vuelve a crear las tablas, los datos iniciales, el administrador y los datos de prueba
(con `DATOS_PRUEBA=true` en el `.env`).

---

## 7. Limitaciones conocidas

- El buscador de miembros **no ignora los acentos**: "lucia" no encuentra "Lucía". Se puede resolver más
  adelante con la extensión `unaccent` de PostgreSQL.
- Con 4 GB de RAM, recompilar las dos imágenes de Docker juntas puede cortarse por falta de memoria;
  compilalas de a una (ver la sección 2).
