using UnityEngine;
using UnityEngine.Events;

// Trigger genérico: dispara un evento cuando el jugador entra.
// Va en un objeto con un Collider marcado como "Is Trigger".
public class PlayerTrigger : MonoBehaviour
{
    public PlayerMovement player;
    public UnityEvent onPlayerEnter = new UnityEvent();
    public UnityEvent onPlayerExit = new UnityEvent();

    void OnTriggerEnter(Collider other)
    {
        if (player != null && other.GetComponentInParent<PlayerMovement>() == player) onPlayerEnter.Invoke();
    }

    void OnTriggerExit(Collider other)
    {
        if (player != null && other.GetComponentInParent<PlayerMovement>() == player) onPlayerExit.Invoke();
    }
}
