using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Controla las reglas del beer pong (según el GDD):
// 7 vasos de colores, una pelota a la vez del color de uno de los vasos,
// hay que encestar X veces en el vaso del mismo color para ganar; si no, se reinicia.
public class BeerPongManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Prefab de la pelota (con el script PongBall).")]
    public PongBall ballPrefab;
    public PongLauncher launcher;
    [Tooltip("Si se deja vacío, se usan los PongCup que sean hijos de este objeto.")]
    public List<PongCup> cups = new List<PongCup>();

    [Header("Facilidad de enceste")]
    [Tooltip("Ancho (en metros) de la zona donde la pelota cuenta como encestada. Más ancho = más fácil. " +
             "La boca del vaso mide ~0.26. Con 0.13 cuentan el 96 % de los tiros a menos de 5 cm del centro (antes, 42 %).")]
    public float catchWidth = 0.13f;
    [Tooltip("Cuánto por encima del borde del vaso empieza a contar (metros). Más alto = más fácil, pero la pelota " +
             "puede contar en un vaso por el que solo pasa de largo.")]
    public float catchAboveRim = 0.04f;

    [Header("Colores")]
    [Tooltip("Si está activo, cada vaso recibe el color de la paleta según su orden en la lista.")]
    public bool assignColorsToCups = true;
    public Color[] palette =
    {
        new Color(1f, 0.55f, 0.6f),   // rosa
        new Color(1f, 0.75f, 0.5f),   // naranja
        new Color(1f, 0.95f, 0.55f),  // amarillo
        new Color(0.6f, 0.95f, 0.6f), // verde
        new Color(0.55f, 0.9f, 1f),   // celeste
        new Color(0.6f, 0.65f, 1f),   // azul
        new Color(0.85f, 0.6f, 1f),   // morado
    };

    [Header("Reglas")]
    public int hitsToWin = 4;
    public int ballsPerRound = 7;
    [Tooltip("Espera entre un tiro y la siguiente pelota.")]
    public float nextBallDelay = 0.8f;
    [Tooltip("Espera antes de reiniciar al perder.")]
    public float restartDelay = 2f;
    public bool autoRestartOnLose = true;

    [Header("Borrachera")]
    [Tooltip("Efecto de borracho de la cámara. Si está vacío se busca en la Main Camera.")]
    public DrunkCameraEffect drunkEffect;
    [Tooltip("Cuánto sube el nivel de borrachera por cada enceste (0 a 1).")]
    [Range(0f, 1f)] public float drunkPerHit = 0.2f;
    [Tooltip("Al reiniciar la ronda, la borrachera vuelve al nivel con el que empezó.")]
    public bool resetDrunkOnRestart = true;

    [Header("Memoria")]
    [Tooltip("Antes de cada tiro los vasos muestran su color, luego se vuelven todos del color oculto y se mezclan.")]
    public bool memoryMode = true;
    [Tooltip("Segundos que se ven los colores reales.")]
    public float revealTime = 3f;
    [Tooltip("Color de todos los vasos cuando se ocultan.")]
    public Color hiddenColor = new Color(0.85f, 0.11f, 0.11f);
    [Tooltip("Todos los vasos cambian de lugar a la vez, repartiéndose los puestos entre ellos (nadie se queda donde estaba). " +
             "Si está apagado, solo se intercambian dos vasos por vez.")]
    public bool shuffleAllCups = true;
    [Tooltip("Cuántas mezclas se hacen antes de cada tiro (cada una mueve a todos los vasos, o a un par si 'Shuffle All Cups' está apagado).")]
    public int shuffleSwaps = 2;
    [Tooltip("Segundos que dura cada intercambio (la mezcla es más lenta y se puede seguir con la vista).")]
    public float swapDuration = 1.3f;
    [Tooltip("Altura del saltito de los vasos al intercambiarse.")]
    public float swapHop = 0.1f;
    [Tooltip("Cuánto se separan de lado para rodearse al intercambiarse.")]
    public float swapSideOffset = 0.15f;
    [Tooltip("Segundos que un vaso muestra su color real al caer la pelota en él.")]
    public float revealOnResult = 1f;

    [Header("Rebotes")]
    [Tooltip("Textos emergentes (¡REBOTE!). Opcional.")]
    public PongPopupUI popups;
    [Tooltip("Se llama en cada rebote de la pelota, con el número de rebotes de ese tiro.")]
    public UnityEvent<int> onBounce;

    [Header("Evento: pirámide")]
    [Tooltip("Entre tiros (nunca con una pelota en el aire) los vasos se apilan en pirámide y hay que derribarlos.")]
    public bool pyramidEnabled = true;
    [Tooltip("No puede haber pirámide hasta haber lanzado este número de pelotas en la ronda.")]
    public int pyramidFromShot = 1;
    [Tooltip("Probabilidad de que ocurra en la primera pausa entre tiros posible.")]
    [Range(0f, 1f)] public float pyramidChance = 0.5f;
    [Tooltip("Si no sale, la probabilidad sube esto en cada pausa siguiente de la ronda (así no se queda sin verla).")]
    [Range(0f, 1f)] public float pyramidChanceGrowth = 0.25f;
    public int maxPyramidsPerRound = 1;
    [Tooltip("Filas de la pirámide (3 = 6 vasos, 4 = 10 vasos). Si hay menos vasos disponibles, se reduce.")]
    [Range(2, 4)] public int pyramidRows = 3;
    [Tooltip("Pelotas para derribarla.")]
    public int pyramidBalls = 3;
    [Tooltip("Segundos que tardan los vasos en formar la pirámide.")]
    public float pyramidBuildTime = 1f;
    [Tooltip("Masa de la pelota durante el evento (la normal, 0.0027, es muy ligera para derribar nada). " +
             "Con vasos de 0.1 y pelota de 0.3 caen normalmente 2-3 de 6 y casi nunca todos de una (probado con simulaciones).")]
    public float pyramidBallMass = 0.3f;
    [Header("Pirámide: física de los vasos")]
    [Tooltip("Más masa = más difícil de tumbar. Súbela o baja la masa de la pelota para endurecer el evento.")]
    public float pyramidCupMass = 0.1f;
    [Range(0f, 1f)] public float pyramidCupFriction = 0.5f;
    public float pyramidCupLinearDamping = 0.05f;
    public float pyramidCupAngularDamping = 0.1f;
    [Header("Pirámide: cordura")]
    [Tooltip("Cordura del jugador (opcional). Cada vaso derribado la restaura.")]
    public SanityManager sanity;
    [Tooltip("Cordura que se recupera por cada vaso derribado.")]
    public float sanityPerCup = 4f;
    [Tooltip("Cordura extra por derribarla entera.")]
    public float sanityAllCupsBonus = 8f;
    [Tooltip("Se llama al terminar la pirámide, con cuántos vasos se derribaron.")]
    public UnityEvent<int> onPyramidFinished;

    [Header("Eventos")]
    public UnityEvent onCupHit;
    public UnityEvent onMiss;
    public UnityEvent onWin;
    public UnityEvent onLose;

    [Header("Debug")]
    public bool showDebugHUD = true;

    public int Hits { get; private set; }
    public int BallsThrown { get; private set; }
    public bool IsFinished { get; private set; }
    // Nivel de borrachera actual (0 a 1); la pelota lo usa para desviarse en el aire.
    public float CurrentDrunk => drunkEffect != null ? drunkEffect.drunkLevel : 0f;
    // Rebotes del último tiro (para la bonificación del resultado).
    public int LastBounceCount { get; private set; }
    // Verdadero mientras dura el evento de la pirámide.
    public bool EventActive => eventActive;

    PongBall currentBall;
    string lastMessage = "";
    float startDrunkLevel;
    bool initialized;
    bool revealing;
    bool eventActive;
    bool eventBallDone;
    int pyramidsThisRound;
    int pyramidSkips;
    Transform savedAim, pyramidAim;

    void Start()
    {
        if (drunkEffect == null && Camera.main != null)
            drunkEffect = Camera.main.GetComponent<DrunkCameraEffect>();
        if (drunkEffect != null)
            startDrunkLevel = drunkEffect.drunkLevel;

        if (cups.Count == 0)
            cups.AddRange(GetComponentsInChildren<PongCup>(true));

        if (assignColorsToCups)
        {
            for (int i = 0; i < cups.Count; i++)
                cups[i].colorIndex = i % palette.Length;
        }

        foreach (var cup in cups) cup.SetCatchZone(catchWidth, catchAboveRim);

        initialized = true;
        StartRound();
    }

    // Al reactivar el script (por ejemplo al volver a entrar a la zona) la partida empieza de cero.
    void OnEnable()
    {
        if (initialized) StartRound();
    }

    // Al desactivarlo se quita la pelota que estuviera en juego.
    void OnDisable()
    {
        StopAllCoroutines();
        AbortEvent();
        if (currentBall != null) Destroy(currentBall.gameObject);
        currentBall = null;
    }

    // Si el evento de la pirámide se interrumpe (se sale de la zona, se reinicia la ronda), deja todo como estaba.
    void AbortEvent()
    {
        if (!eventActive) return;
        eventActive = false;
        foreach (var cup in cups) cup.ResetCup();
        if (launcher != null) launcher.aimTarget = savedAim;
        if (pyramidAim != null) Destroy(pyramidAim.gameObject);
        pyramidAim = null;
    }

    // Salta el minijuego dándolo por ganado (botón de saltar).
    public void ForceWin()
    {
        if (IsFinished) return;
        StopAllCoroutines();
        AbortEvent();
        if (currentBall != null) Destroy(currentBall.gameObject);
        currentBall = null;
        IsFinished = true;
        lastMessage = "¡Ganaste!";
        onWin.Invoke();
    }

    public void StartRound()
    {
        StopAllCoroutines();
        AbortEvent();
        revealing = false;
        if (currentBall != null) Destroy(currentBall.gameObject);

        Hits = 0;
        BallsThrown = 0;
        pyramidsThisRound = 0;
        pyramidSkips = 0;
        LastBounceCount = 0;
        IsFinished = false;
        lastMessage = "";

        if (resetDrunkOnRestart && drunkEffect != null)
            drunkEffect.SetDrunkLevel(startDrunkLevel);

        foreach (var cup in cups)
        {
            cup.ResetCup();
            cup.ApplyColor(GetColor(cup.colorIndex));
        }

        StartCoroutine(NextBall());
    }

    public Color GetColor(int index) => palette[Mathf.Clamp(index, 0, palette.Length - 1)];

    // Prepara el siguiente tiro: pelota nueva y, en modo memoria, mostrar colores -> ocultar -> mezclar.
    IEnumerator NextBall()
    {
        SpawnNextBall();
        if (currentBall == null || !memoryMode) yield break;

        launcher.InputLocked = true;
        var available = cups.FindAll(c => c.IsAvailable);

        lastMessage = "Memoriza dónde está tu color...";
        revealing = true;
        foreach (var cup in available) cup.ApplyColor(GetColor(cup.colorIndex));
        yield return new WaitForSeconds(revealTime);

        revealing = false;
        foreach (var cup in available) cup.ApplyColor(hiddenColor);

        if (available.Count >= 2)
        {
            lastMessage = "¡Se mezclan!";
            for (int s = 0; s < shuffleSwaps; s++)
                yield return shuffleAllCups ? ShuffleAll(available) : SwapRandomPair(available);
        }

        lastMessage = "¡Tira!";
        launcher.InputLocked = false;
    }

    // Todos los vasos cambian de sitio a la vez: se reparten los puestos entre ellos y ninguno se queda donde estaba.
    // Van con un saltito y rodeo distinto cada uno para no atravesarse.
    IEnumerator ShuffleAll(List<PongCup> available)
    {
        int n = available.Count;
        var slots = new Vector3[n];
        for (int i = 0; i < n; i++) slots[i] = available[i].transform.position;
        int[] perm = RandomDerangement(n);

        var moves = new List<Coroutine>();
        for (int i = 0; i < n; i++)
        {
            Vector3 target = slots[perm[i]];
            Vector3 dir = target - slots[i];
            dir.y = 0f;
            Vector3 side = dir.sqrMagnitude > 0.0001f
                ? Vector3.Cross(Vector3.up, dir.normalized) * swapSideOffset * (i % 2 == 0 ? 1f : -1f)
                : Vector3.zero;
            float hop = swapHop * (1f + 0.5f * (i % 3));
            moves.Add(StartCoroutine(available[i].SlideTo(target, swapDuration, hop, side)));
        }
        foreach (var m in moves) yield return m;
    }

    // Permutación al azar en la que nadie queda en su puesto original.
    static int[] RandomDerangement(int n)
    {
        var perm = new int[n];
        for (int attempt = 0; attempt < 50; attempt++)
        {
            for (int i = 0; i < n; i++) perm[i] = i;
            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (perm[i], perm[j]) = (perm[j], perm[i]);
            }
            bool ok = true;
            for (int i = 0; i < n; i++) if (perm[i] == i) { ok = false; break; }
            if (ok) return perm;
        }
        for (int i = 0; i < n; i++) perm[i] = (i + 1) % n; // plan B: todos se corren un puesto
        return perm;
    }

    // Intercambia de sitio dos vasos al azar, deslizándose a la vez y rodeándose.
    IEnumerator SwapRandomPair(List<PongCup> available)
    {
        int a = Random.Range(0, available.Count);
        int b = Random.Range(0, available.Count - 1);
        if (b >= a) b++;
        PongCup cupA = available[a], cupB = available[b];

        Vector3 posA = cupA.transform.position, posB = cupB.transform.position;
        Vector3 dir = posB - posA;
        dir.y = 0f;
        Vector3 side = dir.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, dir.normalized) * swapSideOffset : Vector3.zero;

        var moveA = StartCoroutine(cupA.SlideTo(posB, swapDuration, swapHop, side));
        var moveB = StartCoroutine(cupB.SlideTo(posA, swapDuration, swapHop, -side));
        yield return moveA;
        yield return moveB;
    }

    // Muestra un momento el color real de un vaso y vuelve a ocultarlo (si sigue en juego).
    IEnumerator RevealCup(PongCup cup)
    {
        cup.ApplyColor(GetColor(cup.colorIndex));
        yield return new WaitForSeconds(revealOnResult);
        // Si ya empezó la siguiente fase de memoria, se deja con su color.
        if (memoryMode && cup.IsAvailable && !revealing) cup.ApplyColor(hiddenColor);
    }

    void SpawnNextBall()
    {
        var available = cups.FindAll(c => c.IsAvailable);
        if (available.Count == 0 || ballPrefab == null || launcher == null) return;

        int colorIndex = available[Random.Range(0, available.Count)].colorIndex;
        Transform point = launcher.LaunchPoint;
        currentBall = Instantiate(ballPrefab, point.position, point.rotation);
        currentBall.Init(this, colorIndex, GetColor(colorIndex));
        launcher.InputLocked = false;
        launcher.SetBall(currentBall);
    }

    // Lo llama el PongLauncher al soltar la pelota.
    public void NotifyBallLaunched(PongBall ball)
    {
        LastBounceCount = 0;
        if (!eventActive) BallsThrown++; // las pelotas de la pirámide no cuentan para la ronda
    }

    // Lo llama la pelota en cada rebote (con el número de rebotes de ese tiro y el punto del choque).
    public void NotifyBounce(PongBall ball, Vector3 point, int count)
    {
        LastBounceCount = count;
        if (popups != null) popups.ShowBounce(point, count);
        onBounce.Invoke(count);
    }

    // Lo llama la pelota cuando el tiro termina. cup es null si no cayó en ningún vaso.
    public void OnBallFinished(PongBall ball, PongCup cup)
    {
        if (eventActive)
        {
            // Pelota de la pirámide: no cuenta como enceste ni como fallo.
            eventBallDone = true;
            if (ball == currentBall) currentBall = null;
            Destroy(ball.gameObject, 0.4f);
            return;
        }

        if (IsFinished) return;

        if (cup != null && cup.IsAvailable && cup.colorIndex == ball.ColorIndex)
        {
            Hits++;
            cup.ApplyColor(GetColor(cup.colorIndex));
            cup.Sink();
            if (drunkEffect != null)
                drunkEffect.SetDrunkLevel(drunkEffect.drunkLevel + drunkPerHit);
            lastMessage = "¡Enceste!";
            onCupHit.Invoke();
        }
        else
        {
            lastMessage = cup != null ? "Vaso equivocado" : "Fallaste";
            if (cup != null && cup.IsAvailable) StartCoroutine(RevealCup(cup));
            onMiss.Invoke();
        }

        // Si cayó en un vaso, la pelota baja hasta el fondo antes de desaparecer.
        if (cup != null) ball.DropInto(cup.InsidePoint, 0.25f);
        else Destroy(ball.gameObject, 1f);
        if (ball == currentBall) currentBall = null;

        StartCoroutine(AfterShot());
    }

    IEnumerator AfterShot()
    {
        int ballsLeft = ballsPerRound - BallsThrown;

        if (Hits >= hitsToWin)
        {
            IsFinished = true;
            lastMessage = "¡Ganaste!";
            onWin.Invoke();
            yield break;
        }

        // Si ya no alcanzan las pelotas para ganar, se pierde de una vez.
        if (Hits + ballsLeft < hitsToWin)
        {
            IsFinished = true;
            lastMessage = "Perdiste";
            onLose.Invoke();
            if (autoRestartOnLose)
            {
                yield return new WaitForSeconds(restartDelay);
                StartRound();
            }
            yield break;
        }

        yield return new WaitForSeconds(nextBallDelay);

        // Entre tiros (nunca con una pelota en el aire) puede haber un evento.
        if (ShouldStartPyramid()) yield return PyramidEvent();

        yield return NextBall();
    }

    bool ShouldStartPyramid()
    {
        if (!pyramidEnabled || IsFinished || currentBall != null || eventActive || launcher == null) return false;
        if (pyramidsThisRound >= maxPyramidsPerRound || BallsThrown < pyramidFromShot) return false;
        if (cups.FindAll(c => c.IsAvailable).Count < 3) return false;

        // Cada pausa en la que no sale, la probabilidad sube un poco (llega a 1 si hace falta).
        float chance = Mathf.Clamp01(pyramidChance + pyramidChanceGrowth * pyramidSkips);
        if (Random.value >= chance)
        {
            pyramidSkips++;
            return false;
        }
        pyramidSkips = 0;
        return true;
    }

    // Los vasos se apilan en pirámide y hay que derribarlos con pelotas más pesadas. Cada vaso derribado
    // devuelve cordura. Solo ocurre entre tiros; al acabar, los vasos vuelven a donde estaban.
    IEnumerator PyramidEvent()
    {
        var available = cups.FindAll(c => c.IsAvailable);
        int rows = pyramidRows;
        while (rows > 1 && rows * (rows + 1) / 2 > available.Count) rows--;
        if (rows < 2) yield break;
        int needed = rows * (rows + 1) / 2;

        pyramidsThisRound++;
        eventActive = true;
        launcher.InputLocked = true;
        lastMessage = "¡Pirámide! Derríbala";
        if (popups != null) popups.ShowMessage("¡PIRÁMIDE!", new Color(1f, 0.45f, 0.75f));

        // Los vasos más cercanos al centro forman la pirámide; el resto se aparta mientras dure.
        Vector3 center = Vector3.zero;
        foreach (var c in available) center += c.transform.position;
        center /= available.Count;
        available.Sort((a, b) => FlatDistance(a.transform.position, center).CompareTo(FlatDistance(b.transform.position, center)));
        var chosen = available.GetRange(0, needed);
        for (int i = needed; i < available.Count; i++) available[i].Bench(0.35f);
        foreach (var c in chosen) c.ApplyColor(GetColor(c.colorIndex));

        // Pirámide: filas centradas, de abajo hacia arriba, mirando hacia el lanzador.
        float width = chosen[0].Width * 1.02f;
        float step = chosen[0].Height + 0.004f;
        Vector3 forward = Vector3.ProjectOnPlane(center - launcher.LaunchPoint.position, Vector3.up);
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        float baseY = chosen[0].transform.position.y - chosen[0].PivotToBase;

        var slots = new Vector3[needed];
        int index = 0;
        for (int r = 0; r < rows; r++)
        {
            int inRow = rows - r;
            for (int i = 0; i < inRow; i++)
            {
                Vector3 p = center + right * ((i - (inRow - 1) * 0.5f) * width);
                p.y = baseY + r * step + chosen[index].PivotToBase;
                slots[index++] = p;
            }
        }

        // Los vasos pasan a tener física (quietos) y se colocan en la pirámide.
        var homes = new Vector3[needed];
        var homeRotations = new Quaternion[needed];
        var material = new PhysicsMaterial("VasoPiramide")
        {
            dynamicFriction = pyramidCupFriction,
            staticFriction = pyramidCupFriction + 0.1f,
            bounciness = 0.05f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Average,
        };
        var builds = new List<Coroutine>();
        for (int i = 0; i < needed; i++)
        {
            homes[i] = chosen[i].transform.position;
            homeRotations[i] = chosen[i].transform.rotation;
            chosen[i].EnablePhysics(pyramidCupMass, pyramidCupLinearDamping, pyramidCupAngularDamping, material);
            builds.Add(StartCoroutine(chosen[i].SlideToPose(slots[i], homeRotations[i], pyramidBuildTime, 0.08f)));
        }
        foreach (var b in builds) yield return b;

        // Apuntar al centro de la pirámide mientras dure el evento.
        savedAim = launcher.aimTarget;
        pyramidAim = new GameObject("PiramideAim").transform;
        pyramidAim.position = center + Vector3.up * (baseY + step * rows * 0.5f);
        launcher.aimTarget = pyramidAim;

        // Se sueltan y se asientan.
        foreach (var c in chosen) c.Release();
        yield return new WaitForSeconds(0.8f);
        var rest = new Vector3[needed];
        for (int i = 0; i < needed; i++) rest[i] = chosen[i].transform.position;

        // Tiros para derribarla.
        var knocked = new bool[needed];
        int totalKnocked = 0;
        for (int shot = 0; shot < pyramidBalls && totalKnocked < needed; shot++)
        {
            SpawnEventBall();
            lastMessage = $"Pirámide: pelota {shot + 1}/{pyramidBalls}";
            while (!eventBallDone) yield return null;

            // Esperar a que todo se quede quieto (con tope) antes de contar lo que cayó.
            float waited = 0f;
            while (waited < 3f && !AllSettled(chosen)) { waited += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(0.3f);

            int newly = 0;
            for (int i = 0; i < needed; i++)
            {
                if (knocked[i] || !chosen[i].IsKnocked(rest[i], width * 0.7f)) continue;
                knocked[i] = true;
                totalKnocked++;
                newly++;
            }
            if (newly > 0)
            {
                float restored = newly * sanityPerCup;
                if (sanity != null) sanity.Restore(restored);
                if (popups != null) popups.ShowMessage($"¡{newly} {(newly == 1 ? "vaso" : "vasos")}!  +{restored:0} cordura", new Color(0.5f, 1f, 0.9f));
            }
        }

        if (totalKnocked == needed)
        {
            if (sanity != null) sanity.Restore(sanityAllCupsBonus);
            if (popups != null) popups.ShowMessage($"¡PIRÁMIDE COMPLETA!  +{sanityAllCupsBonus:0}", new Color(1f, 0.95f, 0.45f));
        }
        lastMessage = $"Pirámide: {totalKnocked}/{needed} vasos";
        yield return new WaitForSeconds(0.8f);

        // Todo vuelve a su sitio.
        var back = new List<Coroutine>();
        for (int i = 0; i < needed; i++)
        {
            chosen[i].DisablePhysics();
            back.Add(StartCoroutine(chosen[i].SlideToPose(homes[i], homeRotations[i], pyramidBuildTime, 0.08f)));
        }
        foreach (var c in available) if (!chosen.Contains(c)) c.Unbench(0.35f);
        foreach (var b in back) yield return b;

        launcher.aimTarget = savedAim;
        if (pyramidAim != null) Destroy(pyramidAim.gameObject);
        pyramidAim = null;
        eventActive = false;
        onPyramidFinished.Invoke(totalKnocked);
    }

    // Pelota de la pirámide: blanca y con más masa para poder derribar vasos.
    void SpawnEventBall()
    {
        Transform point = launcher.LaunchPoint;
        currentBall = Instantiate(ballPrefab, point.position, point.rotation);
        currentBall.Init(this, 0, Color.white);
        currentBall.SetMass(pyramidBallMass);
        eventBallDone = false;
        launcher.InputLocked = false;
        launcher.SetBall(currentBall);
    }

    static bool AllSettled(List<PongCup> list)
    {
        foreach (var c in list) if (!c.IsSettled) return false;
        return true;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return (a - b).sqrMagnitude;
    }

    // Aplica un color a todos los renderers (funciona con URP Lit y con shaders clásicos).
    public static void Tint(Renderer[] renderers, Color color)
    {
        var block = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            r.SetPropertyBlock(block);
        }
    }

    void OnGUI()
    {
        if (!showDebugHUD) return;

        GUILayout.BeginArea(new Rect(10, 10, 260, 120), GUI.skin.box);
        GUILayout.Label($"Encestes: {Hits} / {hitsToWin}");
        GUILayout.Label($"Pelotas: {BallsThrown} / {ballsPerRound}");
        if (currentBall != null)
        {
            var prev = GUI.color;
            GUI.color = GetColor(currentBall.ColorIndex);
            GUILayout.Label("■■■ Color de la pelota");
            GUI.color = prev;
        }
        GUILayout.Label(lastMessage);
        GUILayout.EndArea();
    }
}
