using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// QTE de tiempo: una marca va y viene por una barra; hay que pulsar la tecla indicada
// (una al azar en cada pulsación) cuando la marca está dentro de la zona. Un solo intento por pulsación.
public class TimingQTE : MonoBehaviour
{
    [Tooltip("Teclas posibles; en cada pulsación se elige una al azar.")]
    public Key[] possibleKeys = { Key.W, Key.A, Key.S, Key.D };
    [Tooltip("Cuántas pulsaciones tiene la prueba.")]
    public int prompts = 3;
    [Tooltip("Ancho de la zona correcta (0-1 de la barra).")]
    [Range(0.05f, 0.5f)] public float zoneWidth = 0.18f;
    [Tooltip("Idas y vueltas de la marca por segundo.")]
    public float markerSpeed = 0.8f;
    [Tooltip("Segundos máximos por pulsación (si no pulsa, cuenta como fallo).")]
    public float timePerPrompt = 3f;
    [Tooltip("Pausa entre pulsaciones.")]
    public float pauseBetween = 0.6f;

    public bool IsRunning { get; private set; }
    public Key CurrentKey { get; private set; }
    public int CurrentPrompt { get; private set; }
    public int Successes { get; private set; }
    // Posición de la marca y zona, de 0 a 1.
    public float Marker { get; private set; }
    public float ZoneCenter { get; private set; }
    public bool InZone => Mathf.Abs(Marker - ZoneCenter) <= zoneWidth * 0.5f;
    // Resultado de la última pulsación (null mientras se espera).
    public bool? LastResult { get; private set; }

    // onDone(aciertos).
    public IEnumerator Run(Action<int> onDone)
    {
        IsRunning = true;
        Successes = 0;

        for (CurrentPrompt = 0; CurrentPrompt < prompts; CurrentPrompt++)
        {
            CurrentKey = possibleKeys[UnityEngine.Random.Range(0, possibleKeys.Length)];
            ZoneCenter = UnityEngine.Random.Range(zoneWidth, 1f - zoneWidth);
            LastResult = null;
            float t = UnityEngine.Random.value;
            float elapsed = 0f;

            while (LastResult == null && elapsed < timePerPrompt)
            {
                t += Time.deltaTime * markerSpeed;
                Marker = Mathf.PingPong(t * 2f, 1f);
                elapsed += Time.deltaTime;

                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    foreach (var k in possibleKeys)
                    {
                        if (!keyboard[k].wasPressedThisFrame) continue;
                        LastResult = k == CurrentKey && InZone;
                        break;
                    }
                }
                yield return null;
            }

            if (LastResult == true) Successes++;
            if (LastResult == null) LastResult = false;
            yield return new WaitForSeconds(pauseBetween);
        }

        IsRunning = false;
        onDone?.Invoke(Successes);
    }
}
