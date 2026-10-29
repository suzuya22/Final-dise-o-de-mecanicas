using System.Collections.Generic;
using UnityEngine;

// Base para todo lo que el jugador puede usar con E (hilos, objetos...).
// PlayerInteractor elige el más cercano que esté a su alcance.
public abstract class Interactable : MonoBehaviour
{
    [Header("Interacción")]
    [Tooltip("Texto que aparece en pantalla, por ejemplo \"Jalar hilo\".")]
    public string prompt = "Interactuar";
    [Tooltip("Segundos que hay que mantener E. 0 = basta con pulsar.")]
    public float holdTime = 0f;
    [Tooltip("Punto desde el que se mide la distancia. Si está vacío se usa este objeto.")]
    public Transform interactPoint;

    public static readonly List<Interactable> All = new List<Interactable>();

    public Vector3 InteractPosition => interactPoint != null ? interactPoint.position : transform.position;
    public virtual bool CanInteract => isActiveAndEnabled;

    protected virtual void OnEnable() => All.Add(this);
    protected virtual void OnDisable() => All.Remove(this);

    public abstract void Interact(PlayerInteractor player);

    // Progreso de 0 a 1 mientras se mantiene E (para animar, por ejemplo, un hilo tensándose).
    public virtual void OnHoldProgress(float progress) { }
}
