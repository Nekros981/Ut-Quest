# Diálogos compartidos de Campus Quest

## Componentes

- `DialogueDefinition`: asset con ID estable, nombre/retrato por defecto y líneas.
  Cada línea puede cambiar de interlocutor o retrato. Crear desde
  `Assets/Create/Campus Quest/Dialogue` y guardarlo en `Assets/Data/Dialogs/<dominio>`.
- `DialogueSession`: inicio, avance, bloqueo temporal de componentes y finalización.
  No depende del mapa, de Mateo ni de un sistema de misiones.
- `DialoguePlayerContext`: recibe el nombre elegido mediante `SetPlayerName` y
  sustituye `{playerName}` en nombres y textos. Mientras no exista perfil usa
  «Estudiante». No escribe preferencias ni modifica el menú.
- `DialogueView` (en `Scripts/UI/Dialogue`): caja con Pixeloid Sans, nombre, texto,
  retrato opcional y espacio identificado si falta el retrato.
- `RectoriaDialogueInteractor`: adaptador del escenario actual. Reutiliza
  `RectoriaScenePoint.IsFacing`, exige casilla transitable y centra la interacción
  en los NPC que tengan un diálogo asignado.

Hay una sesión y una vista por escena, no una por NPC. Para otro nivel solo se
necesita su adaptador de interacción; los textos y la interfaz usan los mismos
componentes. Otros interactores deben consultar `DialogueSession.IsAnyOpen` y/o
incluir su componente de entrada en `suspendWhileOpen`.

## Contrato de entrada

E, Enter y Enter numérico abren/avanzan por pulsación. La pulsación de apertura no
consume la primera línea; mantener la tecla no avanza repetidamente. No hay
temporizador de avance ni cierre automático. El último avance cierra la caja y
restaura exactamente los estados anteriores de los componentes y objetos ocultos.

La entrada de movimiento se suspende desactivando temporalmente su componente
existente. No se cambian sus scripts, animaciones, escala, velocidades o colliders.
El adaptador abre únicamente cuando el personaje ha terminado su paso y está en
una casilla cardinal adyacente mirando hacia el NPC.

## Finalización e integración pendiente

La bienvenida de la Rectora usa el ID `rectoria.rector_welcome`. Los cinco textos
hablados proceden de la página 5 de `GUION RECTORIA.pdf` del equipo; las acotaciones
de cámara y gestos no se muestran como diálogo. Don Chuy y las cuatro notas del
Archivo ya reutilizan este sistema; ver `Scripts/Minigames/Rectoria/ARCHIVE.md`.
Lic. Mony y Cajero Beto también lo reutilizan en `CREDENTIALIZATION.md` y
`CASHIER.md`, dentro de esa misma carpeta. Guardia Rocky usa la revisión y
desbloqueo persistente de `ROCKY.md`, con señal preparada para las cinemáticas.

- `HasCompleted(id)` y `RectoriaDialogueInteractor.HasSpokenToRectora` permiten
  consultar si se cerró la última línea.
- `onFirstCompletion` emite el ID una sola vez por estado de partida restaurado.
- `Completed(id, firstCompletion)` se emite cada vez que se termina una conversación.
- `Abort()` restaura el control sin marcarla como terminada. También ocurre al
  desactivar o destruir la sesión.
- `CaptureCompletedIds` y `RestoreCompletedIds` son los puntos de enlace con el
  guardado/perfil. `EnrollmentMission` ya guarda y restaura este registro compartido
  para Rectoría; ver `Scripts/Minigames/Rectoria/ENROLLMENT.md`.

Conectar después el nombre del perfil y el bloqueo inicial del pasillo.
La bienvenida activa ahora la misión mediante su evento registrado, pero no entrega
documentos ni abre puertas por sí sola.
No hay retrato de Rectora identificado en los assets actuales; se muestra «Retrato
provisional». Un Sprite asignado al diálogo o a una línea sustituye ese espacio.

Herramienta de configuración: `Campus Quest/Rectoria/5 - Conectar dialogo inicial
de la Rectora`, sobre la escena de arte guardada y fuera de Play Mode. Reutiliza el
asset de bienvenida si ya existe y conserva sus textos editados.
