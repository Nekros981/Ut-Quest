# Office Interiors — AI-Ready Collection, v1.0

Origen local aportado por el usuario: `office_tileset_ai_v1.0.zip`.
El QUICKSTART del paquete declara 12,847 fragmentos de 16×16, perspectiva superior
y cuatro láminas. No se ha identificado un archivo de licencia o atribución dentro
del ZIP. Confirmar condiciones de distribución con la fuente del paquete antes de
publicar una versión que incluya estos recursos; no se atribuye aquí una licencia.

Se conservan intactos los píxeles de las dos láminas utilizadas:

- `OfficeObjects_A.png`: `tilesheets/Untitled design (2)_transparent.png`.
- `OfficeObjects_B.png`: `tilesheets/Untitled design (4)_transparent.png`.

`SpriteSelection.json` registra los rectángulos de los objetos completos. `top` se
mide desde el borde superior de la imagen original. Unity importa esos rectángulos
como sprites múltiples, a 16 PPU, Point/Clamp, sin mipmaps ni compresión. No se
importan los miles de fragmentos individuales ni se modifican otros sprites.

La integración afecta solo a la apariencia del mobiliario sobre las casillas que ya
estaban ocupadas. Los objetos no llevan colliders nuevos. Las piezas se ajustan de
tamaño por instancia para corresponder al mobiliario existente. Las ventanas y
relojes son decoraciones de pared; las notas y accesos permanecen en su lugar.

Herramienta: `Campus Quest/Rectoria/6 - Ambientar con Office Tileset`.
