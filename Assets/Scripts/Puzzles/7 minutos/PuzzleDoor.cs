using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Puerta que se abre girando sobre su bisagra.
// Va en el objeto que hace de bisagra: un objeto vacío en el borde de la puerta, con la puerta como hija.
public class PuzzleDoor : MonoBehaviour
{
    [Tooltip("Grados que gira al abrirse (negativo para el otro sentido).")]
    public float openAngle = 90f;
    [Tooltip("Eje local de giro (normalmente el vertical).")]
    public Vector3 rotationAxis = Vector3.up;
    public float openDuration = 1.5f;
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Opcional")]
    [Tooltip("Luz que se enciende al abrirse (por ejemplo la luz que asusta a la persona).")]
    public Light openLight;
    public float lightIntensity = 3f;
    public AudioSource openSound;

    public UnityEvent onOpened;

    public bool IsOpen { get; private set; }

    Quaternion closedRotation;
    Coroutine anim;

    void Awake()
    {
        closedRotation = transform.localRotation;
        if (openLight != null) openLight.intensity = 0f;
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        if (openSound != null) openSound.Play();
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        Quaternion openRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis);
        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            float k = openCurve.Evaluate(Mathf.Clamp01(t / openDuration));
            transform.localRotation = Quaternion.Slerp(closedRotation, openRotation, k);
            if (openLight != null) openLight.intensity = lightIntensity * k;
            yield return null;
        }
        anim = null;
        onOpened.Invoke();
    }

    public void ResetState()
    {
        if (anim != null) StopCoroutine(anim);
        anim = null;
        IsOpen = false;
        transform.localRotation = closedRotation;
        if (openLight != null) openLight.intensity = 0f;
    }
}
