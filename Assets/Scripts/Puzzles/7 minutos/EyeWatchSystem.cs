using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Luz roja / luz verde con los cuadros de ojos.
// Ciclo: cerrados -> aviso (parpadean y brillan) -> abiertos.
// Con los ojos abiertos el jugador pierde cordura: poca si está quieto mirando abajo (Ctrl), mucha si no.
public class EyeWatchSystem : MonoBehaviour
{
    [Header("Referencias")]
    public EyePainting[] paintings;
    public SanityManager sanity;
    public PlayerMovement player;
    public PlayerLookDown playerLookDown;
    [Tooltip("Opcional: solo afecta si el jugador está dentro de este collider (la habitación).")]
    public Collider roomArea;

    [Header("Tiempos (segundos)")]
    public Vector2 closedTime = new Vector2(4f, 8f);
    [Tooltip("Tiempo de aviso antes de abrirse: lo que tiene el jugador para reaccionar.")]
    public float warningTime = 1.5f;
    public Vector2 openTime = new Vector2(3f, 5f);

    [Header("Cordura por segundo con los ojos abiertos")]
    [Tooltip("Quieto y mirando hacia abajo.")]
    public float drainHidden = 2f;
    [Tooltip("Moviéndose o sin mirar abajo.")]
    public float drainExposed = 12f;

    [Header("Inicio")]
    public bool startOnAwake = true;

    [Header("Sonido (opcional)")]
    public AudioSource audioSource;
    public AudioClip warningClip;
    public AudioClip openClip;

    [Header("Eventos")]
    public UnityEvent onWarning;
    public UnityEvent onOpen;
    public UnityEvent onClose;

    public EyePainting.EyeState State { get; private set; } = EyePainting.EyeState.Closed;
    public bool IsWatching { get; private set; }

    Coroutine cycle;

    void Start()
    {
        foreach (var p in paintings)
            if (p != null && p.lookTarget == null && player != null) p.lookTarget = player.transform;

        if (startOnAwake) StartWatching();
    }

    public void StartWatching()
    {
        StopWatching();
        IsWatching = true;
        cycle = StartCoroutine(Cycle());
    }

    // Por ejemplo, al cortar los 5 hilos.
    public void StopWatching()
    {
        if (cycle != null) StopCoroutine(cycle);
        cycle = null;
        IsWatching = false;
        SetState(EyePainting.EyeState.Closed);
    }

    // Lo llama SevenMinutesPuzzle al reiniciar.
    public void ResetState()
    {
        if (startOnAwake) StartWatching();
        else StopWatching();
    }

    IEnumerator Cycle()
    {
        while (true)
        {
            SetState(EyePainting.EyeState.Closed);
            onClose.Invoke();
            yield return new WaitForSeconds(Random.Range(closedTime.x, closedTime.y));

            SetState(EyePainting.EyeState.Warning);
            Play(warningClip);
            onWarning.Invoke();
            yield return new WaitForSeconds(warningTime);

            SetState(EyePainting.EyeState.Open);
            Play(openClip);
            onOpen.Invoke();
            yield return new WaitForSeconds(Random.Range(openTime.x, openTime.y));
        }
    }

    void SetState(EyePainting.EyeState state)
    {
        State = state;
        foreach (var p in paintings)
            if (p != null) p.SetState(state);
    }

    void Update()
    {
        if (!IsWatching || State != EyePainting.EyeState.Open || sanity == null || player == null) return;
        if (roomArea != null && !roomArea.bounds.Contains(player.transform.position)) return;

        bool hidden = !player.IsMoving && playerLookDown != null && playerLookDown.IsLookingDown;
        sanity.Drain((hidden ? drainHidden : drainExposed) * Time.deltaTime);
    }

    void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
