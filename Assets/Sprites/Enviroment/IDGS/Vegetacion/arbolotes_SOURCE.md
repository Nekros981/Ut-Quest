# Árboles grandes IDGS

Fuente: arbolotes.png, proporcionado por el usuario y generado en PixelLab.
PNG original sin editar: 256×256 RGBA8, alfa binario (0/255).
SHA256: dc140a22abb2a8c6aa21e6b476326cdfb621463e947fcf20b2ce4b3ea203068f

Cuatro componentes opacos independientes; 26,623 píxeles opacos y 38,913 transparentes.
Separación central mínima: 24 px horizontal y 18 px vertical.
Rectángulos ajustados, coordenadas desde esquina superior izquierda del PNG:

| Árbol | X | Y | Ancho | Alto |
|---|---|---|---|---|
| Redondo | 14 | 8 | 100 | 112 |
| Estratificado | 138 | 14 | 109 | 105 |
| Irregular | 17 | 138 | 96 | 109 |
| Dorado | 139 | 138 | 108 | 109 |

Cada árbol es un sprite completo, sin cortar la copa en tiles. Importación:
Multiple, 32 PPU, Point, Clamp, sin mipmaps ni compresión, FullRect, escala 1.
Altura nativa 3.28–3.5 unidades; anchura 3–3.41 unidades.
Pivote al pie, centrado en la huella opaca de las ocho filas inferiores.
Collider independiente de 14–22 × 10 px alrededor del pie; copa transitable.
Ordenación existente IDGSVegetationYSort en la raíz con SortingGroup.

Prefabs: Prefabs/Enviroment/IDGS/Vegetacion/ArbolesGrandes.
Escena: IDGS_Prueba_Vegetacion. Integrador explícito de Editor:
Campus Quest/IDGS/Integrar árboles grandes en la comparación.
