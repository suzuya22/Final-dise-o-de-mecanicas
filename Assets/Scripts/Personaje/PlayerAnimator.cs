using UnityEngine;

// Pasa el estado del movimiento al Animator del personaje
// (parámetros del ProtaController: IsWalking, IsRunning y Speed).
public class PlayerAnimator : MonoBehaviour
{
    public PlayerMovement movement;
    [Tooltip("El Animator del modelo (con el ProtaController).")]
    public Animator animator;

    [Header("Nombres de los parámetros")]
    public string walkingParam = "IsWalking";
    public string runningParam = "IsRunning";
    public string speedParam = "Speed";

    void Update()
    {
        // Si el Animator está apagado (por ejemplo durante la pose sentada), no se toca.
        if (movement == null || animator == null || !animator.isActiveAndEnabled) return;

        bool walking = movement.CanMove && movement.IsMoving;
        bool running = walking && movement.IsRunning;
        animator.SetBool(walkingParam, walking);
        animator.SetBool(runningParam, running);
        animator.SetFloat(speedParam, running ? 2f : walking ? 1f : 0f);
    }
}
