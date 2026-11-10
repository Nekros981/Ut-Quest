# Guardia Rocky · Puerta de Bloques

## Flujo normal

El jugador habla con Rocky desde una casilla cardinal adyacente, mirándolo y con
el paso terminado, mediante E/Enter. Se reutilizan `RectoriaDialogueInteractor`,
`RectoriaScenePoint.IsFacing`, `DialogueSession` y los assets de diálogo compartidos.

Sin cualquiera de los tres documentos, Rocky dice:
«Aún te faltan documentos. Revisa la barra de arriba».
La puerta sigue cerrada y el control vuelve al jugador para completar trámites.

Con acta, credencial y comprobante, dice:
«A ver... acta, credencial y comprobante» y «Todo en regla. Adelante».
La apertura comienza al completar manualmente la última línea; abrir o abortar
la conversación no concede autorización.

## Apertura sobre el acceso existente

`RockyGateReview` modifica únicamente las dos celdas de la puerta original en
`ClosedDoors_Tilemap`: 22,42 y 23,42. Las referencias se obtienen de la ancla
`bloques` al configurar; no se crea otra puerta ni se reconstruye el mapa.

Se añaden solo tres indicadores al candado en la ancla existente: ACTA, CRED. y
PAGO. Durante unos 2.1 s se iluminan sucesivamente, los indicadores de documentos
suben hacia las ranuras y las dos tiles originales se desplazan/desvanecen.
El movimiento y la guía se suspenden usando las referencias de la sesión de diálogo.
No se cambia la cámara, el controlador de Mateo, sus clips ni la escala.

Al terminar se retiran solo las dos tiles de puerta. El suelo existente permanece.
`TilemapCollider2D.ProcessTilemapChanges()` y `Physics2D.SyncTransforms()` actualizan
la colisión física; `RectoriaGridMap.CanEnter` permite pasar al desaparecer esas
tiles. Las otras puertas y obstáculos permanecen intactos.

El minimapa invalida su geometría con `RefreshMap()` y vuelve a representar el
acceso como transitable. El HUD conserva los tres documentos y cambia el objetivo
a «Puerta de Bloques abierta». No se implementa aún una salida a otra escena.

## Guardado y señal para las próximas cinemáticas

El evento persistente `rectoria.blocks_gate.unlocked` pertenece al registro de
`DialogueSession` y se guarda mediante `EnrollmentMission`. No existe otro sistema
de guardado para Rocky. Al cargar se aplica directamente el estado abierto y se
reconstruye la colisión, sin animación ni señal de finalización nueva.

Después de abrir, hablar otra vez con Rocky utiliza una única línea corta. No
repite la revisión, la animación ni la señal. Si una revisión quedó completada
pero su apertura se interrumpió, Rocky ofrece solo «Todo en regla. Adelante» para
continuarla. Nueva partida cierra la puerta y restaura sus tiles, flags y matrices.

Para conectar después el Bug del Cuatrimestre, El Burócrata y la Rectora:

- Evento C#: `RockyGateReview.ReviewCompleted`.
- UnityEvent de Inspector: `onReviewCompleted`, con ID
  `rectoria.rocky.review_complete`.
- Consulta persistente al cargar: `session.HasCompleted(RockyGateReview.UnlockId)`.

La señal se emite una sola vez al terminar una apertura nueva, con el guardado del
desbloqueo ya escrito. Las cinemáticas y transiciones siguen pendientes.

## Pruebas aisladas y protección del progreso actual

En builds normales `EnrollmentMission.ProgressStorageKey` siempre es
`CampusQuest.Progress.v1`. Solo en Editor se admite una clave de pruebas cuyo
prefijo sea `CampusQuest.Test.`. Se configura antes de entrar a Play Mode y
permanece entre recargas del dominio mediante SessionState. Nunca es necesario
vaciar o reemplazar la partida real para probar documentos.

La herramienta de Editor `RockyGateValidation.Prepare()` conserva una copia exacta
del guardado actual en `tmp/rocky-validation/test-context.json`, con existencia,
JSON y hash SHA-256. Verifica la lectura desde disco y crea una clave única
`CampusQuest.Test.Rocky.<guid>`. Antes de cada paso de prueba comprueba tanto la
clave aislada como la igualdad exacta del guardado real. Si algo difiere, aborta;
no intenta reconstruir ni restaurar automáticamente el progreso del usuario.

El respaldo también se prueba funcionalmente guardando, vaciando y restaurando
una partida conocida **solo en la clave temporal**. Al terminar,
`RockyGateValidation.Cleanup()` verifica otra vez el guardado real, borra únicamente
la clave temporal y desactiva la selección de pruebas. Las copias en disco quedan
fuera de Assets y de los commits.

Configuración de escena:
`Campus Quest/Rectoria/10 - Guardia Rocky y puerta existente`.

Validación mediante Unity-MCP:

1. Fuera de Play: `RockyGateValidation.Prepare()`.
2. En Play: `RockyGateValidation.Begin()`.
3. Salir y volver a Play conservando la misma clave temporal;
   `RockyGateValidation.Begin(true)` comprueba la carga.
4. Fuera de Play: `RockyGateValidation.Cleanup()`.

Informes: `tmp/rocky-validation/results.txt` y `reload-results.txt`.
Se prueban las ocho combinaciones de documentos, mirada, paso incompleto,
conversación abortada, las tres luces, bloqueo durante apertura, collider físico,
cruce con el controlador real, minimapa, otras puertas intactas, señal única,
repetición, nueva partida y persistencia. El guardado actual no se modifica.
