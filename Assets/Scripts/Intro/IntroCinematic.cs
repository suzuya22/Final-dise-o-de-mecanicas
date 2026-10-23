using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Cinemática de la pantalla de inicio:
// 1. Al hacer clic en el objeto clickeable, los papeles se hunden en el charco.
// 2. Tras una espera, la cámara rota suavemente de la rotación inicial a la final.
public class IntroCinematic : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Cámara que rota. Si está vacío se usa la Main Camera.")]
    public Camera targetCamera;
    [Tooltip("Objeto al que hay que hacer clic (necesita un Collider).")]
    public Collider clickableObject;
    [Tooltip("Objetos que se hunden al hacer clic (el papel naranja, el amarillo...).")]
    public Transform[] sinkingObjects;

    [Header("Hundimiento")]
    [Tooltip("Cuánto bajan los papeles, en metros.")]
    public float sinkDepth = 0.1f;
    [Tooltip("Segundos que tardan en hundirse.")]
    public float sinkDuration = 2f;
    [Tooltip("Desactivar los papeles cuando terminan de hundirse.")]
    public bool hideAfterSink = true;

    [Header("Cámara")]
    public Vector3 startRotation = new Vector3(25.949f, 119.891f, 2.795f);
    public Vector3 endRotation = new Vector3(-6.812f, 70.967f, 2.745f);
    [Tooltip("Espera entre que terminan de hundirse los papeles y empieza a girar la cámara.")]
    public float delayBeforeCamera = 1f;
    [Tooltip("Segundos que dura el giro de la cámara.")]
    public float cameraDuration = 4f;
    [Tooltip("Suavizado del giro: empieza lento, acelera y frena al final.")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Outline al pasar el mouse")]
    [Tooltip("Modelo que recibe el contorno (el papel naranja).")]
    public MeshRenderer outlineTarget;
    public Color outlineColor = Color.white;
    [Tooltip("Grosor del contorno (0.06 = 6 % más grande que el objeto).")]
    [Range(0f, 0.3f)] public float outlineThickness = 0.06f;
    [Tooltip("Cuánto se aleja el contorno de la cámara para quedar detrás del objeto.")]
    public float outlineDepthOffset = 0.01f;
    [Tooltip("Material opcional para el contorno. Si está vacío se usa uno sin iluminación con Outline Color.")]
    public Material outlineMaterial;

    [Header("Eventos")]
    public UnityEvent onCinematicStart;
    public UnityEvent onCinematicEnd;

    bool played;
    GameObject outline;
    Vector3 outlineBaseLocalPosition;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        targetCamera.transform.rotation = Quaternion.Euler(startRotation);
        CreateOutline();
    }

    void Update()
    {
        if (played) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        Ray ray = targetCamera.ScreenPointToRay(mouse.position.ReadValue());
        bool hovering = Physics.Raycast(ray, out RaycastHit hit) && hit.collider == clickableObject;
        ShowOutline(hovering);

        if (hovering && mouse.leftButton.wasPressedThisFrame)
        {
            played = true;
            ShowOutline(false);
            StartCoroutine(PlayCinematic());
        }
    }

    // El contorno es una copia del modelo, un poco más grande y de un solo color,
    // colocada justo detrás del original: solo se ve el borde que sobresale.
    void CreateOutline()
    {
        if (outlineTarget == null) return;
        var meshFilter = outlineTarget.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        if (outlineMaterial == null)
        {
            outlineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            outlineMaterial.SetColor("_BaseColor", outlineColor);
        }

        Mesh mesh = meshFilter.sharedMesh;
        outline = new GameObject("Outline");
        outline.transform.SetParent(outlineTarget.transform, false);

        // Escala desde el centro del modelo, no desde su pivote.
        float scale = 1f + outlineThickness;
        outlineBaseLocalPosition = mesh.bounds.center - mesh.bounds.center * scale;
        outline.transform.localPosition = outlineBaseLocalPosition;
        outline.transform.localScale = Vector3.one * scale;

        outline.AddComponent<MeshFilter>().sharedMesh = mesh;
        var outlineRenderer = outline.AddComponent<MeshRenderer>();
        var materials = new Material[mesh.subMeshCount];
        for (int i = 0; i < materials.Length; i++) materials[i] = outlineMaterial;
        outlineRenderer.sharedMaterials = materials;
        outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;

        outline.SetActive(false);
    }

    void ShowOutline(bool show)
    {
        if (outline == null || outline.activeSelf == show) return;

        if (show)
        {
            outline.transform.localPosition = outlineBaseLocalPosition;
            Vector3 awayFromCamera = (outline.transform.position - targetCamera.transform.position).normalized;
            outline.transform.position += awayFromCamera * outlineDepthOffset;
        }
        outline.SetActive(show);
    }

    IEnumerator PlayCinematic()
    {
        onCinematicStart.Invoke();

        yield return SinkObjects();
        yield return new WaitForSeconds(delayBeforeCamera);
        yield return RotateCamera();

        onCinematicEnd.Invoke();
    }

    IEnumerator SinkObjects()
    {
        var startPositions = new Vector3[sinkingObjects.Length];
        for (int i = 0; i < sinkingObjects.Length; i++)
            startPositions[i] = sinkingObjects[i].position;

        float t = 0f;
        while (t < sinkDuration)
        {
            t += Time.deltaTime;
            float k = easeCurve.Evaluate(Mathf.Clamp01(t / sinkDuration));
            for (int i = 0; i < sinkingObjects.Length; i++)
                sinkingObjects[i].position = startPositions[i] + Vector3.down * (sinkDepth * k);
            yield return null;
        }

        if (hideAfterSink)
        {
            foreach (var obj in sinkingObjects) obj.gameObject.SetActive(false);
        }
    }

    IEnumerator RotateCamera()
    {
        Quaternion from = Quaternion.Euler(startRotation);
        Quaternion to = Quaternion.Euler(endRotation);
        Transform cam = targetCamera.transform;

        float t = 0f;
        while (t < cameraDuration)
        {
            t += Time.deltaTime;
            cam.rotation = Quaternion.Slerp(from, to, easeCurve.Evaluate(Mathf.Clamp01(t / cameraDuration)));
            yield return null;
        }
        cam.rotation = to;
    }

    [ContextMenu("Guardar rotación actual como inicial")]
    void SaveCurrentAsStart()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        RecordUndo();
        startRotation = targetCamera.transform.eulerAngles;
    }

    [ContextMenu("Guardar rotación actual como final")]
    void SaveCurrentAsEnd()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        RecordUndo();
        endRotation = targetCamera.transform.eulerAngles;
    }

    // Para que el cambio se guarde en la escena y se pueda deshacer con Ctrl+Z.
    void RecordUndo()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Guardar rotación de cámara");
#endif
    }
}
