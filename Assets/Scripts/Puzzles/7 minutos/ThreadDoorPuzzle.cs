using UnityEngine;
using UnityEngine.Events;

// Cuenta los hilos cortados. Al cortar todos, la persona abre la puerta del pasillo.
public class ThreadDoorPuzzle : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Los hilos que hay que cortar (pueden estar en cualquier zona).")]
    public PullableThread[] threads;
    [Tooltip("La puerta que se abre al cortarlos todos (opcional).")]
    public PuzzleDoor door;
    [Tooltip("Transición que se desbloquea al cortarlos todos (por ejemplo la entrada al pasillo).")]
    public ZoneTeleport entranceToUnlock;

    [Header("Eventos")]
    [Tooltip("Al cortar cada hilo, con el número de hilos cortados.")]
    public UnityEvent<int> onThreadCut = new UnityEvent<int>();
    [Tooltip("Al cortar el último hilo (por ejemplo: apagar los ojos con EyeWatchSystem.StopWatching).")]
    public UnityEvent onAllThreadsCut = new UnityEvent();

    public int CutCount { get; private set; }
    public bool IsComplete { get; private set; }

    void Awake()
    {
        foreach (var t in threads)
            if (t != null) t.onCut.AddListener(OnThreadCut);
    }

    void OnThreadCut()
    {
        if (IsComplete) return;

        CutCount++;
        onThreadCut.Invoke(CutCount);

        if (CutCount >= threads.Length)
        {
            IsComplete = true;
            onAllThreadsCut.Invoke();
            if (door != null) door.Open();
            if (entranceToUnlock != null) entranceToUnlock.Unlock();
        }
    }

    // Lo llama SevenMinutesPuzzle al reiniciar.
    public void ResetState()
    {
        CutCount = 0;
        IsComplete = false;
        foreach (var t in threads)
            if (t != null) t.ResetState();
        if (door != null) door.ResetState();
        if (entranceToUnlock != null) entranceToUnlock.ResetState();
    }
}
