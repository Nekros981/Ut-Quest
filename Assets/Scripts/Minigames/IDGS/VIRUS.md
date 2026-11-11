# Elimina el virus

La estación este del CMD abre el panel desde (12,7), mirando al norte,
mediante E o Enter. Reutiliza onInteracted, IDGSActivityModal y GameProgress.
No añade otro sistema de interacción, misiones o persistencia.

El tablero tiene 4×4 fragmentos. X rojo significa infectado; OK verde oscuro
o cian significa limpio. Cada pulsación cambia la casilla y sus vecinos
ortogonales existentes. No cambia diagonales ni envuelve filas o columnas.
Las flechas seleccionan sin modificar el tablero y E o Enter confirma;
el mouse activa directamente una casilla. El borde dorado identifica la
selección del teclado. Un mismo fotograma acepta una sola pulsación.

IDGSVirusBoard genera desde las 16 casillas limpias aplicando de cuatro a
seis pulsaciones aleatorias distintas. Rechaza todos los estados alcanzables
con cero, una, dos o tres pulsaciones. Un límite de intentos y una secuencia
de respaldo de cuatro esquinas mantienen la generación acotada. Cada partida
es solucionable en cuatro a seis movimientos y comienza sin resolver.
La secuencia de generación solo se expone a la validación dentro del Editor;
el panel no muestra una solución ni pistas que identifiquen las pulsaciones.

Reiniciar o R restaura exactamente el tablero inicial y pone el contador a
cero, sin penalización ni cambios en el guardado. Escape o Salir cancela la
partida; al reabrir se genera otro tablero. No se guardan movimientos parciales.
El movimiento, las otras estaciones y el HUD se suspenden mediante el modal
existente. Al cerrar se restauran sus estados anteriores.

Al limpiar las 16 casillas se muestra «¡Virus eliminado!» y un barrido cian
breve. GameProgress.RegisterEvent registra idgs.cmd.virus una sola vez y
la estación muestra completado. El HUD usa ese evento definitivo, conserva
los otros cinco y llega a 6/6. Repetir permite jugar desde cero sin otro
premio ni reescribir el JSON guardado. Un fallo de escritura ofrece reintento.
El evento reservado antiguo idgs.cmd.virus.completed no se convierte en un
acierto. El sello se conecta en IDGSCompletion: al reunir los seis eventos
se indica ir al CMD; la entrega ocurre al salir por su puerta hacia el campus.

La verificación se divide en:

- IDGSVirusValidation.ModelChecks: 2,000 semillas y una búsqueda independiente
  de distancias mínimas, reversibilidad de los 16 patrones, límites y reinicio.
- IDGSServerValidation.Begin: regresión completa de los otros cinco juegos,
  incluyendo sus controles, finalización, guardado y repetición del servidor.
- IDGSVirusValidation.Begin: mouse, flechas, E/Enter, vecinos, diagonales,
  reinicio por R y botón, cancelación, victoria, repetición, HUD 6/6 y viajes.
- VerifyRestart: restauración real después de parar e iniciar Play Mode.

Las pruebas usan un slot temporal respaldado por IDGSInteriorValidation.
La configuración para recibir entradas sin foco se clona temporalmente y
se restaura al terminar; no se cambian las preferencias del jugador.
Los reportes y capturas se guardan en tmp/idgs-virus, fuera de los assets.
Cleanup archiva y elimina el slot después de verificar que el progreso real
y las preferencias permanecen intactos. Todas las herramientas son de Editor.
