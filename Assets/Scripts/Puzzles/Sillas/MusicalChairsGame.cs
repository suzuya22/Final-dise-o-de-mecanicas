using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Sillas musicales:
// 1. Suena la música: todos caminan en círculo. Si el jugador toca a otro participante o se sale de la fila, pierde cordura.
// 2. Para la música: todos corren a por una silla; el jugador se sienta con E. Siempre hay una silla menos.
// 3. Si el jugador se sentó, la silla lo muerde: forcejeo (MashQTE); si falla, pierde cordura.
//    El NPC sin silla queda eliminado y se retira una silla. Si solo queda el jugador, gana.
// 4. Si el jugador se queda sin silla, lo arrastran al centro: prueba de tiempo (TimingQTE);
//    cada acierto reduce la penalización de cordura. El minijuego termina perdido.
[RequireComponent(typeof(MashQTE), typeof(TimingQTE))]
public class MusicalChairsGame : MonoBehaviour
{
    enum Phase { Idle, Music, Rush, Biting, Dragged, Finished }

    [Header("Referencias")]
    public PlayerMovement player;
    public SanityManager sanity;
    [Tooltip("Centro del círculo de sillas.")]
    public Transform circleCenter;
    public MusicalChair[] chairs;
    public ChairNPC[] npcs;
    [Tooltip("Opcional: la música de feria.")]
    public AudioSource music;

    [Header("Sentarse")]
    [Tooltip("El personaje sentado queda con los pies en el suelo (la pose sentada baja la cadera hasta la silla). Desactívalo si quieres que quede a la altura exacta del Seat Point.")]
    public bool seatAtFloorLevel = true;
    [Tooltip("Animator del modelo del jugador (para la pose sentada).")]
    public Animator playerAnimator;
    [Tooltip("Clip de la pose sentada (\"Armature|Sit down\" dentro de Prota sentao.fbx). Vacío = se queda de pie.")]
    public AnimationClip playerSitClip;
    [Tooltip("Segundos que tarda en sentarse (0 = de golpe).")]
    public float playerSitDuration = 0.4f;

    [Header("Inicio")]
    [Tooltip("Empieza solo al darle Play (útil para escenas de prueba). Si no, llama a StartGame (por ejemplo con un PlayerTrigger).")]
    public bool startOnPlay = false;

    [Header("Círculo")]
    [Tooltip("Radio por el que se camina. 0 = automático (un poco por fuera de las sillas).")]
    public float walkRadius = 0f;
    [Tooltip("Distancia extra por fuera de las sillas cuando el radio es automático.")]
    public float walkOffset = 0.9f;
    [Tooltip("Cuánto puede alejarse el jugador de la fila sin penalización.")]
    public float ringTolerance = 0.6f;
    [Tooltip("Sentido del giro.")]
    public bool clockwise = false;

    [Header("Rondas")]
    [Tooltip("Segundos que suena la música (mínimo y máximo).")]
    public Vector2 musicTime = new Vector2(6f, 12f);
    [Tooltip("Segundos de gracia al empezar cada ronda para volver a la fila.")]
    public float returnGrace = 2.5f;
    [Tooltip("Segundos máximos para sentarse después de parar la música.")]
    public float rushTimeout = 5f;

    [Header("Cordura")]
    [Tooltip("Por segundo, fuera de la fila mientras suena la música.")]
    public float outOfRingDrain = 8f;
    [Tooltip("Por segundo, tocando a otro participante mientras suena la música.")]
    public float touchDrain = 10f;
    [Tooltip("Distancia a la que se considera que toca a otro participante.")]
    public float touchDistance = 0.6f;
    [Tooltip("Al fallar el forcejeo con la silla.")]
    public float biteFailDamage = 10f;
    [Tooltip("Penalización al quedarse sin silla (sin aciertos en la prueba del centro).")]
    public float losePenalty = 40f;
    [Tooltip("Parte de la penalización que se puede evitar acertando todas las pulsaciones (0.75 = 75 %).")]
    [Range(0f, 1f)] public float maxPenaltyReduction = 0.75f;

    [Header("UI (opcional)")]
    public TMP_Text hudText;
    [Tooltip("Image con Image Type = Filled para la barra del QTE.")]
    public Image hudFill;
    public bool showDebugHUD = true;

    [Header("Eventos")]
    public UnityEvent onGameStart;
    public UnityEvent onRoundStart;
    public UnityEvent onPlayerSeated;
    public UnityEvent onWin;
    public UnityEvent onLose;

    public bool ChairsOpen => phase == Phase.Rush;
    public bool IsPlaying => phase != Phase.Idle && phase != Phase.Finished;
    public Vector3 Center => circleCenter != null ? circleCenter.position : transform.position;
    public float WalkRadius { get; private set; }
    public float Direction => clockwise ? -1f : 1f;

    Phase phase = Phase.Idle;
    MashQTE mash;
    TimingQTE timing;
    MusicalChair playerChair;
    Coroutine playerSitRoutine;
    int round;
    float graceUntil;
    string message = "";

    void Awake()
    {
        mash = GetComponent<MashQTE>();
        timing = GetComponent<TimingQTE>();
        foreach (var c in chairs)
            if (c != null) c.Init(this);
    }

    void Start()
    {
        if (startOnPlay) StartGame();
    }

    // Conéctalo, por ejemplo, a un PlayerTrigger en la zona del juego.
    public void StartGame()
    {
        if (IsPlaying) return;
        if (player == null || sanity == null || chairs == null || chairs.Length == 0)
        {
            Debug.LogError("MusicalChairsGame: falta asignar Player, Sanity o Chairs.", this);
            return;
        }
        StopAllCoroutines();
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        // Preparación: una silla menos que participantes.
        var activeNpcs = new List<ChairNPC>();
        foreach (var n in npcs) if (n != null) activeNpcs.Add(n);
        int chairsNeeded = activeNpcs.Count; // participantes (NPCs + jugador) - 1
        for (int i = 0; i < chairs.Length; i++)
        {
            if (chairs[i] == null) continue;
            chairs[i].ResetState();
            chairs[i].gameObject.SetActive(i < chairsNeeded);
        }

        WalkRadius = walkRadius > 0f ? walkRadius : AutoRadius();
        int participants = activeNpcs.Count + 1;
        float step = Mathf.PI * 2f / participants;
        float playerAngle = 0f;
        player.Teleport(RingPoint(playerAngle), Quaternion.LookRotation(RingTangent(playerAngle), Vector3.up));
        for (int i = 0; i < activeNpcs.Count; i++)
            activeNpcs[i].Init(this, playerAngle + step * (i + 1));

        round = 0;
        onGameStart.Invoke();

        while (true)
        {
            // ---- Música
            round++;
            phase = Phase.Music;
            message = $"Ronda {round}: ¡camina en la fila!";
            graceUntil = Time.time + returnGrace;
            foreach (var n in activeNpcs) n.StartCircling();
            if (music != null)
            {
                // La primera ronda empieza la canción; las siguientes la retoman donde se pausó.
                if (round == 1) music.Play();
                else music.UnPause();
            }
            onRoundStart.Invoke();
            yield return new WaitForSeconds(Random.Range(musicTime.x, musicTime.y));

            // ---- Para la música: a por las sillas
            if (music != null) music.Pause();
            phase = Phase.Rush;
            message = "¡Siéntate! (E junto a una silla libre)";
            foreach (var n in activeNpcs) n.StartRush();
            float rushEnd = Time.time + rushTimeout;
            while (Time.time < rushEnd && playerChair == null && FreeChairCount() > 0) yield return null;
            // Si el jugador ya se sentó, se deja a los NPCs ocupar las sillas que queden.
            float settleEnd = Mathf.Max(rushEnd, Time.time + 1.5f);
            while (playerChair != null && Time.time < settleEnd && FreeChairCount() > 0) yield return null;

            foreach (var n in activeNpcs) n.Stop();

            if (playerChair == null)
            {
                yield return LoseSequence();
                yield break;
            }

            // ---- Sentado: la silla muerde
            phase = Phase.Biting;
            message = "¡La silla te muerde! Forcejea";
            playerChair.SetBiting(true);
            bool freed = false;
            yield return mash.Run(ok => freed = ok);
            playerChair.SetBiting(false);
            if (!freed) sanity.Drain(biteFailDamage);
            message = freed ? "¡Te liberaste!" : "La silla te hirió...";

            // El NPC que se quedó sin silla queda eliminado.
            for (int i = activeNpcs.Count - 1; i >= 0; i--)
            {
                if (activeNpcs[i].IsSeated) continue;
                activeNpcs[i].Eliminate();
                activeNpcs.RemoveAt(i);
            }

            // Todos se levantan y se retira una silla.
            foreach (var n in activeNpcs) n.StandUp();
            StandPlayerUp();
            RemoveOneChair();

            if (activeNpcs.Count == 0)
            {
                phase = Phase.Finished;
                message = "¡Ganaste las sillas musicales!";
                if (music != null) music.Stop();
                onWin.Invoke();
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator LoseSequence()
    {
        phase = Phase.Dragged;
        message = "Te quedaste sin silla...";
        if (music != null) music.Stop();
        player.SetCanMove(false);

        // Lo arrastran al centro del círculo.
        Vector3 from = player.transform.position;
        Vector3 to = Center;
        to.y = from.y;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.2f)
        {
            Vector3 p = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            player.Teleport(p, player.transform.rotation);
            yield return null;
        }

        message = "¡Pulsa la tecla cuando la marca esté en la zona!";
        int hits = 0;
        yield return timing.Run(h => hits = h);

        float reduction = timing.prompts > 0 ? (float)hits / timing.prompts * maxPenaltyReduction : 0f;
        float penalty = losePenalty * (1f - reduction);
        sanity.Drain(penalty);

        phase = Phase.Finished;
        message = $"Perdiste. Aciertos: {hits}/{timing.prompts} (cordura -{penalty:F0})";
        player.SetCanMove(true);
        onLose.Invoke();
    }

    // Lo llama la silla cuando el jugador pulsa E.
    public void PlayerTrySit(MusicalChair chair)
    {
        if (!ChairsOpen || playerChair != null || !chair.TryOccupy(player)) return;
        playerChair = chair;
        player.SetCanMove(false);
        player.Teleport(SeatPosition(chair), SeatRotation(chair));
        if (playerSitClip != null && playerAnimator != null) playerSitRoutine = StartCoroutine(PlayerSitPose());
        onPlayerSeated.Invoke();
    }

    // Pose sentada del jugador: se apaga su Animator y se recorre el clip hasta el último fotograma.
    IEnumerator PlayerSitPose()
    {
        playerAnimator.enabled = false;
        for (float t = 0f; ; t += Time.deltaTime)
        {
            float k = playerSitDuration > 0f ? Mathf.Clamp01(t / playerSitDuration) : 1f;
            playerSitClip.SampleAnimation(playerAnimator.gameObject, k * playerSitClip.length);
            yield return null;
        }
    }

    void EndPlayerSitPose()
    {
        if (playerSitRoutine != null) StopCoroutine(playerSitRoutine);
        playerSitRoutine = null;
        if (playerAnimator != null) playerAnimator.enabled = true;
    }

    // Dónde queda sentado un participante.
    public Vector3 SeatPosition(MusicalChair chair)
    {
        Vector3 p = chair.Seat.position;
        if (seatAtFloorLevel) p.y = Center.y;
        return p;
    }

    // Hacia dónde mira sentado: solo el giro horizontal del Seat Point; si este apunta hacia arriba o abajo
    // (por ejemplo por la rotación del FBX de la silla), mira hacia fuera del círculo.
    public Quaternion SeatRotation(MusicalChair chair)
    {
        Vector3 f = chair.Seat.forward;
        if (chair.seatPoint != null && Mathf.Abs(f.y) < 0.7f)
            return Quaternion.LookRotation(Flat(f).normalized, Vector3.up);
        Vector3 outward = Flat(chair.Seat.position - Center);
        return outward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(outward.normalized, Vector3.up) : chair.transform.rotation;
    }

    void StandPlayerUp()
    {
        EndPlayerSitPose();
        if (playerChair == null) return;
        Vector3 offset = playerChair.Seat.position - Center;
        float a = Mathf.Atan2(offset.z, offset.x);
        playerChair.Release();
        playerChair = null;
        player.Teleport(RingPoint(a), Quaternion.LookRotation(RingTangent(a), Vector3.up));
        player.SetCanMove(true);
    }

    void RemoveOneChair()
    {
        var active = new List<MusicalChair>();
        foreach (var c in chairs) if (c != null && c.gameObject.activeSelf) active.Add(c);
        if (active.Count > 0) active[Random.Range(0, active.Count)].gameObject.SetActive(false);
    }

    void Update()
    {
        if (phase != Phase.Music || player == null || sanity == null || Time.time < graceUntil) return;

        // Fuera de la fila
        float dist = Flat(player.transform.position - Center).magnitude;
        if (Mathf.Abs(dist - WalkRadius) > ringTolerance)
            sanity.Drain(outOfRingDrain * Time.deltaTime);

        // Tocando a otro participante
        foreach (var n in npcs)
        {
            if (n == null || !n.gameObject.activeInHierarchy || !n.IsCircling) continue;
            if (Flat(n.transform.position - player.transform.position).magnitude < touchDistance)
            {
                sanity.Drain(touchDrain * Time.deltaTime);
                break;
            }
        }
    }

    // ---- Utilidades para los NPCs

    public Vector3 RingPoint(float angle)
    {
        Vector3 c = Center;
        return new Vector3(c.x + Mathf.Cos(angle) * WalkRadius, c.y, c.z + Mathf.Sin(angle) * WalkRadius);
    }

    public Vector3 RingTangent(float angle)
    {
        return new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * Direction;
    }

    public MusicalChair NearestFreeChair(Vector3 from)
    {
        MusicalChair best = null;
        float bestDist = float.MaxValue;
        foreach (var c in chairs)
        {
            if (c == null || !c.IsFree) continue;
            float d = Flat(c.Seat.position - from).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = c; }
        }
        return best;
    }

    int FreeChairCount()
    {
        int n = 0;
        foreach (var c in chairs) if (c != null && c.IsFree) n++;
        return n;
    }

    float AutoRadius()
    {
        float sum = 0f;
        int count = 0;
        foreach (var c in chairs)
        {
            if (c == null) continue;
            sum += Flat(c.transform.position - Center).magnitude;
            count++;
        }
        return (count > 0 ? sum / count : 2f) + walkOffset;
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    // ---- HUD

    string HudLine()
    {
        if (phase == Phase.Biting && mash.IsRunning)
            return $"{message}\nPulsa [{mash.CurrentKey}] rápido: {mash.Presses}/{mash.pressesNeeded}  ({mash.TimeLeft:F1}s)";
        if (phase == Phase.Dragged && timing.IsRunning)
        {
            string last = timing.LastResult == true ? " ¡Bien!" : timing.LastResult == false ? " Fallo" : "";
            return $"{message}\n[{timing.CurrentKey}]  {timing.CurrentPrompt + 1}/{timing.prompts}  aciertos {timing.Successes}{last}";
        }
        return message;
    }

    void LateUpdate()
    {
        if (hudText != null) hudText.text = phase == Phase.Idle ? "" : HudLine();
        if (hudFill != null)
        {
            bool show = (phase == Phase.Biting && mash.IsRunning) || (phase == Phase.Dragged && timing.IsRunning);
            hudFill.gameObject.SetActive(show);
            if (show) hudFill.fillAmount = phase == Phase.Biting ? mash.Progress : timing.Marker;
        }
    }

    void OnGUI()
    {
        if (!showDebugHUD || phase == Phase.Idle) return;

        GUILayout.BeginArea(new Rect(10, 120, 420, 160), GUI.skin.box);
        if (sanity != null) GUILayout.Label($"Cordura: {sanity.Current:F0} / {sanity.maxSanity:F0}");
        GUILayout.Label(HudLine());
        if (phase == Phase.Biting && mash.IsRunning)
        {
            DrawBar(mash.Progress, -1f, 0f);
        }
        else if (phase == Phase.Dragged && timing.IsRunning)
        {
            DrawBar(timing.Marker, timing.ZoneCenter, timing.zoneWidth);
        }
        GUILayout.EndArea();
    }

    // Barra simple: relleno/marca y, opcionalmente, la zona correcta.
    void DrawBar(float value, float zoneCenter, float zoneWidth)
    {
        Rect r = GUILayoutUtility.GetRect(400, 20);
        GUI.Box(r, GUIContent.none);
        var prev = GUI.color;
        if (zoneCenter >= 0f)
        {
            GUI.color = new Color(0.3f, 1f, 0.4f, 0.8f);
            GUI.DrawTexture(new Rect(r.x + r.width * (zoneCenter - zoneWidth * 0.5f), r.y, r.width * zoneWidth, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(r.x + r.width * value - 2, r.y - 3, 4, r.height + 6), Texture2D.whiteTexture);
        }
        else
        {
            GUI.color = new Color(1f, 0.55f, 0.85f);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(value), r.height), Texture2D.whiteTexture);
        }
        GUI.color = prev;
    }

    // Para reiniciar a mano desde un evento (por ejemplo para volver a jugar).
    public void ResetGame()
    {
        EndPlayerSitPose();
        StopAllCoroutines();
        if (music != null) music.Stop();
        if (playerChair != null) { playerChair.Release(); playerChair = null; }
        foreach (var c in chairs) if (c != null) c.ResetState();
        foreach (var n in npcs) if (n != null) n.ResetState();
        if (player != null) player.SetCanMove(true);
        phase = Phase.Idle;
        message = "";
    }
}
