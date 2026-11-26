# BePhoto: BackEnd

API en ASP.NET Core (.NET 10) con PostgreSQL y Cloudinary. Las fotos se suben a Cloudinary
y los datos de cada recuerdo (título, descripción, fecha y coordenadas) se guardan en PostgreSQL.

## Qué necesitas

- Visual Studio con la carga de trabajo **ASP.NET y desarrollo web**
- **PostgreSQL** instalado en tu PC (usuario `postgres` y la contraseña que elegiste al instalarlo)
- Las claves de Cloudinary (pídelas al equipo, **no se suben al repositorio**)

## Configurar las claves (una sola vez por PC)

Las claves se guardan en tu PC con *user-secrets*, así no quedan en Git. En una terminal:

```
cd BackEnd\BePhoto.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=bephoto;Username=postgres;Password=TU_CLAVE_DE_POSTGRES"
dotnet user-secrets set "Jwt:Key" "UN_TEXTO_LARGO_DE_MINIMO_32_CARACTERES"
dotnet user-secrets set "Cloudinary:CloudName" "TU_CLOUD_NAME"
dotnet user-secrets set "Cloudinary:ApiKey" "TU_API_KEY"
dotnet user-secrets set "Cloudinary:ApiSecret" "TU_API_SECRET"
```

La base `bephoto` y sus tablas se crean solas al iniciar la API la primera vez. Las tablas que se agregan
después (como la de los códigos de recuperación) también se crean solas al iniciar, sin borrar nada.

### Correo de recuperación de contraseña (Resend)

El código para recuperar la contraseña se envía por correo con [Resend](https://resend.com/docs/api-reference/emails/send-email).
Es opcional para arrancar la API:

```
dotnet user-secrets set "Resend:ApiKey" "re_TU_CLAVE_DE_RESEND"
dotnet user-secrets set "Resend:DestinoPruebas" "correo-del-dueno-de-la-cuenta@gmail.com"
```

- Sin `Resend:ApiKey`, en desarrollo el código se muestra en la consola de la API (para probar el flujo sin correo).
- Sin un dominio verificado en Resend, solo se puede enviar al correo del **dueño de la cuenta** de Resend, y desde
  `onboarding@resend.dev`. Con `Resend:DestinoPruebas` todos los códigos llegan a ese correo (el asunto dice para quién
  eran), así se puede probar con cuentas de mentira. **Quítalo cuando haya un dominio verificado.**
- También se puede dar la clave con la variable de entorno `RESEND_API_KEY`.

## Ejecutar

Abre `BackEnd/BackEnd.slnx` y presiona F5 (perfil `http`). La API queda en `http://localhost:5080`.
Desde el emulador de Android, esa misma API se ve en `http://10.0.2.2:5080`.

Para probar, abre `BePhoto.Api/BePhoto.Api.http` y pulsa **Enviar solicitud** sobre cada petición.

## Endpoints

Todos devuelven JSON. Los marcados con 🔒 necesitan el encabezado `Authorization: Bearer <token>`.

| Método | Ruta | Para qué | Historia |
|---|---|---|---|
| POST | `/api/auth/registro` | Crear cuenta. Devuelve el token. | HU-BP-0001 |
| POST | `/api/auth/login` | Iniciar sesión. Devuelve el token. | HU-BP-0001 |
| POST 🔒 | `/api/auth/cambiar-contrasena` | Cambiar la contraseña | |
| POST | `/api/auth/olvide-contrasena` | Envía un código de 6 dígitos al correo (vale 15 min). Responde igual exista o no la cuenta. | |
| POST | `/api/auth/restablecer-contrasena` | Cambia la contraseña con el código recibido (`correo`, `codigo`, `nuevaContrasena`). 5 intentos por código. | |
| GET 🔒 | `/api/perfil` | Datos del usuario y cuántos recuerdos y lugares tiene | HU-BP-0008 |
| GET 🔒 | `/api/recuerdos` | Galería, del más reciente al más antiguo | HU-BP-0004 |
| GET 🔒 | `/api/recuerdos/{id}` | Detalle de un recuerdo | HU-BP-0005 |
| POST 🔒 | `/api/recuerdos` | Crear un recuerdo (`multipart/form-data`) | HU-BP-0002, 0003 |
| PUT 🔒 | `/api/recuerdos/{id}` | Editar título y descripción | HU-BP-0006 |
| DELETE 🔒 | `/api/recuerdos/{id}` | Eliminar el recuerdo y su foto | HU-BP-0007 |

### Crear un recuerdo (`POST /api/recuerdos`)

Campos del formulario: `foto` (archivo), `titulo`, `descripcion` (opcional), `latitud`, `longitud`,
`altitud` (opcional) y `fecha` (opcional, con zona horaria, por ejemplo `2026-11-24T20:30:00-05:00`).
Los números van con **punto** decimal. La foto debe pesar hasta 10 MB.

### Respuestas de error

Los errores siempre traen el mensaje listo para mostrar en el campo `detail`:

```json
{ "title": "Unauthorized", "status": 401, "detail": "Correo o contraseña incorrectos" }
```

| Código | Cuándo |
|---|---|
| 400 | Datos no válidos (título vacío, correo mal escrito, foto muy pesada...) |
| 401 | Credenciales incorrectas o token vencido |
| 404 | El recuerdo no existe o es de otro usuario |
| 409 | Ya existe una cuenta con ese correo |
| 502 | No se pudo subir la foto a Cloudinary |

## Reglas de los datos

- Nombre y apellido: hasta 50 caracteres. Correo: hasta 100. Contraseña: de 8 a 50.
- Título del recuerdo: obligatorio, hasta 60. Descripción: hasta 300.
- Cada usuario solo ve y modifica sus propios recuerdos.
- Las fechas se guardan en UTC.
