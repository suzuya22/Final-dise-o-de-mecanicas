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

    PongBall currentBall;
    string lastMessage = "";
    float startDrunkLevel;
    bool initialized;

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

        SpawnNextBall();
    }

    public Color GetColor(int index) => palette[Mathf.Clamp(index, 0, palette.Length - 1)];

    void SpawnNextBall()
    {
        var available = cups.FindAll(c => c.IsAvailable);
        if (available.Count == 0 || ballPrefab == null || launcher == null) return;

        int colorIndex = available[Random.Range(0, available.Count)].colorIndex;
        Transform point = launcher.LaunchPoint;
        currentBall = Instantiate(ballPrefab, point.position, point.rotation);
        currentBall.Init(this, colorIndex, GetColor(colorIndex));
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
            cup.Sink();
            if (drunkEffect != null)
                drunkEffect.SetDrunkLevel(drunkEffect.drunkLevel + drunkPerHit);
            lastMessage = "¡Enceste!";
            onCupHit.Invoke();
        }
        else
        {
            lastMessage = cup != null ? "Vaso equivocado" : "Fallaste";
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
        SpawnNextBall();
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
