# Archivo · Don Chuy

## Estado del contenido original

La tabla definitiva de la página 7 de `GUION RECTORIA` fue proporcionada por el
usuario e integrada en `Assets/Data/Dialogs/Rectoria/Archivo_Carpetas.asset`.
El trámite está habilitado en la partida normal al completar las cuatro pistas.

| Posición inicial | Color | Año |
|---|---|---|
| 1 | Roja | 2006 |
| 2 | Azul | 2007 |
| 3 | Verde | 2008 |
| 4 | Amarilla | 2007 |
| 5 | Azul | 2008 |
| 6 | Naranja | 2006 |
| 7 | Morada | 2007 |
| 8 | Amarilla | 2008 |
| 9 | Azul | 2006 |

Se muestra en este orden al abrir, de izquierda a derecha y de arriba abajo.
Todas las carpetas usan la misma geometría, tamaño, fuente y reglas de selección.
Las tres azules comparten exactamente el mismo tono. Ninguna marca indica la
respuesta antes de confirmar; el borde dorado sigue únicamente al cursor.

Los diálogos de Don Chuy son una adaptación de la descripción del trámite;
las cuatro pistas sí reproducen exactamente el texto proporcionado por el usuario.
Se reutiliza el sprite existente de Don Chuy como retrato. Los iconos de carpeta
son siluetas provisionales de pixel art creadas con geometría de UI, pues no se
localizó arte de inventario adecuado. La fuente es Pixeloid Sans.

## Flujo

1. Terminar la bienvenida de la Rectora activa la misión existente.
2. Hablar con Don Chuy mediante E/Enter, en una casilla cardinal adyacente,
   mirándolo y con el paso de Mateo terminado. Don Chuy explica las cuatro notas.
3. Leer cada nota con E/Enter en las anclas existentes `nota_1`…`nota_4`, sin mover
   muebles ni cambiar celdas. Se puede releer y el orden es libre. Solo completar
   la lectura registra la pista; abrir y abortar una conversación no la completa.
4. Volver a Don Chuy. Antes de cuatro notas recuerda que faltan pistas; con las
   cuatro y una tabla válida, su diálogo abre el acertijo 3×3.
5. Flechas para elegir, E/Enter para confirmar, Escape para salir. Tres intentos
   por ronda. El tercer error baraja las mismas nueve opciones, garantiza un orden
   diferente, restaura tres intentos y conserva las pistas. No hay límite de rondas.
6. Azul de 2008 siempre es la respuesta. El éxito llama exclusivamente a
   `EnrollmentMission.TryAwardDocument(EnrollmentDocument.BirthCertificate)`.
   La confirmación de éxito aparece también al repetir, pero el mensaje animado
   de recompensa se muestra solo si la función devuelve `true`.

El movimiento y la guía se suspenden con los mismos componentes configurados en
`DialogueSession.suspendWhileOpen`. Al cerrar se restauran sus estados anteriores;
no se cambian sus scripts, animaciones, colisiones ni velocidades. La entrada que
cierra un diálogo no selecciona accidentalmente una carpeta. Ninguna simulación
del acertijo se ejecuta automáticamente ni aparece como control del jugador.

## Guardado e integración

Se usa el mismo registro de `DialogueSession`, no un segundo inventario:

| Hito | ID |
|---|---|
| Introducción terminada | `rectoria.archive.don_chuy_intro` |
| Cada nota leída | `rectoria.archive.note_1`…`rectoria.archive.note_4` |
| Acta obtenida | `rectoria.document.birth_certificate` |

Cada cambio dispara el guardado existente `CampusQuest.Progress.v1` en PlayerPrefs.
La restauración en `EnrollmentMission.Awake` recupera tanto las notas como el acta.
Nueva partida elimina estos eventos junto con el resto de la partida; si el
acertijo está abierto, también lo cierra y libera el control.
La selección, los intentos y el orden de carpetas son temporales por ronda;
al volver a abrir se inicia una ronda nueva. Las pistas y el documento sí persisten.

La tabla contiene exactamente nueve pares únicos, con una sola carpeta azul de
2008. El recurso valida estos requisitos. Solo los reintentos tras tres fallos
reorganizan las opciones. Al volver a abrir, incluso después de ganar, se recupera
el orden inicial del guion.
La configuración de escena está en
`Campus Quest/Rectoria/7 - Conectar Archivo con Don Chuy`.

## Verificación mediante Unity-MCP

`ArchiveChallengeValidation.Begin()` es una suite explícita, exclusiva del Editor,
para ejecutar en Play Mode mediante `script-execute`. Cambia el progreso de prueba:
respaldar antes `EnrollmentMission.SaveKey` y restaurarlo después.
El informe se escribe en `tmp/archive-validation/results.txt`, fuera de Assets.

La suite utiliza exclusivamente el recurso definitivo del juego; falla si falta
o no coincide con las nueve opciones originales. Ya no crea datos sintéticos.

La suite cubre diálogos reales, adyacencia, mirada, paso incompleto, lectura exacta,
notas repetidas, bloqueo por pistas, selección, intentos, reorganización, éxito,
HUD, recompensa única, repetición, restauración y nueva partida durante el modal.
También inyecta E, Enter, las cuatro flechas y Escape en el Input System para
comprobar la ruta de entrada real. Se comprueba por separado la restauración de
introducción, cuatro notas y acta al salir y volver a Play Mode.

Con la tabla definitiva pasaron 101 comprobaciones. También se verificó por
separado que un reinicio conserva exactamente las notas 1 y 3 dejando 2 y 4 sin
leer, y que una partida completa restaurada permite repetir azul 2008 sin otra
acta ni animación de recompensa. Se inspeccionó la cuadrícula inicial en Game View.

Pendiente: revisión editorial de los diálogos de Don Chuy, arte final de carpetas si el equipo
lo proporciona y prueba en un ejecutable. Esta etapa no abre la Puerta de Bloques
ni modifica funcionalidades de Chino.
