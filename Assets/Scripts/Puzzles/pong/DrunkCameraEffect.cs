using UnityEngine;

// Efecto de borracho: la cámara se balancea, se inclina y "respira".
// Ponlo en la cámara. Si la cámara la mueve otro script, conviene que la cámara
// sea hija de un objeto "rig" y que ese otro script mueva el rig.
public class DrunkCameraEffect : MonoBehaviour
{
    [Header("Borrachera")]
    [Range(0f, 1f)] public float drunkLevel = 0f;
    [Tooltip("Qué tan rápido se adapta el efecto cuando cambia el nivel.")]
    public float smoothing = 2f;
    [Tooltip("Cómo crece la intensidad según el nivel (por defecto suave al principio).")]
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Intensidad máxima (nivel = 1)")]
    [Tooltip("Desplazamiento de la cámara en metros.")]
    public float maxPositionSway = 0.08f;
    [Tooltip("Cabeceo y giro lateral en grados.")]
    public float maxPitchYaw = 5f;
    [Tooltip("Inclinación lateral en grados.")]
    public float maxRoll = 8f;
    [Tooltip("Variación del campo de visión en grados.")]
    public float maxFovPulse = 4f;

    [Header("Velocidad del balanceo")]
    public float minFrequency = 0.15f;
    public float maxFrequency = 0.6f;

    Vector3 basePosition;
    Quaternion baseRotation;
    float baseFov;
    Camera cam;
    float currentLevel;
    float noiseTime;
    float[] seeds;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
        if (cam != null) baseFov = cam.fieldOfView;

        seeds = new float[7];
        for (int i = 0; i < seeds.Length; i++) seeds[i] = Random.Range(0f, 100f);
    }

    void OnDisable()
    {
        transform.localPosition = basePosition;
        transform.localRotation = baseRotation;
        if (cam != null) cam.fieldOfView = baseFov;
    }

    // Para controlarlo desde otros sistemas (cordura, bebidas...).
    public void SetDrunkLevel(float level)
    {
        drunkLevel = Mathf.Clamp01(level);
    }

    void LateUpdate()
    {
        currentLevel = Mathf.Lerp(currentLevel, drunkLevel, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        float k = intensityCurve.Evaluate(currentLevel);

        noiseTime += Time.deltaTime * Mathf.Lerp(minFrequency, maxFrequency, currentLevel);

        Vector3 pos = new Vector3(Noise(0), Noise(1) * 0.6f, Noise(2) * 0.3f) * (maxPositionSway * k);

        // El roll suma un vaivén lento tipo "barco" al ruido.
        float roll = (Noise(5) + Mathf.Sin(noiseTime * Mathf.PI) * 0.5f) * maxRoll;
        Vector3 rot = new Vector3(Noise(3) * maxPitchYaw, Noise(4) * maxPitchYaw, roll) * k;

        transform.localPosition = basePosition + pos;
        transform.localRotation = baseRotation * Quaternion.Euler(rot);

        if (cam != null)
            cam.fieldOfView = baseFov + Noise(6) * maxFovPulse * k;
    }

    // Ruido suave entre -1 y 1.
    float Noise(int channel)
    {
        return Mathf.Clamp01(Mathf.PerlinNoise(seeds[channel], noiseTime)) * 2f - 1f;
    }
}
