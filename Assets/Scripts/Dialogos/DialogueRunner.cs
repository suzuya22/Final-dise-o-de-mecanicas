using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Lleva una conversación de principio a fin: avanza las líneas, espera las respuestas del jugador
// y aplica los efectos de cada línea (cordura, pistas, objetos, eventos).
// Va en un objeto de la escena (uno solo). Las conversaciones las inician los DialogueTrigger.
//
// Controles: E / Espacio / Enter avanza (o termina de escribir el texto).
// Con respuestas: W/S o flechas para elegir y E / Enter para confirmar; también 1-4 o clic.
public class DialogueRunner : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La interfaz de diálogos (prefab Dialogos).")]
    public DialogueUI ui;
    [Tooltip("Se bloquea mientras dura la conversación.")]
    public PlayerMovement player;
    [Tooltip("Se apaga mientras dura la conversación, para que E no active otra cosa.")]
    public PlayerInteractor interactor;

    [Header("Efectos de las líneas (opcionales)")]
    public SanityManager sanity;
    public PlayerInventory inventory;
    public ClueLog clues;

    [Header("Eventos")]
    public UnityEvent onDialogueStarted;
    public UnityEvent onDialogueEnded;
    [Tooltip("Se llama con el nombre del evento cuando una línea tiene Event Name.")]
    public UnityEvent<string> onDialogueEvent;

    public bool IsRunning { get; private set; }

    DialogueData data;
    int index;
    int selected;
    int startFrame;
    bool playerCouldMove;
    Action<string> eventCallback;
    Action endCallback;

    // Inicia una conversación. onEvent recibe los Event Name de las líneas; onEnd se llama al terminar.
    public void StartDialogue(DialogueData dialogue, Action<string> onEvent = null, Action onEnd = null)
    {
        if (IsRunning || dialogue == null) return;

        data = dialogue;
        eventCallback = onEvent;
        endCallback = onEnd;
        IsRunning = true;
        startFrame = Time.frameCount;

        if (player != null)
        {
            playerCouldMove = player.CanMove;
            player.SetCanMove(false);
        }
        if (interactor != null) interactor.enabled = false;

        ui.Show();
        onDialogueStarted.Invoke();
        GoTo(0);
    }

    void Update()
    {
        if (!IsRunning) return;
        // Pausa o inventario abiertos: no se avanza.
        if (Time.timeScale == 0f) return;
        // La misma pulsación de E que inició la conversación no debe avanzarla.
        if (Time.frameCount == startFrame) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        bool confirm = kb[Key.E].wasPressedThisFrame || kb[Key.Enter].wasPressedThisFrame ||
                       kb[Key.NumpadEnter].wasPressedThisFrame;
        bool advance = confirm || kb[Key.Space].wasPressedThisFrame;
        var line = data.lines[index];

        if (ui.IsTyping)
        {
            if (advance) ui.CompleteTyping();
            return;
        }

        if (line.HasResponses)
        {
            if (!ui.ShowingResponses)
            {
                selected = 0;
                ui.ShowResponses(line.responses, ChooseResponse);
                return;
            }

            int count = ui.ResponseCount;
            if (kb[Key.W].wasPressedThisFrame || kb[Key.UpArrow].wasPressedThisFrame)
                selected = (selected - 1 + count) % count;
            if (kb[Key.S].wasPressedThisFrame || kb[Key.DownArrow].wasPressedThisFrame)
                selected = (selected + 1) % count;
            ui.SetSelected(selected);

            Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
            for (int i = 0; i < count && i < digits.Length; i++)
            {
                if (kb[digits[i]].wasPressedThisFrame)
                {
                    ChooseResponse(i);
                    return;
                }
            }

            if (confirm) ChooseResponse(selected);
            return;
        }

        if (advance)
        {
            if (line.endsDialogue) End();
            else GoTo(NextIndex(line.goTo, index));
        }
    }

    void ChooseResponse(int responseIndex)
    {
        if (!IsRunning) return;
        var line = data.lines[index];
        if (responseIndex < 0 || responseIndex >= line.responses.Length) return;

        ui.HideResponses();
        if (line.endsDialogue) End();
        else GoTo(NextIndex(line.responses[responseIndex].goTo, index));
    }

    // Muestra la línea i. Las líneas sin texto solo aplican sus efectos y se pasan de largo.
    void GoTo(int i)
    {
        for (int guard = 0; guard < 1000; guard++)
        {
            if (i < 0 || i >= data.lines.Count)
            {
                End();
                return;
            }

            index = i;
            var line = data.lines[i];
            ApplyEffects(line);
            // Un evento de la línea pudo haber cerrado la conversación.
            if (!IsRunning) return;

            if (!string.IsNullOrEmpty(line.text) || line.HasResponses)
            {
                ui.ShowLine(line.speaker, line.text);
                return;
            }

            if (line.endsDialogue)
            {
                End();
                return;
            }
            i = NextIndex(line.goTo, i);
        }

        Debug.LogError($"[Diálogos] '{data.name}' tiene un bucle de líneas vacías.", data);
        End();
    }

    int NextIndex(string goTo, int current)
    {
        if (string.IsNullOrEmpty(goTo)) return current + 1;

        int target = data.FindIndex(goTo);
        if (target < 0)
            Debug.LogWarning($"[Diálogos] En '{data.name}' no existe la línea con id '{goTo}'. Termino la conversación.", data);
        return target;
    }

    void ApplyEffects(DialogueLine line)
    {
        if (sanity != null)
        {
            if (line.sanityChange < 0f) sanity.Drain(-line.sanityChange);
            else if (line.sanityChange > 0f) sanity.Restore(line.sanityChange);
        }
        if (clues != null && !string.IsNullOrEmpty(line.addClue)) clues.Add(line.addClue);
        if (inventory != null && !string.IsNullOrEmpty(line.giveItem)) inventory.Add(line.giveItem);

        if (!string.IsNullOrEmpty(line.eventName))
        {
            eventCallback?.Invoke(line.eventName);
            onDialogueEvent.Invoke(line.eventName);
        }
    }

    // Termina la conversación (también se puede llamar desde fuera para cortarla).
    public void End()
    {
        if (!IsRunning) return;
        IsRunning = false;

        ui.Hide();
        if (player != null) player.SetCanMove(playerCouldMove);
        // El interactor vuelve un fotograma después: si no, la E que cerró el diálogo lo abriría otra vez.
        if (interactor != null) StartCoroutine(EnableInteractorNextFrame());

        var callback = endCallback;
        endCallback = null;
        eventCallback = null;
        data = null;

        onDialogueEnded.Invoke();
        callback?.Invoke();
    }

    IEnumerator EnableInteractorNextFrame()
    {
        yield return null;
        // Si en ese fotograma empezó otra conversación, se queda apagado.
        if (!IsRunning) interactor.enabled = true;
    }
}
