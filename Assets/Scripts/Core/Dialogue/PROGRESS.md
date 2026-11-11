# Progreso compartido

`GameProgress` es el único dueño del registro de eventos y del guardado.
Su estado permanece en memoria entre escenas. `DialogueSession` conserva su
API y reenvía las consultas, registros y notificaciones al perfil compartido.
`EnrollmentMission` deriva el HUD y los documentos de ese mismo registro.
Sus métodos públicos de guardado, restauración y partida nueva siguen
disponibles como adaptadores de compatibilidad.

## Datos y compatibilidad

La clave principal sigue siendo `CampusQuest.Progress.v1`. El JSON conserva
`version: 1` y `events`, y añade `playerName` como campo opcional. Los JSON
anteriores se leen sin escribirlos ni exigir una conversión previa. Todos los
identificadores se conservan, incluidos los narrativos desconocidos para la
escena actual. Los eventos se ordenan y deduplican al guardar.

Antes de reemplazar un valor válido se conserva su copia exacta en
`CampusQuest.Progress.v1.backup`. La primera escritura de un perfil antiguo
también conserva su original en `.legacy-backup`. Al recuperar un principal
ilegible desde un respaldo válido, la lectura deja intacto el principal.
La siguiente escritura lo conserva en `.unreadable` antes de reemplazarlo.
Si no hay un guardado válido, la carga informa del error y bloquea los
registros y guardados automáticos. La partida nueva es una acción explícita.

## Integración de IDGS

- Consultar `GameProgress.HasEvent(id)` o `session.HasCompleted(id)`.
- Registrar éxitos con `GameProgress.RegisterEvent(id)` o `session.RegisterEvent(id)`.
  Solo devuelve verdadero si el evento es nuevo y se guardó correctamente.
  Condicionar la recompensa y su animación a ese resultado.
- Usar los identificadores `idgs.*` definidos en la propuesta de interiores.
  El sistema almacena esos eventos. La condición de seis actividades y la
  entrega automática del sello pertenecen a la futura misión IDGS.
- Cambiar el nombre con `GameProgress.SetPlayerName(value)` o el método
  existente `DialoguePlayerContext.SetPlayerName(value)`. Los contextos nuevos
  resuelven el nombre compartido y usan «Estudiante» cuando está vacío.
- `GameProgress.StartNewGame()` limpia eventos y nombre y avisa a las sesiones
  para cerrar las conversaciones. Guarda el perfil vacío y conserva el anterior
  como respaldo. No toca las claves de opciones de Chino.
- `RestoreCompletedIds` reemplaza explícitamente el perfil completo. No usarlo
  para registrar el subconjunto de eventos de una escena. Al entrar a una
  escena se usa `EnsureLoaded`, que conserva el perfil ya cargado.

Los cambios se guardan inmediatamente. Cada escena puede tener su propia
sesión y UI de diálogo. Las sesiones no contienen copias locales del registro.
Una escena sin `EnrollmentMission` ni `DialogueSession` puede usar directamente
`GameProgress` y la carga se realiza en el primer acceso.

## Pruebas de Editor

Se conserva la selección temporal de slot mediante
`CampusQuest.Progress.EditorTestKey`, con prefijo obligatorio `CampusQuest.Test.`.
Los scripts anteriores pueden seguir usando los alias de `EnrollmentMission`.
El resultado del refactor se comprobó con `SharedProgressValidation` mediante
Unity-MCP, en una copia temporal con formato antiguo, cambios reales de escena,
reinicio de Play Mode, recompensas repetidas, nombre, partida nueva y recuperación
de respaldos. Las escenas del juego no incorporan herramientas de pruebas.
