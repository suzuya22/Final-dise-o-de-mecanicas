using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Parte visual de los diálogos: caja de texto, nombre, retrato y respuestas.
// No decide nada: solo muestra lo que le pide DialogueRunner.
// Va en la raíz del prefab Dialogos (Prefabs/Interfaz/Dialogos.prefab).
public class DialogueUI : MonoBehaviour
{
    [Serializable]
    public class ResponseSlot
    {
        public GameObject root;
        public Button button;
        public TMP_Text label;
    }

    [Serializable]
    public class SpeakerPortrait
    {
        [Tooltip("Nombre tal como aparece en el campo Speaker del diálogo.")]
        public string speaker;
        public Sprite portrait;
    }

    [Header("Caja de diálogo")]
    [Tooltip("Objeto que contiene toda la interfaz. Se apaga cuando no hay conversación.")]
    public GameObject root;
    public TMP_Text speakerText;
    public TMP_Text bodyText;
    [Tooltip("Indicador de \"continuar\" que parpadea al terminar el texto.")]
    public GameObject continueIcon;
    [Tooltip("Letras por segundo. 0 = el texto aparece de golpe.")]
    public float charsPerSecond = 40f;

    [Header("Retrato (opcional)")]
    [Tooltip("Marco del retrato. Se oculta si quien habla no tiene retrato.")]
    public GameObject portraitBox;
    public Image portraitImage;
    public SpeakerPortrait[] portraits = new SpeakerPortrait[0];

    [Header("Respuestas")]
    [Tooltip("Cajas de respuesta, ordenadas DE ABAJO HACIA ARRIBA (la 0 es la pegada a la caja de diálogo).")]
    public ResponseSlot[] responseSlots = new ResponseSlot[0];
    public Color responseColor = Color.white;
    public Color selectedResponseColor = new Color(1f, 0.85f, 0.3f);

    [Header("Fuente según la cordura (opcional)")]
    [Tooltip("Cordura del jugador. Si está vacío, siempre se usa la fuente normal.")]
    public SanityManager sanity;
    [Tooltip("Cordura por encima de 'goodAbove'.")]
    public TMP_FontAsset goodFont;
    [Tooltip("Cordura entre 'badBelow' y 'goodAbove'.")]
    public TMP_FontAsset normalFont;
    [Tooltip("Cordura por debajo de 'badBelow'.")]
    public TMP_FontAsset badFont;
    [Tooltip("Cordura por debajo de 'veryBadBelow'.")]
    public TMP_FontAsset veryBadFont;
    [Range(0f, 1f)] public float goodAbove = 0.75f;
    [Range(0f, 1f)] public float badBelow = 0.5f;
    [Range(0f, 1f)] public float veryBadBelow = 0.25f;

    public bool IsTyping { get; private set; }
    public bool ShowingResponses { get; private set; }
    public int ResponseCount { get; private set; }

    float typed;
    Action<int> onResponseClicked;

    void Awake()
    {
        for (int i = 0; i < responseSlots.Length; i++)
        {
            int slot = i;
            if (responseSlots[i].button != null)
                responseSlots[i].button.onClick.AddListener(() => ClickSlot(slot));
        }
        Hide();
    }

    void Update()
    {
        if (IsTyping)
        {
            // Tiempo normal: si el juego está en pausa, el texto también se detiene.
            typed += charsPerSecond * Time.deltaTime;
            int total = bodyText.textInfo.characterCount;
            bodyText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(typed));
            if (bodyText.maxVisibleCharacters >= total) IsTyping = false;
        }

        if (continueIcon != null)
        {
            bool blinkOn = Mathf.FloorToInt(Time.unscaledTime * 3f) % 2 == 0;
            continueIcon.SetActive(root.activeSelf && !IsTyping && !ShowingResponses && blinkOn);
        }
    }

    public void Show() => root.SetActive(true);

    public void Hide()
    {
        HideResponses();
        IsTyping = false;
        if (root != null) root.SetActive(false);
    }

    public void ShowLine(string speaker, string text)
    {
        HideResponses();
        ApplyFont();

        if (speakerText != null) speakerText.text = speaker;
        SetPortrait(speaker);

        bodyText.text = text;
        bodyText.ForceMeshUpdate();
        if (charsPerSecond <= 0f)
        {
            bodyText.maxVisibleCharacters = int.MaxValue;
            IsTyping = false;
        }
        else
        {
            bodyText.maxVisibleCharacters = 0;
            typed = 0f;
            IsTyping = true;
        }
    }

    // Muestra de golpe el texto que se estaba escribiendo.
    public void CompleteTyping()
    {
        bodyText.maxVisibleCharacters = int.MaxValue;
        IsTyping = false;
    }

    // Muestra las respuestas (la primera queda arriba). onClicked recibe el índice de la respuesta.
    public void ShowResponses(DialogueResponse[] responses, Action<int> onClicked)
    {
        ResponseCount = Mathf.Min(responses.Length, responseSlots.Length);
        if (responses.Length > responseSlots.Length)
            Debug.LogWarning($"[Diálogos] Hay {responses.Length} respuestas pero solo {responseSlots.Length} cajas.", this);

        onResponseClicked = onClicked;
        ShowingResponses = true;

        for (int s = 0; s < responseSlots.Length; s++)
        {
            bool used = s < ResponseCount;
            responseSlots[s].root.SetActive(used);
            if (used) responseSlots[s].label.text = responses[SlotToResponse(s)].text;
        }
        SetSelected(0);
    }

    public void HideResponses()
    {
        ShowingResponses = false;
        ResponseCount = 0;
        onResponseClicked = null;
        foreach (var slot in responseSlots)
            if (slot.root != null) slot.root.SetActive(false);
    }

    public void SetSelected(int responseIndex)
    {
        for (int s = 0; s < ResponseCount; s++)
            responseSlots[s].label.color = SlotToResponse(s) == responseIndex ? selectedResponseColor : responseColor;
    }

    // Las cajas van de abajo hacia arriba y las respuestas se leen de arriba hacia abajo.
    int SlotToResponse(int slot) => ResponseCount - 1 - slot;

    void ClickSlot(int slot)
    {
        if (!ShowingResponses || slot >= ResponseCount) return;
        onResponseClicked?.Invoke(SlotToResponse(slot));
    }

    void SetPortrait(string speaker)
    {
        Sprite sprite = null;
        foreach (var p in portraits)
            if (p.speaker == speaker) { sprite = p.portrait; break; }

        if (portraitImage != null)
        {
            portraitImage.sprite = sprite;
            portraitImage.enabled = sprite != null;
        }
        if (portraitBox != null) portraitBox.SetActive(sprite != null);
    }

    void ApplyFont()
    {
        TMP_FontAsset font = normalFont;
        if (sanity != null)
        {
            float s = sanity.Normalized;
            if (s < veryBadBelow && veryBadFont != null) font = veryBadFont;
            else if (s < badBelow && badFont != null) font = badFont;
            else if (s > goodAbove && goodFont != null) font = goodFont;
        }
        if (font == null) return;

        bodyText.font = font;
        if (speakerText != null) speakerText.font = font;
        foreach (var slot in responseSlots)
            if (slot.label != null) slot.label.font = font;
    }
}
