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
    Interfaz/Scripts para inventario/       InventoryMenu, InventoryIconBob
    Interfaz/UniqueEventSystem.cs
    Dialogos/                  (vacío)
  Editor/                      SevenMinutesSceneBuilder.cs, ProtaAnimatorSetupTool.cs, JefeAnimatorSetupTool.cs
  Prefabs/Minujuegos/beer pong/Juego beer pong prefab.prefab
  Prefabs/Interfaz/            Pausa.prefab, Inventario.prefab (UI reutilizable entre escenas)
  Arte/
    Animaciones/               Prota.fbx (rig + clips Idle, Walk, Run), ProtaController.controller
    Assets para puzzles/
      Pong/                    Vaso.fbx, Pelota.fbx (+ Vasos.fbx, Pelotas.fbx sin usar)
      7 minutos en el cielo/   Base.fbx, Cartones flotantes.fbx, Cuadrosblend.fbx, Final boss.fbx, Piso2.fbx
      Sillas/                  Chair.fbx
      Jefe/                    Jefe.fbx (rig + 5 clips), Animaciones/ (clips individuales), Materiales/ (4 materiales URP), Texturas/
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
- **La borrachera ya no tuerce el tiro** (se quitó el desvío lateral de la pelota porque no funcionaba bien con la
  parábola); solo afecta a la cámara (`DrunkCameraEffect`). La mezcla de vasos dura `swapDuration` = 1.3 s.
- `BeerPongManager`: reglas, paleta pastel, eventos `onCupHit/onMiss/onWin/onLose` y un HUD de debug por OnGUI.
  Cada enceste sube la borrachera (`drunkPerHit`). `OnEnable` reinicia la partida, así que cada vez que se
  entra a la zona empieza de cero.
- `PongLauncher`: clic y arrastre. Lo vertical da la fuerza, lo horizontal la dirección. El tiro recto
  apunta al centro de los vasos (o a `aimTarget`). Muestra la parábola con un LineRenderer, y el script
  configura siempre su ancho y material. La línea sigue tras el primer choque, con la velocidad rebotada según el
  `bounciness` de la pelota (`visibleBounces` = 1, `maxPointsAfterBounce`), y se corta en el segundo.
- `PongBall`: Rigidbody con CCD, rebote por `PhysicsMaterial` y fin del tiro por tiempo, altura o reposo.
- `PongCup`: MeshCollider no convexo más un trigger `ZonaDeteccion` en el fondo. El color se aplica con
  **textura de dos tonos generada en runtime** (cuerpo del color de la paleta, labio e interior blancos),
  vía `MaterialPropertyBlock`.
- `DrunkCameraEffect`: en la cámara del beer pong. Slider `drunkLevel` de 0 a 1 y `SetDrunkLevel()`, pensado
  para conectarlo luego con el sistema de cordura y bebidas.
- Dentro del prefab: `Camara beer pong`, `Game controoler` (manager + launcher, con los vasos como hijos) y `launcher` (punto de lanzamiento).
- **Facilidad de enceste (lección aprendida):** el trigger de detección original era una caja de 14×9 cm en el *fondo*
  del vaso, y con el aro fino y rebote 0.8 solo contaban los tiros a menos de ~4 cm del eje (el 42 % de los apuntados
  a <5 cm). Ahora `BeerPongManager.catchWidth` (0.13) y `catchAboveRim` (0.04) reconfiguran el trigger de cada vaso
  al empezar (`PongCup.SetCatchZone`: del suelo interior hasta 4 cm sobre el borde): simulado, cuenta el 96 % de los
  tiros a <5 cm y el 64 % de los de 5-10 cm, y se cuenta el vaso equivocado solo en un 2 %. Subir `catchAboveRim`
  facilita más pero cuenta vasos por los que la pelota solo pasa. Al encestar, la pelota baja hasta el fondo del vaso
  (`PongBall.DropInto`) antes de desaparecer. La física del lanzamiento no cambió (parábola a 60°).
- **Mezcla total:** con `shuffleAllCups` (por defecto) cada mezcla reparte los puestos entre TODOS los vasos a la vez
  (permutación sin puntos fijos: nadie se queda donde estaba), con saltito y rodeo distinto por vaso. Apagado = el
  intercambio de dos vasos de antes.
- **Rebotes:** `PongBall` cuenta cada choque con velocidad ≥ `minBounceSpeed` (`BounceCount`) y avisa a
  `BeerPongManager.NotifyBounce`, que muestra el popup "¡REBOTE! xN" (`PongPopupUI`, prefab `Prefabs/Interfaz/PopupsBeerPong.prefab`:
  junto al impacto, crece y cambia de color con cada rebote; hay que asignarle `worldCamera` en la escena) y guarda
  `LastBounceCount` para una futura bonificación. Hoy solo es texto.
- **Shader de pantalla:** `Arte/Shader/RetroDither Effect.shadergraph` (dither Bayer 4x4 + cuantización de color) es un
  `FullScreenPassRendererFeature` en `PC_Renderer`; su material es `Shader Graphs_RetroDither Effect.mat`. Valores
  actuales: `_PixelHeight` 360, `_ColorRes` 48, `_DitherSpread` 0.02. `_PixelHeight` son las filas de píxeles de la
  "grilla": usa divisores enteros de la altura (270, 360, 540) para que cada píxel mida un número exacto de píxeles de
  pantalla (con 420 en 1080p medía 2.57 y se veía una rejilla irregular). Lo que cansaba la vista eran las motas de
  COLOR: se cuantiza cada canal RGB por separado, y con pocos niveles (24) cada canal cruza su umbral en un sitio
  distinto. Con más niveles (48) el patrón casi desaparece y los bordes siguen siendo bloques. Si quieres más "paleta
  retro", baja `_ColorRes` (32 aún deja cuadrícula visible en superficies grises); `_DitherSpread` 0 = sin puntos.
- **Evento de la pirámide:** solo entre tiros (nunca con una pelota en el aire), en `AfterShot`. Probabilidad
  `pyramidChance` (0.5) en la primera pausa posible (desde el tiro 1) y `pyramidChanceGrowth` (+0.25) en cada pausa
  en que no sale, así que sale en la ronda salvo rondas muy cortas. Los 6 vasos más
  cercanos al centro (con `pyramidRows = 3`; los demás se encogen y vuelven) se apilan y pasan a ser `Rigidbody`
  (`PongCup.EnablePhysics`: MeshCollider convexo, trigger de detección apagado). Se lanzan `pyramidBalls` pelotas
  blancas con masa `pyramidBallMass` (0.3; la normal, 0.0027, no tumba nada) y las pelotas del evento no cuentan para
  la ronda. Cada vaso derribado devuelve `sanityPerCup` de cordura (campo `sanity`, hay que asignarlo en la escena);
  derribarla entera da `sanityAllCupsBonus`. Con vaso 0.1 kg y pelota 0.3 kg, simulado con `Physics.Simulate`, caen
  normalmente 2-3 de 6 y casi nunca todos. Ojo: el lanzamiento es una parábola fija a 60°, así que solo llegan a
  la pirámide las potencias de ~3.8 a 5.4 m/s. Al acabar, todo vuelve a su sitio.
- En `Juego.unity` el jugador ahora lleva `SanityManager` y hay una instancia de `CorduraHUD.prefab` para ver la recompensa.

### Prefab del jugador (`Prefabs/Personaje/Jugador.prefab`)
El jugador animado (Prota Animado + `ProtaController`, `PlayerMovement`, `PlayerAnimator`, `SanityManager`,
`PlayerInventory`, `PlayerInteractor`, `PlayerLookDown`, `BallThrower`, `Camara jugador` con `CameraCollision`) es un
prefab sin referencias externas, usado en `Juego` y en `Test de juego de las sillas`. En `Juego` va con **escala 0.6**
en la raíz (el autobús mide 2.1 m de alto y el jugador de 1.6 m no cabía bien; la escala encoge también el
`CharacterController` y la cámara). Si lo pones en otra escena, ajusta esa escala y reconecta lo que apunte al jugador
(`MinigameZone.player/playerCamera`, `BeerPongManager.sanity`, `SanityBarUI.sanity`...). El `Juego.unity` también tiene
el prefab `Inventario` (con `closeWithEscape` apagado, porque Escape ya sale del minijuego) y la pausa suelta.

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
- **Jefe animado:** `Arte/Assets para puzzles/Jefe/Jefe.fbx` (clips Walk_Cycle, Caida_1/2, Levantada/_2) reemplaza al
  modelo viejo. `HuggerBoss.bossAnimator` (opcional) usa los parámetros `IsWalking`, `Fall`, `GetUp`, `FallVariant`;
  el menú *La Chiva > Configurar Animator Controller del Jefe* genera `JefeController`. Los puntos débiles son hijos de
  los huesos `spine2`/`spine3`. Sus texturas no existen aún (el `.blend` apunta a `..\texturas\Jefe\` y no está).
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
- **Dificultad por ronda (campos de `MusicalChairsGame`):** los NPC suben `npcSpeedPerRound` de velocidad por ronda
  (`ChairNPC.speedMultiplier`, tope `maxNpcSpeedMultiplier`); desde `rotateChairsFromRound` (3) las sillas giran como
  un anillo alrededor del centro **solo mientras suena la música** (sentido contrario a quien camina; vuelven a su sitio
  al reiniciar); desde `blackoutFromRound` (2) hay un apagón al azar durante la música (`ChairsBlackout`: parpadeo,
  luces y ambiente casi a cero y una capa negra en un Canvas de orden -2, por debajo del HUD). Dura mínimo 5 s
  (`blackoutDuration`, con `[Min(5)]`) y si hace falta la música se alarga para que quepa.
  Desde `swapFromRound` (3) puede haber un **cambio de lugares**: el jugador y los NPC se reparten los puestos del
  círculo (`SwapPlaces`, todos cambian de sitio) con un parpadeo oscuro corto (`ChairsBlackout.Blink`); puede coincidir
  con un apagón. Los eventos de cada ronda se programan en una línea de tiempo dentro de `GameLoop`.
- **HUD de cordura:** `Prefabs/Interfaz/CorduraHUD.prefab` (cerebro + barra vertical con `SanityBarUI`); en cada escena
  hay que asignar `sanity` a mano. La tecla del QTE sale grande y de color (`keyColor`) en el texto del juego.
- **UI de los QTE:** `ChairsQTEBars` (en el Canvas `Canvas HUD sillas`, orden -1 para quedar bajo la pausa) solo
  muestra lo que calculan `MashQTE` y `TimingQTE`. Las barras laten en pasos de pixel a ~12 fps, más rápido y rojizas
  al acabarse el tiempo (`TimingQTE.PromptTimeLeft` alimenta la urgencia), y dan un golpe con cada pulsación.
  Forcejeo = barra que se rellena; tiempo = marca sobre una zona rosa
  (`Area to hit`). Elige al azar una de las 6 barras de `Arte/UI/UI/Minigame Musical Chairs/Bars` en cada QTE (todas
  miden lo mismo: 176x18 px dibujados dentro de 256x256, de ahí `contentLeft/Width/Height`). El mensaje va en
  `MusicalChairsGame.hudText`; `showDebugHUD` está apagado.
- **Seat Point de cada silla:** su eje Z debe apuntar igual que el frente de la silla (el modelo mira hacia su propio
  `forward`, que en el círculo es hacia fuera) y su posición debe quedar en el centro del asiento (el asiento mide
  0.5x0.5 y está centrado en el pivote de la silla). Las esferas son marcadores: su `SphereCollider` está **desactivado**
  porque el `CharacterController` del jugador chocaba con ella y lo expulsaba medio metro hacia atrás al sentarse.
- Al sentarse solo se usa el giro horizontal del `Seat Point` (las esferas de la escena heredan la rotación del FBX
  de la silla); con `seatAtFloorLevel` la posición queda a la altura del suelo del círculo.

### Menú de pausa (`Juego.unity`, objeto "Controlador de la interfaz")
- `PauseMenu`: la tecla P congela el juego (`timeScale = 0`) y el panel entra desde la izquierda.
  Los botones se asignan en campos propios (`continueButton`, etc.) y el script les conecta el `onClick`.
- `PauseTitleWobble`: campo `titleText`. Inclina el título sin voltearlo y le da volumen falso duplicando
  el TMP detrás (`Instantiate` del original; no copies las propiedades a mano, eso lo rompió una vez).
- `PauseTitleWobble` también sirve para mensajes que cambian (se usa en el mensaje de las sillas): `syncText` copia el
  texto a las capas de profundidad (sin etiquetas de color) y `popOnMessageChange` hace rebotar el texto cuando cambia
  su primera línea. Ponlo en un objeto APARTE del texto (el script duplica el objeto del texto al empezar).
- `UIHoverJump`: uno por opción, con campos `text` e `icon`. En la escena **el texto es hijo del icono**,
  así que el script solo mueve el padre. El hover se detecta por polling del rectángulo original para que no parpadee.
- Todas las animaciones de UI usan `Time.unscaledDeltaTime`, porque el juego está congelado en pausa.
- **Prefab `Prefabs/Interfaz/Pausa.prefab`** (Canvas + panel + `EventSystem` con `UniqueEventSystem`): se arrastra a
  cualquier escena. Sin EventSystem los botones no reciben clics; el del prefab se elimina solo si la escena ya trae
  uno. Está aplicado en `7 minutos en el cielo` y `Test de juego de las sillas`; `Juego.unity` aún tiene su copia suelta.

### Inventario (`Prefabs/Interfaz/Inventario.prefab`, solo visual)
- Canvas propio (orden 50, escala 1920x1080). El diseño es la captura `Arte/UI/UI/Inventory/Inventory Guide.png`
  (320x180) a escala 6: cada sprite va a 1:1 en una capa `Diseno 320x180` con `localScale = 6`.
- `InventoryMenu`: tecla I abre/cierra (Escape cierra), congela el juego y libera el cursor. Entra deslizándose con
  rebote, igual que el pausa. Campos de escena (no caben en el prefab): `pauseMenu` (opcional; no abre en pausa y
  oculta el triángulo mientras hay pausa) y `hideWhileOpen` (objetos a ocultar, p. ej. el HUD de cordura).
- El triángulo (`pointer`) está fuera del panel, siempre visible: cerrado en el borde izquierdo mirando a la derecha;
  abierto viaja con el panel y se voltea (escala X -1) mirando hacia dentro.
- `InventoryIconBob`: sube y baja "cortado" (estilo retro: pocos fps y píxel entero) en todos los elementos, con `phase`.
- El hover por slot se descartó: los slots vienen dibujados dentro del sprite. Las barras de vida/cordura son decoración.

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
