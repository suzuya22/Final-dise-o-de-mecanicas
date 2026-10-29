using UnityEngine;
using UnityEngine.Events;

// Objeto que se recoge con E y se guarda en el inventario del jugador
// (por ejemplo, el objeto que pide la familia del piso 1).
public class PickupItem : Interactable
{
    [Header("Objeto")]
    [Tooltip("Identificador con el que se guarda en el inventario.")]
    public string itemId = "objeto_familia";

    public UnityEvent onPickedUp;

    PlayerInventory pickedBy;

    void Reset()
    {
        prompt = "Recoger";
        holdTime = 0f;
    }

    public override void Interact(PlayerInteractor player)
    {
        pickedBy = player.inventory;
        if (pickedBy != null) pickedBy.Add(itemId);
        onPickedUp.Invoke();
        gameObject.SetActive(false);
    }

    // Lo llama SevenMinutesPuzzle al reiniciar: vuelve a su sitio y sale del inventario.
    public void ResetState()
    {
        if (pickedBy != null) pickedBy.Remove(itemId);
        pickedBy = null;
        gameObject.SetActive(true);
    }
}
