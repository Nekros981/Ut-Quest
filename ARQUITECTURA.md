# Arquitectura de Ut-Quest

Este archivo explica cómo está organizado el proyecto, qué fuente usa la interfaz y qué colores hay que respetar. Sirve para cualquier persona o agente que vaya a agregar contenido.

## Cómo está organizada

El proyecto usa una arquitectura **feature-driven, modular por dominio**.

En la práctica funciona en dos niveles:

1. Primero, por **tipo de archivo**. Unity espera las animaciones juntas, los sprites juntos y los scripts juntos.
2. Después, por **dominio**. Un dominio es una parte del juego: el jugador, un minijuego, la interfaz, un jefe.

Un sprite de Mateo no vive junto a su script. El dibujo va en `Assets/Sprites/Characters/Mateo` y el código que lo mueve va en `Assets/Scripts/Player`. Los dos pertenecen al mismo dominio (el personaje), pero cada uno está en la carpeta de su tipo.

Al crear algo nuevo, elige el tipo y luego el dominio. No abras una carpeta suelta en la raíz de `Assets` para un minijuego.

## Carpetas de Assets

| Carpeta | Qué va aquí |
|---|---|
| `Animations` | Clips y controladores. Dentro: `Player`, `Bosses`, `UI`. Mateo está en `Animations/Player/Mateo`. |
| `Audio` | El mixer (`MainMixer.mixer`), la música en `Musica` y los efectos en `SFX`. |
| `Data` | Datos que no son dibujos: `Dialogs` para diálogos e `Items` para la información de objetos. |
| `Editor` | Herramientas que solo corren dentro del editor de Unity. No forman parte del juego. Aquí están el generador del menú de opciones y las herramientas de Rectoría. |
| `Fonts` | La fuente del juego. Ver la sección de fuente. |
| `Inputs` | Reservada para assets de input propios del juego. Las acciones actuales están en `Settings/InputSystem_Actions.inputactions`. |
| `Materials` | Materiales compartidos que no pertenecen a un escenario concreto. |
| `Prefabs` | Prefabs reutilizables, separados en `Characters`, `Bosses`, `Enviroment` y `UI`. |
| `Scenes` | Escenas jugables. `SampleScene` es el menú principal. `Rectoria_Tutorial_Blockout` y `Rectoria_Tutorial_Arte_v01` son el tutorial de Rectoría. |
| `Scripts` | Código del juego, separado por dominio. Ver la tabla de abajo. |
| `Settings` | Configuración de Unity: render URP 2D, volumen e input. |
| `Sprites` | Dibujos. Dentro: `Characters`, `Bosses`, `Enviroment`, `Items`, `UI` y `MainMenu`. |
| `UI` | Piezas gráficas compartidas de la interfaz, como las puntas y la barra de los sliders. |
| `TextMesh Pro` | Recursos que trae TextMesh Pro. No se guardan aquí las fuentes del juego. |
| `Plugins` | Código de terceros. No es contenido del juego. |

La carpeta de escenario se llama `Enviroment` (sin la segunda n). Hay que usar ese nombre para que los archivos coincidan con lo que ya existe.

## Dominios de código

Todo el código de juego está en `Assets/Scripts`.

| Carpeta | Qué va aquí |
|---|---|
| `Core` | Sistemas compartidos por varias partes del juego. Todavía está vacía. |
| `Player` | El personaje jugable. Hoy: el driver visual de Mateo (`CampusQuestMateoVisualDriver`), que elige idle o caminar y la dirección. |
| `MainMenu` | La secuencia de entrada: splash del logo, splash del estudio y aparición del menú. |
| `UI` | Ventanas e interfaz reutilizable. Hoy: el menú de opciones (controles, audio, pantalla e iluminación). |
| `Audio` | Código de sonido. Hoy: la música en loop del menú, conectada al grupo `Music` del mixer. |
| `Minigames/Rectoria` | El minijuego de Rectoría: mapa de casillas, cámara que sigue al jugador y control de prueba. El mapa de texto está en `Maps`. |
| `Minigames/Gastro` | Reservada para el minijuego de gastronomía. |
| `Minigames/IDGS` | Reservada para el minijuego de IDGS. |
| `Minigames/Seguridad` | Reservada para el minijuego de seguridad. |
| `Bosses` | Lógica de jefes. Todavía está vacía. |
| `Networking` | Código de red. Todavía está vacía. |

El código de Rectoría usa el namespace `CampusQuest.Rectoria`. El resto de los scripts todavía está en el namespace global.

En Rectoría, si una casilla se puede pisar se decide con los tilemaps (`RectoriaGridMap`), no con el color del sprite.

## Fuente

La fuente del juego es **Pixeloid Sans**.

- Archivo: `Assets/Fonts/PixeloidSans.ttf`
- Asset de TextMesh Pro: `Assets/Fonts/PixeloidSans SDF.asset`

El menú y la ventana de opciones usan ese asset. El texto de la interfaz va en blanco.

`LiberationSans`, dentro de `Assets/TextMesh Pro`, es la fuente por defecto del paquete. Solo se usa como respaldo si el asset de Pixeloid Sans no se puede cargar. No es la fuente del juego.

## Colores

Hay dos paletas distintas. La de la interfaz es la que hay que repetir en menús y botones. La del blockout solo sirve para los cuadros provisionales de Rectoría.

### Interfaz

Definidos en `Assets/Editor/OptionsWindowBuilder.cs` y en la escena del menú (`Assets/Scenes/SampleScene.unity`).

| Nombre | Hex | RGB 0–1 | Dónde se usa |
|---|---|---|---|
| Dorado | `#E9D8A6` | `0.914, 0.847, 0.651` | Botones del menú, pestañas, borde dorado y el botón Volver. |
| Dorado de controles | `#E8D9A6` | `0.91, 0.85, 0.65` | Relleno, puntas y mango de los sliders, y la paloma del interruptor. Es el mismo dorado, escrito en código. |
| Relleno de botón | `#1B202D` | `0.106, 0.125, 0.176` | Interior de los botones (`Relleno`). |
| Fondo de panel | `#1A181E` | `0.102, 0.094, 0.118` | Fondo oscuro de los paneles de opciones (`Fondo_Oscuro`). |
| Riel | `#2E2E33` | `0.18, 0.18, 0.2` | Carril del slider y fondo del interruptor de pantalla completa. |
| Texto | `#FFFFFF` | `1, 1, 1` | Letras de botones, pestañas y etiquetas. |
| Negro | `#000000` | `0, 0, 0` | Fondos negros del splash. |
| Velado | `#000000` al 75 % | `0, 0, 0, 0.75` | Capa que oscurece el menú cuando las opciones están abiertas. |

El marco dorado del menú también existe como sprite: `Assets/Sprites/MainMenu/Marco_Dorado.png`.

### Blockout de Rectoría

Estos colores salen de `RectoriaBlockoutGenerator` y `RectoriaTilemapRepair`. Son cuadros de un solo color para probar el mapa. No se usan en el menú. El arte final está en `Assets/Sprites/Enviroment/Rectoria/ArtV1`.

| Uso | Hex | RGB 0–255 |
|---|---|---|
| Piso | `#CECBAF` | `206, 203, 175` |
| Muro | `#4D5364` | `77, 83, 100` |
| Obstáculo | `#936545` | `147, 101, 69` |
| Puerta cerrada | `#C73C48` | `199, 60, 72` |
| Indicador | `#51AFC7` | `81, 175, 199` |
| Mateo provisional | `#3C9E6C` | `60, 158, 108` |
| Fondo de cámara | `#1C2129` | `28, 33, 41` |
