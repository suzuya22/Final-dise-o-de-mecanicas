using UnityEngine;
using UnityEngine.Events;

// Hilo que se jala manteniendo E. Al completarse, se corta (desaparece) y avisa al ThreadDoorPuzzle.
public class PullableThread : Interactable
{
    [Header("Hilo")]
    [Tooltip("Lo que se estira mientras se jala (el modelo del hilo). Si está vacío no se anima.")]
    public Transform visual;
    [Tooltip("Cuánto se estira al jalar del todo (0.3 = 30 %).")]
    public float stretchAmount = 0.3f;
    [Tooltip("Eje local del modelo a lo largo del hilo.")]
    public Vector3 stretchAxis = Vector3.up;
    [Tooltip("Cuánto tiembla mientras se jala.")]
    public float shakeAmount = 0.01f;
    [Tooltip("Otros objetos que desaparecen al cortarlo (por ejemplo el modelo del hilo dentro del FBX del nivel).")]
    public GameObject[] hideOnCut;

    public UnityEvent onCut;

    public bool IsCut { get; private set; }

    Vector3 visualBaseScale;
    Vector3 visualBasePosition;

    void Reset()
    {
        prompt = "Jalar hilo";
        holdTime = 1.2f;
    }

    void Awake()
    {
        if (visual != null)
        {
            visualBaseScale = visual.localScale;
            visualBasePosition = visual.localPosition;
        }
    }

    public override bool CanInteract => base.CanInteract && !IsCut;

    public override void OnHoldProgress(float progress)
    {
        if (visual == null) return;
        visual.localScale = visualBaseScale + Vector3.Scale(visualBaseScale, stretchAxis.normalized) * (stretchAmount * progress);
        visual.localPosition = visualBasePosition + (progress > 0f ? Random.insideUnitSphere * (shakeAmount * progress) : Vector3.zero);
    }

    public override void Interact(PlayerInteractor player)
    {
        if (IsCut) return;
        IsCut = true;
        OnHoldProgress(0f);
        onCut.Invoke();
        SetExtrasActive(false);
        gameObject.SetActive(false);
    }

    public void ResetState()
    {
        IsCut = false;
        gameObject.SetActive(true);
        SetExtrasActive(true);
        OnHoldProgress(0f);
    }

    void SetExtrasActive(bool active)
    {
        if (hideOnCut == null) return;
        foreach (var go in hideOnCut)
            if (go != null) go.SetActive(active);
    }
}
