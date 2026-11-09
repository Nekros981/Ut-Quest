# Rectoría — escenario y tutorial

## Alcance implementado

La primera etapa prepara `Assets/Scenes/Rectoria_Tutorial_Arte_v01.unity` sobre su
Grid y Tilemaps originales. `Rectoria_Tutorial_Blockout` conserva la referencia base.
No se cambian tamaño de celda, escala de Mateo, velocidades ni casillas ocupadas.

- Oficinas identificadas, muebles existentes con arte de oficina y señalización.
- Cinco figuras geométricas con nombre, marcadas explícitamente como provisionales.
- Cuatro notas sobre muebles, cámara y marca de foto, mesa con seis recibos.
- Oficina de Trámites y Puerta de Bloques conservadas cerradas.
- Indicador de zona y contexto de orientación con Pixeloid Sans.
- Puntos serializados que reconocen adyacencia y dirección de mirada. La Rectora
  conecta ahora con el sistema compartido de diálogos; los demás puntos no conceden
  documentos, no leen notas ni ejecutan minijuegos todavía.
- Giro del sprite en reposo al mirar hacia un NPC/obstáculo, reutilizando el driver
  visual existente.

La preparación se ejecuta desde `Campus Quest/Rectoria/4 - Preparar escenario
jugable (sin regenerar mapa)`, con la escena de arte guardada y fuera de Play Mode.
La herramienta actualiza sus propios objetos por nombre, sin duplicarlos. No usa
el generador original para reconstruir el mapa ni elimina los marcadores antiguos:
los deja ocultos como referencia.

## Anclas del escenario

Las coordenadas son casillas del Grid original, no posiciones en píxeles.

| ID | Elemento | Casilla | Aproximación |
|---|---|---|---|
| rectora | Rectora | 22,18 | 23,18 |
| don_chuy | Archivo | 7,30 | 8,30 |
| lic_mony | Credencialización | 35,30 | 36,30 |
| beto | Caja | 7,40 | 8,40 |
| rocky | Guardia | 25,39 | 24,39 |
| nota_1 | Nota sobre pizarrón provisional | 5,23 | 5,24 |
| nota_2 | Nota sobre escritorio | 10,24 | 9,24 |
| nota_3 | Nota sobre archivero provisional | 3,25 | 4,25 |
| nota_4 | Nota sobre estante provisional | 11,30 | 11,29 |
| foto | Marca de fotografía | 34,26 | 34,26 |
| recibos | Mesa de recibos | 10,35 | 10,34 |
| tramites | Oficina cerrada | 19,39 | 20,39 |
| bloques | Puerta de Bloques | 22,42 | 22,41 |

La Puerta de Bloques ocupa además 23,42. La casilla de aproximación documenta una
ruta comprobada; un NPC admite cualquier lado cardinal adyacente si el jugador lo
está mirando y ha terminado su paso. Las figuras no añaden un segundo sistema de
colisiones: ocupan las casillas N ya bloqueadas en Collision_Tilemap.

## Guion aprobado para las siguientes etapas

Referencia: `GUION RECTORIA.pdf` del equipo, complementado por las decisiones más
recientes del usuario. Estas decisiones prevalecen sobre las versiones antiguas:

- El estudiante elegirá su nombre; Mateo sigue siendo el nombre del recurso visual.
- Movimiento continuo en cuatro direcciones; Shift izquierdo o derecho para correr. Sin salto ni charco
  obligatorio. No se implementa giro automático al NPC más cercano.
- Conversaciones desde casilla adyacente, mirando al NPC; E/Enter para avanzar
  manualmente. Movimiento bloqueado durante diálogo, puzle y cinemática.
- La Rectora inicia la inscripción. Después se obtienen tres documentos en orden libre.
- Archivo: leer cuatro notas antes de las nueve carpetas. Respuesta azul 2008;
  tres intentos por ronda; al agotarlos solo cambian las posiciones.
- Credencialización: permanecer quieto en la marca, cuenta 3-2-1 y flash; puzle
  deslizante 3×3 con ocho piezas, mezclado con aproximadamente diez movimientos
  legales desde la solución.
- Caja: recibos únicos de 300, 450, 600, 700, 850 y 1000; aceptar toda selección
  que sume exactamente 1850.
- Guardar al obtener documentos y en hitos de historia. Repetir puzles no duplica
  documentos ni vuelve a ejecutar la animación de recompensa.
- Rocky revisa los tres documentos y abre Bloques. Sigue la presentación del Bug
  del Cuatrimestre y de El Burócrata, sello PENDIENTE, sin combate; la Rectora
  introduce los bloques y la transición al mapa de carreras.
- Cinemáticas obligatorias la primera vez; omitibles en partidas posteriores.

Implementado después del escenario: bienvenida de la Rectora con los cinco textos
del guion, E/Enter manual, bloqueo temporal del control y señal de finalización.
Ver `Scripts/Core/Dialogue/README.md` para reutilización y enlaces con el futuro guardado.

Implementados misión, HUD de documentos y guardado persistente: ver `ENROLLMENT.md`
para integración de recompensas, nueva partida y herramientas exclusivas del Editor.

Archivo con Don Chuy: conversaciones, lectura persistente de cuatro pistas y
acertijo habilitado con las nueve carpetas originales de la página 7 del guion.
Orden inicial fijo y reorganización solo tras tres errores. Ver `ARCHIVE.md`.

Credencialización, carrera con ambos Shift y minimapa de la cuadrícula lógica
implementados. Ver `CREDENTIALIZATION.md` para controles, persistencia y pruebas.

Todavía pendientes: diálogos de los otros NPC, bloqueo inicial de salida hasta hablar con la Rectora,
puzle de Caja y su recompensa, apertura de puertas,
cinemáticas y conexión al mapa de carreras. No se modifica el menú de Chino para
simular esas funciones. Los storyboards visuales no se han localizado como archivos
identificados; el plano original, el guion y los assets existentes guían esta etapa.
