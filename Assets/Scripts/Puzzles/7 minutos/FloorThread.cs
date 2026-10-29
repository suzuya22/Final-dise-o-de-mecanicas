using UnityEngine;

// Hilo en el suelo del pasillo. Pulsar E lo tensa durante un momento.
// Si la persona se lanza hacia el lado de este hilo mientras está tenso, tropieza y cae.
// Su posición es lo que decide de qué lado está: colócalo a un lado del pasillo.
public class FloorThread : Interactable
{
    [Header("Tensión")]
    [Tooltip("Segundos que dura tenso.")]
    public float tenseDuration = 1f;
    [Tooltip("Segundos de espera antes de poder volver a tensarlo.")]
    public float cooldown = 1.5f;

    [Header("Visual (opcional)")]
    [Tooltip("Línea del hilo; se dibuja de Start Point a End Point.")]
    public LineRenderer line;
    public Transform startPoint;
    public Transform endPoint;
    [Tooltip("Cuánto cuelga el hilo cuando está flojo.")]
    public float slackSag = 0.15f;
    [Tooltip("Cuánto se eleva el hilo cuando está tenso.")]
    public float tenseLift = 0.25f;

    public bool IsTense => Time.time < tenseUntil;

    float tenseUntil = -1f;
    float readyAt = -1f;
    readonly Vector3[] points = new Vector3[3];

    void Reset()
    {
        prompt = "Tensar hilo";
        holdTime = 0f;
    }

    public override bool CanInteract => base.CanInteract && Time.time >= readyAt;

    public override void Interact(PlayerInteractor player)
    {
        tenseUntil = Time.time + tenseDuration;
        readyAt = tenseUntil + cooldown;
    }

    void Update()
    {
        if (line == null || startPoint == null || endPoint == null) return;

        Vector3 a = startPoint.position;
        Vector3 b = endPoint.position;
        Vector3 mid = (a + b) * 0.5f + Vector3.up * (IsTense ? tenseLift : -slackSag);
        points[0] = a;
        points[1] = mid;
        points[2] = b;
        line.positionCount = 3;
        line.SetPositions(points);
    }

    public void ResetState()
    {
        tenseUntil = -1f;
        readyAt = -1f;
    }
}
