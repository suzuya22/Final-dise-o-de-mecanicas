using UnityEngine;

// Cuadro con ojo. Va en la raíz de cada prefab de cuadro.
// El ojo se abre y se cierra escalándolo, parpadea durante el aviso y sigue con la mirada a su objetivo.
public class EyePainting : MonoBehaviour
{
    public enum EyeState { Closed, Warning, Open }

    [Header("Ojo")]
    [Tooltip("El objeto del ojo. Se escala para abrir/cerrar.")]
    public Transform eye;
    [Tooltip("Lo que gira para mirar (la pupila). Si está vacío gira el ojo entero.")]
    public Transform pupil;
    [Tooltip("Eje local del ojo que se aplasta al cerrarse (normalmente el vertical).")]
    public Vector3 blinkAxis = Vector3.up;
    [Tooltip("Tamaño del ojo cerrado respecto al abierto, en el eje de parpadeo.")]
    [Range(0f, 1f)] public float closedAmount = 0.05f;
    [Tooltip("Qué tan rápido se abre y se cierra.")]
    public float openSpeed = 10f;

    [Header("Mirada")]
    [Tooltip("A quién mira. Si está vacío, lo asigna el EyeWatchSystem (el jugador).")]
    public Transform lookTarget;
    [Tooltip("Eje local de la pupila que apunta hacia fuera del cuadro (hacia donde mira).")]
    public Vector3 frontAxis = Vector3.forward;
    [Tooltip("Ángulo máximo que puede girar la pupila desde su posición inicial.")]
    public float maxLookAngle = 35f;
    public float lookSpeed = 6f;
    [Tooltip("Ojo siempre abierto (para los cuadros decorativos del pasillo).")]
    public bool alwaysOpen = false;

    [Header("Aviso")]
    [Tooltip("Parpadeos por segundo durante el aviso.")]
    public float warningBlinkRate = 6f;
    [Tooltip("Luz opcional que se enciende durante el aviso y con el ojo abierto.")]
    public Light glowLight;
    public float glowIntensity = 2f;

    public EyeState State { get; private set; } = EyeState.Closed;

    Vector3 openScale;
    Vector3 closedScale;
    Quaternion pupilBaseRotation;
    float openAmount;
    float warningTime;

    Transform LookPivot => pupil != null ? pupil : eye;

    void Awake()
    {
        if (eye == null) return;
        openScale = eye.localScale;
        Vector3 axis = new Vector3(Mathf.Abs(blinkAxis.x), Mathf.Abs(blinkAxis.y), Mathf.Abs(blinkAxis.z)).normalized;
        closedScale = Vector3.Scale(openScale, Vector3.one - axis * (1f - closedAmount));
        pupilBaseRotation = LookPivot.localRotation;

        openAmount = alwaysOpen ? 1f : 0f;
        eye.localScale = Vector3.Lerp(closedScale, openScale, openAmount);
        if (glowLight != null) glowLight.intensity = 0f;
    }

    public void SetState(EyeState state)
    {
        State = state;
        warningTime = 0f;
    }

    void Update()
    {
        if (eye == null) return;

        float target;
        if (alwaysOpen || State == EyeState.Open)
        {
            target = 1f;
        }
        else if (State == EyeState.Warning)
        {
            // Parpadeo rápido: alterna entre abierto y cerrado.
            warningTime += Time.deltaTime;
            target = Mathf.Repeat(warningTime * warningBlinkRate, 1f) < 0.5f ? 1f : 0f;
        }
        else
        {
            target = 0f;
        }

        openAmount = Mathf.MoveTowards(openAmount, target, openSpeed * Time.deltaTime);
        eye.localScale = Vector3.Lerp(closedScale, openScale, openAmount);

        if (glowLight != null)
        {
            float glow = State == EyeState.Closed && !alwaysOpen ? 0f : glowIntensity;
            glowLight.intensity = Mathf.Lerp(glowLight.intensity, glow, 1f - Mathf.Exp(-8f * Time.deltaTime));
        }

        UpdateLook();
    }

    void UpdateLook()
    {
        Transform pivot = LookPivot;
        Quaternion baseWorld = pivot.parent != null ? pivot.parent.rotation * pupilBaseRotation : pupilBaseRotation;
        Quaternion desired = baseWorld;

        if (lookTarget != null && openAmount > 0.1f)
        {
            Vector3 dir = lookTarget.position - pivot.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                // Gira el eje frontal hacia el objetivo, limitado para que no se dé la vuelta.
                Vector3 front = baseWorld * frontAxis.normalized;
                Quaternion turn = Quaternion.FromToRotation(front, dir);
                desired = Quaternion.RotateTowards(Quaternion.identity, turn, maxLookAngle) * baseWorld;
            }
        }

        pivot.rotation = Quaternion.Slerp(pivot.rotation, desired, 1f - Mathf.Exp(-lookSpeed * Time.deltaTime));
    }
}
