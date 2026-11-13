using UnityEngine;

// Hace "respirar" un panel de la pared: lo empuja despacio hacia el centro de la sala y lo suelta.
// Va en cada Panel_Respiracion de la sala de las sillas (cada uno con su propia fase para que no respiren a la vez).
public class BreathingPanel : MonoBehaviour
{
    [Tooltip("Punto hacia el que se infla el panel (el objeto Centro de la sala).")]
    public Transform roomCenter;
    [Tooltip("Metros que avanza hacia el centro al inflarse.")]
    public float amplitude = 0.25f;
    [Tooltip("Segundos que dura una respiración completa.")]
    public float period = 5f;
    [Tooltip("Desfase de 0 a 1 para que los paneles no respiren a la vez.")]
    [Range(0f, 1f)] public float phase;
    [Tooltip("Cuánto crece además, en proporción (0 = solo se desplaza).")]
    public float stretch = 0.03f;

    Vector3 basePosition;
    Vector3 baseScale;
    Vector3 direction;

    void Awake()
    {
        basePosition = transform.position;
        baseScale = transform.localScale;
        Vector3 toCenter = (roomCenter != null ? roomCenter.position : Vector3.zero) - basePosition;
        toCenter.y = 0f;
        direction = toCenter.sqrMagnitude > 0.0001f ? toCenter.normalized : Vector3.zero;
    }

    void Update()
    {
        // 0 = desinflado, 1 = inflado, con curva suave.
        float k = 0.5f - 0.5f * Mathf.Cos((Time.time / Mathf.Max(0.1f, period) + phase) * Mathf.PI * 2f);
        transform.position = basePosition + direction * (amplitude * k);
        transform.localScale = baseScale * (1f + stretch * k);
    }
}
