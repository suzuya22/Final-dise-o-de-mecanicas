using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Apagón del juego de las sillas: la luz parpadea, se va unos segundos y vuelve, y cuesta ver más.
// Baja la intensidad de las luces indicadas, el ambiente y una capa oscura de UI.
// Los fundidos son "cortados" (pocos pasos por segundo), estilo retro, como el resto de la UI.
public class ChairsBlackout : MonoBehaviour
{
    [Header("Luces de la escena")]
    [Tooltip("Luces que se apagan (por ejemplo la Directional Light).")]
    public Light[] lights;
    [Tooltip("Fracción de intensidad que les queda en lo más oscuro (0 = apagadas del todo).")]
    [Range(0f, 1f)] public float lightsLeft = 0.03f;
    [Tooltip("También oscurece la luz ambiental de la escena.")]
    public bool dimAmbient = true;
    [Range(0f, 1f)] public float ambientLeft = 0.05f;

    [Header("Capa oscura (UI)")]
    [Tooltip("Image negra a pantalla completa, en un Canvas por DEBAJO del HUD para que el texto y las barras se sigan viendo.")]
    public Image overlay;
    [Tooltip("Opacidad máxima de la capa. Más alto = cuesta más ver.")]
    [Range(0f, 1f)] public float overlayAlpha = 0.85f;

    [Header("Animación")]
    public float fadeTime = 0.3f;
    [Tooltip("Parpadeos de la luz antes de apagarse.")]
    public int flickers = 2;
    public float flickerTime = 0.07f;
    [Tooltip("Pasos por segundo de los fundidos (más bajo = más cortado).")]
    public float stepsPerSecond = 12f;

    public bool IsActive { get; private set; }

    float[] lightBase;
    Color ambientBase;
    float ambientIntensityBase;
    Coroutine routine;
    Coroutine blinkRoutine;

    void Awake()
    {
        lightBase = new float[lights != null ? lights.Length : 0];
        for (int i = 0; i < lightBase.Length; i++)
            if (lights[i] != null) lightBase[i] = lights[i].intensity;
        ambientBase = RenderSettings.ambientLight;
        ambientIntensityBase = RenderSettings.ambientIntensity;
        SetLevel(0f);
    }

    // Se apaga, se queda a oscuras "hold" segundos y vuelve.
    public void Trigger(float hold)
    {
        Cancel();
        routine = StartCoroutine(Run(hold));
    }

    // Oscurecimiento corto para disimular un cambio brusco (por ejemplo, el cambio de lugares).
    // "atDark" se ejecuta justo cuando la pantalla está más oscura.
    public void Blink(System.Action atDark)
    {
        if (IsActive || blinkRoutine != null)
        {
            atDark?.Invoke();   // ya está oscuro (o parpadeando): no hace falta otro parpadeo
            return;
        }
        blinkRoutine = StartCoroutine(BlinkRun(atDark));
    }

    IEnumerator BlinkRun(System.Action atDark)
    {
        for (int i = 1; i <= 3; i++) { SetLevel(i / 3f); yield return new WaitForSeconds(0.04f); }
        atDark?.Invoke();
        yield return new WaitForSeconds(0.15f);
        for (int i = 2; i >= 0; i--) { SetLevel(i / 3f); yield return new WaitForSeconds(0.05f); }
        blinkRoutine = null;
    }

    // Vuelve a la luz normal al instante (por ejemplo al reiniciar el juego).
    public void Cancel()
    {
        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = null;
        if (routine != null) StopCoroutine(routine);
        routine = null;
        IsActive = false;
        SetLevel(0f);
    }

    IEnumerator Run(float hold)
    {
        IsActive = true;

        for (int i = 0; i < flickers; i++)
        {
            SetLevel(0.55f);
            yield return new WaitForSeconds(flickerTime);
            SetLevel(0f);
            yield return new WaitForSeconds(flickerTime);
        }

        yield return Fade(0f, 1f);
        yield return new WaitForSeconds(hold);
        yield return Fade(1f, 0f);

        SetLevel(0f);
        IsActive = false;
        routine = null;
    }

    IEnumerator Fade(float from, float to)
    {
        int steps = Mathf.Max(1, Mathf.RoundToInt(stepsPerSecond * fadeTime));
        for (int i = 1; i <= steps; i++)
        {
            SetLevel(Mathf.Lerp(from, to, (float)i / steps));
            yield return new WaitForSeconds(fadeTime / steps);
        }
    }

    // 0 = luz normal, 1 = a oscuras.
    void SetLevel(float level)
    {
        for (int i = 0; i < lightBase.Length; i++)
            if (lights[i] != null) lights[i].intensity = Mathf.Lerp(lightBase[i], lightBase[i] * lightsLeft, level);

        if (dimAmbient)
        {
            float k = Mathf.Lerp(1f, ambientLeft, level);
            RenderSettings.ambientLight = ambientBase * k;
            RenderSettings.ambientIntensity = ambientIntensityBase * k;
        }

        if (overlay != null)
        {
            var c = overlay.color;
            c.a = overlayAlpha * level;
            overlay.color = c;
        }
    }

    // Si se desactiva o se destruye a mitad, deja la escena como estaba.
    void OnDisable()
    {
        if (lightBase != null) Cancel();
    }
}
