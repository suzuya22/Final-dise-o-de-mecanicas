using System;
using UnityEngine;
using UnityEngine.Events;

// Inicia una conversación. Va en el NPC (u objeto) con el que se habla.
//  - Con "interactWithE" el jugador la inicia con E al acercarse.
//  - También se puede iniciar desde un UnityEvent llamando a Play()
//    (por ejemplo desde BeerPongManager.onWin o desde un PlayerTrigger).
public class DialogueTrigger : Interactable
{
    [Serializable]
    public class Variant
    {
        [Tooltip("Pista o marca del ClueLog que debe existir para usar este diálogo.")]
        public string requiredClue;
        public DialogueData dialogue;
    }

    [Serializable]
    public class NamedEvent
    {
        [Tooltip("Nombre tal como está en el campo Event Name de la línea.")]
        public string name;
        public UnityEvent onEvent;
    }

    [Header("Diálogo")]
    public DialogueRunner runner;
    [Tooltip("Conversación normal.")]
    public DialogueData dialogue;
    [Tooltip("Conversaciones que sustituyen a la normal cuando el jugador ya tiene cierta pista o marca. " +
             "Si se cumplen varias, gana la de más abajo. Necesita que el DialogueRunner tenga asignado el ClueLog.")]
    public Variant[] variants = new Variant[0];

    [Header("Cómo se inicia")]
    [Tooltip("El jugador la inicia con E. Apágalo si solo se inicia desde un evento (Play).")]
    public bool interactWithE = true;
    [Tooltip("Solo se puede tener una vez.")]
    public bool playOnce = false;
    [Tooltip("Empieza sola al comenzar la escena (por ejemplo la llegada de los amigos).")]
    public bool playOnStart = false;
    [Tooltip("Segundos de espera antes de empezar sola.")]
    public float startDelay = 1f;

    [Header("Eventos")]
    [Tooltip("Se llama al terminar la conversación.")]
    public UnityEvent onFinished;
    [Tooltip("Qué pasa cuando una línea lanza un Event Name (por ejemplo \"jugar\").")]
    public NamedEvent[] namedEvents = new NamedEvent[0];

    bool played;

    void Start()
    {
        if (playOnStart) StartCoroutine(PlayAfterDelay());
    }

    System.Collections.IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        // Si hay otra conversación en curso, espera a que termine.
        while (runner != null && runner.IsRunning) yield return null;
        Play();
    }

    // Activa o desactiva el poder hablar con E (por ejemplo cuando ya no tiene nada más que decir).
    public void SetInteractable(bool value) => interactWithE = value;

    public override bool CanInteract =>
        base.CanInteract && interactWithE && runner != null && !runner.IsRunning && !(playOnce && played);

    public override void Interact(PlayerInteractor player) => Play();

    public void Play()
    {
        if (runner == null || runner.IsRunning || (playOnce && played)) return;

        DialogueData chosen = dialogue;
        if (runner.clues != null)
        {
            foreach (var v in variants)
                if (v.dialogue != null && runner.clues.Has(v.requiredClue)) chosen = v.dialogue;
        }
        if (chosen == null) return;

        played = true;
        runner.StartDialogue(chosen, HandleEvent, () => onFinished.Invoke());
    }

    void HandleEvent(string eventName)
    {
        foreach (var e in namedEvents)
            if (e.name == eventName) e.onEvent.Invoke();
    }

    // Permite volver a tener una conversación marcada como "playOnce".
    public void ResetPlayed() => played = false;
}
