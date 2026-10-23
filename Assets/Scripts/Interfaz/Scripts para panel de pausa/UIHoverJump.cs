using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Al pasar el mouse sobre el texto o el icono asignados, ambos saltan
// un poco hacia arriba y cambian de color.
// Puede ir en cualquier objeto; lo importante es asignar el texto y el icono.
// Funciona aunque el texto sea hijo del icono (o al revés).
public class UIHoverJump : MonoBehaviour
{
    [Header("Elementos a animar")]
    public TMP_Text text;
    public Image icon;

    [Header("Salto")]
    [Tooltip("Cuánto suben, en píxeles.")]
    public float jumpHeight = 8f;
    [Tooltip("Qué tan rápido suben y bajan.")]
    public float speed = 14f;

    [Header("Color")]
    public Color hoverColor = new Color(1f, 0.45f, 0.8f);
    public bool tintText = true;
    public bool tintIcon = true;

    RectTransform textRt, iconRt;
    Vector2 textBasePosition, iconBasePosition;
    Color textBaseColor, iconBaseColor;
    bool moveText, moveIcon;
    Camera canvasCamera;
    float current;

    void Awake()
    {
        if (text != null)
        {
            textRt = text.rectTransform;
            textBasePosition = textRt.anchoredPosition;
            textBaseColor = text.color;
        }
        if (icon != null)
        {
            iconRt = icon.rectTransform;
            iconBasePosition = iconRt.anchoredPosition;
            iconBaseColor = icon.color;
        }

        // Si uno es hijo del otro, solo se mueve el padre (el hijo lo sigue solo).
        moveIcon = iconRt != null && (textRt == null || !iconRt.IsChildOf(textRt));
        moveText = textRt != null && (iconRt == null || !textRt.IsChildOf(iconRt));

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null && textRt != null) canvas = textRt.GetComponentInParent<Canvas>();
        if (canvas == null && iconRt != null) canvas = iconRt.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = canvas.rootCanvas.worldCamera;
    }

    // Tiempo sin escalar: el menú de pausa congela el juego.
    void Update()
    {
        float target = IsMouseOver() ? 1f : 0f;
        if (Mathf.Approximately(current, target)) return;

        current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        if (Mathf.Abs(current - target) < 0.001f) current = target;
        Apply();
    }

    // Comprueba el mouse contra la posición original (sin el salto),
    // para que no parpadee cuando el texto sube y deja de estar bajo el mouse.
    bool IsMouseOver()
    {
        var mouse = Mouse.current;
        if (mouse == null) return false;

        Vector2 mousePos = mouse.position.ReadValue();
        return IsOver(textRt, mousePos) || IsOver(iconRt, mousePos);
    }

    bool IsOver(RectTransform rt, Vector2 mousePos)
    {
        if (rt == null || !rt.gameObject.activeInHierarchy) return false;

        // Cuánto se ha desplazado en pantalla por el salto.
        Vector3 worldOffset = rt.parent.TransformVector(Vector3.up * (jumpHeight * current));
        Vector2 screenNow = RectTransformUtility.WorldToScreenPoint(canvasCamera, rt.position);
        Vector2 screenBase = RectTransformUtility.WorldToScreenPoint(canvasCamera, rt.position - worldOffset);

        return RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos + (screenNow - screenBase), canvasCamera);
    }

    void Apply()
    {
        Vector2 offset = Vector2.up * (jumpHeight * current);

        if (textRt != null)
        {
            if (moveText) textRt.anchoredPosition = textBasePosition + offset;
            if (tintText) text.color = Color.Lerp(textBaseColor, hoverColor, current);
        }
        if (iconRt != null)
        {
            if (moveIcon) iconRt.anchoredPosition = iconBasePosition + offset;
            if (tintIcon) icon.color = Color.Lerp(iconBaseColor, hoverColor, current);
        }
    }

    // Si el panel se cierra con el mouse encima, vuelve a su estado normal.
    void OnDisable()
    {
        current = 0f;
        Apply();
    }
}
