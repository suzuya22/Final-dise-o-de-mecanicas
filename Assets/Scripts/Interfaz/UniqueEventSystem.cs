using UnityEngine;
using UnityEngine.EventSystems;

// Para prefabs de UI que traen su propio EventSystem (por ejemplo el menú de pausa) y deben funcionar
// en cualquier escena: si la escena ya tiene otro EventSystem propio, este se elimina solo para no
// tener dos. Ponlo en el mismo objeto que el EventSystem.
[RequireComponent(typeof(EventSystem))]
public class UniqueEventSystem : MonoBehaviour
{
    void Start()
    {
        var mine = GetComponent<EventSystem>();
        foreach (var other in FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (other == mine) continue;

            bool otherIsPrefabCopy = other.GetComponent<UniqueEventSystem>() != null;
            // Si el otro es de la escena, cedo. Si ambos son copias de prefab, se queda el de menor ID.
            if (!otherIsPrefabCopy || other.GetInstanceID() < mine.GetInstanceID())
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
