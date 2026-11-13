using System;
using System.Collections.Generic;
using UnityEngine;

// Una conversación completa. Se crea con clic derecho > Create > La Chiva > Diálogo
// y se edita en el Inspector.
//
// Cómo se lee: las líneas se muestran EN ORDEN, una tras otra. Para hacer ramas:
//  - Ponle un "id" a la línea donde empieza cada rama.
//  - En las respuestas del prota (o en "goTo" de una línea) escribe ese id para saltar allí.
//  - Marca "endsDialogue" en la última línea de cada rama; si no, seguiría con la línea de abajo.
[CreateAssetMenu(menuName = "La Chiva/Diálogo", fileName = "Dialogo")]
public class DialogueData : ScriptableObject
{
    public List<DialogueLine> lines = new List<DialogueLine>();

    // Índice de la línea con ese id, o -1 si no existe.
    public int FindIndex(string id)
    {
        if (string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < lines.Count; i++)
            if (lines[i].id == id) return i;
        return -1;
    }
}

[Serializable]
public class DialogueLine
{
    [Tooltip("Opcional. Nombre de esta línea para poder saltar a ella desde una respuesta o un goTo.")]
    public string id;
    [Tooltip("Quién habla (aparece sobre el texto).")]
    public string speaker;
    [TextArea(2, 5)]
    [Tooltip("Lo que dice. Si está vacío y no hay respuestas, la línea no se muestra: solo aplica sus efectos.")]
    public string text;
    [Tooltip("Nota de actuación del documento (\"se rasca la cabeza\"). No se muestra; es solo para ustedes.")]
    public string action;

    [Header("Respuestas del prota (opcional)")]
    [Tooltip("Si hay respuestas, el jugador elige una al terminar el texto.")]
    public DialogueResponse[] responses = new DialogueResponse[0];

    [Header("Qué sigue")]
    [Tooltip("Id de la línea a la que salta después. Vacío = la siguiente de la lista.")]
    public string goTo;
    [Tooltip("La conversación termina después de esta línea.")]
    public bool endsDialogue;

    [Header("Efectos al mostrarse (opcionales)")]
    [Tooltip("Cambio de cordura: negativo quita, positivo devuelve.")]
    public float sanityChange;
    [Tooltip("Pista o marca que se guarda en el ClueLog (por ejemplo \"lin_chat\").")]
    public string addClue;
    [Tooltip("Objeto que se añade al inventario (por ejemplo \"botella\").")]
    public string giveItem;
    [Tooltip("Nombre de un evento. Lo recibe el DialogueTrigger que inició la conversación (lista Named Events) " +
             "y también el evento On Dialogue Event del DialogueRunner.")]
    public string eventName;

    public bool HasResponses => responses != null && responses.Length > 0;
}

[Serializable]
public class DialogueResponse
{
    [Tooltip("Lo que responde el prota.")]
    public string text;
    [Tooltip("Id de la línea a la que lleva esta respuesta. Vacío = la siguiente de la lista.")]
    public string goTo;
}
