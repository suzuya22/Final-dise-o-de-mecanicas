using TMPro;
using UnityEngine;

// Efecto "loco" para el título: se inclina rápido en todas direcciones sin llegar a voltearse
// (siempre se ve de frente) y se le añade profundidad falsa con copias del texto detrás,
// para que parezcan letras 3D con volumen.
public class PauseTitleWobble : MonoBehaviour
{
    [Header("Texto")]
    [Tooltip("El texto TextMeshPro al que se le aplica el efecto.")]
    public TextMeshProUGUI titleText;

    [Header("Movimiento")]
    [Tooltip("Inclinación máxima hacia adelante/atrás, en grados.")]
    public float maxTiltX = 25f;
    [Tooltip("Inclinación máxima a los lados, en grados.")]
    public float maxTiltY = 35f;
    [Tooltip("Giro máximo de frente (como un volante), en grados.")]
    public float maxRollZ = 6f;
    [Tooltip("Velocidad del movimiento.")]
    public float speed = 2.5f;
    [Tooltip("Temblor de posición, en píxeles.")]
    public float positionJitter = 6f;
    [Tooltip("Cuánto crece y se encoge (0.05 = 5 %).")]
    public float scalePulse = 0.05f;

    [Header("Profundidad 3D")]
    [Tooltip("Copias del texto detrás para dar volumen. 0 = sin profundidad.")]
    public int depthLayers = 8;
    [Tooltip("Separación entre copias, en píxeles.")]
    public float depthStep = 1.5f;
    [Tooltip("Color del lado/volumen de las letras.")]
    public Color depthColor = new Color(0.45f, 0.1f, 0.4f);
    [Tooltip("Hacia dónde se ve el volumen cuando el título está quieto.")]
    public Vector2 depthDirection = new Vector2(1f, -1f);

    RectTransform rt;
    Vector2 basePosition;
    Quaternion baseRotation;
    Vector3 baseScale;
    RectTransform[] layers;
    float noiseTime;
    float seed;

    void Awake()
    {
        if (titleText == null)
        {
            Debug.LogWarning("PauseTitleWobble: falta asignar el texto del título.", this);
            enabled = false;
            return;
        }

        rt = titleText.rectTransform;
        basePosition = rt.anchoredPosition;
        baseRotation = rt.localRotation;
        baseScale = rt.localScale;
        seed = Random.Range(0f, 100f);
        CreateLayers();
    }

    void CreateLayers()
    {
        layers = new RectTransform[Mathf.Max(0, depthLayers)];

        // Se crean de la más lejana a la más cercana, justo antes del título,
        // para que se dibujen detrás de él.
        for (int i = layers.Length - 1; i >= 0; i--)
        {
            // Se duplica el texto original para que tenga exactamente la misma configuración.
            var go = Instantiate(titleText.gameObject, rt.parent);
            go.name = $"{titleText.name}_Profundidad{i}";
            var layerRt = (RectTransform)go.transform;
            layerRt.SetSiblingIndex(rt.GetSiblingIndex());

            // La copia solo necesita el texto; se quita cualquier otro script que tuviera.
            foreach (var mb in go.GetComponents<MonoBehaviour>())
                if (!(mb is TextMeshProUGUI)) Destroy(mb);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.enableVertexGradient = false;
            // Las capas más lejanas son un poco más oscuras.
            text.color = Color.Lerp(depthColor, depthColor * 0.6f, (float)i / Mathf.Max(1, layers.Length - 1));

            layers[i] = layerRt;
        }
    }

    // Tiempo sin escalar: el menú de pausa congela el juego.
    void LateUpdate()
    {
        // Las capas siguen al título si este se oculta.
        bool visible = titleText.gameObject.activeInHierarchy;
        foreach (var l in layers)
            if (l.gameObject.activeSelf != titleText.gameObject.activeSelf) l.gameObject.SetActive(titleText.gameObject.activeSelf);
        if (!visible) return;

        noiseTime += Time.unscaledDeltaTime * speed;

        float tiltX = Noise(0f);
        float tiltY = Noise(10f);

        Quaternion rotation = baseRotation * Quaternion.Euler(tiltX * maxTiltX, tiltY * maxTiltY, Noise(20f) * maxRollZ);
        Vector2 position = basePosition + new Vector2(Noise(30f), Noise(40f)) * positionJitter;
        Vector3 scale = baseScale * (1f + Noise(50f) * scalePulse);

        rt.localRotation = rotation;
        rt.anchoredPosition = position;
        rt.localScale = scale;

        // El volumen se desplaza según hacia dónde está inclinado el título.
        Vector2 dir = depthDirection.normalized + new Vector2(-tiltY, tiltX) * 0.8f;
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i].localRotation = rotation;
            layers[i].localScale = scale;
            layers[i].anchoredPosition = position + dir * (depthStep * (i + 1));
        }
    }

    // Ruido suave entre -1 y 1.
    float Noise(float offset)
    {
        return Mathf.Clamp01(Mathf.PerlinNoise(seed + offset, noiseTime)) * 2f - 1f;
    }
}
