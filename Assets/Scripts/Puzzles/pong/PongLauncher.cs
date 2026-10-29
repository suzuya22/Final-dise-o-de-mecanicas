using UnityEngine;
using UnityEngine.InputSystem;

// Lanzamiento arrastrando el mouse:
// - Mantén clic izquierdo y arrastra hacia arriba: cuanto más arrastras, más fuerza.
// - Lo que te desvías a izquierda/derecha cambia la dirección del tiro.
// - Al soltar se lanza. Mientras arrastras se ve la parábola.
public class PongLauncher : MonoBehaviour
{
    [Header("Referencias")]
    public BeerPongManager manager;
    [Tooltip("Desde dónde sale la pelota. NO debe ser hijo de la cámara para que el efecto de borracho no mueva el tiro. Si está vacío se crea frente a la cámara.")]
    public Transform launchPoint;
    [Tooltip("Hacia dónde apunta el tiro recto. Si está vacío se apunta al centro de los vasos.")]
    public Transform aimTarget;
    [Tooltip("Si está vacío se crea uno automáticamente.")]
    public LineRenderer trajectoryLine;
    [Tooltip("Material de la línea. Si está vacío se usa uno sin iluminación.")]
    public Material lineMaterial;

    [Header("Arrastre (fracción de la pantalla)")]
    [Tooltip("Arrastre vertical para llegar a la fuerza máxima.")]
    public float fullPowerDrag = 0.35f;
    [Tooltip("Arrastre horizontal para llegar al ángulo lateral máximo.")]
    public float fullSideDrag = 0.25f;
    [Tooltip("Arrastre mínimo para que cuente como tiro (si no, se cancela).")]
    public float minDrag = 0.03f;

    [Header("Lanzamiento")]
    public float minSpeed = 2f;
    public float maxSpeed = 6f;
    [Tooltip("Ángulo de elevación del tiro en grados.")]
    public float launchAngle = 40f;
    [Tooltip("Desvío lateral máximo en grados.")]
    public float maxSideAngle = 25f;

    [Header("Parábola")]
    public int maxPoints = 60;
    public float timeStep = 0.03f;
    public LayerMask trajectoryMask = ~0;
    public float lineWidth = 0.01f;
    public Color lineColor = new Color(1f, 1f, 1f, 0.8f);

    public Transform LaunchPoint
    {
        get
        {
            if (launchPoint == null) CreateLaunchPoint();
            return launchPoint;
        }
    }

    // Lo controla el BeerPongManager: mientras es true no se puede apuntar ni tirar.
    public bool InputLocked { get; set; }

    PongBall ball;
    bool dragging;
    Vector2 dragStart;
    Vector3[] points;

    void Awake()
    {
        if (launchPoint == null) CreateLaunchPoint();
        if (trajectoryLine == null) trajectoryLine = gameObject.AddComponent<LineRenderer>();
        SetupLine();
        points = new Vector3[maxPoints];
        trajectoryLine.enabled = false;
    }

    void CreateLaunchPoint()
    {
        var cam = Camera.main;
        launchPoint = new GameObject("PuntoLanzamiento").transform;
        if (cam != null)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            launchPoint.position = cam.transform.position + flatForward * 0.3f - Vector3.up * 0.15f;
            launchPoint.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
        }
    }

    // Se aplica siempre, aunque el LineRenderer se haya añadido a mano,
    // para que no salga con el ancho por defecto (1 metro) ni sin material (rosa).
    void SetupLine()
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            lineMaterial = new Material(shader);
            lineMaterial.SetColor("_BaseColor", Color.white);
        }
        trajectoryLine.material = lineMaterial;
        trajectoryLine.useWorldSpace = true;
        trajectoryLine.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        trajectoryLine.startColor = lineColor;
        trajectoryLine.endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0f);
        trajectoryLine.widthMultiplier = lineWidth;
        trajectoryLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trajectoryLine.receiveShadows = false;
    }

    // Si se desactiva a mitad de un arrastre, se oculta la parábola.
    void OnDisable()
    {
        dragging = false;
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    public void SetBall(PongBall newBall)
    {
        ball = newBall;
        dragging = false;
    }

    void Update()
    {
        if (ball == null || ball.Launched) return;

        ball.transform.SetPositionAndRotation(launchPoint.position, launchPoint.rotation);

        // Bloqueado mientras los vasos se muestran y se mezclan.
        if (InputLocked)
        {
            dragging = false;
            trajectoryLine.enabled = false;
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            dragging = true;
            dragStart = mousePos;
        }

        if (!dragging) return;

        Vector3 velocity = GetLaunchVelocity(mousePos - dragStart, out bool valid);

        if (mouse.leftButton.isPressed)
        {
            trajectoryLine.enabled = valid;
            if (valid) DrawTrajectory(launchPoint.position, velocity);
        }
        else
        {
            dragging = false;
            trajectoryLine.enabled = false;
            if (valid) Throw(velocity);
        }
    }

    Vector3 GetLaunchVelocity(Vector2 dragPixels, out bool valid)
    {
        // Normaliza el arrastre para que no dependa de la resolución.
        float up = dragPixels.y / Screen.height;
        float side = dragPixels.x / Screen.width;
        valid = up >= minDrag;

        float power01 = Mathf.Clamp01(up / fullPowerDrag);
        float side01 = Mathf.Clamp(side / fullSideDrag, -1f, 1f);

        float speed = Mathf.Lerp(minSpeed, maxSpeed, power01);
        Quaternion aim = Quaternion.AngleAxis(side01 * maxSideAngle, Vector3.up)
                       * Quaternion.LookRotation(GetForward(), Vector3.up)
                       * Quaternion.AngleAxis(-launchAngle, Vector3.right);

        return aim * Vector3.forward * speed;
    }

    // Dirección horizontal del tiro recto (sin arrastre lateral): hacia los vasos,
    // así no depende de cómo esté rotado el punto de lanzamiento.
    Vector3 GetForward()
    {
        Vector3 target;
        if (aimTarget != null)
        {
            target = aimTarget.position;
        }
        else if (manager != null && manager.cups.Count > 0)
        {
            target = Vector3.zero;
            foreach (var cup in manager.cups) target += cup.transform.position;
            target /= manager.cups.Count;
        }
        else
        {
            return Vector3.ProjectOnPlane(launchPoint.forward, Vector3.up).normalized;
        }

        Vector3 dir = Vector3.ProjectOnPlane(target - launchPoint.position, Vector3.up);
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : launchPoint.forward;
    }

    void DrawTrajectory(Vector3 start, Vector3 velocity)
    {
        Vector3 gravity = Physics.gravity;
        int count = 1;
        points[0] = start;

        for (int i = 1; i < maxPoints; i++)
        {
            float t = i * timeStep;
            Vector3 p = start + velocity * t + 0.5f * t * t * gravity;

            // Corta la línea donde choca con algo (mesa, vaso...).
            if (Physics.Linecast(points[i - 1], p, out RaycastHit hit, trajectoryMask, QueryTriggerInteraction.Ignore))
            {
                points[count++] = hit.point;
                break;
            }

            points[count++] = p;
        }

        trajectoryLine.positionCount = count;
        trajectoryLine.SetPositions(points);
    }

    void Throw(Vector3 velocity)
    {
        ball.Launch(velocity);
        if (manager != null) manager.NotifyBallLaunched(ball);
        ball = null;
    }
}
