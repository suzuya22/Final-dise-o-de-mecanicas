using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú de pausa: al presionar P el panel entra arrastrándose desde la izquierda
// (con todo su contenido) y el juego se congela. Al volver a presionar, sale por la izquierda.
public class PauseMenu : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El panel morado, con todos los textos e iconos como hijos.")]
    public RectTransform panel;

    [Header("Controles")]
    public Key pauseKey = Key.P;
    [Tooltip("Congela el juego (Time.timeScale = 0) mientras está en pausa.")]
    public bool freezeTime = true;

    [Header("Animación")]
    public float slideDuration = 0.5f;
    [Tooltip("Curva de entrada. Por defecto se pasa un poco y vuelve, como un rebote.")]
    public AnimationCurve slideInCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.5f),
        new Keyframe(0.75f, 1.05f),
        new Keyframe(1f, 1f));
    public AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Botones")]
    public Button continueButton;
    public Button settingsButton;
    public Button menuButton;
    public Button quitButton;
    [Tooltip("Nombre de la escena del menú principal (debe estar en Build Profiles).")]
    public string menuSceneName = "";

    [Header("Eventos")]
    public UnityEvent onPause;
    public UnityEvent onResume;
    [Tooltip("Qué hacer al pulsar Configuración (aún no hay pantalla de configuración).")]
    public UnityEvent onSettings;

    public bool IsPaused { get; private set; }

    Vector2 shownPosition;
    Coroutine slide;
    CursorLockMode previousLock;
    bool previousCursorVisible;

    void Awake()
    {
        shownPosition = panel.anchoredPosition;
        panel.gameObject.SetActive(false);

        if (continueButton != null) continueButton.onClick.AddListener(Resume);
        if (settingsButton != null) settingsButton.onClick.AddListener(onSettings.Invoke);
        if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[pauseKey].wasPressedThisFrame)
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

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

        StartSlide(shownPosition, slideInCurve, false);
        onPause.Invoke();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        if (freezeTime) Time.timeScale = 1f;
        Cursor.lockState = previousLock;
        Cursor.visible = previousCursorVisible;

        StartSlide(GetHiddenPosition(), slideOutCurve, true);
        onResume.Invoke();
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(menuSceneName)) SceneManager.LoadScene(menuSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void StartSlide(Vector2 target, AnimationCurve curve, bool hideAtEnd)
    {
        if (slide != null) StopCoroutine(slide);
        slide = StartCoroutine(Slide(target, curve, hideAtEnd));
    }

    // Usa tiempo sin escalar para funcionar aunque el juego esté congelado.
    IEnumerator Slide(Vector2 target, AnimationCurve curve, bool hideAtEnd)
    {
        Vector2 from = panel.anchoredPosition;
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            panel.anchoredPosition = Vector2.LerpUnclamped(from, target, curve.Evaluate(Mathf.Clamp01(t / slideDuration)));
            yield return null;
        }
        panel.anchoredPosition = target;

        if (hideAtEnd) panel.gameObject.SetActive(false);
        slide = null;
    }

    // Posición en la que el borde derecho del panel queda fuera de la pantalla por la izquierda.
    Vector2 GetHiddenPosition()
    {
        Canvas canvas = panel.GetComponentInParent<Canvas>().rootCanvas;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        var corners = new Vector3[4];
        panel.GetWorldCorners(corners);
        Vector3 shownOffset = (Vector3)(panel.anchoredPosition - shownPosition);
        float rightEdge = RectTransformUtility.WorldToScreenPoint(cam, corners[2]).x;
        float distance = rightEdge / canvas.scaleFactor - shownOffset.x + 20f;

        return shownPosition - new Vector2(distance, 0f);
    }

    void OnDestroy()
    {
        if (IsPaused && freezeTime) Time.timeScale = 1f;
    }
}
