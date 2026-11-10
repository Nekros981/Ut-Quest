# Terreno IDGS · hoja de prueba

`tierraymas.png` conserva el archivo entregado por el usuario desde PixelLab.
SHA256 `c52849118070da16b41a9a5094cccf7928a32b66e163fe6dafbce671b6066ed4`.
Mide 256×256, RGBA8. Tiene 31 361 píxeles transparentes y 34 175 opacos,
sin alfa intermedio. La cuadrícula de 8×8 tiene 61 celdas ocupadas. Las vacías
son F1C1, F3C1 y F3C4. Filas y columnas se cuentan desde arriba a la izquierda.

El importador conserva celdas de 32×32 con pivote al centro, 32 PPU,
Point, Clamp, Full Rect, sin mipmaps, compresión ni cambio de tamaño.
La escena `Assets/Scenes/IDGS_Prueba_Terreno.unity` usa Tilemaps de 1 unidad
por casilla. El fondo violeta revela huecos en el render.

| Pieza | Identificación y resultado |
|---|---|
| F1C5 | Pasto oliva. Área visible de 30×31, con hueco horizontal de 2 px al repetir lado a lado y una textura reconocible que se repite. |
| F2C1 | Concreto liso. Área visible de 30×30. La junta transparente mide 2 px y el contorno claro forma parte del dibujo. F2C4–F2C8 incluyen juntas, desgaste u otros detalles. |
| F3C5–F3C7, F8C5 | Tierra dispersa, manchas y detalle pedregoso. Son candidatos para detalles, no rellenos aprobados. |
| F8C8 | Esquina de tierra de 31×31. Repite sombras oscuras arriba e izquierda y deja juntas de 1 px. |
| F4C1, F4C4 | Unión recta y esquina de pasto/concreto. El verde de las piezas difiere del pasto oliva. |
| F5C1 | Borde pasto/tierra. La mitad derecha de la muestra usa tierra debajo y cubre el hueco interior, pero mantiene juntas y diferencias de color. |
| F7C8 | Diagonal con pasto, concreto y tierra. Al repetir como tramo recto, las diagonales se reinician en cada celda. |

Las filas 4–7 mezclan bordes, esquinas y cruces. Las filas 1 y 8 también
incluyen detalles vegetales o decorativos. Para construir el exterior, pedir
a PixelLab rellenos opacos de 32×32 con bordes repetibles y colores acordes
con las transiciones. El concreto base necesita una variante sin bisel. La
tierra base necesita una variante sin sombra perimetral. Las transiciones
deben compartir perfiles alineados y un juego coherente de rectas y esquinas.
