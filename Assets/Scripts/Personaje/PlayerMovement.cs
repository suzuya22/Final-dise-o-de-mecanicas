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
    [Tooltip("Velocidad al caminar hacia atrás (modo tanque).")]
    public float backwardSpeed = 2f;
    public float gravity = -20f;

    [Header("Giro")]
    [Tooltip("Grados por segundo al girar con A/D (modo tanque).")]
    public float turnSpeed = 150f;
    [Tooltip("Sensibilidad del mouse (modo mouse).")]
    public float mouseSensitivity = 0.15f;

    public bool CanMove { get; private set; } = true;

    CharacterController controller;
    float verticalVelocity;

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

    void UpdateCursor()
    {
        bool lockCursor = CanMove && controlMode == ControlMode.Mouse;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    void Update()
    {
        Vector2 input = Vector2.zero;
        var keyboard = Keyboard.current;
        if (CanMove && keyboard != null)
        {
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
        }

        Vector3 move;
        if (controlMode == ControlMode.Tanque)
        {
            transform.Rotate(0f, input.x * turnSpeed * Time.deltaTime, 0f);
            float speed = input.y >= 0f ? moveSpeed : backwardSpeed;
            move = transform.forward * (input.y * speed);
        }
        else
        {
            var mouse = Mouse.current;
            if (CanMove && mouse != null)
                transform.Rotate(0f, mouse.delta.ReadValue().x * mouseSensitivity, 0f);

            move = (transform.forward * input.y + transform.right * input.x);
            move = Vector3.ClampMagnitude(move, 1f) * moveSpeed;
        }

        // Gravedad: pegado al suelo mientras está apoyado.
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }
}
