using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Textos emergentes del beer pong: "¡REBOTE!" junto al punto donde rebota la pelota (crece y cambia de color
// con cada rebote seguido) y avisos centrales ("¡PIRÁMIDE!"). Solo es visual: no tiene lógica de juego.
// Va en un Canvas propio; la plantilla es un TMP desactivado que se duplica para cada popup.
public class PongPopupUI : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Cámara que ve el minijuego (para pasar el punto del rebote a pantalla). Si está vacía se usa la Main Camera.")]
    public Camera worldCamera;
    [Tooltip("RectTransform del Canvas donde aparecen los textos.")]
    public RectTransform canvasRect;
    [Tooltip("Texto de plantilla (desactivado). Se duplica para cada popup.")]
    public TMP_Text template;

    [Header("Rebote")]
    public string bounceText = "¡REBOTE!";
    [Tooltip("Del primer rebote al último: el color va cambiando. Si hay más rebotes, se queda en el último.")]
    public Color[] bounceColors =
    {
        new Color(1f, 1f, 1f),        // 1: blanco
        new Color(1f, 0.95f, 0.45f),  // 2: amarillo
        new Color(1f, 0.65f, 0.3f),   // 3: naranja
        new Color(1f, 0.4f, 0.7f),    // 4: rosa
        new Color(0.85f, 0.35f, 1f),  // 5+: morado
    };
    [Tooltip("Tamaño de letra del primer rebote.")]
    public float baseSize = 46f;
    [Tooltip("Cuánto crece el texto con cada rebote extra (0.3 = +30 %).")]
    public float growthPerBounce = 0.3f;
    [Tooltip("Tope del crecimiento (3 = hasta el triple).")]
    public float maxGrowth = 2.6f;
    [Tooltip("Desplazamiento respecto al punto del rebote, en unidades del canvas (hacia el lado y arriba).")]
    public Vector2 offset = new Vector2(90f, 40f);

    [Header("Animación")]
    [Tooltip("Segundos que dura el popup.")]
    public float lifetime = 1f;
    [Tooltip("Cuánto sube mientras dura, en unidades del canvas.")]
    public float rise = 60f;
    [Tooltip("Cuánto se infla al aparecer (rebote de resorte).")]
    public float popScale = 1.6f;

    [Header("Avisos centrales")]
    [Tooltip("Posición de los avisos (por ejemplo \"¡PIRÁMIDE!\") desde el centro de la pantalla.")]
    public Vector2 messagePosition = new Vector2(0f, 330f);
    public float messageSize = 90f;
    public float messageLifetime = 1.8f;

    class Popup
    {
        public TMP_Text text;
        public float age, life, rise;
        public Vector2 start;
        public float size;
        public Color color;
        public bool active;
    }

    readonly List<Popup> popups = new List<Popup>();

    void Awake()
    {
        if (template != null) template.gameObject.SetActive(false);
    }

    // Lo llama el BeerPongManager en cada rebote de la pelota.
    public void ShowBounce(Vector3 worldPoint, int count)
    {
        var cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null || canvasRect == null || template == null) return;

        Vector3 screen = cam.WorldToScreenPoint(worldPoint);
        if (screen.z <= 0f) return; // detrás de la cámara

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local)) return;

        int i = Mathf.Clamp(count - 1, 0, Mathf.Max(0, bounceColors.Length - 1));
        float growth = Mathf.Min(maxGrowth, 1f + growthPerBounce * (count - 1));
        string label = count <= 1 ? bounceText : $"{bounceText} x{count}";
        Spawn(label, bounceColors.Length > 0 ? bounceColors[i] : Color.white, baseSize * growth, local + offset, lifetime, rise);
    }

    // Aviso en el centro de la pantalla.
    public void ShowMessage(string message, Color color)
    {
        if (canvasRect == null || template == null) return;
        Spawn(message, color, messageSize, messagePosition, messageLifetime, rise * 0.5f);
    }

    void Spawn(string label, Color color, float size, Vector2 position, float life, float riseAmount)
    {
        var p = GetFree();
        p.text.text = label;
        p.text.color = color;
        p.text.fontSize = size;
        p.size = size;
        p.color = color;
        p.start = position;
        p.age = 0f;
        p.life = life;
        p.rise = riseAmount;
        p.active = true;
        p.text.rectTransform.anchoredPosition = position;
        p.text.gameObject.SetActive(true);
        p.text.transform.SetAsLastSibling();
    }

    Popup GetFree()
    {
        foreach (var p in popups) if (!p.active) return p;

        var t = Instantiate(template, template.transform.parent);
        t.raycastTarget = false;
        var created = new Popup { text = t };
        popups.Add(created);
        return created;
    }

    void Update()
    {
        foreach (var p in popups)
        {
            if (!p.active) continue;

            p.age += Time.deltaTime;
            float k = Mathf.Clamp01(p.age / p.life);
            if (k >= 1f)
            {
                p.active = false;
                p.text.gameObject.SetActive(false);
                continue;
            }

            // Se infla de golpe y se asienta como un resorte; sube; en el último tramo se desvanece.
            float spring = 1f + (popScale - 1f) * Mathf.Exp(-7f * p.age) * Mathf.Cos(p.age * 22f);
            p.text.rectTransform.localScale = Vector3.one * spring;
            p.text.rectTransform.anchoredPosition = p.start + Vector2.up * (p.rise * k);

            var c = p.color;
            c.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
            p.text.color = c;
        }
    }
}
