using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Inventario sencillo: guarda los identificadores de los objetos recogidos.
public class PlayerInventory : MonoBehaviour
{
    [Tooltip("Se llama al recoger un objeto, con su identificador.")]
    public UnityEvent<string> onItemAdded;

    readonly List<string> items = new List<string>();
    public IReadOnlyList<string> Items => items;

    public void Add(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || items.Contains(itemId)) return;
        items.Add(itemId);
        onItemAdded.Invoke(itemId);
    }

    public bool Has(string itemId) => items.Contains(itemId);
    public void Remove(string itemId) => items.Remove(itemId);
}
