# Misión de inscripción

La escena `Rectoria_Tutorial_Arte_v01` tiene `EnrollmentMission` junto a
`DialogueSession` y un HUD con Pixeloid Sans, fondo oscuro y borde dorado.
El HUD permanece oculto hasta cerrar la última línea de `rectoria.rector_welcome`.
Abortar el diálogo no activa la misión. Repetirlo no reinicia documentos.

## Registro y guardado

Se reutiliza el conjunto de eventos de `DialogueSession`: `RegisterEvent(id)`
acepta cada ID una sola vez y emite `ProgressChanged`. Las conversaciones
mantienen `Completed` y `onFirstCompletion` con su comportamiento anterior.
La misión y su objetivo se calculan a partir de los eventos, sin otro inventario.

`EnrollmentMission` guarda automáticamente cada cambio con `PlayerPrefs.Save()`:
clave `CampusQuest.Progress.v1`, JSON `{version:1, events:[...]}`.
En Windows Editor/Standalone PlayerPrefs usa el registro de Windows del producto;
no es un archivo en Assets. El progreso persiste entre ejecuciones. Es una sola
partida local; los perfiles múltiples y sincronización quedan fuera de esta etapa.
Se restaura en `Awake`, antes de las interacciones. Un JSON inválido o una versión
incompatible genera una advertencia y no reemplaza el estado en memoria.

`StartNewGame()` aborta cualquier conversación abierta, limpia el registro
compartido y guarda inmediatamente el estado vacío. No borra las preferencias de
opciones. El futuro botón Nueva partida debe llamar esta función sobre la sesión
de juego; no se modifica ahora el menú de Chino.

## Conectar los minijuegos

Después de validar la victoria, llamar al componente `EnrollmentMission`:

| Minijuego | Recompensa | Evento |
|---|---|---|
| Archivo | `TryAwardDocument(EnrollmentDocument.BirthCertificate)` | `rectoria.document.birth_certificate` |
| Credencialización | `TryAwardDocument(EnrollmentDocument.StudentCard)` | `rectoria.document.student_card` |
| Caja | `TryAwardDocument(EnrollmentDocument.PaymentReceipt)` | `rectoria.document.payment_receipt` |

La función devuelve `true` solamente al conceder un documento nuevo con la misión
activa. Mostrar la animación de recompensa únicamente si devuelve `true`.
No llamar directamente `RegisterEvent` desde los minijuegos: usar esta función
para respetar el inicio de misión y la validación de documentos.
El orden es libre. Cada recompensa actualiza HUD y guardado inmediatamente.
Con tres documentos el objetivo pasa a `Dirígete a la Puerta de Bloques`.
No se abre la puerta ni se programa ningún minijuego en esta etapa.

## Desarrollo y pruebas

Las simulaciones están en `Assets/Editor/EnrollmentMissionTools.cs`, excluidas
del juego compilado. En Play Mode: menú `Campus Quest/Desarrollo/Inscripción`:
obtener cada documento, restaurar guardado y nueva partida. No hay teclas de
trampa, botones de prueba ni simulaciones automáticas para el jugador.

La opción `Campus Quest/Rectoria/6 - Configurar misión de inscripción` instala
el componente y HUD en la escena guardada fuera de Play Mode. Si ya existe,
no duplica ni reconstruye nada.

Validado con Unity-MCP en Play Mode: bienvenida real de cinco líneas, ausencia de
activación al abrir/abortar, contador 0/3, los seis órdenes, duplicados rechazados,
restauración tras salir/entrar de Play Mode con 2/3 y con credencial sola, objetivo
final 3/3, restauración completa, nueva partida persistente y JSON inválido.
No se ha probado cerrando físicamente el proceso del Editor ni en un ejecutable.
Las pruebas restauran el guardado que existía antes de iniciarlas.
