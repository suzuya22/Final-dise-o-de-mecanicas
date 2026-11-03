using UnityEngine;

// Sube y baja el objeto de forma "cortada", estilo retro: pocos fotogramas por segundo
// y saltos de pixel entero, no un movimiento suave. Es sutil por defecto.
// Usa tiempo sin escalar porque el juego está congelado con el inventario abierto.
public class InventoryIconBob : MonoBehaviour
{
    [Tooltip("Cuánto sube y baja desde su posición, en unidades del RectTransform " +
             "(en el prefab del inventario 1 unidad = 1 píxel del arte).")]
    public float amplitude = 1.5f;
    [Tooltip("Tamaño de cada escalón. 1 = salta de pixel en pixel.")]
    public float stepSize = 1f;
    [Tooltip("Segundos que tarda en subir y bajar una vez.")]
    public float period = 1.4f;
    [Tooltip("Fotogramas por segundo del movimiento. Más bajo = más cortado.")]
    public float stepsPerSecond = 6f;
    [Tooltip("Desfase del ciclo (0 a 1). Distintos valores en cada elemento hacen que no suban todos a la vez.")]
    [Range(0f, 1f)] public float phase = 0f;

    RectTransform rt;
    Vector2 basePosition;
    bool initialized;

    void OnEnable()
    {
        if (!initialized)
        {
            rt = (RectTransform)transform;
            basePosition = rt.anchoredPosition;
            initialized = true;
        }
    }

    void Update()
    {
        // El tiempo se congela en fotogramas de 1/stepsPerSecond, y el valor en escalones de stepSize.
        float t = Mathf.Floor(Time.unscaledTime * stepsPerSecond) / stepsPerSecond;
        float wave = Mathf.Sin((t / Mathf.Max(0.01f, period) + phase) * Mathf.PI * 2f) * amplitude;
        float snapped = stepSize > 0f ? Mathf.Round(wave / stepSize) * stepSize : wave;
        rt.anchoredPosition = basePosition + Vector2.up * snapped;
    }

    void OnDisable()
    {
        if (initialized) rt.anchoredPosition = basePosition;
    }
}
