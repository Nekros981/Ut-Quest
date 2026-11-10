# Prueba independiente de vegetación IDGS

Abrir `Assets/Scenes/IDGS_Prueba_Vegetacion.unity` y pulsar Play.
WASD/flechas caminan; ambos Shift corren; R vuelve al punto de inicio.
Es una escena pequeña de evaluación, no el mapa de IDGS ni el controlador definitivo.
No utiliza misión, guardado ni objetos de Rectoría.

## Recursos

- Sprites: `Assets/Sprites/Enviroment/IDGS/Vegetacion/vegeta.png` y `SOURCE.md`.
- Vegetación: `Assets/Prefabs/Enviroment/IDGS/Vegetacion/Arboles`, `Arbustos`,
  `Decoracion`. Tres árboles, dos arbustos y un conjunto de hierbas/plantas.
- Mateo de prueba: `Assets/Prefabs/Characters/IDGS/Mateo_Prueba_Vegetacion.prefab`.
  Conserva sprites, 110 PPU del personaje, escala 1 y Animator existentes.
- Materiales propios: `Assets/Materials/IDGS`.
- Suelo provisional: malla cuadriculada en `Assets/Data/IDGS/PruebaVegetacion`.
  Una cuadrícula de 1 unidad corresponde a un tile de terreno de 32 px. No se
  genera ni importa un tileset de terreno definitivo.

## Árboles y colisiones

Cada raíz está en la base del tronco; los hijos Tronco y Copa tienen escala 1.
Los sprites de vegetación se importan a 32 PPU y sus pivotes respetan la base opaca.
La copa queda a 18 píxeles de altura sobre el origen para unirla al tronco.
Solo la raíz tiene un `BoxCollider2D`: 8×6 píxeles de ancho/alto, desplazado 3
píxeles hacia arriba. La copa y los hijos no llevan colliders. Los arbustos y las
hierbas son transitables en esta prueba.

`IDGSVegetationTestPlayer` usa Rigidbody2D, un collider pequeño en los pies y las
animaciones visuales existentes. Este controlador se añade exclusivamente al
prefab de prueba: no cambia movimiento, escala, animaciones ni escenas de Rectoría.

## Profundidad

`IDGSVegetationYSort` se añade solo a raíces de IDGS con `SortingGroup`.
Su orden es `baseOrder - round(Y × 128)`, determinado por la posición de los pies.
Mateo de prueba participa con otro SortingGroup en el mismo criterio. Su orden
interno 50 no supera la prioridad global del grupo. Tronco y copa se mantienen
juntos; las hierbas se dibujan por debajo del personaje.

No se modifica el driver original de Mateo, las capas globales, el renderer URP
compartido ni los ajustes de ordenación de Rectoría. El componente también permite
previsualizar el orden correcto en el Editor.

## Herramientas y pruebas

`Campus Quest/IDGS/Preparar prueba de vegetación` importa las 64 celdas, preserva
sus identificadores al reimportar y crea los recursos y la escena. Requiere las
escenas actuales guardadas para conservar cualquier trabajo del usuario.

En la escena de prueba, durante Play, ejecutar con Unity-MCP:
`IDGSVegetationValidation.Begin()`.
La herramienta usa un teclado virtual temporal y lo elimina al terminar. Comprueba
los parámetros de importación, los 64 recortes, el vacío sin objeto, la escala 1,
las colisiones reales, rutas alrededor de los tres árboles y el movimiento bajo
la copa. Compara capturas renderizadas con Mateo delante/detrás para verificar
la oclusión real, además de los valores de SortingGroup.

Las capturas manuales refrescan los SortingGroups antes de `Camera.Render` porque
la comparación activa/desactiva renderers varias veces en una misma actualización.
Esto evita usar la agrupación nativa del render anterior al evaluar la oclusión.

Resultados y capturas: `tmp/idgs-vegetation/`, fuera de Assets. No se escribe en
PlayerPrefs ni se prueba sobre el guardado del jugador. Los assets existentes de
Rectoría y Mateo se verifican mediante hashes antes y después.
