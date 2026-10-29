using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// QTE de forcejeo: pulsar rápido la tecla indicada (una al azar) antes de que se acabe el tiempo.
public class MashQTE : MonoBehaviour
{
    [Tooltip("Teclas posibles; en cada intento se elige una al azar.")]
    public Key[] possibleKeys = { Key.W, Key.A, Key.S, Key.D };
    [Tooltip("Pulsaciones necesarias para liberarse.")]
    public int pressesNeeded = 12;
    [Tooltip("Segundos para lograrlo.")]
    public float timeLimit = 3f;

    public bool IsRunning { get; private set; }
    public Key CurrentKey { get; private set; }
    public int Presses { get; private set; }
    public float TimeLeft { get; private set; }
    public float Progress => pressesNeeded > 0 ? (float)Presses / pressesNeeded : 1f;

    // onDone(true) si lo logra a tiempo.
    public IEnumerator Run(Action<bool> onDone)
    {
        IsRunning = true;
        CurrentKey = possibleKeys[UnityEngine.Random.Range(0, possibleKeys.Length)];
        Presses = 0;
        TimeLeft = timeLimit;

        while (TimeLeft > 0f && Presses < pressesNeeded)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[CurrentKey].wasPressedThisFrame) Presses++;
            TimeLeft -= Time.deltaTime;
            yield return null;
        }

        IsRunning = false;
        onDone?.Invoke(Presses >= pressesNeeded);
    }
}
