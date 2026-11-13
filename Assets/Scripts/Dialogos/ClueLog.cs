using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Registro de pistas y marcas de progreso (identificadores de texto, como "lin_chat" o "pong_dijo_no").
// Los diálogos las añaden con el campo "addClue"; Verdad o reto las usará para contradecir a Lin.
// Va en el jugador o en un controlador de la escena: solo debe haber uno.
public class ClueLog : MonoBehaviour
{
    [Tooltip("Pistas con las que se empieza. Útil para probar una habitación sin jugar las anteriores.")]
    public List<string> initialClues = new List<string>();

    [Tooltip("Se llama al conseguir una pista nueva, con su identificador.")]
    public UnityEvent<string> onClueAdded;

    readonly List<string> clues = new List<string>();
    public IReadOnlyList<string> Clues => clues;

    void Awake()
    {
        foreach (var c in initialClues)
            if (!string.IsNullOrEmpty(c) && !clues.Contains(c)) clues.Add(c);
    }

    public void Add(string clueId)
    {
        if (string.IsNullOrEmpty(clueId) || clues.Contains(clueId)) return;
        clues.Add(clueId);
        onClueAdded.Invoke(clueId);
    }

    public bool Has(string clueId) => clues.Contains(clueId);
    public void Remove(string clueId) => clues.Remove(clueId);
}
