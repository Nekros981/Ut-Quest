# Credencialización, carrera y minimapa

## Credencialización

La escena de arte de Rectoría conecta `CredentialChallenge` con la sesión de
diálogos, la misión y la marca de fotografía ya existentes. Hablar con Lic. Mony
usa las mismas reglas de casilla cardinal adyacente, mirada y E/Enter que los
otros NPC. Sus dos líneas y la confirmación final reproducen el texto solicitado.
Solo requiere la bienvenida de la Rectora; no depende del acta de Archivo.

Después de hablar, entrar en la marca y permanecer quieto 1.5 s inicia la captura.
Salir o moverse reinicia la espera. Durante la cuenta 3–2–1, el flash y el puzle,
se suspenden los mismos controles configurados para las conversaciones. Escape
permite cancelar y restaura los estados anteriores. Para otra foto hay que salir
y volver a entrar en la marca: quedarse parado tras ganar no repite la captura.

El tablero `SlidingCredentialBoard` contiene ocho piezas y un hueco. Se mezcla
mediante diez movimientos legales desde la solución, evitando invertir de
inmediato el movimiento anterior. Si vuelve a la solución, realiza otro paso.
La mezcla es resoluble por construcción y no empieza resuelta. Solo una pieza
adyacente al hueco puede moverse. Flechas seleccionan, E/Enter o clic mueve,
R o el control Reiniciar restaura la misma mezcla y pone movimientos a cero.
No hay reloj ni penalización. La imagen es provisional, identificada en pantalla;
está dibujada con geometría de UI y se comparte entre las piezas y la referencia.

Al resolver se llama a
`TryAwardDocument(EnrollmentDocument.StudentCard)` y se muestra el resultado.
La animación de recompensa depende exclusivamente del `true` devuelto por esa
función. Repetir muestra el éxito sin otro documento ni animación. E/Enter tras
el resultado abre la conversación final: «¡Listo! Ya eres oficialmente estudiante».

Los hitos `rectoria.credential.mony_intro`, `rectoria.credential.photo_taken` y
`rectoria.document.student_card` usan el registro y guardado existente
`CampusQuest.Progress.v1`. La credencial y la conversación persisten; el tablero,
los movimientos y los segundos de captura son temporales. Tras reiniciar, una
nueva sesión de foto comienza desde la marca, sin duplicar el documento ya ganado.
Nueva partida limpia los hitos y libera el control si hay una captura o puzle.

## Carrera

Mantener Shift izquierdo o derecho con WASD o flechas selecciona la duración
existente de carrera: `1/6 s` por casilla. Sin Shift se conservan `0.25 s` por
casilla. Soltar Shift vuelve a caminata al comenzar la siguiente casilla; el paso
en curso termina con la interpolación existente. Z ya no selecciona carrera.
No se modifica el Animator, sus ocho clips, la escala, los obstáculos ni los
algoritmos de interpolación o transitabilidad. Los textos de controles y los
generadores de escena usan Shift. El menú de Chino no contenía una referencia
a Z/carrera y no se modifica.

## Minimap

`RectoriaMinimap` dibuja directamente las 44×46 casillas de `RectoriaGridMap` como
geometría de UI: `CanEnter` determina transitabilidad y el Tilemap de paredes las
paredes. El suelo bloqueado se distingue del transitable. La diferenciación entre
pasillos y habitaciones reutiliza `RectoriaSceneGuide.AreaAt`; no hay otra tabla
manual de celdas. No usa otra cámara, textura de render ni una copia del escenario.

La orientación permanece fija. El punto celeste sigue la posición interpolada de
Mateo mediante los ejes del Grid, incluso durante carrera y entre centros de celda.
Los cuatro marcadores A/C/$/B reutilizan las anclas de Archivo, Credencialización,
Caja y Puerta de Bloques. No se muestran otros NPC ni objetos.

Documentos queda arriba a la izquierda y el minimapa arriba a la derecha. El mapa
no recibe teclado ni bloquea raycasts. Sigue visible durante conversaciones y se
oculta con CanvasGroup durante Archivo, captura y puzle de Credencialización;
al cerrar se restaura. No se cambian dimensiones, Tilemaps ni colisiones.

## Configuración y validación

`Campus Quest/Rectoria/8 - Credencialización y minimapa` instala las interfaces y
referencias en la escena guardada fuera de Play Mode. Las pruebas explícitas están
en `Assets/Editor/CredentialExperienceValidation.cs`, fuera del juego compilado.
Ejecutar `CredentialExperienceValidation.Begin()` con Unity-MCP en Play Mode;
respaldar primero el guardado y restaurarlo al terminar. Resultados y capturas se
escriben en `tmp/credential-validation`, fuera de Assets.

La suite comprueba 500 mezclas, movimientos legales, reinicio, diálogos exactos,
reset de quietud, cuenta y flash, bloqueo de controles, resolución, recompensa,
repetición, clics, Shift izquierdo/derecho y liberación, minimapa en movimiento,
las 2024 casillas, compatibilidad con Archivo y nueva partida. También se valida
por separado la restauración tras salir y volver a Play Mode y las referencias.

Pendiente de arte final: sustituir la imagen provisional de credencial.
Queda por validar en un ejecutable y en resoluciones adicionales a Game View.
Caja y apertura de Bloques continúan fuera de esta etapa.
