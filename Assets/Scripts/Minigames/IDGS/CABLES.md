# Reconecta los cables

La estación oeste de IDGS_Docencia_Interior abre el panel mediante el evento
onInteracted existente. Mateo debe estar en (3,7), mirando al norte.
E o Enter abre la actividad. Se arrastra con el botón izquierdo del mouse.
Escape o Salir cierra y restaura el movimiento y la interfaz del interior.

Los cuatro colores tienen también número y nombre. Los destinos se mezclan
al abrir y ninguno conserva su fila inicial. Las conexiones correctas quedan
fijas. Una suelta incorrecta devuelve el cable al origen y permite intentar
de nuevo. Las conexiones parciales se reinician al cerrar.

Al acertar las cuatro se muestra «¡Conexión restablecida!» y un breve pulso
eléctrico. La primera victoria guarda el evento exacto idgs.docencia.cables
mediante GameProgress.RegisterEvent. La estación actualiza su rótulo y luces.
La repetición mantiene el mensaje de éxito y permite volver a jugar, con
RewardGrantedThisRun en falso y sin otro registro de recompensa.

IDGSCablesChallenge maneja el estado de la actividad y suspende el motor y
el interactor existentes. IDGSCableHandle utiliza los eventos de arrastre de
Unity UI. IDGSCablesGraphic dibuja los cables con geometría de interfaz.
El panel usa el EventSystem y el InputSystemUIInputModule de Unity.

La clave del guardado y su formato continúan perteneciendo a GameProgress.
Las pruebas IDGSCablesValidation usan un slot temporal del validador de
interiores. Comprueban mouse, errores, cancelación, ambas teclas de apertura,
victoria, retorno desde el exterior, reinicio de Play Mode y repetición.
Los reportes y capturas quedan en tmp/idgs-cables.
