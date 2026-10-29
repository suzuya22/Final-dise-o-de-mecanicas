# La Chiva: guía para agentes

Resumen del proyecto para cualquier agente de IA que trabaje en este repo. Léelo antes de tocar nada.
Si algo de aquí deja de ser cierto, actualiza este archivo en el mismo cambio.

## Qué es el juego

**La Chiva**: horror cósmico "tierno" para PC, un jugador, en tercera persona. El jugador sube a una chiva
(bus de fiesta colombiano) que resulta ser una entidad viva que lo está digiriendo. Para sobrevivir juega
minijuegos de fiesta retorcidos mientras sus amigos desaparecen. Estilo 3D estilizado con colores pastel,
que contrasta con el horror corporal.

- **GDD completo:** `GDD importante/Game-Design-Document-La chiva.docx`. Es la base de todo; ante la duda, manda el GDD.
- **Atributos del jugador:** cordura/vida (cambia el filtro visual y lo grotesco que se ve) y noción del tiempo
  (se degrada tras cada minijuego). Las bebidas restauran cordura.
- **Minijuegos del GDD:** 7 minutos en el cielo, Yo nunca nunca, **Beer pong** (hecho), Sillas musicales,
  Verdad o reto (boss final).
- **Zonas:** piso principal, segundo piso (zona VIP, el Líder) y techo (El Loco). Tres finales según la cordura y las decisiones.

## Técnico

- **Unity 6 (6000.4.5f1)** con **URP 17.4**.
- **Solo el Input System nuevo** (`activeInputHandler: 1`). Usa `Keyboard.current` / `Mouse.current`.
  `Input.GetKey` y `OnMouseDown` **no funcionan**.
- UI con **uGUI + TextMeshPro**. El EventSystem debe usar `InputSystemUIInputModule`.
- Física de Unity 6: `Rigidbody.linearVelocity`, `linearDamping`, `PhysicsMaterial`, `PhysicsMaterialCombine`.
- No hay Python en la máquina. Para leer el GDD (.docx) usa PowerShell con `System.IO.Compression.ZipFile`.
- Ramas: `main`, `diseño` (la de trabajo actual) y `sebas`.

## El usuario y cómo trabajar con él

- Habla **español**. Responde y comenta el código en español; los identificadores van en inglés.
- **Quiere asignar las referencias él mismo en el Inspector.** Expón campos públicos claros (`[Header]`,
  `[Tooltip]`) en vez de buscar objetos automáticamente por nombre o tipo. Los sistemas "mágicos" lo confunden.
- Cuando entregues un script, dile **en qué objeto va y qué arrastrar en cada campo** (una tabla le funciona bien).
  No le llenes la respuesta de solución de problemas que no pidió.
- En tareas grandes suele pedir **el plan antes de implementar**. Si lo pide, espera su visto bueno.
- Antes de escribir en una carpeta, comprueba la estructura actual: el usuario mueve y reorganiza scripts.

## Estructura

```
Assets/
  Scenes/
    Cinematica inicial.unity   Pantalla de inicio (única escena en Build Settings)
    Juego.unity                Zona jugable: personaje, interior de la chiva, beer pong, menú de pausa
    Juego beer pong.unity      Escena de pruebas del beer pong
    Chiva dentro.unity         Casi vacía
  Scripts/
    Intro/IntroCinematic.cs
    Personaje/PlayerMovement.cs
    Personaje/                 SanityManager, SanityBarUI, Interactable, PlayerInteractor, PlayerLookDown,
                               PlayerInventory, BallThrower, PlayerAnimator
    Puzzles/MinigameZone.cs, Puzzles/PlayerTrigger.cs, Puzzles/ZoneTeleport.cs
    Puzzles/pong/              BeerPongManager, PongBall, PongCup, PongLauncher, DrunkCameraEffect
    Puzzles/Sillas/            MusicalChairsGame, MusicalChair, ChairNPC, MashQTE, TimingQTE
    Puzzles/7 minutos/         SevenMinutesPuzzle, EyeWatchSystem, EyePainting, PullableThread, ThreadDoorPuzzle,
                               PuzzleDoor, HuggerBoss, FloorThread, BossWeakPoint, ThrownBall, ClapBeacon,
                               PickupItem, SwingingCardboard
    Interfaz/Scripts para panel de pausa/   PauseMenu, PauseTitleWobble, UIHoverJump
    Dialogos/                  (vacío)
  Editor/                      SevenMinutesSceneBuilder.cs, ProtaAnimatorSetupTool.cs
  Prefabs/Minujuegos/beer pong/Juego beer pong prefab.prefab
  Arte/
    Animaciones/               Prota.fbx (rig + clips Idle, Walk, Run), ProtaController.controller
    Assets para puzzles/
      Pong/                    Vaso.fbx, Pelota.fbx (+ Vasos.fbx, Pelotas.fbx sin usar)
      7 minutos en el cielo/   Base.fbx, Cartones flotantes.fbx, Cuadrosblend.fbx, Final boss.fbx, Piso2.fbx
    Entorno/                   Chiva_adentro.fbx, Chiva_afuera.fbx, Portada.fbx
    Personajes/Protagonist.fbx
    Materiales/{Chiva,Pong}/   Materiales URP Lit
    Texturas/Texturas pong/    Texturas de chiva, pelotas y vasos
GDD importante/                GDD (.docx)
```

## Sistemas existentes

### Cinemática inicial (`Cinematica inicial.unity`)
`IntroCinematic` (en "Controlador de la camara"): al hacer clic en el papel naranja (Portada.fbx, con
MeshCollider) los papeles se hunden en el charco y luego la cámara rota solo en rotación, sin moverse, de
`startRotation` a `endRotation`. Al pasar el mouse por encima, el papel muestra un contorno: una copia del
mesh un poco más grande y sin iluminación, porque los contornos con shader fallan en mallas planas. La
detección del clic es por raycast con el Input System.

### Personaje y zonas de minijuego (`Juego.unity`)
- `PlayerMovement`: CharacterController. Modo `Tanque` (W/S avanza, A/D gira; es el actual) o `Mouse`.
  `SetCanMove(bool)` lo bloquea. La cámara del personaje es hija del Protagonist (tercera persona).
- **Cuidado:** el Protagonist está a escala 0.03, y eso encoge el CharacterController. Lo recomendado es un
  padre `Jugador` con escala 1 que lleve el CharacterController y `PlayerMovement`.
- `MinigameZone`: un trigger que, al entrar el jugador, apaga la cámara del jugador, enciende la del
  minijuego, bloquea al jugador y **habilita los scripts del minijuego** (`Behaviour[] minigameScripts`).
  Escape sale. Para volver a entrar hay que salir de la zona. Activa y desactiva componentes, no objetos,
  porque los vasos son hijos del controlador y desaparecerían.

### Beer pong (`Scripts/Puzzles/pong`)
Reglas del GDD: 7 vasos de colores, una pelota a la vez del color de un vaso que quede, hay que encestar
**4 en el vaso de su color** con 7 pelotas. Si ya no se puede ganar, se reinicia.
- **Modo memoria** (`memoryMode`): antes de cada tiro los vasos muestran su color `revealTime` segundos, luego se
  vuelven todos `hiddenColor` (rojo) y se hacen `shuffleSwaps` intercambios de dos vasos (`PongCup.SlideTo`,
  rodeándose). El lanzador está bloqueado mientras tanto (`PongLauncher.InputLocked`). Al caer la pelota, el vaso
  muestra su color real. `PongCup.ResetCup` devuelve cada vaso a su posición inicial.
- **La borrachera tuerce el tiro:** `PongBall` recibe, mientras vuela (hasta el primer contacto), una fuerza
  lateral con ruido Perlin proporcional a `BeerPongManager.CurrentDrunk` (`drunkDrift`, `drunkDriftFrequency`).
  La parábola no la muestra.
- `BeerPongManager`: reglas, paleta pastel, eventos `onCupHit/onMiss/onWin/onLose` y un HUD de debug por OnGUI.
  Cada enceste sube la borrachera (`drunkPerHit`). `OnEnable` reinicia la partida, así que cada vez que se
  entra a la zona empieza de cero.
- `PongLauncher`: clic y arrastre. Lo vertical da la fuerza, lo horizontal la dirección. El tiro recto
  apunta al centro de los vasos (o a `aimTarget`). Muestra la parábola con un LineRenderer, y el script
  configura siempre su ancho y material.
- `PongBall`: Rigidbody con CCD, rebote por `PhysicsMaterial` y fin del tiro por tiempo, altura o reposo.
- `PongCup`: MeshCollider no convexo más un trigger `ZonaDeteccion` en el fondo. El color se aplica con
  **textura de dos tonos generada en runtime** (cuerpo del color de la paleta, labio e interior blancos),
  vía `MaterialPropertyBlock`.
- `DrunkCameraEffect`: en la cámara del beer pong. Slider `drunkLevel` de 0 a 1 y `SetDrunkLevel()`, pensado
  para conectarlo luego con el sistema de cordura y bebidas.
- Dentro del prefab: `Camara beer pong`, `Game controoler` (manager + launcher, con los vasos como hijos) y `launcher` (punto de lanzamiento).

### Sistemas del jugador (`Scripts/Personaje/`)
- `SanityManager`: cordura (`Drain`, `Restore`, `ResetSanity`, eventos `onSanityChanged(float 0-1)` y `onSanityDepleted`).
  Por ahora solo la usa 7 minutos; la cordura global del juego aún no está diseñada.
- `SanityBarUI`: barra con una Image Filled. `PlayerInventory`: lista de IDs (string).
- `Interactable` (base abstracta, con registro estático `All`) + `PlayerInteractor`: E sobre el más cercano dentro
  de `interactRadius`; soporta mantener pulsado (`holdTime`) y muestra el texto en un TMP opcional.
- `PlayerLookDown`: mantener Ctrl izquierdo inclina la cámara del jugador (`IsLookingDown`).
- `BallThrower`: clic izquierdo lanza una `ThrownBall`; está desactivado salvo en la pelea de 7 minutos.
- `PlayerMovement` expone además `IsMoving`, `IsRunning` (manteniendo Shift al avanzar, con `runSpeed`), `Teleport(pos, rot)` y `AddKnockback(vel)`.
- `PlayerAnimator`: sincroniza `IsMoving` e `IsRunning` de `PlayerMovement` con los parámetros `IsWalking`, `IsRunning` y `Speed` del `Animator` del personaje (`Prota.fbx`). La herramienta de editor *La Chiva > Configurar Animator Controller del Prota* (`ProtaAnimatorSetupTool`) configura el loop y los materiales de `Prota.fbx` y genera `Assets/Arte/Animaciones/ProtaController.controller`.

### 7 minutos en el cielo (`Scripts/Puzzles/7 minutos/`)
Diseño del usuario: hay que cortar 5 hilos (repartidos por la habitación de los ojos, el laberinto de sonido del
piso 2 y la habitación de cartones colgantes) para que la persona del pasillo abra la puerta. Luego viene la pelea.
Si la cordura llega a 0, se reinicia **todo** el puzzle.
- `SevenMinutesPuzzle`: controlador. Escucha `onSanityDepleted` y llama al `ResetState()` de cada parte.
- `EyeWatchSystem` + `EyePainting` (uno en cada uno de los 5 prefabs de cuadro): luz roja/verde. Ciclo
  cerrados → aviso (parpadeo y luz) → abiertos. Con los ojos abiertos se pierde poca cordura si el jugador está
  quieto mirando abajo, y mucha si no. Los ojos se animan escalándolos por código; la pupila gira hacia
  `lookTarget` usando un `frontAxis` configurable.
- `PullableThread` (mantener E) + `ThreadDoorPuzzle` (cuenta los hilos y abre una `PuzzleDoor`).
- Pelea: `HuggerBoss` (se acerca quitando cordura por cercanía, hace amagues de lado y se lanza),
  `FloorThread` (2, uno a cada lado; E lo tensa un momento, y si la persona se lanza hacia su lado, tropieza),
  `BossWeakPoint` (se exponen al caer) y `ThrownBall`. Con 3 golpes se abre la puerta de salida (con luz) y la
  persona se esconde en `hidePoint`. Se inicia con `PlayerTrigger` → `HuggerBoss.StartFight`.
- `ClapBeacon` (aplausos 3D en el laberinto), `PickupItem` (objeto de la familia → inventario) y
  `SwingingCardboard` (péndulo con Rigidbody cinemático; su trigger empuja al jugador).
- Los "grupos de personas seguras" del documento de diseño se descartaron.
- **Escena de pruebas:** `Assets/Editor/SevenMinutesSceneBuilder.cs` añade el menú
  *La Chiva > Crear escena de 7 minutos en el cielo*. Ese menú genera `Assets/Scenes/7 minutos en el cielo.unity` con
  los **FBX reales a escala 0.18**: la persona mide 9.4 unidades y queda en 1.7 m. Todo se coloca leyendo la
  geometría de los FBX por el nombre exacto de cada objeto dentro de su FBX. Volver a ejecutarlo **reemplaza** la escena
  (pide confirmación). Genera: los 5 prefabs de cuadro en `Prefabs/Minujuegos/7 minutos/Cuadros`, la pelota, y una
  malla invertida del pasillo en `.../Generado`.
- Escalas en la escena de pruebas: pisos (`Base`, `Piso2`) y cartones a **0.20** (lo pidió el usuario); pasillo,
  persona y cuadros a 0.18. Los cartones van girados 90° en Y (de frente al pasillo) y se balancean de lado a lado.
- **Protagonista animado** (`Arte/Animaciones/Prota Animado.fbx` + `ProtaController`): sus materiales ya están
  remapeados a `Arte/Texturas/Personaje`. Pero **las UV de ojos y boca de ese FBX no coinciden con sus texturas**:
  `Eyes_2D_View`/`Mouth_2D_View` están pintadas para las UV de `Arte/Personajes/Protagonist.fbx`, que tiene la misma
  geometría. Por eso no se le ve la cara. El tool crea `Generado/Prota cara corregida.asset` copiando esas UV desde
  `Protagonist.fbx`. La solución de raíz sería reexportar el FBX animado con las UV originales.
- **Análisis de los FBX de 7 minutos** (unidades del FBX, Y arriba):
  - `Base.fbx` = primer piso: caja con las caras hacia dentro, escalera (`Cube.001`-`.003`), paredes (`Cube.004`-`.009`),
    cortinas con un hueco donde está la puerta al pasillo, y 3 hilos (`BézierCurve*`).
  - `Piso2.fbx` = segundo piso: caja con las caras hacia dentro, laberinto (`Cube.003`-`.046`), hueco de escalera
    (`Cube.001`), arco (`Cube.002`) y 2 hilos.
  - `Cartones flotantes.fbx` ya está modelado en las coordenadas de `Piso2` (cada cartón con su cable).
  - `Final boss.fbx` = el pasillo entero: caja con las caras **hacia fuera** (por eso se crea una copia invertida),
    `Character_base`, puerta `Cube.001`, aros `Torus*` y 5 hilos de suelo.
  - `Cuadrosblend.fbx` = 5 marcos (`Cube`-`Cube.003`, `Cylinder`) y 9 ojos (`Cube.004`-`.012`).
  - La escalera de `Base` no encaja con el hueco de `Piso2`, así que pisos y pasillo se conectan con `ZoneTeleport`.
- `ZoneTeleport` (transición al entrar o con E, bloqueable) y `CameraCollision` (la cámara no atraviesa paredes).
- `BallThrower` apunta hacia donde se hace clic (raycast desde `aimCamera` y tiro parabólico bajo que compensa la gravedad).

### Sillas musicales (`Scripts/Puzzles/Sillas/`)
Solo lógica; el usuario asigna todo. Asset: `Arte/Assets para puzzles/Sillas/Chair.fbx`, que se importa diminuto
(unos 2 cm) y hay que escalarlo. Los NPCs son instancias del personaje animado sin textura.
- `MusicalChairsGame` (requiere `MashQTE` y `TimingQTE` en el mismo objeto): siempre una silla menos que
  participantes. Música (tiempo al azar) → todos caminan en círculo (radio automático o `walkRadius`); el jugador
  pierde cordura si se sale de la fila (`ringTolerance`) o toca a un NPC → la música para y todos corren a por una
  silla (el jugador con E) → si se sentó: forcejeo `MashQTE` (WASD al azar; si falla, pierde cordura), el NPC sin
  silla queda eliminado y se retira una silla; si solo queda el jugador, gana → si el jugador se queda sin silla:
  lo arrastran al centro, `TimingQTE` (WASD al azar en el momento justo) reduce la penalización y el juego termina
  perdido. Tiene HUD de prueba por OnGUI y TMP/Image opcionales. Se inicia con `StartGame()`.
- `MusicalChair : Interactable` (E para sentarse solo cuando `ChairsOpen`), `ChairNPC` (mueve `IsWalking`,
  `IsRunning` y `Speed` del Animator según su estado, no según el movimiento de cada fotograma).
- Pose sentada: `Arte/Personajes/Prota sentao.fbx` tiene el clip `Armature|Sit down` sobre el mismo esqueleto que
  el Prota Animado. Al sentarse, se apaga el Animator y se aplica el clip con `SampleAnimation`; al levantarse, se
  vuelve a encender. Campos: `ChairNPC.sitClip` y `MusicalChairsGame.playerSitClip` + `playerAnimator`.
  `PlayerAnimator` y `ChairNPC` no tocan el Animator mientras está apagado.
- Al sentarse solo se usa el giro horizontal del `Seat Point` (las esferas de la escena heredan la rotación del FBX
  de la silla); con `seatAtFloorLevel` la posición queda a la altura del suelo del círculo.

### Menú de pausa (`Juego.unity`, objeto "Controlador de la interfaz")
- `PauseMenu`: la tecla P congela el juego (`timeScale = 0`) y el panel entra desde la izquierda.
  Los botones se asignan en campos propios (`continueButton`, etc.) y el script les conecta el `onClick`.
- `PauseTitleWobble`: campo `titleText`. Inclina el título sin voltearlo y le da volumen falso duplicando
  el TMP detrás (`Instantiate` del original; no copies las propiedades a mano, eso lo rompió una vez).
- `UIHoverJump`: uno por opción, con campos `text` e `icon`. En la escena **el texto es hijo del icono**,
  así que el script solo mueve el padre. El hover se detecta por polling del rectángulo original para que no parpadee.
- Todas las animaciones de UI usan `Time.unscaledDeltaTime`, porque el juego está congelado en pausa.

## Assets del vaso (lecciones aprendidas)

- `Vaso.fbx`: 56 polígonos, **un solo material** ("verde"), eje Y arriba. UV divididas por U:
  base exterior `0.125–0.375`, pared exterior `0.375–0.608`, bisel del labio `0.608–0.625`,
  aro e interior `0.625–0.875`.
- El importador remapea "verde" a `Materiales/Pong/M_Vaso_Naranja.mat` (`externalObjects` en `Vaso.fbx.meta`).
- `naranja_Base_color.png`: rojo #D81C1C hasta la columna 1244 de 2048 (U 0.6074) y blanco después.
- `naranja_Mixed_AO.png` **no coincide** con las UV de `Vaso.fbx` (está horneado para otro despliegue). No lo asignes.

## Reglas para no romper cosas

1. **No modifiques los FBX** (ni en Blender ni regenerándolos) salvo que el usuario lo pida expresamente.
   Cambiar los slots de material de `Vaso.fbx` dejó invisibles los vasos del prefab. Para cambiar materiales
   usa el remapeo del importador o un material asignado.
2. **No hagas scripts de editor que busquen objetos por nombre parcial** (`"chiva"`, `"vaso"`…) en las escenas
   abiertas. Uno así sobreescribió los materiales de `Chiva_adentro`. Limita cualquier cambio a referencias explícitas.
3. Trabaja solo en los assets de la tarea. No toques otras escenas o prefabs "de paso".
4. Al editar YAML de Unity (`.unity`, `.prefab`, `.mat`, `.meta`) a mano, conserva los GUID y los fileID.
   No borres ni regeneres archivos `.meta`, porque eso rompe las referencias.
5. No dejes scripts temporales ni archivos de prueba en `Assets/`. Usa una carpeta fuera del proyecto.
6. No puedes ejecutar Unity: di claramente qué no se ha probado y pídele al usuario que lo revise en el editor.
7. Haz commit solo cuando el usuario lo pida.

## Errores conocidos que no son bugs del proyecto

- `SerializedObjectNotCreatableException ... ButtonEditor.OnEnable`: es un fallo del editor cuando el
  Inspector muestra un Button que deja de existir (al darle Play, al recompilar). Es inofensivo; se evita
  deseleccionando antes de darle Play.
