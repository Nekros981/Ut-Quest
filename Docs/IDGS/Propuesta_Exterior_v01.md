# Exterior IDGS — propuesta para revisión

Estado: distribución propuesta; no se ha construido una escena exterior.
Referencias: plano1 (funciones y accesos) y plano2 (relaciones espaciales).
No es un levantamiento a escala: se compactan distancias para el recorrido.

## Dimensiones

64 × 52 casillas, a 32 px y 32 PPU: 64 × 52 unidades Unity.
Origen (0,0) al suroeste; norte arriba. Los intervalos de esta tabla son inclusivos.

| Zona | X | Y | Tamaño aproximado |
|---|---|---|---|
| Bosquecito TICS | 3–26 | 37–50 | 24 × 14 |
| Estacionamiento | 6–29 | 24–35 | 24 × 12 |
| CMD | 34–51 | 25–34 | 18 × 10 |
| Docencia | 23–44 | 4–13 | 22 × 10 |
| Pasto noreste | 34–60 | 37–50 | 27 × 14 |
| Pasto este | 54–61 | 18–34 | 8 × 17 |
| Volumen suroeste por identificar | 4–17 | 6–19 | Reserva de 14 × 14 |

El bosquecito utiliza árboles grandes con claros y senderos; los pequeños y
arbustos acompañan jardines y rutas. Las copas pueden rebasar su celda de base.
La reserva suroeste refleja el volumen de las referencias sin atribuirle una
función o interior que todavía no conocemos.

## Rutas y futuras entradas

- Concreto, ancho 3 casillas: acceso norte de Docencia (34,14), tramo hasta
  (34,20), enlace a (32,20), subida a (32,29) y entrada oeste del CMD (33,29).
  Ramal este por Y=20 hasta X=55. Estacionamiento conecta por (30,29).
- Tierra, ancho 2: (25,14) → (21,20) → (23,23) → (30,23) → (32,27).
  Continúa por el borde este del estacionamiento hacia (30,37) y el bosque
  (26,39). Mantiene una alternativa con vegetación sin atravesar edificios.
- Preparar posteriormente identificadores de transición `idgs.docencia` e
  `idgs.cmd`, con punto de retorno exterior y puerta independiente de la fachada.
  Las posiciones de acceso se revisarán al contar con fachadas definitivas.
- El recorrido principal ronda 20–30 casillas entre entradas: unos 5–8 segundos
  caminando a 4 unidades/s, más exploración. No se atribuyen metros reales.

Ver `Propuesta_Exterior_v01.svg`. La aprobación de esta distribución precede
a cualquier construcción del exterior en Unity.

## Recursos pendientes de PixelLab

1. Fachadas y cubiertas de Docencia y CMD, con puertas norte/oeste respectivamente,
   vista compatible con el juego, tamaño nativo a 32 PPU y entradas separables.
2. Terrenos modulares 32×32: pasto con variaciones, concreto y tierra; bordes,
   esquinas, cruces y transiciones entre superficies.
3. Estacionamiento: asfalto, cajones, guarniciones y acceso/rampa. Coches opcionales.
4. Señalética de edificios y TICS; después bancos, luminarias y botes según necesidad.
5. Fachada del volumen suroeste solamente después de confirmar su identidad.

Los árboles y arbustos actuales ya cubren la primera vegetación. Hasta tener
terrenos/fachadas finales, cualquier bloque provisional debe estar identificado.
