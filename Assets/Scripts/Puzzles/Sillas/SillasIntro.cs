using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Entrada a la sala de las sillas musicales: los NPC juegan una partida de demostración, la cámara hace un
// recorrido viéndolos y al terminar se lanza la conversación de invitación (onTourFinished). Si el jugador
// acepta (Accept), un parpadeo oscuro oculta el cambio y empieza la partida de verdad.
//
// Conexiones: la puerta de entrada llama a Begin() (en vez de a MusicalChairsGame.StartGame);
// onTourFinished llama al Play() del DialogueTrigger de la persona que invita; el evento "jugar" de ese
// diálogo llama a Accept().
public class SillasIntro : MonoBehaviour
{
    [Header("Referencias")]
    public MusicalChairsGame game;
    public PlayerMovement player;
    [Tooltip("Cámara del jugador (se apaga durante el recorrido).")]
    public Camera playerCamera;
    [Tooltip("Cámara del recorrido. Déjala desactivada y con su propio AudioListener.")]
    public Camera tourCamera;
    [Tooltip("Hacia dónde mira la cámara durante el recorrido (el centro del círculo).")]
    public Transform lookTarget;
    [Tooltip("Ruta de la cámara, en orden. Se recorre con una curva suave.")]
    public Transform[] waypoints = new Transform[0];

    [Header("Recorrido")]
    [Min(1f)] public float tourDuration = 7f;
    [Tooltip("Cuánto por encima de lookTarget mira la cámara (metros).")]
    public float lookHeight = 0.8f;
    [Tooltip("El recorrido solo se hace la primera vez que entras; las siguientes solo empieza la demostración.")]
    public bool tourOnlyFirstTime = true;

    [Header("Al aceptar jugar")]
    [Tooltip("Opcional: el parpadeo oscuro que oculta el cambio de la demostración a la partida.")]
    public ChairsBlackout blackout;

    [Header("Eventos")]
    [Tooltip("Al entrar a la sala (por ejemplo para volver a permitir hablar con quien invita).")]
    public UnityEvent onBegin;
    [Tooltip("Al terminar el recorrido (por ejemplo para iniciar la conversación de invitación).")]
    public UnityEvent onTourFinished;

    bool tourSeen;
    Coroutine tour;

    public void Begin()
    {
        game.StartDemo();
        onBegin.Invoke();

        if (tourOnlyFirstTime && tourSeen) return;
        if (waypoints == null || waypoints.Length < 2 || tourCamera == null) return;

        tourSeen = true;
        if (tour != null) StopCoroutine(tour);
        tour = StartCoroutine(Tour());
    }

    // Lo llama el diálogo cuando el jugador acepta jugar.
    public void Accept()
    {
        if (blackout != null) blackout.Blink(StartPlaying);
        else StartPlaying();
    }

    void StartPlaying()
    {
        game.StopDemo();
        game.StartGame();
    }

    IEnumerator Tour()
    {
        player.SetCanMove(false);
        playerCamera.gameObject.SetActive(false);
        tourCamera.gameObject.SetActive(true);

        for (float t = 0f; t < tourDuration; t += Time.deltaTime)
        {
            PlaceCamera(Mathf.SmoothStep(0f, 1f, t / tourDuration));
            yield return null;
        }
        PlaceCamera(1f);

        tourCamera.gameObject.SetActive(false);
        playerCamera.gameObject.SetActive(true);
        player.SetCanMove(true);
        tour = null;
        onTourFinished.Invoke();
    }

    // Curva Catmull-Rom por los puntos de la ruta (k de 0 a 1), mirando siempre al centro.
    void PlaceCamera(float k)
    {
        int segments = waypoints.Length - 1;
        float f = Mathf.Clamp01(k) * segments;
        int i = Mathf.Min(segments - 1, Mathf.FloorToInt(f));
        float u = f - i;

        Vector3 p0 = waypoints[Mathf.Max(i - 1, 0)].position;
        Vector3 p1 = waypoints[i].position;
        Vector3 p2 = waypoints[i + 1].position;
        Vector3 p3 = waypoints[Mathf.Min(i + 2, waypoints.Length - 1)].position;
        float u2 = u * u, u3 = u2 * u;
        Vector3 pos = 0.5f * ((2f * p1) + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 + (-p0 + 3f * p1 - 3f * p2 + p3) * u3);

        tourCamera.transform.position = pos;
        Vector3 target = (lookTarget != null ? lookTarget.position : Vector3.zero) + Vector3.up * lookHeight;
        Vector3 dir = target - pos;
        if (dir.sqrMagnitude > 0.0001f) tourCamera.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    void OnDisable()
    {
        // Si se corta a mitad (cambio de escena, desactivar la sala), la cámara del jugador no se queda apagada.
        if (tour != null && playerCamera != null) playerCamera.gameObject.SetActive(true);
    }
}
