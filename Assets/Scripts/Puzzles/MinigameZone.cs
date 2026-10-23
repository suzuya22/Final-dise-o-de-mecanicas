using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Zona que activa un minijuego al acercarse el personaje:
// cambia a la cámara del minijuego, bloquea al personaje y activa los scripts del minijuego.
// Con Escape se sale y se vuelve a la cámara del personaje.
// Va en un objeto con un Collider marcado como "Is Trigger".
public class MinigameZone : MonoBehaviour
{
    [Header("Personaje")]
    public PlayerMovement player;
    public Camera playerCamera;

    [Header("Minijuego")]
    public Camera minigameCamera;
    [Tooltip("Scripts del minijuego que se activan al entrar (en el beer pong: BeerPongManager y PongLauncher).")]
    public Behaviour[] minigameScripts;

    [Header("Controles")]
    public Key exitKey = Key.Escape;

    [Header("Eventos")]
    public UnityEvent onEnter;
    public UnityEvent onExit;

    public bool IsPlaying { get; private set; }

    // Tras salir con Escape hay que dejar la zona antes de poder entrar de nuevo.
    bool waitingToLeave;

    void Start()
    {
        // El minijuego empieza apagado.
        minigameCamera.gameObject.SetActive(false);
        SetScriptsEnabled(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlaying || waitingToLeave || !IsPlayer(other)) return;
        EnterMinigame();
    }

    void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other)) waitingToLeave = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (IsPlaying && keyboard != null && keyboard[exitKey].wasPressedThisFrame)
            ExitMinigame();
    }

    public void EnterMinigame()
    {
        IsPlaying = true;

        player.SetCanMove(false);
        playerCamera.gameObject.SetActive(false);
        minigameCamera.gameObject.SetActive(true);
        SetScriptsEnabled(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        onEnter.Invoke();
    }

    public void ExitMinigame()
    {
        IsPlaying = false;
        waitingToLeave = true;

        SetScriptsEnabled(false);
        minigameCamera.gameObject.SetActive(false);
        playerCamera.gameObject.SetActive(true);
        player.SetCanMove(true);

        onExit.Invoke();
    }

    void SetScriptsEnabled(bool value)
    {
        foreach (var script in minigameScripts)
            if (script != null) script.enabled = value;
    }

    bool IsPlayer(Collider other)
    {
        return other.GetComponentInParent<PlayerMovement>() == player;
    }
}
