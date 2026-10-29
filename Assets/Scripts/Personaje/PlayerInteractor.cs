using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Permite al jugador usar con E el objeto interactuable más cercano.
public class PlayerInteractor : MonoBehaviour
{
    public Key interactKey = Key.E;
    [Tooltip("Distancia máxima para interactuar, en metros.")]
    public float interactRadius = 1.5f;

    [Header("Referencias")]
    public PlayerInventory inventory;

    [Header("UI (opcional)")]
    [Tooltip("Texto donde aparece \"E - ...\".")]
    public TMP_Text promptText;
    [Tooltip("Image con Image Type = Filled que muestra el progreso al mantener E.")]
    public Image holdFill;

    Interactable current;
    float holdTimer;

    void Update()
    {
        Interactable nearest = FindNearest();
        if (nearest != current)
        {
            if (current != null) current.OnHoldProgress(0f);
            current = nearest;
            holdTimer = 0f;
        }

        UpdateUI();
        if (current == null) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (current.holdTime <= 0f)
        {
            if (keyboard[interactKey].wasPressedThisFrame) current.Interact(this);
            return;
        }

        if (keyboard[interactKey].isPressed)
        {
            holdTimer += Time.deltaTime;
            current.OnHoldProgress(Mathf.Clamp01(holdTimer / current.holdTime));
            if (holdTimer >= current.holdTime)
            {
                var target = current;
                holdTimer = 0f;
                target.OnHoldProgress(0f);
                target.Interact(this);
            }
        }
        else if (holdTimer > 0f)
        {
            holdTimer = 0f;
            current.OnHoldProgress(0f);
        }
    }

    Interactable FindNearest()
    {
        Interactable best = null;
        float bestDist = interactRadius;
        foreach (var it in Interactable.All)
        {
            if (!it.CanInteract) continue;
            float d = Vector3.Distance(transform.position, it.InteractPosition);
            if (d <= bestDist)
            {
                bestDist = d;
                best = it;
            }
        }
        return best;
    }

    void UpdateUI()
    {
        bool show = current != null;
        if (promptText != null)
        {
            promptText.gameObject.SetActive(show);
            if (show) promptText.text = $"{interactKey} - {current.prompt}";
        }
        if (holdFill != null)
        {
            bool showFill = show && current.holdTime > 0f && holdTimer > 0f;
            holdFill.gameObject.SetActive(showFill);
            if (showFill) holdFill.fillAmount = holdTimer / current.holdTime;
        }
    }
}
