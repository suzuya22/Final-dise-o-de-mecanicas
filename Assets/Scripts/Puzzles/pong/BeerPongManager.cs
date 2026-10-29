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
    [Tooltip("Empujón lateral sobre la pelota en el aire con borrachera máxima (m/s²). 0 = sin efecto. La parábola no lo muestra.")]
    public float drunkDrift = 3f;
    [Tooltip("Qué tan rápido cambia de dirección el empujón.")]
    public float drunkDriftFrequency = 1.5f;

    [Header("Memoria")]
    [Tooltip("Antes de cada tiro los vasos muestran su color, luego se vuelven todos del color oculto y se mezclan.")]
    public bool memoryMode = true;
    [Tooltip("Segundos que se ven los colores reales.")]
    public float revealTime = 3f;
    [Tooltip("Color de todos los vasos cuando se ocultan.")]
    public Color hiddenColor = new Color(0.85f, 0.11f, 0.11f);
    [Tooltip("Cuántos intercambios de dos vasos se hacen antes de cada tiro.")]
    public int shuffleSwaps = 2;
    [Tooltip("Segundos que dura cada intercambio.")]
    public float swapDuration = 0.6f;
    [Tooltip("Altura del saltito de los vasos al intercambiarse.")]
    public float swapHop = 0.1f;
    [Tooltip("Cuánto se separan de lado para rodearse al intercambiarse.")]
    public float swapSideOffset = 0.15f;
    [Tooltip("Segundos que un vaso muestra su color real al caer la pelota en él.")]
    public float revealOnResult = 1f;

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

    PongBall currentBall;
    string lastMessage = "";
    float startDrunkLevel;
    bool initialized;
    bool revealing;

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
        if (currentBall != null) Destroy(currentBall.gameObject);
        currentBall = null;
    }

    public void StartRound()
    {
        StopAllCoroutines();
        revealing = false;
        if (currentBall != null) Destroy(currentBall.gameObject);

        Hits = 0;
        BallsThrown = 0;
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
                yield return SwapRandomPair(available);
        }

        lastMessage = "¡Tira!";
        launcher.InputLocked = false;
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
        BallsThrown++;
    }

    // Lo llama la pelota cuando el tiro termina. cup es null si no cayó en ningún vaso.
    public void OnBallFinished(PongBall ball, PongCup cup)
    {
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

        Destroy(ball.gameObject, cup != null ? 0f : 1f);
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
        yield return NextBall();
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
