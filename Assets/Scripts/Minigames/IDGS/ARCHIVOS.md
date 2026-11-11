# Organiza los archivos y HUD de IDGS

La estación oeste de CMD (aproximación 3,7 mirando al norte) abre el escritorio
con E o Enter. Utiliza onInteracted, IDGSActivityModal y GameProgress existentes.
El panel se dibuja con UI, Pixeloid Sans y referencias serializadas en CMD;
no requiere imágenes nuevas ni otra implementación de guardado.

Arrastrar foto.png y logo.jpg a Imágenes, horario.pdf y reporte.txt a Documentos,
y musica.wav y efecto.mp3 a Audio. Una carpeta incorrecta parpadea en rojo y el
archivo regresa al lugar de origen; soltar fuera tampoco lo clasifica. Los
aciertos permanecen durante la partida. Escape o Salir cancela sin guardar
aciertos parciales; una repetición comienza de nuevo con seis archivos.

Al clasificar los seis, muestra «¡Archivos organizados!» y un breve pulso cian
en las carpetas. Registra idgs.cmd.archivos una sola vez. La estación restaura
su rótulo de completada al cargar. Repetir muestra éxito sin otro premio ni
reescribir el guardado. El panel ofrece reintentar si falla la escritura.

IDGSProgressHud está instalado únicamente en los tres escenarios jugables
IDGS, dentro del HUD existente. No captura entradas; se oculta junto al HUD
al abrir cualquier actividad. Cuenta directamente estos eventos:

| Actividad | Evento |
|---|---|
| Cables | idgs.docencia.cables |
| Circuito | idgs.docencia.circuito |
| Memoria | idgs.docencia.memoria |
| Archivos | idgs.cmd.archivos |
| Servidor (reservado) | idgs.cmd.server.completed |
| Virus (reservado) | idgs.cmd.virus.completed |

No guarda un contador independiente. GameProgress.Changed refresca texto e
indicadores; al cargar se calcula nuevamente. Los eventos antiguos de prueba
como idgs.cmd.files.completed se conservan, pero no cuentan como el nuevo
acierto. Los dos eventos previstos conservan sus identificadores documentados
hasta implementar sus estaciones. Este cambio no otorga todavía el sello.

IDGSFilesValidation usa el perfil aislado respaldado por IDGSInteriorValidation,
mouse/teclado reales del Input System y la ruta EventSystem de arrastre. Prueba
HUD 0–6, escenas IDGS y ajenas, errores, seis aciertos, apertura E/Enter, bloqueo,
transición de salida/entrada, guardado y repetición. VerifyRestart comprueba
un reinicio real de Play Mode. Cleanup verifica perfil real y preferencias,
archiva el JSON temporal y elimina el slot de prueba. Reportes y capturas van
a tmp/idgs-files, fuera de los assets y del repositorio.
