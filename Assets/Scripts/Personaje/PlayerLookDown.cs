using UnityEngine;
using UnityEngine.InputSystem;

// Mantener Ctrl para mirar hacia abajo: la cámara se inclina hacia el suelo.
// Lo usa el puzzle de los ojos (mirar abajo y quedarse quieto hace perder menos cordura).
public class PlayerLookDown : MonoBehaviour
{
    public Key lookDownKey = Key.LeftCtrl;
    [Tooltip("La cámara del jugador.")]
    public Transform cameraTransform;
    [Tooltip("Grados que se inclina la cámara hacia abajo.")]
    public float lookDownAngle = 45f;
    [Tooltip("Qué tan rápido baja y sube la mirada.")]
    public float speed = 6f;

    // True cuando la mirada ya está casi abajo del todo.
    public bool IsLookingDown => amount > 0.8f;

    Quaternion baseRotation;
    float amount;

    void Start()
    {
        if (cameraTransform != null) baseRotation = cameraTransform.localRotation;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        bool held = keyboard != null && keyboard[lookDownKey].isPressed;

        amount = Mathf.Lerp(amount, held ? 1f : 0f, 1f - Mathf.Exp(-speed * Time.deltaTime));
        if (cameraTransform != null)
            cameraTransform.localRotation = baseRotation * Quaternion.Euler(lookDownAngle * amount, 0f, 0f);
    }

    // Al desactivarse (por ejemplo si se cambia de cámara) la mirada vuelve a su sitio.
    void OnDisable()
    {
        amount = 0f;
        if (cameraTransform != null) cameraTransform.localRotation = baseRotation;
    }
}
