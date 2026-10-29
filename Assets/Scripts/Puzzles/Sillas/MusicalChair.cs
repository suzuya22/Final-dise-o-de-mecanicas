using UnityEngine;

// Silla del juego de las sillas musicales. El jugador se sienta con E cuando para la música;
// los NPCs la ocupan llegando a ella. Mientras alguien está sentado puede "morder" (tiembla).
public class MusicalChair : Interactable
{
    [Header("Silla")]
    [Tooltip("Dónde queda el participante sentado (su eje Z azul es hacia donde mira). Si está vacío se usa la silla.")]
    public Transform seatPoint;
    [Tooltip("Lo que tiembla cuando la silla muerde (el modelo). Si está vacío no tiembla.")]
    public Transform visual;
    public float biteShake = 0.03f;

    public Component Occupant { get; private set; }
    public bool IsFree => Occupant == null && gameObject.activeInHierarchy;
    public Transform Seat => seatPoint != null ? seatPoint : transform;

    MusicalChairsGame game;
    bool biting;
    Vector3 visualBasePosition;

    void Reset()
    {
        prompt = "Sentarse";
        holdTime = 0f;
    }

    void Awake()
    {
        if (visual != null) visualBasePosition = visual.localPosition;
    }

    public void Init(MusicalChairsGame owner) => game = owner;

    // Solo se puede usar cuando el juego abre las sillas (al parar la música).
    public override bool CanInteract => base.CanInteract && IsFree && game != null && game.ChairsOpen;

    public override void Interact(PlayerInteractor player)
    {
        if (game != null) game.PlayerTrySit(this);
    }

    public bool TryOccupy(Component who)
    {
        if (!IsFree) return false;
        Occupant = who;
        return true;
    }

    public void Release()
    {
        Occupant = null;
        SetBiting(false);
    }

    public void SetBiting(bool value)
    {
        biting = value;
        if (!biting && visual != null) visual.localPosition = visualBasePosition;
    }

    void Update()
    {
        if (biting && visual != null)
            visual.localPosition = visualBasePosition + Random.insideUnitSphere * biteShake;
    }

    public void ResetState()
    {
        Release();
        gameObject.SetActive(true);
    }
}
