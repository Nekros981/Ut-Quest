# Repara el circuito

La estación central-norte de Docencia abre el tablero mediante onInteracted.
La aproximación sigue en (8,8), mirando al norte. E o Enter abre el panel.
Un clic gira la pieza 90 grados. Las flechas seleccionan otra pieza y E o
Enter la gira. Escape o Salir vuelve a la sala.

IDGSCircuitBoard genera piezas de dos conexiones, rectas o en esquina.
Elige una de seis rutas válidas de cinco o siete casillas entre la entrada
oeste y la salida este, y luego gira las nueve piezas. El tablero inicial
siempre queda sin resolver. La solución conocida se conserva para comprobar
la generación en Editor, y no se muestra en la interfaz del juego.

La energía se propaga solamente cuando las conexiones entre dos vecinas son
recíprocas. La victoria exige alcanzar la pieza de salida y que su puerto
este esté abierto. Las piezas ajenas a la ruta pueden permanecer desconectadas.
La interfaz ilumina la sección energizada y muestra un breve pulso al ganar.

El evento exacto es idgs.docencia.circuito, registrado mediante GameProgress.
La estación actualiza su rótulo y luces al cargar o cambiar el progreso.
Las repeticiones permiten resolver un tablero nuevo y muestran el éxito,
con RewardGrantedThisRun en falso.

IDGSActivityModal concentra la apertura, el cierre y la suspensión/restauración
de controles, HUD y cursor. Cables y Circuito usan ese mismo componente lógico.
El movimiento del personaje, las transiciones y el guardado conservan sus
implementaciones existentes.

IDGSCircuitValidation comprueba 2000 semillas mediante soluciones reversibles,
giros con mouse y teclado, rutas inválidas, victoria, bloqueo de controles,
transiciones, reinicio de Play Mode y repetición. IDGSCablesValidation vuelve
a comprobar el flujo de cables tras compartir el modal. Las pruebas usan un
perfil temporal; los reportes quedan en tmp/idgs-circuit.
