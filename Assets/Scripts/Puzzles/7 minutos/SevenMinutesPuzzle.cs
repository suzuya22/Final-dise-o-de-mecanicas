using UnityEngine;
using UnityEngine.Events;

// Controlador del puzzle "7 minutos en el cielo".
// Si la cordura llega a 0, reinicia todo el puzzle: jugador al punto de inicio, cordura llena,
// hilos, puertas, ojos, persona y objetos recogidos como al principio.
public class SevenMinutesPuzzle : MonoBehaviour
{
    [Header("Jugador")]
    public PlayerMovement player;
    public SanityManager sanity;
    [Tooltip("Dónde reaparece el jugador al reiniciar.")]
    public Transform respawnPoint;

    [Header("Partes del puzzle")]
    public EyeWatchSystem eyeSystem;
    public ThreadDoorPuzzle threadPuzzle;
    public HuggerBoss boss;
    [Tooltip("Objetos recogibles del puzzle (por ejemplo el objeto de la familia del laberinto).")]
    public PickupItem[] pickupItems;

    [Header("Mapa con varias habitaciones")]
    [Tooltip("El puzzle solo se reinicia por cordura si el jugador está dentro. Déjalo activado en una escena " +
             "que solo tenga este puzzle; en el mapa general apágalo y llama a EnterPuzzle / LeavePuzzle desde las puertas.")]
    public bool playerInside = true;

    [Header("Eventos")]
    public UnityEvent onPuzzleReset;
    public UnityEvent onPuzzleCompleted;

    void Awake()
    {
        if (sanity != null) sanity.onSanityDepleted.AddListener(OnSanityDepleted);
        if (boss != null) boss.onDefeated.AddListener(onPuzzleCompleted.Invoke);
    }

    // La cordura es global: si se acaba en otra habitación, este puzzle no debe reaccionar.
    void OnSanityDepleted()
    {
        if (playerInside) ResetPuzzle();
    }

    public void EnterPuzzle() => playerInside = true;
    public void LeavePuzzle() => playerInside = false;

    public void ResetPuzzle()
    {
        if (player != null && respawnPoint != null) player.Teleport(respawnPoint.position, respawnPoint.rotation);
        if (sanity != null) sanity.ResetSanity();

        if (threadPuzzle != null) threadPuzzle.ResetState();
        if (boss != null) boss.ResetState();
        if (eyeSystem != null) eyeSystem.ResetState();
        foreach (var item in pickupItems)
            if (item != null) item.ResetState();

        onPuzzleReset.Invoke();
    }
}
