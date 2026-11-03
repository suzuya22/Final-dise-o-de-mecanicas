using UnityEngine;
using UnityEngine.UI;

// Barras de UI de los dos QTE de las sillas. Solo muestra lo que ya calculan MashQTE y TimingQTE:
// no tiene lógica de juego propia.
//  - Forcejeo (te sientas y la silla te muerde): la barra se va llenando con las pulsaciones.
//  - Prueba de tiempo (te arrastran al centro): una marca recorre la barra y hay que pulsar sobre la zona rosa.
// En cada QTE se elige al azar una de las barras de "Barras" (relojes, ojos, hongos...).
// Las barras laten: saltan en pasos de pixel a pocos fotogramas por segundo (estilo retro), más rápido y
// más rojizas cuanto menos tiempo queda, y dan un golpe con cada pulsación.
public class ChairsQTEBars : MonoBehaviour
{
    [Header("QTE (la lógica sigue siendo del minijuego)")]
    public MashQTE mash;
    public TimingQTE timing;

    [Header("Aspecto de la barra")]
    [Tooltip("Variantes de barra. En cada QTE se elige una al azar. Todas deben tener la misma forma.")]
    public Sprite[] barSprites;
    [Tooltip("Columna del sprite (en píxeles) donde empieza la barra dibujada.")]
    public float contentLeft = 40f;
    [Tooltip("Ancho de la barra dibujada, en píxeles del sprite.")]
    public float contentWidth = 176f;
    [Tooltip("Alto de la barra dibujada, en píxeles del sprite.")]
    public float contentHeight = 18f;

    [Header("Forcejeo (te sientas)")]
    public GameObject mashRoot;
    [Tooltip("Barra vacía, de fondo (se oscurece sola).")]
    public Image mashBackground;
    [Tooltip("Misma barra encima, con Image Type = Filled / Horizontal. Se rellena con el progreso.")]
    public Image mashFill;
    public Color mashEmptyTint = new Color(0.35f, 0.3f, 0.5f, 1f);

    [Header("Prueba de tiempo (te atrapan)")]
    public GameObject timingRoot;
    public Image timingBackground;
    [Tooltip("Cuadro rosa (Area to hit). Hijo de la barra, con anclas en el centro vertical a la izquierda.")]
    public RectTransform zone;
    [Tooltip("Marca que recorre la barra. Hijo de la barra, con las mismas anclas que la zona.")]
    public RectTransform marker;
    [Tooltip("Parte interior de la marca, para colorearla según el resultado.")]
    public Image markerFill;
    public Color markerColor = Color.white;
    public Color hitColor = new Color(0.45f, 1f, 0.9f);
    public Color missColor = new Color(1f, 0.3f, 0.35f);

    [Header("Pulsos (estilo retro)")]
    [Tooltip("Latidos por segundo con mucho tiempo.")]
    public float pulseRateCalm = 1.2f;
    [Tooltip("Latidos por segundo cuando se acaba el tiempo.")]
    public float pulseRateUrgent = 6f;
    [Tooltip("Cuántos píxeles del arte salta la barra en cada latido.")]
    public int hopPixels = 2;
    [Tooltip("Fotogramas por segundo de la animación. Más bajo = más cortado.")]
    public float stepsPerSecond = 12f;
    [Tooltip("Color al que se tiñe la barra, en cada latido, cuando queda poco tiempo.")]
    public Color urgentTint = new Color(1f, 0.45f, 0.5f, 1f);
    [Tooltip("Desde qué urgencia (0 a 1) empieza a teñirse.")]
    [Range(0f, 1f)] public float tintFromUrgency = 0.5f;
    [Tooltip("Píxeles extra que salta la barra con cada pulsación del forcejeo.")]
    public int pressKickPixels = 2;

    // Estado de animación de una barra.
    class Pulse
    {
        public RectTransform rect;
        public Vector2 basePosition;
        public float phase, kick, nextTick, shown;
        public int lastPresses;
    }

    readonly Pulse mashPulse = new Pulse();
    readonly Pulse timingPulse = new Pulse();
    bool mashShown, timingShown;

    void Awake()
    {
        if (mashRoot != null) { mashPulse.rect = (RectTransform)mashRoot.transform; mashPulse.basePosition = mashPulse.rect.anchoredPosition; mashRoot.SetActive(false); }
        if (timingRoot != null) { timingPulse.rect = (RectTransform)timingRoot.transform; timingPulse.basePosition = timingPulse.rect.anchoredPosition; timingRoot.SetActive(false); }
    }

    void LateUpdate()
    {
        UpdateMash();
        UpdateTiming();
    }

    void UpdateMash()
    {
        bool show = mash != null && mash.IsRunning;
        if (show != mashShown)
        {
            mashShown = show;
            if (mashRoot != null) mashRoot.SetActive(show);
            if (show)
            {
                var sprite = RandomSprite();
                if (sprite != null)
                {
                    if (mashBackground != null) mashBackground.sprite = sprite;
                    if (mashFill != null) mashFill.sprite = sprite;
                }
                mashPulse.phase = 0f; mashPulse.kick = 0f; mashPulse.lastPresses = 0;
            }
        }
        if (!show || mashFill == null || mashFill.sprite == null) return;

        // La barra dibujada no ocupa todo el sprite, así que el relleno se ajusta a su zona.
        float w = mashFill.sprite.rect.width;
        mashFill.fillAmount = (contentLeft + Mathf.Clamp01(mash.Progress) * contentWidth) / w;

        // Cada pulsación da un golpe a la barra.
        if (mash.Presses > mashPulse.lastPresses) mashPulse.kick = 1f;
        mashPulse.lastPresses = mash.Presses;

        float urgency = mash.timeLimit > 0f ? 1f - Mathf.Clamp01(mash.TimeLeft / mash.timeLimit) : 0f;
        bool tinted;
        Animate(mashPulse, mashFill.rectTransform.rect.width / w, urgency, out tinted);
        mashBackground.color = tinted ? Color.Lerp(mashEmptyTint, urgentTint, 0.6f) : mashEmptyTint;
        mashFill.color = tinted ? urgentTint : Color.white;
    }

    void UpdateTiming()
    {
        bool show = timing != null && timing.IsRunning;
        if (show != timingShown)
        {
            timingShown = show;
            if (timingRoot != null) timingRoot.SetActive(show);
            if (show)
            {
                var sprite = RandomSprite();
                if (sprite != null && timingBackground != null) timingBackground.sprite = sprite;
                timingPulse.phase = 0f; timingPulse.kick = 0f;
            }
        }
        if (!show || timingBackground == null || timingBackground.sprite == null) return;

        // Escala de píxel del sprite a la pantalla.
        float s = timingBackground.rectTransform.rect.width / timingBackground.sprite.rect.width;
        float width = contentWidth * s;
        float left = contentLeft * s;

        if (zone != null)
        {
            zone.sizeDelta = new Vector2(width * timing.zoneWidth, contentHeight * s);
            zone.anchoredPosition = new Vector2(left + width * timing.ZoneCenter, 0f);
        }
        if (marker != null)
        {
            marker.sizeDelta = new Vector2(marker.sizeDelta.x, (contentHeight + 8f) * s);
            marker.anchoredPosition = new Vector2(left + width * timing.Marker, 0f);
        }
        if (markerFill != null)
            markerFill.color = timing.LastResult == true ? hitColor : timing.LastResult == false ? missColor : markerColor;

        // Con la pulsación ya resuelta no hay urgencia; mientras se espera, crece al acabarse el intento.
        float urgency = timing.LastResult != null || timing.timePerPrompt <= 0f
            ? 0f : 1f - Mathf.Clamp01(timing.PromptTimeLeft / timing.timePerPrompt);
        bool tinted;
        Animate(timingPulse, s, urgency, out tinted);
        timingBackground.color = tinted ? urgentTint : Color.white;
    }

    // Latido en pasos: la barra salta unos píxeles del arte y cae, a pocos fps.
    void Animate(Pulse p, float pixelScale, float urgency, out bool tinted)
    {
        float dt = Time.deltaTime;
        float rate = Mathf.Lerp(pulseRateCalm, pulseRateUrgent, urgency);
        p.phase += rate * dt;
        p.kick = Mathf.MoveTowards(p.kick, 0f, dt * 6f);

        if (Time.time >= p.nextTick)
        {
            p.nextTick = Time.time + 1f / Mathf.Max(1f, stepsPerSecond);
            float beat = 1f - (p.phase - Mathf.Floor(p.phase)); // 1 al latir, cae a 0
            float hop = Mathf.Round(beat * beat * hopPixels);
            float kick = Mathf.Round(p.kick * pressKickPixels);
            p.shown = Mathf.Max(hop, kick);
        }

        if (p.rect != null)
            p.rect.anchoredPosition = p.basePosition + Vector2.up * (p.shown * pixelScale);

        // Se tiñe solo en lo alto del latido, y solo cuando ya queda poco tiempo.
        tinted = urgency >= tintFromUrgency && p.shown >= 1f;
    }

    Sprite RandomSprite()
    {
        if (barSprites == null || barSprites.Length == 0) return null;
        return barSprites[Random.Range(0, barSprites.Length)];
    }
}
