# Vegetación IDGS · PixelLab

Original proporcionado por el equipo: `vegeta.png`, generado en PixelLab.
SHA-256: `6c44ef5486e85a4ff750833673d17fb3a6b6ad3eeaaf4eccfc46881020bc1853`.
La copia se conserva sin editar ni reescalar: PNG RGBA 256×256, alfa 0/255.

Importación: Multiple, 32 PPU, Point, Clamp, sin mipmaps ni compresión.
64 rectángulos de 32×32 en cuadrícula 8×8. Los nombres `R01_C01`…`R08_C08`
cuentan desde arriba a la izquierda. Unity almacena los rectángulos desde abajo:
`x = columna × 32`, `y = 256 - (fila + 1) × 32`.

`Vegetacion_R08_C08_Vacia` se conserva como recorte vacío, sin prefab ni objeto de
escena. Se evitan cortes automáticos por componentes alfa para no fragmentar las
plantas y conjuntos de hojas. Los pivotes están en el borde inferior del dibujo
opaco, manteniendo el rectángulo completo. Todos los sprites usan escala 1.

| Prefab | Copa/follaje | Tronco | Configuración |
|---|---|---|---|
| Arbol_Redondo | R01_C01 | R03_C01 | Copa a 18/32 u sobre la base |
| Arbol_Frondoso | R02_C01 | R03_C02 | Copa a 18/32 u sobre la base |
| Arbol_Amarillo | R01_C04 | R03_C04 | Copa a 18/32 u sobre la base |
| Arbusto_Verde | R04_C01 | — | Decorativo, transitable |
| Arbusto_Dorado | R05_C04 | — | Decorativo, transitable |
| Hierbas_Y_Plantas | R07_C01, R07_C05, R06_C03 | — | Grupo decorativo transitable |

La superposición entre copa y tronco une las piezas sin alterar su escala ni su
proporción. La silueta compuesta mide aproximadamente 48 píxeles opacos de alto:
1.5 unidades/celdas; su rectángulo completo mide 50 píxeles por el margen alfa.
Las copas tienen unos 27–30 píxeles opacos de ancho. Son árboles compactos; para
árboles adultos más anchos conviene producir arte de mayor resolución nativa.

Prefabs: `Assets/Prefabs/Enviroment/IDGS/Vegetacion/`.
Escena de evaluación: `Assets/Scenes/IDGS_Prueba_Vegetacion.unity`.
