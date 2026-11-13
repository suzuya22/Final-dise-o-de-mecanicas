using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Beber: gasta un objeto del inventario (la botella) y devuelve cordura. Va en el jugador.
public class SanityDrink : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerInventory inventory;
    public SanityManager sanity;

    [Header("Bebida")]
    [Tooltip("Identificador del objeto en el inventario.")]
    public string itemId = "botella";
    [Tooltip("Cordura que devuelve.")]
    public float restoreAmount = 30f;
    public Key drinkKey = Key.Q;

    [Tooltip("Se llama al beber (para un sonido o una animación).")]
    public UnityEvent onDrink;

    void Update()
    {
        // Pausa o inventario abiertos.
        if (Time.timeScale == 0f) return;

        var kb = Keyboard.current;
        if (kb != null && kb[drinkKey].wasPressedThisFrame) Drink();
    }

    public void Drink()
    {
        if (inventory == null || sanity == null) return;
        if (!inventory.Has(itemId)) return;
        // No se desperdicia con la cordura llena.
        if (sanity.Current >= sanity.maxSanity) return;

        inventory.Remove(itemId);
        sanity.Restore(restoreAmount);
        onDrink.Invoke();
    }
}
