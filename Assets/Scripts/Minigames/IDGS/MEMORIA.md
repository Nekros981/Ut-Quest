# Secuencia de memoria

La estación este de Docencia abre el panel desde (12,7), mirando al norte,
con E o Enter. Reutiliza IDGSActivityModal para bloquear movimiento,
interacciones y HUD, y restaurarlos al salir mediante Escape o Salir.

Cada partida genera cinco colores. Las rondas reproducen prefijos de una,
dos, tres, cuatro y cinco luces del mismo patrón. Se responde con clics o
1 rojo, 2 azul, 3 verde y 4 amarillo; también funciona el teclado numérico.
Los botones permanecen bloqueados durante la reproducción y esperan que
se suelten las entradas antes del turno del jugador. Los colores llevan
números y etiquetas. Se usa iluminación visual, sin agregar sonidos ajenos
al proyecto. Al fallar se repite el patrón de la ronda actual, conservando
las rondas anteriores. Salir antes de ganar no registra finalización.

Después de cinco aciertos completos se muestra «¡Secuencia restaurada!» y
un breve pulso de las luces. GameProgress.RegisterEvent registra y guarda
idgs.docencia.memoria una sola vez. El rótulo y las luces de la estación
reflejan el evento al cargar; repetir permite jugar sin otro premio.
Si falla el guardado, el panel ofrece reintentarlo.

IDGSMemoryValidation usa un perfil temporal aislado, mouse y teclado del
Input System con procesamiento normal de Unity. Verifica apertura por E y
Enter, entrada bloqueada durante la secuencia, cinco rondas, errores en las
rondas tres y cinco, movimiento bloqueado, cierre, cambio exterior/interior,
repetición sin cambios en el JSON y restauración tras reiniciar Play Mode.
Las capturas y el reporte quedan en tmp/idgs-memory; no se incluyen en Git.
Cleanup verifica que el progreso real y las preferencias sigan intactos,
archiva el perfil temporal y lo elimina. Las herramientas solo compilan
en Editor y no agregan controles de prueba a la partida normal.
