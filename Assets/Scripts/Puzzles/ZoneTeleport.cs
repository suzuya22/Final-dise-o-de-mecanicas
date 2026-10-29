using UnityEngine;
using UnityEngine.Events;

// Transición entre zonas: lleva al jugador a otro punto.
// - Al entrar: con un Collider "Is Trigger" (por ejemplo al pisar la escalera).
// - Con E: como una puerta (puede estar bloqueada hasta que algo la desbloquee).
public class ZoneTeleport : Interactable
{
    public enum Activation { AlEntrar, ConE }

    [Header("Transición")]
    public Activation activation = Activation.ConE;
    public PlayerMovement player;
    [Tooltip("Dónde aparece el jugador (su eje Z azul es hacia donde mira).")]
    public Transform destination;
    [Tooltip("Bloqueada no funciona (por ejemplo hasta cortar los 5 hilos).")]
    public bool locked;
    [Tooltip("Opcional: se enciende al desbloquearse (por ejemplo una luz en la puerta).")]
    public GameObject showWhenUnlocked;

    public UnityEvent onTeleport;

    bool startLocked;

    void Awake()
    {
        startLocked = locked;
        RefreshVisual();
    }

    public override bool CanInteract => base.CanInteract && activation == Activation.ConE && !locked;

    public override void Interact(PlayerInteractor interactor) => Teleport();

    void OnTriggerEnter(Collider other)
    {
        if (activation != Activation.AlEntrar || locked || player == null) return;
        if (other.GetComponentInParent<PlayerMovement>() == player) Teleport();
    }

    public void Teleport()
    {
        if (player == null || destination == null) return;
        player.Teleport(destination.position, destination.rotation);
        onTeleport.Invoke();
    }

    public void Unlock()
    {
        locked = false;
        RefreshVisual();
    }

    public void Lock()
    {
        locked = true;
        RefreshVisual();
    }

    // Lo llama quien la controle al reiniciar el puzzle.
    public void ResetState()
    {
        locked = startLocked;
        RefreshVisual();
    }

    void RefreshVisual()
    {
        if (showWhenUnlocked != null) showWhenUnlocked.SetActive(!locked);
    }
}
