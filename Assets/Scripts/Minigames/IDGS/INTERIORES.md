# Interiores jugables IDGS

Escenas IDGS_Docencia_Interior e IDGS_CMD_Interior, ambas de 16×12 casillas.
WASD o flechas caminan con interpolación por casillas. Ambos Shift corren.
Se conservan las velocidades de 4 y 6 unidades/s y el Animator existente.

## Transiciones

Los índices de casilla se convierten a posiciones Unity sumando (0.5,0.5).

| Acceso exterior | Casilla de aproximación | Mirar | Retorno al exterior |
|---|---|---|---|
| Docencia III | (36,3) | Norte | (36,2), hacia sur |
| CMD | (43,24) | Norte | (43,23), hacia sur |

E o Enter entra. Ambos interiores reciben a Mateo en (8,2), mirando al norte.
La salida se activa desde (8,1), mirando al sur. Hay que soltar la confirmación
antes de activar otro acceso para evitar transiciones repetidas.

## Estaciones y recursos

Las estaciones usan las aproximaciones (3,7), (8,8) y (12,7), mirando al norte.
IDGSInteractionPoint ofrece un identificador y onInteracted para conectar cada
actividad. Actualmente muestra «Próximamente». Una victoria futura registrará
pointId + ".completed" mediante GameProgress.RegisterEvent. La interacción de
esta etapa no registra éxitos ni premios.

Cada escena separa Cuadrícula, Colisiones, Estaciones y mobiliario auxiliar.
Se reutilizan objetos de Rectoría mediante copias propias de IDGS. Los objetos
conservan 16 PPU y las superficies usan 32 PPU, con Point y sin mipmaps ni
compresión. El atlas corrige márgenes transparentes de las piezas originales.
Pizarrón, paneles y rack conservan identificación provisional.

IDGSPlayerMotor controla las escenas jugables de IDGS. La prueba de vegetación
conserva su controlador. IDGSExteriorCamera adapta la vista a los límites de
cada sala. EditorBuildSettings añade las tres escenas IDGS conservando las
entradas anteriores.

IDGSSceneTravel transporta una solicitud temporal de llegada. El progreso
continúa perteneciendo exclusivamente a GameProgress.

IDGSInteriorValidation prueba el ciclo completo mediante teclado y física en
un slot aislado, y comprueba estaciones, carrera, colisiones, orientación,
cámara y conservación del perfil. Los reportes quedan en tmp/idgs-interiors.
