# Calibrar servidor

La estación central-norte de CMD abre la actividad mediante onInteracted.
Mateo se aproxima a (8,8), mirando al norte, y confirma con E o Enter.
IDGSActivityModal suspende el motor, las otras interacciones y el HUD,
y restaura sus estados al salir con Escape o el botón Salir.

Los tres módulos avanzan de izquierda a derecha, de uno en uno. Espacio o
el botón Calibrar detiene el indicador blanco. La zona válida incluye ambos
extremos normalizados 0.56 y 0.84. Las velocidades son 0.26, 0.32 y 0.38
barras por segundo: ofrecen aproximadamente 1.08, 0.88 y 0.74 segundos de
acierto. El muestreo se realiza al confirmar usando tiempo no escalado;
la victoria no depende de detectar un cruce entre dos fotogramas.

Al fallar hay un aviso breve de 0.7 segundos y el módulo vuelve al inicio.
Los anteriores permanecen calibrados. Una preparación de 0.55 segundos
separa los módulos. La tecla mantenida no dispara nuevos intentos, y hay
una protección contra confirmaciones duplicadas en el mismo fotograma.
No hay límite de intentos, tiempo ni penalización.

Al completar los tres se muestra «¡Servidor calibrado!» y un pulso breve en
los paneles. GameProgress.RegisterEvent registra idgs.cmd.servidor una sola
vez. La estación restaura su estado de completada al cargar y el HUD cuenta
el evento directamente. Repetir comienza desde cero módulos, permite jugar
y mostrar éxito sin otra recompensa ni reescribir el progreso. Un fallo de
escritura permite reintentar el guardado desde el panel.

El identificador provisional idgs.cmd.server.completed ya no es la fuente
del HUD para esta estación. No se transforman eventos de prueba en victorias.
Los documentos de Rectoría, las preferencias y el registro de otros juegos
siguen perteneciendo a los sistemas existentes.

IDGSServerValidation prueba el juego con mouse y teclado normales del Input
System en un perfil temporal aislado. Incluye aciertos y errores en los tres
módulos, apertura E/Enter, guardado, repetición, transiciones, HUD, reinicio
de Play Mode y una regresión completa de Archivos, Cables, Circuito y Memoria.
El reporte y las capturas se generan en tmp/idgs-server. La limpieza usa la
verificación de perfiles y preferencias de IDGSInteriorValidation.
