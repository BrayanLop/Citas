# Política de privacidad de Natural (BORRADOR)

> **Borrador para revisar.** Es el texto que Google pide publicado en una URL pública para verificar la app.
> Antes de publicarlo: completa lo marcado con `[…]`, revísalo con quien lleve lo legal de la empresa y
> publícalo en una página tuya (p. ej. `https://bia.solutions/natural/privacidad`).

**Última actualización:** [fecha]
**Responsable:** [razón social], [correo de contacto de privacidad]

## Qué datos tratamos

Natural es una aplicación de gestión de servicios, comisiones y citas para negocios. Tratamos:

- Datos de la cuenta: nombre, correo, celular.
- Datos del negocio: servicios, citas, horarios y registros de trabajo.
- Si conectas tu **Google Calendar** (opcional): tu correo de Google y los datos de tu calendario que se
  describen abajo.

## Uso de Google Calendar

Cuando un empleado conecta su Google Calendar, Natural solicita el permiso
`https://www.googleapis.com/auth/calendar.events` (y `openid`/`email` para mostrar con qué cuenta se conectó) para:

1. **Crear, actualizar y eliminar en tu calendario los eventos de las citas** que se agendan contigo en Natural.
2. **Leer los eventos de tu calendario únicamente para saber en qué horas estás ocupado**, y así no
   ofrecer esas horas a los clientes. De esos eventos usamos solo la hora de inicio, la de fin y si ocupan
   tiempo; **no guardamos ni mostramos sus títulos, descripciones, invitados ni ubicaciones.**

Qué guardamos de tu conexión: un permiso de acceso (*refresh token*) **cifrado**, tu correo de Google, y para
cada cita el identificador del evento que creamos. Nada más de tu calendario.

El uso que Natural hace de la información recibida de las APIs de Google cumple la
[Política de datos de usuario de los servicios de API de Google](https://developers.google.com/terms/api-services-user-data-policy),
incluidos los requisitos de **uso limitado**. En particular:

- Usamos los datos de Google Calendar solo para ofrecer las dos funciones descritas arriba.
- **No vendemos ni transferimos** esos datos a terceros, salvo lo necesario para prestar el servicio o por obligación legal.
- **No los usamos** para publicidad ni para entrenar modelos de inteligencia artificial.
- Ninguna persona lee tus datos de calendario, salvo con tu consentimiento expreso, por seguridad, o por obligación legal.

## Cómo retirar el permiso

Puedes **desconectar** tu calendario cuando quieras desde la app (Agenda → Ajustes → Desconectar): borramos el
permiso guardado y los bloqueos importados. También puedes revocar el acceso en
<https://myaccount.google.com/permissions>. Si no vuelves a conectar, no leemos ni escribimos en tu calendario.

## Conservación y seguridad

- El permiso de Google se guarda cifrado (AES-256-GCM) y se elimina al desconectar.
- La comunicación es siempre por HTTPS.
- Las citas y datos del negocio se conservan mientras la cuenta del negocio esté activa; puedes pedir su eliminación escribiendo a [correo].

## Tus derechos

Puedes pedir acceso, corrección o eliminación de tus datos escribiendo a [correo]. [Mencionar la normativa
aplicable: p. ej. Ley 1581 de 2012 (Colombia)].

## Cambios

Si cambiamos esta política publicaremos la nueva versión en esta página con su fecha.
