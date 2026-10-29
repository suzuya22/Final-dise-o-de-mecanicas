using UnityEngine;
using UnityEngine.UI;

// Barra de cordura: rellena una Image (Image Type = Filled) según la cordura actual.
public class SanityBarUI : MonoBehaviour
{
    public SanityManager sanity;
    [Tooltip("Image con Image Type = Filled.")]
    public Image fillImage;
    [Tooltip("Qué tan rápido sigue la barra al valor real.")]
    public float smoothSpeed = 8f;

    [Header("Color")]
    public Color fullColor = new Color(1f, 0.55f, 0.85f);
    public Color emptyColor = new Color(0.45f, 0.1f, 0.35f);

    float shown = 1f;

    void Update()
    {
        if (sanity == null || fillImage == null) return;

        shown = Mathf.Lerp(shown, sanity.Normalized, 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime));
        fillImage.fillAmount = shown;
        fillImage.color = Color.Lerp(emptyColor, fullColor, shown);
    }
}
