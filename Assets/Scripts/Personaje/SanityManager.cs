using UnityEngine;
using UnityEngine.Events;

// Cordura del jugador. Otros scripts la bajan con Drain() o la suben con Restore().
// Por ahora solo la usa el puzzle de 7 minutos en el cielo (al llegar a 0 reinicia el puzzle).
public class SanityManager : MonoBehaviour
{
    public float maxSanity = 100f;

    [Header("Eventos")]
    [Tooltip("Se llama cada vez que cambia, con la cordura de 0 a 1.")]
    public UnityEvent<float> onSanityChanged;
    [Tooltip("Se llama una vez al llegar a 0.")]
    public UnityEvent onSanityDepleted;

    public float Current { get; private set; }
    public float Normalized => maxSanity > 0f ? Current / maxSanity : 0f;
    public bool IsDepleted { get; private set; }

    void Awake()
    {
        Current = maxSanity;
    }

    public void Drain(float amount)
    {
        if (IsDepleted || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        onSanityChanged.Invoke(Normalized);

        if (Current <= 0f)
        {
            IsDepleted = true;
            onSanityDepleted.Invoke();
        }
    }

    public void Restore(float amount)
    {
        if (amount <= 0f) return;
        Current = Mathf.Min(maxSanity, Current + amount);
        IsDepleted = Current <= 0f;
        onSanityChanged.Invoke(Normalized);
    }

    public void ResetSanity()
    {
        Current = maxSanity;
        IsDepleted = false;
        onSanityChanged.Invoke(Normalized);
    }
}
