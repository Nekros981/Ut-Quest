# Caja · Cajero Beto

## Flujo normal

1. Terminar la bienvenida de la Rectora. No se requieren acta ni credencial.
2. Hablar con Beto desde una casilla cardinal adyacente, mirándolo, con E/Enter.
   Explica el costo de $1,850 y los seis recibos. Abortar el diálogo no desbloquea
   el acertijo: hay que completar la conversación.
3. Acercarse a la mesa existente `recibos`, mirarla y presionar E/Enter.
   No se mueve la mesa ni se cambian sus celdas o colisiones.
4. Seleccionar/deseleccionar recibos únicos de $300, $450, $600, $700, $850 y $1,000.
   La UI muestra total, objetivo, cuánto falta o cuánto se excede en tiempo real.
5. Confirmar explícitamente. Un total incorrecto conserva las elecciones y permite
   seguir intentando sin penalización. Toda selección que sume $1,850 es válida.
6. El éxito concede el comprobante, actualiza el HUD y guarda automáticamente.
   Continuar abre la conversación de Beto:
   «¡Todo en orden! Bienvenido a la UT».

Las dos combinaciones válidas son $850 + $1,000 y $300 + $700 + $850. La lógica no
contiene una lista de respuestas: `ReceiptSelection.IsExact` compara la suma
contra el objetivo. Cada recibo aparece una vez y solo tiene un estado booleano
de selección, por lo que no se puede sumar el mismo recibo repetidamente.

## Controles e interfaz

- Flechas o Tab: mover foco entre seis recibos y Confirmar. Desde la última fila,
  flecha abajo lleva a Confirmar; flecha arriba vuelve a los recibos.
- E/Enter: marcar/quitar el recibo enfocado o activar Confirmar.
- Espacio: marcar/quitar el recibo enfocado. C: confirmar directamente.
- Clic: marcar/quitar recibos y activar Confirmar, Limpiar o Continuar.
- R/Limpiar: quitar todos los recibos. Escape: volver al escenario.

La interfaz usa Pixeloid Sans, paneles oscuros y detalles dorados. Se reutiliza el
sprite de Cajero Beto como retrato. Como no existe un recurso específico de sello,
se dibuja «PAGADO» con UI y se anima brevemente su escala, posición y opacidad.
Esta animación se muestra únicamente al entregar por primera vez el comprobante.

El movimiento y la guía se suspenden/restauran usando las mismas referencias
configuradas en `DialogueSession.suspendWhileOpen` y `hideWhileOpen`.
El minimapa se oculta durante Caja y reaparece al cerrar. No se modifican
movimiento, carrera, Animator, escala, Tilemaps, colisiones ni menú de Chino.

## Registro, documento y guardado

`CashierChallenge` se integra con los componentes existentes:

| Hito | Registro existente |
|---|---|
| Explicación completada | `rectoria.cashier.beto_intro` |
| Comprobante concedido | `rectoria.document.payment_receipt` |
| Despedida completada | `rectoria.cashier.beto_success` |

El documento se concede exclusivamente con
`EnrollmentMission.TryAwardDocument(EnrollmentDocument.PaymentReceipt)`.
Solo su retorno `true` habilita el sello. Repetir mantiene el acertijo disponible,
muestra el éxito y permite hablar con Beto, sin documento adicional ni animación.
La sesión y `EnrollmentMission` siguen siendo los dueños del registro y guardado:
`CampusQuest.Progress.v1` en PlayerPrefs, escrito tras cada hito. No hay un archivo
ni sistema de guardado independiente para Caja. Las elecciones de recibos son
temporales: cada apertura comienza con selección vacía.

Nueva partida borra los hitos, cierra Caja si estaba abierta y libera el control.
Con los tres documentos, el objetivo existente pasa a
«Dirígete a la Puerta de Bloques». Esta etapa no abre la puerta.

## Configuración y pruebas

`Campus Quest/Rectoria/9 - Conectar Caja con Cajero Beto` configura la escena de
arte guardada, fuera de Play Mode. Reutiliza los puntos existentes de Beto y mesa,
el diálogo compartido, el HUD de misión y el minimapa.

La suite explícita `CashierChallengeValidation.Begin()` se ejecuta mediante
Unity-MCP en Play Mode. Está en Assets/Editor y no entra en el juego compilado.
Respaldar primero `EnrollmentMission.SaveKey` y restaurarlo al terminar.
La suite también genera automáticamente un respaldo persistente con nombre único
`tmp/cashier-validation/progress-before-<fecha>.json`, antes de cambiar la partida.
Contiene `hadSave` y `progressJson` para poder restaurar el estado anterior incluso
si el Editor se cierra. No depender únicamente de `SessionState` para el respaldo.
Informe y capturas: `tmp/cashier-validation`, fuera de Assets.

Valida las 64 selecciones en el modelo y en el componente real, ambas respuestas,
errores por defecto y exceso, selección/deselección por teclado y mouse, controles,
adyacencia, mirada, diálogo abortado, Caja como primer documento, sello único,
repetición, HUD, objetivo final de tres documentos y nueva partida durante Caja.
La restauración y repetición tras salir/entrar a Play Mode se comprueban aparte.

## Siguiente etapa: Guardia Rocky

Los tres trámites ya entregan documentos con el mismo contrato. Para cerrar el
tutorial falta conectar la interacción y diálogo de Rocky a la consulta de los
tres documentos, registrar un hito persistente de autorización/apertura, abrir
las celdas de la puerta existente y restaurar ese estado al cargar. Después se
conectan las escenas/cinemáticas del cierre y la transición al mapa de carreras
según el guion aprobado. No se implementa aquí ninguna apertura ni transición.
