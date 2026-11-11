# Decoración exterior de IDGS

`objetos_campus.png` es una copia sin editar de `objetos.png`, entregado por
el usuario en Descargas. El importador de esta copia selecciona la banca
(columna 4, fila 7), papelera (columna 7, fila 5) y maceta (columna 1, fila 6).
Filas y columnas se cuentan desde la esquina superior izquierda, empezando en 1.

Usa sprites de 32×32, 32 PPU, escala 1, Point, sin mipmaps ni compresión.
Los pivotes se sitúan en el pie de cada objeto. Los prefabs tienen un collider
pequeño en la base y reutilizan `IDGSVegetationYSort`.

Los prefabs en `Prefabs/Enviroment/IDGS/Vegetacion/Detalles` también reutilizan
las filas 4–8 de `vegeta.png` para arbustos, helechos, flores, hongos,
hierbas secas, hojas, ramas y troncos caídos. Las plantas decorativas y la
hojarasca son transitables. La hojarasca se dibuja sobre el suelo.

En `IDGS_Exterior`, el grupo `Detalle ambiental` organiza los elementos por
senderos, sotobosque, pasto, jardines de acceso y mobiliario.
