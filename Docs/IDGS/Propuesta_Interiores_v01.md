# IDGS · propuesta de interiores y progreso

Las salas de 16×12 y sus seis estaciones ya tienen una primera versión jugable.
Ver Assets/Scripts/Minigames/IDGS/INTERIORES.md para accesos y validación.
Las actividades quedan pendientes de implementación.

El objetivo de IDGS será completar seis actividades, en cualquier orden,
obtener automáticamente el sello de la carrera y marcar el nivel completado.
El programa de espacios se concentra en los dos laboratorios y sus accesos.

## Distribución común

Cada interior ocuparía **16 × 12 casillas**, incluyendo el perímetro de paredes.
El espacio interior libre de paredes sería de 14 × 10. Cada casilla equivale
a una unidad a 32 PPU. Las coordenadas siguientes empiezan en (0,0) al
suroeste y representan índices de casilla, con el norte arriba. El centro
de una casilla corresponde a (X+0.5,Y+0.5) en unidades de la escena interior.

- Paredes en X=0, X=15, Y=0 y Y=11.
- Puerta sur de dos casillas, (7,0) y (8,0).
- Llegada interior en (8,2), mirando al norte.
- Salida interactuable desde (8,1), mirando al sur y pulsando E o Enter.
- Pasillo de llegada en X=6–9, Y=1–6. Circulación transversal en Y=6–7.
- Tres puestos distribuidos al oeste, centro norte y este. Cada puesto tiene
  su propio frente de interacción y permanece accesible al terminar otro.
- Mobiliario sólido solamente en las huellas indicadas. Los elementos de
  decoración quedan fuera de las casillas de aproximación y circulación.

El tamaño permite ver casi toda la sala con una cámara fija centrada. En
pantallas estrechas se ajustaría el encuadre para mantener la sala visible.
La cámara exterior y sus límites pertenecen al exterior.

## Docencia III · laboratorio de electrónica

Escena propuesta `Assets/Scenes/IDGS_Docencia_Interior.unity`.

| Actividad | Representación | Huella de mobiliario, inclusive | Punto de interacción | Aproximación de Mateo |
|---|---|---|---|---|
| Reconecta los cables | Mesa con cuatro conectores de colores | X=2–4, Y=8–9 | (3,8) | (3,7), hacia norte |
| Repara el circuito | Banco electrónico con placa y herramientas | X=6–9, Y=9–10 | (8,9) | (8,8), hacia norte |
| Secuencia de memoria | Consola con cuatro luces | X=11–13, Y=8–9 | (12,8) | (12,7), hacia norte |

Esquema de zonas, con orientación norte arriba. Los dibujos son esquemáticos;
la tabla define las casillas exactas.

```text
                 NORTE
┌──────────────────────────────────┐
│           CIRCUITO               │
│ CABLES                   MEMORIA │
│   ↑           ↑             ↑    │
│                                  │
│      CIRCULACIÓN TRANSVERSAL      │
│                                  │
│             PASILLO              │
│                                  │
│           LLEGADA (8,2)          │
└───────────── PUERTA ─────────────┘
                  SUR
```

Usaría piso neutro, mesas de trabajo y pequeños acentos de color por estación.
El circuito queda centrado y un poco más al norte para que las tres siluetas
se distingan. Desde la llegada, las tres estaciones están a unos 6–10 pasos
cardinales, sin atravesar mobiliario.

## CMD · laboratorio digital

Escena propuesta `Assets/Scenes/IDGS_CMD_Interior.unity`.

| Actividad | Representación | Huella de mobiliario, inclusive | Punto de interacción | Aproximación de Mateo |
|---|---|---|---|---|
| Organiza los archivos | Escritorio con computadora y bandejas | X=2–4, Y=8–9 | (3,8) | (3,7), hacia norte |
| Calibrar servidor | Rack con consola de tres indicadores | X=6–9, Y=9–10 | (8,9) | (8,8), hacia norte |
| Elimina el virus | Terminal de diagnóstico | X=11–13, Y=8–9 | (12,8) | (12,7), hacia norte |

```text
                 NORTE
┌──────────────────────────────────┐
│           SERVIDOR               │
│ ARCHIVOS                   VIRUS │
│   ↑           ↑             ↑    │
│                                  │
│      CIRCULACIÓN TRANSVERSAL      │
│                                  │
│             PASILLO              │
│                                  │
│           LLEGADA (8,2)          │
└───────────── PUERTA ─────────────┘
                  SUR
```

Mantendría la misma circulación y controles de Docencia para reducir el
aprendizaje. Escritorios, equipos y racks darían al CMD una identidad distinta.
El mobiliario de `objetos.png` ya ofrece computadoras, bandejas y gabinetes
para una primera versión. El rack definitivo se puede representar inicialmente
con un gabinete identificado, sujeto a revisión del arte disponible.

## Accesos desde el exterior

Ambas fachadas están orientadas al sur. Se conservarían sus anclas existentes.
Estas posiciones son unidades del exterior, no índices de casilla interior.

| Edificio | Identificador del acceso | Ancla exterior existente | Llegada interior | Retorno exterior propuesto |
|---|---|---|---|---|
| Docencia III | `idgs.docencia` | (34.5,3.15) | Casilla (8,2), hacia norte | (34.5,2.5), hacia sur |
| CMD | `idgs.cmd` | (43,24.15) | Casilla (8,2), hacia norte | (43,23.5), hacia sur |

La futura interacción de entrada usaría E o Enter frente al acceso. La salida
dejaría a Mateo fuera del umbral, con el movimiento detenido y la pulsación
consumida, para evitar entrar de nuevo inmediatamente. Cambiar de edificio
conservaría el progreso de las seis estaciones.

## Actividades y experiencia propuesta

Las seis estaciones estarían disponibles desde la primera visita a IDGS.
Cada una mostraría su nombre, una instrucción breve y su estado pendiente o
completado. E o Enter abriría la actividad desde su casilla adyacente,
mirando al puesto. El movimiento quedaría suspendido mientras está abierta.

| Actividad | Alcance de la primera versión |
|---|---|
| Reconecta los cables | Cuatro parejas de igual color. Añadir símbolos a los extremos para distinguirlas además del color. |
| Repara el circuito | Piezas giratorias 3×3, con entrada y salida identificadas. Tablero inicial generado a partir de una conexión válida. |
| Secuencia de memoria | Propuesta inicial de tres rondas con patrones de 3, 4 y 5 luces. Un error permite repetir la ronda. |
| Organiza los archivos | Seis archivos con extensión visible, dos por cada categoría Imágenes, Documentos y Audio. |
| Calibrar servidor | Detener tres indicadores. Los aciertos se mantienen al repetir un indicador fallado. |
| Elimina el virus | Tablero 4×4. Cada pulsación cambia su casilla y las vecinas ortogonales. Generar desde el estado resuelto mediante pulsaciones legales. |

Las cantidades de rondas y las ayudas son decisiones propuestas para revisión.
El objetivo de duración sería aproximadamente 30–90 segundos por actividad,
sujeto a pruebas con jugadores. No exigiría un orden entre estaciones.

La interfaz conservaría Pixeloid Sans, paneles oscuros y detalles dorados.
Mostraría **Actividades IDGS 0/6** y seis indicadores, con el sello al completar
el conjunto. Cada actividad ofrecería reiniciar y salir. Al cancelar, el
tablero en curso podría reiniciarse en la siguiente visita; las actividades
ya completadas seguirían guardadas. El requisito persistente sería la
finalización, sin guardar cada movimiento del tablero en esta primera versión.

## Arquitectura existente revisada

- `Assets/Scripts/Core/Dialogue/DialogueSession.cs` mantiene el registro
  idempotente mediante `RegisterEvent`, consulta con `HasCompleted` y publica
  `ProgressChanged`. `CaptureCompletedIds` y `RestoreCompletedIds` permiten
  guardar y restaurar la lista completa de eventos.
- `Assets/Scripts/Minigames/Rectoria/EnrollmentMission.cs` deriva los documentos
  de ese registro. Actualmente contiene también la carga y escritura en
  PlayerPrefs bajo `CampusQuest.Progress.v1`, con JSON de versión 1 y una
  lista `events`. El refactor posterior extrajo la persistencia a `GameProgress`,
  manteniendo los métodos de misión como adaptadores de compatibilidad.
- `DialogueDefinition`, `DialogueView` y `DialogueSession` ya resuelven las
  conversaciones y su presentación. Los minijuegos de Rectoría muestran el
  patrón de suspender movimiento, restaurar la interfaz y condicionar la
  animación al primer premio.
- `RectoriaScenePoint.IsFacing` y `RectoriaDialogueInteractor` dependen del mapa
  y controlador de Rectoría. IDGS necesita un adaptador de interacción para
  su movimiento físico actual, conservando adyacencia, orientación y E/Enter.
- El exterior de IDGS usa `IDGSVegetationTestPlayer`, el Animator existente,
  colliders físicos y `IDGSExteriorCamera`. Las transiciones entre escenas
  y los interiores siguen pendientes.

## Registro de finalización y sello

Los siguientes identificadores son nuevos y propuestos. Usarían el mismo
registro compartido, con `idgs.*` para diferenciarlos de `rectoria.*`.

| Finalización | Evento propuesto |
|---|---|
| Cables | `idgs.docencia.cables` |
| Circuito | `idgs.docencia.circuit.completed` |
| Memoria | `idgs.docencia.memory.completed` |
| Archivos | `idgs.cmd.files.completed` |
| Servidor | `idgs.cmd.server.completed` |
| Virus | `idgs.cmd.virus.completed` |
| Sello | `idgs.seal.awarded` |
| Nivel completado | `idgs.level.completed` |

Al ganar una actividad, `RegisterEvent` devolvería verdadero solamente en
su primera finalización. Una repetición mostraría el éxito y permitiría
seguir jugando. La recompensa y su animación dependerían del primer registro.

El contador se calcularía con los seis identificadores concretos de la tabla.
Cuando existan los seis, se registrarían el sello y el cierre del nivel.
La animación del sello dependería del primer registro de `idgs.seal.awarded`.
Al cargar se volvería a comprobar esa condición para completar un registro
interrumpido entre el sexto éxito y la entrega del sello, sin repetir la
animación de un sello ya registrado. Una restauración sería silenciosa.

`GameProgress` ya separa la persistencia de la presentación de `EnrollmentMission`,
reutilizando el formato y manteniendo una lista compartida entre escenas.
Las sesiones locales de Docencia o CMD usarán sus métodos de registro para
conservar también los eventos del tutorial. Ver
`Assets/Scripts/Core/Dialogue/PROGRESS.md` para la integración.

`TryAwardDocument` seguiría dedicado al acta, credencial y comprobante.
El sello se derivaría de los eventos IDGS. La compatibilidad conservaría los
eventos guardados anteriormente y trataría los identificadores IDGS ausentes
como pendientes. Una partida nueva limpiaría la misma lista de progreso,
manteniendo las preferencias de audio y video. Las pruebas usarían el slot
temporal de Editor existente y una copia segura del guardado.

## Comprobaciones para la siguiente etapa

1. Llegar desde ambas entradas a los tres puestos y regresar al exterior.
2. Completar las seis actividades en órdenes distintos y cambiar de edificio.
3. Cerrar y cargar tras varios éxitos, conservando también los documentos y
   eventos de Rectoría.
4. Repetir cada actividad sin duplicar premios y recibir el sello una sola vez.
5. Cargar un progreso con seis actividades y sin sello, y recuperar el cierre.
6. Iniciar una partida temporal nueva y comprobar el reinicio del progreso.

Las escenas están conectadas al exterior. Las actividades se implementarán
en la siguiente etapa mediante los puntos de interacción preparados.
