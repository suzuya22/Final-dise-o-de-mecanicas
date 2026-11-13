using UnityEngine;

// Red de seguridad: si el jugador cae al vacío cerca de este punto, lo devuelve a un lugar seguro.
// Pon uno por zona (la chiva, cada habitación). Solo actúa si el jugador está cerca de su zona, así
// una caída en una habitación no manda al jugador a otra.
public class FallRescue : MonoBehaviour
{
    public PlayerMovement player;
    [Tooltip("A dónde vuelve el jugador (su eje Z azul es hacia donde mira). Es también el centro de la zona vigilada.")]
    public Transform rescuePoint;
    [Tooltip("Por debajo de esta altura (en el mundo) se considera que cayó al vacío.")]
    public float minHeight = -5f;
    [Tooltip("Solo se rescata al jugador si cayó a menos de esta distancia horizontal del punto de rescate.")]
    public float maxHorizontalDistance = 16f;

    void Update()
    {
        if (player == null || rescuePoint == null) return;

        Vector3 p = player.transform.position;
        if (p.y > minHeight) return;

        Vector3 flat = p - rescuePoint.position;
        flat.y = 0f;
        if (flat.magnitude > maxHorizontalDistance) return;

        player.Teleport(rescuePoint.position, rescuePoint.rotation);
    }
}
