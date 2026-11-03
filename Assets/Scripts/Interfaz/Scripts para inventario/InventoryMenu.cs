using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Inventario (solo visual por ahora): la tecla I abre y cierra el panel, que entra deslizándose
// desde la izquierda como el menú de pausa, y el juego se congela mientras está abierto.
// Pensado para ir en un prefab con su propio Canvas, así funciona en cualquier escena.
public class InventoryMenu : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Panel que se desliza (con todos los sprites como hijos). Ocupa toda la pantalla.")]
    public RectTransform panel;
    [Header("Triángulo (siempre visible)")]
    [Tooltip("Contenedor del triángulo. Debe estar FUERA del panel para verse con el inventario cerrado. " +
             "Su posición inicial es la posición de cerrado.")]
    public RectTransform pointer;
    [Tooltip("Posición del triángulo con el inventario abierto (mismas unidades que su posición ancorada).")]
    public Vector2 pointerOpenPosition;
    [Tooltip("Escala X del triángulo cerrado (1 = mira a la derecha, como el sprite original).")]
    public float pointerClosedFacing = 1f;
    [Tooltip("Escala X del triángulo abierto (-1 = mira a la izquierda, hacia el panel).")]
    public float pointerOpenFacing = -1f;
    [Tooltip("Segundos que tarda en darse la vuelta.")]
    public float pointerTurnTime = 0.15f;

    [Header("Pausa")]
    [Tooltip("Opcional: el menú de pausa de la escena. Evita abrir el inventario estando en pausa " +
             "y cierra el inventario si se pausa. Déjalo vacío si no hay pausa.")]
    public PauseMenu pauseMenu;

    [Tooltip("Objetos de la escena que se ocultan mientras el inventario está abierto " +
             "(por ejemplo el HUD de cordura) y vuelven al cerrarlo.")]
    public GameObject[] hideWhileOpen;

    [Header("Controles")]
    public Key openKey = Key.I;
    [Tooltip("Escape también cierra el inventario.")]
    public bool closeWithEscape = true;
    [Tooltip("Congela el juego (Time.timeScale = 0) mientras está abierto.")]
    public bool freezeTime = true;

    [Header("Animación")]
    public float slideDuration = 0.5f;
    [Tooltip("Curva de entrada. Por defecto se pasa un poco y vuelve, como el menú de pausa.")]
    public AnimationCurve slideInCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.5f),
        new Keyframe(0.75f, 1.05f),
        new Keyframe(1f, 1f));
    public AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Eventos")]
    public UnityEvent onOpen;
    public UnityEvent onClose;

    public bool IsOpen { get; private set; }

    Vector2 shownPosition;
    Coroutine slide;
    Coroutine turn;
    CursorLockMode previousLock;
    bool previousCursorVisible;
    Vector2 pointerClosedPosition;

    void Awake()
    {
        shownPosition = panel.anchoredPosition;
        panel.gameObject.SetActive(false);

        if (pointer != null)
        {
            pointerClosedPosition = pointer.anchoredPosition;
            SetPointerFacing(pointerClosedFacing);
        }
    }

    void Update()
    {
        bool paused = pauseMenu != null && pauseMenu.IsPaused;

        // El triángulo (siempre visible) se oculta mientras está el menú de pausa.
        if (pointer != null && pointer.parent != null && pointer.parent.gameObject.activeSelf == paused)
            pointer.parent.gameObject.SetActive(!paused);

        // Si se pausa con el inventario abierto, la pausa manda.
        if (IsOpen && paused)
        {
            ForceClose();
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[openKey].wasPressedThisFrame)
        {
            if (IsOpen) Close();
            else Open();
        }
        else if (IsOpen && closeWithEscape && keyboard[Key.Escape].wasPressedThisFrame)
        {
            Close();
        }
    }

    public void Open()
    {
        if (IsOpen) return;
        if (pauseMenu != null && pauseMenu.IsPaused) return;
        // Si otro sistema ya congeló el juego (pausa, minijuego...), no abrimos encima.
        if (freezeTime && Time.timeScale == 0f) return;
        IsOpen = true;

        bool wasHidden = !panel.gameObject.activeSelf;
        panel.gameObject.SetActive(true);
        if (wasHidden)
        {
            panel.anchoredPosition = shownPosition;
            Canvas.ForceUpdateCanvases();
            panel.anchoredPosition = GetHiddenPosition();
        }

        if (freezeTime) Time.timeScale = 0f;

        previousLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetHidden(true);
        StartSlide(shownPosition, pointerOpenPosition, pointerOpenFacing, slideInCurve, false);
        onOpen.Invoke();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (freezeTime) Time.timeScale = 1f;
        Cursor.lockState = previousLock;
        Cursor.visible = previousCursorVisible;

        SetHidden(false);
        StartSlide(GetHiddenPosition(), pointerClosedPosition, pointerClosedFacing, slideOutCurve, true);
        onClose.Invoke();
    }

    void SetHidden(bool hidden)
    {
        if (hideWhileOpen == null) return;
        foreach (var go in hideWhileOpen)
            if (go != null) go.SetActive(!hidden);
    }

    // Cierra al instante sin tocar el tiempo ni el cursor (lo gestiona quien tomó el control).
    void ForceClose()
    {
        IsOpen = false;
        if (slide != null) StopCoroutine(slide);
        slide = null;
        panel.anchoredPosition = shownPosition;
        panel.gameObject.SetActive(false);
        if (pointer != null)
        {
            if (turn != null) StopCoroutine(turn);
            turn = null;
            pointer.anchoredPosition = pointerClosedPosition;
            SetPointerFacing(pointerClosedFacing);
        }
        SetHidden(false);
        onClose.Invoke();
    }

    void StartSlide(Vector2 panelTarget, Vector2 pointerTarget, float pointerFacing, AnimationCurve curve, bool hideAtEnd)
    {
        if (slide != null) StopCoroutine(slide);
        slide = StartCoroutine(Slide(panelTarget, pointerTarget, curve, hideAtEnd));

        if (pointer != null)
        {
            if (turn != null) StopCoroutine(turn);
            turn = StartCoroutine(Turn(pointerFacing));
        }
    }

    // Tiempo sin escalar: el juego está congelado mientras el inventario está abierto.
    // El triángulo viaja con el panel usando la misma curva.
    IEnumerator Slide(Vector2 panelTarget, Vector2 pointerTarget, AnimationCurve curve, bool hideAtEnd)
    {
        Vector2 panelFrom = panel.anchoredPosition;
        Vector2 pointerFrom = pointer != null ? pointer.anchoredPosition : Vector2.zero;
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = curve.Evaluate(Mathf.Clamp01(t / slideDuration));
            panel.anchoredPosition = Vector2.LerpUnclamped(panelFrom, panelTarget, k);
            if (pointer != null) pointer.anchoredPosition = Vector2.LerpUnclamped(pointerFrom, pointerTarget, k);
            yield return null;
        }
        panel.anchoredPosition = panelTarget;
        if (pointer != null) pointer.anchoredPosition = pointerTarget;

        if (hideAtEnd) panel.gameObject.SetActive(false);
        slide = null;
    }

    // El triángulo se da la vuelta rápido (pasa por escala 0, como un giro de ficha).
    IEnumerator Turn(float targetFacing)
    {
        while (!Mathf.Approximately(pointer.localScale.x, targetFacing))
        {
            float step = Mathf.Abs(pointerOpenFacing - pointerClosedFacing) / Mathf.Max(0.01f, pointerTurnTime) * Time.unscaledDeltaTime;
            SetPointerFacing(Mathf.MoveTowards(pointer.localScale.x, targetFacing, step));
            yield return null;
        }
        turn = null;
    }

    void SetPointerFacing(float facing)
    {
        var s = pointer.localScale;
        s.x = facing;
        pointer.localScale = s;
    }

    // Posición en la que el panel queda fuera de la pantalla por la izquierda.
    Vector2 GetHiddenPosition()
    {
        var canvasRect = (RectTransform)panel.GetComponentInParent<Canvas>().rootCanvas.transform;
        return shownPosition - new Vector2(canvasRect.rect.width + 20f, 0f);
    }

    void OnDestroy()
    {
        if (IsOpen && freezeTime) Time.timeScale = 1f;
    }
}
