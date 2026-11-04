# Emails reales en el servidor (quitar Mailpit) — Huella de Carbono (HDC)

> **Última actualización:** 03/11/2026 · Versión del sistema: 2.8 (ver [CHANGELOG](CHANGELOG.md))

Pasos para quitar Mailpit de un servidor donde ya está alojado el proyecto y lograr que los emails de recuperación
de contraseña lleguen a casillas reales (Gmail, Outlook, institucionales, etc.). El código no cambia: la API envía
por SMTP estándar, así que solo se modifican `.env` y `docker-compose.yml`.

Los comandos son para un servidor Linux y se ejecutan desde la carpeta `HDC`.

## Antes de empezar

Necesitás:

- Acceso al servidor por terminal.
- Una dirección pública con HTTPS donde responda el sistema (en esta guía, `https://huella.midominio.com.ar`).
- Acceso a la configuración DNS del dominio, o a alguien que pueda modificarla.

> **Atención:** si guardás en Git el `docker-compose.yml` modificado, Mailpit también desaparece en desarrollo. Para
> seguir usándolo en tu PC, mirá [Conservar Mailpit en desarrollo](#conservar-mailpit-en-desarrollo).

## Paso 1. Elegir el proveedor de email

| Opción | Cuándo conviene | Host | Puerto | Seguridad |
|---|---|---|---|---|
| SMTP de la institución | Si sistemas puede darte un acceso y un remitente | Lo indica sistemas | Lo indica sistemas | Lo indica sistemas |
| Brevo | Si no hay SMTP institucional | `smtp-relay.brevo.com` | `587` | `StartTls` |
| Amazon SES | Si ya usan AWS o hay mucho volumen | `email-smtp.<región>.amazonaws.com` | `587` | `StartTls` |

No se recomienda una cuenta personal de Gmail en producción: tiene límite diario y la entrega es menos confiable.

Del proveedor tenés que obtener estos datos:

- Host y puerto SMTP.
- Usuario SMTP.
- Contraseña o clave SMTP. Suele ser distinta de la contraseña con la que entrás al panel.

Los pasos siguientes usan Brevo como ejemplo. Los nombres de las pantallas pueden cambiar con el tiempo.

## Paso 2. Autorizar el dominio remitente

Sin este paso, los emails pueden terminar en spam o ser rechazados.

1. En el panel del proveedor, agregá tu dominio como remitente.
2. Copiá los registros DNS que te indique el panel (SPF y DKIM) y cargalos en tu proveedor de DNS.
3. Agregá también un registro DMARC. Si el dominio ya tiene uno, no lo dupliques:

   ```
   Nombre: _dmarc
   Tipo:   TXT
   Valor:  v=DMARC1; p=none; rua=mailto:sistemas@midominio.com.ar
   ```

4. Esperá a que el panel muestre el dominio como verificado. Los cambios de DNS pueden demorar hasta 48 horas.

El remitente tiene que pertenecer a ese dominio, por ejemplo `no-responder@midominio.com.ar`. No uses
`no-responder@hdc.local` ni una dirección `@gmail.com` a través del proveedor.

## Paso 3. Comprobar que el servidor puede salir por el puerto SMTP

Algunos proveedores de servidores bloquean el envío de correo por defecto. Probalo con el host y el puerto elegidos:

```bash
nc -vz smtp-relay.brevo.com 587
```

Tiene que responder algo como `succeeded` u `open`. Si se queda esperando o da error, pedile a tu proveedor de
servidores que habilite la salida por ese puerto, o revisá el firewall.

## Paso 4. Hacer un respaldo

El respaldo del `.env` va fuera del proyecto porque contiene contraseñas:

```bash
cp .env ~/hdc-env.respaldo
chmod 600 ~/hdc-env.respaldo
cp docker-compose.yml ~/hdc-compose.respaldo.yml
```

## Paso 5. Configurar el `.env`

1. Abrí el archivo con `nano .env`.
2. Cambiá estas variables por tus datos:

   ```env
   SMTP_HOST=smtp-relay.brevo.com
   SMTP_PUERTO=587
   SMTP_USUARIO=el-usuario-smtp-del-proveedor
   SMTP_CONTRASENA='la-clave-smtp-del-proveedor'
   SMTP_SEGURIDAD=StartTls
   EMAIL_REMITENTE=no-responder@midominio.com.ar
   EMAIL_NOMBRE_REMITENTE=Huella de Carbono
   URL_PUBLICA=https://huella.midominio.com.ar
   ```

3. Borrá la línea `MAILPIT_PORT=8025`.
4. Protegé el archivo para que solo lo pueda leer tu usuario:

   ```bash
   chmod 600 .env
   ```

Tené en cuenta lo siguiente:

- **`SMTP_CONTRASENA`:** dejá las comillas simples, porque un `$` sin comillas se interpretaría como una variable.
- **`SMTP_SEGURIDAD`:** usá `StartTls` con el puerto 587 y `SslOnConnect` con el 465.
- **`URL_PUBLICA`:** es la dirección con la que se arma el link del email. Va sin barra al final. Si queda en
  `localhost`, el email llega pero el link no funciona.

## Paso 6. Quitar Mailpit de `docker-compose.yml`

Abrí el archivo con `nano docker-compose.yml` y hacé estos tres cambios.

**a) Borrá el servicio completo, incluidos sus comentarios:**

```yaml
  # Servidor de correo de PRUEBA: recibe los emails que manda la API (recuperación de contraseña)
  # y los muestra en http://localhost:8025 sin enviarlos a nadie. En producción se configura un
  # SMTP real en el .env (SMTP_HOST, SMTP_PUERTO, ...) y este servicio deja de usarse.
  mailpit:
    image: axllent/mailpit:v1.21
    ports:
      - "127.0.0.1:${MAILPIT_PORT:-8025}:8025"
    restart: unless-stopped
```

**b) En el servicio `api`, borrá la dependencia de Mailpit.** Si queda, Docker Compose da error porque el servicio
ya no existe. Borrá solo estas dos líneas y conservá la dependencia de `db`:

```yaml
      mailpit:
        condition: service_started
```

**c) En el servicio `api`, cambiá los valores por defecto:**

```yaml
      # Antes
      Email__Host: ${SMTP_HOST:-mailpit}
      Email__Puerto: ${SMTP_PUERTO:-1025}
      Email__Seguridad: ${SMTP_SEGURIDAD:-Ninguna}

      # Después
      Email__Host: ${SMTP_HOST:?Falta SMTP_HOST en el .env}
      Email__Puerto: ${SMTP_PUERTO:-587}
      Email__Seguridad: ${SMTP_SEGURIDAD:-StartTls}
```

Con `:?`, si falta `SMTP_HOST`, el proyecto no arranca y muestra el error. De lo contrario, la API intentaría
conectarse a un Mailpit que ya no existe, y los emails solo fallarían en los logs.

Antes de seguir, comprobá el resultado:

```bash
docker compose config --services
```

Tienen que aparecer `db`, `api` y `frontend`, sin `mailpit`.

## Paso 7. Aplicar los cambios

```bash
docker compose up -d --remove-orphans
docker compose ps
```

Este comando recrea la API con la configuración nueva y elimina el contenedor de Mailpit. No recompila el proyecto ni
borra la base de datos. Los emails que estaban guardados en Mailpit se pierden.

No uses `docker compose down -v`, porque `-v` borra la base de datos.

Como paso opcional, para liberar espacio, borrá la imagen de Mailpit:

```bash
docker image rm axllent/mailpit:v1.21
```

## Paso 8. Probar el envío

El sistema solo envía a cuentas activas que existen en la base. Con otra dirección, la pantalla muestra el mismo
mensaje, pero no sale ningún email.

1. Asegurate de tener en el sistema un usuario con una casilla real a la que puedas entrar.
2. Entrá a `https://huella.midominio.com.ar/login`, tocá **¿Olvidaste tu contraseña?** y escribí esa dirección.
3. Revisá el resultado en los logs:

   ```bash
   docker compose logs api | grep -i email
   ```

   Si funcionó, aparece `Email "Recuperá tu contraseña" enviado a ...`.

4. Comprobá que el email llegó a la bandeja de entrada y no a spam.
5. Hacé click en el botón y confirmá que abre `https://huella.midominio.com.ar/restablecer-contrasena?...`.
6. Cambiá la contraseña y comprobá que llega el segundo email, que avisa el cambio.
7. En Gmail, abrí el email, entrá a **Mostrar original** y verificá que SPF, DKIM y DMARC digan `PASS`.
8. Repetí la prueba con una casilla de otro proveedor, como Outlook o una institucional.

Para probar varias veces, tené en cuenta que la recuperación admite hasta 5 pedidos cada 15 minutos.

## Si algo falla

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| Error `Falta SMTP_HOST en el .env` al levantar | Falta la variable o está vacía | Revisá el paso 5 |
| Error de `depends_on` o de `mailpit` al levantar | Quedó una referencia a Mailpit | Revisá el paso 6 |
| Log `No se pudo enviar` con error de autenticación (`535`) | Usuario o clave SMTP incorrectos | Generá una clave nueva en el proveedor y volvé a aplicar el paso 7 |
| Log `No se pudo enviar` con timeout o error de conexión | Puerto bloqueado, o host o puerto incorrectos | Revisá el paso 3 |
| Log `No se pudo enviar` con error de TLS o SSL | `SMTP_SEGURIDAD` no coincide con el puerto | Usá `StartTls` con el 587 y `SslOnConnect` con el 465 |
| Log `No se pudo enviar` por remitente rechazado | El remitente no está autorizado | Revisá el paso 2 y `EMAIL_REMITENTE` |
| No aparece ninguna línea de envío | La cuenta no existe, o está deshabilitada | Verificá el usuario en el sistema |
| El email llega a spam | Falta SPF, DKIM o DMARC, o no los pasa | Revisá el paso 2 y **Mostrar original** |
| El link abre `localhost` | `URL_PUBLICA` mal configurada | Corregila y volvé a aplicar el paso 7 |
| El link da "no es válido o ya venció" | Link vencido o reemplazado por uno más nuevo | Pedí uno nuevo y usá solo el último |

Después de editar el `.env`, ejecutá `docker compose up -d` para aplicar los cambios. `docker compose restart` no
alcanza, porque no lee las variables nuevas.

La API intenta enviar cada email 3 veces. Si fallan, o si la API se reinicia con emails pendientes, esos emails se
pierden y el usuario tiene que volver a pedir el link.

## Volver atrás

Con los respaldos del paso 4:

```bash
cp ~/hdc-env.respaldo .env
cp ~/hdc-compose.respaldo.yml docker-compose.yml
docker compose up -d --remove-orphans
```

## Después de terminar

- Borrá los respaldos cuando confirmes que todo funciona, porque contienen datos sensibles:
  `rm ~/hdc-env.respaldo ~/hdc-compose.respaldo.yml`.
- Si la clave SMTP se filtra, revocala en el panel del proveedor y generá una nueva.
- Completá el resto de la lista de control de la
  [Guía del desarrollador § 9](GUIA_DESARROLLADOR.md#9-puesta-en-producción).

## Conservar Mailpit en desarrollo

Si guardaste en Git el `docker-compose.yml` sin Mailpit, hacé esto **solo en tu PC, no en el servidor**.

1. Creá `HDC/docker-compose.override.yml`. Docker Compose lo lee automáticamente:

   ```yaml
   services:
     mailpit:
       image: axllent/mailpit:v1.21
       ports:
         - "127.0.0.1:8025:8025"
   ```

2. Agregá `docker-compose.override.yml` al `.gitignore` para que no llegue al servidor.
3. En el `.env` de tu PC, dejá estos valores:

   ```env
   SMTP_HOST=mailpit
   SMTP_PUERTO=1025
   SMTP_SEGURIDAD=Ninguna
   ```
