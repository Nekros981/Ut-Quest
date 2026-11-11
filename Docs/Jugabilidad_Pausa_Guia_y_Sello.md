# Pausa, orientación y revisión final de IDGS

La configuración común está en Assets/Resources/Gameplay/CampusQuestGameplayConfig.asset.
El bootstrap instala los componentes de ejecución en Rectoría, los tres
escenarios jugables de IDGS y las dos escenas de SP. No aparece en el menú
principal, pantallas de carga, Game Over ni escenas de prueba.

## Pausa

P, Start del mando o el botón PAUSA abren el menú en cualquier momento de
la partida. Escape también lo abre mientras se explora; dentro de los
minijuegos conserva su función existente de volver. El menú ofrece Reanudar,
Reiniciar, Salir y Menú principal. Se puede usar mouse, teclado y la
navegación del mando que ya utiliza la UI.

La pausa conserva el estado del diálogo o acertijo abierto. Suspende física,
animadores, partículas activas, audio e interacción de las otras interfaces.
CampusQuestGameClock mantiene el tiempo de juego independiente de la escala,
descontando las pausas: la secuencia de memoria, las barras del servidor,
la fotografía, las recompensas y el sello no vencen durante la pausa.

Reiniciar limpia solo los eventos del prefijo del nivel actual mediante
GameProgress.ResetLevel y vuelve a su entrada: Rectoría, IDGS_Exterior o
Sp_Bosque. Conserva eventos de otros niveles, nombre y preferencias. SP
también reinicia sus estados transitorios de combate. Se eliminó el borrado
automático del perfil que antes ocurría al entrar en Play Mode.

Menú principal guarda y vuelve a SampleScene, la pantalla original con
Iniciar. Salir guarda y llama Application.Quit en una compilación; en Editor
detiene Play Mode. Los botones de opciones y niveles de Chino se conservan.

## Retratos y orientación

Los diálogos de Rectoría referencian los sprites frontales ya importados de
Rectora, Don Chuy, Lic. Mony, Cajero Beto y Guardia Rocky. Las notas no tienen
retrato ni texto provisional. Los cuadros conservan proporciones del sprite.

El último mensaje de cada conversación incorpora una indicación calculada
con los documentos, notas e introducciones que realmente constan en el
registro. Don Chuy también confirma la primera entrega del acta mediante un
diálogo propio. Mony y Beto conservan sus mensajes de éxito y reciben el
siguiente paso dinámico. Los trámites siguen admitiendo cualquier orden.

Las flechas naranjas proceden de flechas.png. La ruta usa búsqueda por
casillas transitables: RectoriaGridMap en el tutorial y piso/colisiones
existentes en IDGS. No modifica muros, escenarios ni colliders. Se oculta
durante diálogos, actividades y pausa. El HUD orienta hacia notas, marca de
foto, recibos, Rocky, edificios y estaciones pendientes según corresponda.

## Sello de IDGS

Con los seis eventos de actividad aparece «Dirígete al CMD». Dentro del
edificio se indica salir al campus para recibir el sello. Solo esa salida
con los seis juegos completados registra idgs.cmd.final_review_requested
y permite mostrar la animación en el exterior. Entrar al CMD o cruzar su
salida con menos de seis actividades no entrega sello.

El GIF suministrado tiene nueve cuadros de 256×256 a 130 ms cada uno. Se
convirtió sin redibujar a PNG y se muestra con fondo claro y filtro Point.
Una vez termina, GameProgress.RegisterEvents guarda atómicamente
idgs.seal.awarded e idgs.level.completed. El botón Continuar restaura el
control. Si el guardado falla se permite reintentar; una solicitud pendiente
puede reanudarse al cargar IDGS_Exterior. Después de la entrega, las visitas siguientes
no repiten la animación ni la recompensa.

## Validación

GameplayFinishValidation usa teclado, mouse y mando virtuales sobre las rutas
normales de UI. Sus casos de reinicio se ejecutan exclusivamente en un slot
CampusQuest.Test.IDGSInteriors.*, respaldado por IDGSInteriorValidation.
Comprueba pausa de juego y temporizadores, retratos, siguientes pasos,
rutas, condiciones de puerta/sello, repetición, reinicios por nivel, menú y
salida. El cierre del proceso se intercepta solo en la herramienta de Editor
para verificarlo sin apagar la sesión del usuario. VerifyRestart comprueba
un nuevo Play Mode y Cleanup verifica los datos reales y las preferencias.
Los reportes y las capturas quedan en tmp/gameplay-finish.
