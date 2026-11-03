using System.Collections;
using UnityEngine;

// Vaso del beer pong. Tiene colisión real (para que la pelota rebote en el borde)
// y un trigger en el fondo que detecta cuando la pelota entra.
public class PongCup : MonoBehaviour
{
    [Tooltip("Índice del color en la paleta del BeerPongManager.")]
    public int colorIndex;
    public bool tintRenderers = true;

    [Header("Vaso de dos tonos")]
    [Tooltip("Si está activo, el color de la paleta solo pinta el cuerpo; el labio y el interior quedan del Rim Color.")]
    public bool keepRimWhite = true;
    public Color rimColor = Color.white;
    [Tooltip("Coordenada U de la textura donde termina el cuerpo y empieza el labio/interior (según las UV de Vaso.fbx).")]
    [Range(0f, 1f)] public float rimSplitU = 0.6074f;

    // Una textura por color, compartida entre todos los vasos.
    static readonly System.Collections.Generic.Dictionary<Color, Texture2D> twoToneCache =
        new System.Collections.Generic.Dictionary<Color, Texture2D>();

    [Header("Zona de detección")]
    [Tooltip("Trigger en el fondo del vaso. Si está vacío se crea uno automáticamente.")]
    public Collider detectionTrigger;
    [Range(0.05f, 1f)] public float triggerHeight01 = 0.3f;
    [Range(0.05f, 1f)] public float triggerWidth01 = 0.55f;

    [Header("Al encestar")]
    public float sinkDuration = 0.35f;

    public bool IsAvailable { get; private set; } = true;

    // Medidas del vaso de pie (en el mundo), para colocarlo en una pirámide.
    public float Width { get; private set; }
    public float Height { get; private set; }
    // Distancia del pivote a la base del vaso.
    public float PivotToBase { get; private set; }
    // Con física activa (evento de la pirámide) el vaso es un Rigidbody que se puede derribar.
    public bool PhysicsActive => body != null;

    Vector3 initialScale;
    Vector3 initialPosition;
    Rigidbody body;
    Coroutine benchRoutine;

    void Awake()
    {
        initialScale = transform.localScale;
        initialPosition = transform.localPosition;
        if (detectionTrigger == null) CreateDetectionTrigger();
        MeasureSize();
    }

    // Mide el vaso de pie (se llama al empezar; también se puede llamar a mano tras moverlo).
    public void MeasureSize()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        Width = Mathf.Max(b.size.x, b.size.z);
        Height = b.size.y;
        PivotToBase = transform.position.y - b.min.y;
    }

    // Se ejecuta al añadir el componente en el editor: deja las colisiones guardadas en la escena.
    void Reset()
    {
        SetupColliders();
    }

    [ContextMenu("Configurar colisiones")]
    void SetupColliders()
    {
        // MeshCollider no convexo para que la pelota pueda entrar al vaso y rebotar en las paredes.
        foreach (var mf in GetComponentsInChildren<MeshFilter>())
        {
            if (mf.GetComponent<Collider>() != null) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }

        if (detectionTrigger == null) CreateDetectionTrigger();
    }

    void CreateDetectionTrigger()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        var go = new GameObject("ZonaDeteccion");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(b.center.x, b.min.y + b.size.y * triggerHeight01 * 0.5f, b.center.z);
        go.transform.rotation = Quaternion.identity;
        // Compensa la escala del padre para poder trabajar en unidades del mundo.
        Vector3 s = transform.lossyScale;
        go.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);

        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(b.size.x * triggerWidth01, b.size.y * triggerHeight01, b.size.z * triggerWidth01);
        detectionTrigger = box;
    }

    // Zona donde la pelota cuenta como encestada: una caja centrada en el eje del vaso, desde el fondo hasta un poco
    // por encima del borde. Un trigger solo en el fondo era demasiado exigente: la pelota rebotaba en el aro y no
    // llegaba (con él solo contaban los tiros a menos de ~4 cm del centro; con esta zona, ~13 cm de ancho).
    public void SetCatchZone(float width, float aboveRim)
    {
        var box = detectionTrigger as BoxCollider;
        if (box == null) return;

        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        float y0 = b.min.y + b.size.y * 0.2f;           // justo sobre el suelo interior
        float y1 = b.max.y + Mathf.Max(0f, aboveRim);   // un poco sobre el borde
        box.transform.rotation = Quaternion.identity;
        box.transform.position = new Vector3(b.center.x, (y0 + y1) * 0.5f, b.center.z);
        box.center = Vector3.zero;
        Vector3 s = box.transform.lossyScale;
        box.size = new Vector3(width / s.x, (y1 - y0) / s.y, width / s.z);
    }

    // Un punto dentro del vaso, cerca del fondo (adonde cae la pelota que encesta).
    public Vector3 InsidePoint
    {
        get
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return transform.position;
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return new Vector3(b.center.x, b.min.y + b.size.y * 0.3f, b.center.z);
        }
    }

    public void ApplyColor(Color color)
    {
        if (!tintRenderers) return;

        if (!keepRimWhite)
        {
            BeerPongManager.Tint(GetComponentsInChildren<Renderer>(), color);
            return;
        }

        // En vez de multiplicar todo el vaso por el color (lo que ensucia el blanco y oscurece el cuerpo),
        // se cambia la textura por una de dos tonos: cuerpo del color de la paleta, labio e interior blancos.
        var block = new MaterialPropertyBlock();
        Texture2D tex = GetTwoToneTexture(color);
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.GetPropertyBlock(block);
            block.SetTexture("_BaseMap", tex);
            block.SetTexture("_MainTex", tex);
            block.SetColor("_BaseColor", Color.white);
            block.SetColor("_Color", Color.white);
            r.SetPropertyBlock(block);
        }
    }

    Texture2D GetTwoToneTexture(Color bodyColor)
    {
        bodyColor.a = 1f;
        if (twoToneCache.TryGetValue(bodyColor, out var cached) && cached != null) return cached;

        // Las UV del vaso solo cambian de zona en U, así que basta una textura de una fila.
        const int width = 2048;
        // Sin mipmaps: así el corte entre color y blanco no se difumina al ver los vasos de lejos.
        var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false)
        {
            name = "VasoDosTonos",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        int split = Mathf.RoundToInt(rimSplitU * width);
        var pixels = new Color32[width];
        for (int x = 0; x < width; x++) pixels[x] = x < split ? bodyColor : rimColor;
        tex.SetPixels32(pixels);
        tex.Apply(false, true);

        twoToneCache[bodyColor] = tex;
        return tex;
    }

    public void Sink()
    {
        IsAvailable = false;
        StartCoroutine(SinkRoutine());
    }

    IEnumerator SinkRoutine()
    {
        float t = 0f;
        while (t < sinkDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t / sinkDuration);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    public void ResetCup()
    {
        StopAllCoroutines();
        benchRoutine = null;
        DisablePhysics();
        if (initialScale != Vector3.zero) transform.localScale = initialScale;
        transform.localPosition = initialPosition;
        gameObject.SetActive(true);
        IsAvailable = true;
    }

    // Desliza el vaso hasta otra posición (al mezclar), con un pequeño salto y un rodeo lateral
    // para no atravesar al vaso con el que se intercambia.
    public IEnumerator SlideTo(Vector3 target, float duration, float hopHeight, Vector3 sideOffset)
    {
        Vector3 start = transform.position;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            float arc = Mathf.Sin(k * Mathf.PI);
            transform.position = Vector3.Lerp(start, target, k) + Vector3.up * (hopHeight * arc) + sideOffset * arc;
            yield return null;
        }
        transform.position = target;
    }

    // ---- Evento de la pirámide: el vaso pasa a ser un objeto con física que se puede derribar.

    // Mientras está activa, el cuerpo del vaso es un casco convexo (sólido) y el trigger de detección se apaga.
    // Empieza en modo cinemático (para colocarlo); Release() lo suelta.
    public void EnablePhysics(float mass, float linearDamping, float angularDamping, PhysicsMaterial material)
    {
        if (body != null) return;

        foreach (var mc in GetComponentsInChildren<MeshCollider>(true))
        {
            mc.convex = true;
            mc.sharedMaterial = material;
        }
        if (detectionTrigger != null) detectionTrigger.enabled = false;

        body = gameObject.AddComponent<Rigidbody>();
        body.mass = mass;
        body.linearDamping = linearDamping;
        body.angularDamping = angularDamping;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.isKinematic = true;
    }

    public void Release()
    {
        if (body != null) body.isKinematic = false;
    }

    public void DisablePhysics()
    {
        if (body != null)
        {
            body.isKinematic = true;
            Destroy(body);
            body = null;
        }
        foreach (var mc in GetComponentsInChildren<MeshCollider>(true))
        {
            mc.convex = false;
            mc.sharedMaterial = null;
        }
        if (detectionTrigger != null) detectionTrigger.enabled = true;
    }

    // Quieto del todo (o dormido): se puede evaluar si cayó.
    public bool IsSettled =>
        body == null || body.isKinematic || body.IsSleeping() ||
        (body.linearVelocity.sqrMagnitude < 0.0004f && body.angularVelocity.sqrMagnitude < 0.01f);

    // Está derribado si se inclinó mucho o se movió bastante de su sitio en la pirámide.
    public bool IsKnocked(Vector3 slotPosition, float distanceThreshold)
    {
        float tilt = Vector3.Angle(transform.up, Vector3.up);
        return tilt > 40f || (transform.position - slotPosition).sqrMagnitude > distanceThreshold * distanceThreshold;
    }

    // Va a una posición y giro concretos (para formar la pirámide y para volver a su sitio después).
    public IEnumerator SlideToPose(Vector3 target, Quaternion targetRotation, float duration, float hopHeight)
    {
        Vector3 start = transform.position;
        Quaternion startRotation = transform.rotation;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            transform.position = Vector3.Lerp(start, target, k) + Vector3.up * (hopHeight * Mathf.Sin(k * Mathf.PI));
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, k);
            yield return null;
        }
        transform.SetPositionAndRotation(target, targetRotation);
    }

    // Se encoge y desaparece (se aparta mientras dura el evento)...
    public void Bench(float duration)
    {
        if (benchRoutine != null) StopCoroutine(benchRoutine);
        benchRoutine = StartCoroutine(ScaleRoutine(Vector3.zero, duration, true));
    }

    // ...y vuelve a crecer en su sitio.
    public void Unbench(float duration)
    {
        if (benchRoutine != null) StopCoroutine(benchRoutine);
        gameObject.SetActive(true);
        benchRoutine = StartCoroutine(ScaleRoutine(initialScale, duration, false));
    }

    IEnumerator ScaleRoutine(Vector3 target, float duration, bool hideAtEnd)
    {
        Vector3 start = transform.localScale;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        transform.localScale = target;
        if (hideAtEnd) gameObject.SetActive(false);
        benchRoutine = null;
    }

    void OnDrawGizmosSelected()
    {
        if (detectionTrigger == null) return;
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawCube(detectionTrigger.bounds.center, detectionTrigger.bounds.size);
    }
}
