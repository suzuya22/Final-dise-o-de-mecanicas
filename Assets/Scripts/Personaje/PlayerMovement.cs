using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento del personaje con CharacterController.
// - Tanque:  W/S avanza y retrocede, A/D gira al personaje.
// - Mouse:   el mouse gira al personaje, WASD mueve hacia los lados (cursor bloqueado).
// La cámara, al ser hija del personaje, lo sigue sola.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public enum ControlMode { Tanque, Mouse }

    [Header("Controles")]
    public ControlMode controlMode = ControlMode.Tanque;

    [Header("Movimiento")]
    [Tooltip("Velocidad al caminar, en metros por segundo.")]
    public float moveSpeed = 3f;
    [Tooltip("Velocidad al correr (manteniendo Left Shift), en metros por segundo.")]
    public float runSpeed = 5.5f;
    [Tooltip("Velocidad al caminar hacia atrás (modo tanque).")]
    public float backwardSpeed = 2f;
    public float gravity = -20f;

    [Header("Giro")]
    [Tooltip("Grados por segundo al girar con A/D (modo tanque).")]
    public float turnSpeed = 150f;
    [Tooltip("Sensibilidad del mouse (modo mouse).")]
    public float mouseSensitivity = 0.15f;

    [Header("Empujones")]
    [Tooltip("Qué tan rápido se frena un empujón (por ejemplo el golpe de un cartón).")]
    public float knockbackDamping = 6f;

    public bool CanMove { get; private set; } = true;
    // True si el jugador está pulsando teclas de movimiento (avanzar o girar).
    public bool IsMoving { get; private set; }
    // True si el jugador se está moviendo y mantiene presionado Shift para correr.
    public bool IsRunning { get; private set; }

    CharacterController controller;
    float verticalVelocity;
    Vector3 knockback;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        UpdateCursor();
    }

    // Lo usan otros sistemas (por ejemplo los minijuegos) para bloquear al personaje.
    public void SetCanMove(bool value)
    {
        CanMove = value;
        UpdateCursor();
    }

    // Mueve al jugador al instante (por ejemplo al reiniciar un puzzle).
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = true;
        verticalVelocity = 0f;
        knockback = Vector3.zero;
    }

    // Empuja al jugador; el empujón se va frenando solo.
    public void AddKnockback(Vector3 velocity)
    {
        velocity.y = 0f;
        knockback += velocity;
    }

    void UpdateCursor()
    {
        bool lockCursor = CanMove && controlMode == ControlMode.Mouse;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    void Update()
    {
        Vector2 input = Vector2.zero;
        bool shiftHeld = false;
        var keyboard = Keyboard.current;
        if (CanMove && keyboard != null)
        {
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            shiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }
        IsMoving = input != Vector2.zero;

        Vector3 move;
        if (controlMode == ControlMode.Tanque)
        {
            transform.Rotate(0f, input.x * turnSpeed * Time.deltaTime, 0f);
            IsRunning = shiftHeld && input.y > 0f;
            float forwardSpeed = IsRunning ? runSpeed : moveSpeed;
            float speed = input.y >= 0f ? forwardSpeed : backwardSpeed;
            move = transform.forward * (input.y * speed);
        }
        else
        {
            var mouse = Mouse.current;
            if (CanMove && mouse != null)
                transform.Rotate(0f, mouse.delta.ReadValue().x * mouseSensitivity, 0f);

            IsRunning = shiftHeld && IsMoving;
            float currentSpeed = IsRunning ? runSpeed : moveSpeed;
            move = (transform.forward * input.y + transform.right * input.x);
            move = Vector3.ClampMagnitude(move, 1f) * currentSpeed;
        }

        // Gravedad: pegado al suelo mientras está apoyado.
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        move += knockback;
        knockback = Vector3.Lerp(knockback, Vector3.zero, 1f - Mathf.Exp(-knockbackDamping * Time.deltaTime));

        controller.Move(move * Time.deltaTime);
    }
}
