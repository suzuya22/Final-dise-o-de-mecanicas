using UnityEngine;
using UnityEngine.UI;

// Botón de "Saltar" que aparece en una esquina mientras hay un minijuego activo.
// Va en la raíz del prefab BotonSaltar (Prefabs/Interfaz/BotonSaltar.prefab). No decide qué pasa al saltar:
// avisa al MinigameSkip del minijuego que lo pidió, que tiene su propio evento onSkip.
public class MinigameSkipUI : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Objeto que contiene el botón. Se apaga cuando no hay minijuego.")]
    public GameObject root;
    public Button button;

    [Header("Opciones")]
    [Tooltip("Apágalo para quitar el botón de todos los minijuegos (por ejemplo en la versión final).")]
    public bool allowSkip = true;

    MinigameSkip current;

    void Awake()
    {
        if (button != null) button.onClick.AddListener(OnClick);
        if (root != null) root.SetActive(false);
    }

    public void Show(MinigameSkip owner)
    {
        current = owner;
        if (root != null) root.SetActive(allowSkip);
    }

    // Solo oculta si el botón sigue siendo de ese minijuego (otro pudo haberlo reclamado).
    public void Hide(MinigameSkip owner)
    {
        if (owner != null && owner != current) return;
        current = null;
        if (root != null) root.SetActive(false);
    }

    void OnClick()
    {
        var owner = current;
        if (owner == null) return;
        Hide(owner);
        owner.Skip();
    }
}
