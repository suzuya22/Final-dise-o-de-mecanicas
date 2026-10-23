using System.Collections;
using UnityEngine;

// Vaso del beer pong. Tiene colisión real (para que la pelota rebote en el borde)
// y un trigger en el fondo que detecta cuando la pelota entra.
public class PongCup : MonoBehaviour
{
    [Tooltip("Índice del color en la paleta del BeerPongManager.")]
    public int colorIndex;
    public bool tintRenderers = true;

    [Header("Zona de detección")]
    [Tooltip("Trigger en el fondo del vaso. Si está vacío se crea uno automáticamente.")]
    public Collider detectionTrigger;
    [Range(0.05f, 1f)] public float triggerHeight01 = 0.3f;
    [Range(0.05f, 1f)] public float triggerWidth01 = 0.55f;

    [Header("Al encestar")]
    public float sinkDuration = 0.35f;

    public bool IsAvailable { get; private set; } = true;

    Vector3 initialScale;

    void Awake()
    {
        initialScale = transform.localScale;
        if (detectionTrigger == null) CreateDetectionTrigger();
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

    public void ApplyColor(Color color)
    {
        if (tintRenderers) BeerPongManager.Tint(GetComponentsInChildren<Renderer>(), color);
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
        if (initialScale != Vector3.zero) transform.localScale = initialScale;
        gameObject.SetActive(true);
        IsAvailable = true;
    }

    void OnDrawGizmosSelected()
    {
        if (detectionTrigger == null) return;
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawCube(detectionTrigger.bounds.center, detectionTrigger.bounds.size);
    }
}
