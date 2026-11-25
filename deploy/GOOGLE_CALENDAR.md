# Google Calendar: puesta en marcha

Cada empleado conecta **su propio** Google Calendar desde la app. Con eso:

- **Citas → Google:** cada cita nueva, reprogramada o cancelada se refleja en su calendario (aparece en segundos).
- **Google → Citas:** los eventos ocupados de su calendario bloquean esas horas para los clientes
  (sin copiar el título de los eventos: son privados).

La integración es **opcional**: sin `Google__ClientId` queda apagada y la agenda funciona igual.

## 1. Crear el proyecto en Google Cloud (lo haces tú, una vez)

1. <https://console.cloud.google.com> → proyecto nuevo (p. ej. `natural-citas`).
2. **APIs y servicios → Biblioteca** → activar **Google Calendar API**.
3. **Pantalla de consentimiento de OAuth** (en el menú de *Google Auth Platform*):
   - Tipo de usuario: **Externo**. Nombre de la app: *Natural*. Correo de soporte y de contacto: el tuyo.
   - Dominio autorizado: `bia.solutions`.
   - Enlace de **política de privacidad**: debe estar publicado (borrador en `deploy/politica-privacidad.md`).
   - Permisos (*scopes*), solo estos tres: `openid`, `email` y
     `https://www.googleapis.com/auth/calendar.events`.
4. **Credenciales → Crear credenciales → ID de cliente de OAuth** → tipo **Aplicación web**.
   - URI de redirección autorizado: `https://citas.bia.solutions/Google/Callback` (exactamente igual a `Google__RedirectUri`).
   - Guarda el **ID de cliente** y el **secreto**.

## 2. Estados de la app en Google: *Pruebas* y *Producción*

| Estado | Quién puede conectar | Límite |
|---|---|---|
| **Pruebas** | Solo los correos que agregues como "usuarios de prueba" (hasta 100) | Los permisos concedidos pueden caducar a los 7 días (hay que volver a conectar) |
| **Producción** (verificada) | Cualquiera | Sin caducidad; Google revisa la app (suele tardar unos 10 días hábiles) |

Para desarrollar y para el primer negocio piloto alcanza con **Pruebas**. Para publicar a todos hay que
enviar la app a **verificación** (el permiso de calendario es "sensible"): Google pide la política de
privacidad publicada, el dominio verificado y un video corto que muestre cómo se usa el permiso.
**Conviene pedirla desde ya** porque es lo que más tarda.

## 3. Variables en el servidor (`~/citas.env`)

Ver `deploy/citas.env.example`. Las cuatro obligatorias:

```
Google__ClientId=...apps.googleusercontent.com
Google__ClientSecret=...
Google__RedirectUri=https://citas.bia.solutions/Google/Callback
Google__ClaveCifradoBase64=<salida de: openssl rand -base64 32>
```

- La clave cifra los permisos guardados en la base de datos (AES-256-GCM). **Guárdala en un gestor de
  secretos**: si se pierde, cada empleado tendría que volver a conectar su calendario. No la cambies
  mientras haya cuentas conectadas.
- Opcional, para que los cambios hechos en Google lleguen al instante en vez de cada 5 minutos:
  `Google__UrlWebhook=https://citas.bia.solutions/Google/Notificaciones` (https obligatorio).

Si defines `Google__ClientId` pero falta alguna otra, **la API no arranca** y el mensaje dice cuál.

## 4. Cómo se comporta

- **Si el empleado retira el permiso** (o Google lo vence): la conexión pasa a "Revocada", se quitan los
  bloqueos que vinieron de Google y la app muestra **Reconectar**. La agenda de Citas sigue funcionando.
- **Si Google falla** (caída, límite de uso): se reintenta solo, esperando 1, 2, 4… hasta 60 minutos. Nada se duplica.
- **Eventos de Google que no bloquean:** los marcados como "disponible", cumpleaños, ubicación de trabajo,
  los que el empleado rechazó y los que ya pasaron. Los de día completo bloquean el día entero (hora del negocio).
- **Desconectar** avisa a Google, borra el permiso guardado y los bloqueos importados. Los eventos que Citas
  ya había creado en el calendario del empleado **no se borran** (son suyos).

## 5. Probar que funciona (después de desplegar)

1. En la app, un trabajador: **Agenda → Ajustes → Conectar Google Calendar** y acepta el permiso.
2. Crea una cita para él: debe aparecer en su Google Calendar en menos de un minuto.
3. Crea en Google un evento de una hora: en 5 minutos (o al instante con el webhook) esa hora deja de
   ofrecerse a los clientes.
4. Quita el acceso en <https://myaccount.google.com/permissions>: en la siguiente lectura la app debe
   mostrar **Reconectar**.
