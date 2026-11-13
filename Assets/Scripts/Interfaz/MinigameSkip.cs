using UnityEngine;
using UnityEngine.Events;

// Permite saltar UN minijuego. Va en un objeto de ese minijuego (uno por minijuego).
//  - Begin(): muestra el botón de saltar (conéctalo al evento de "empieza el minijuego").
//  - End():   lo oculta (conéctalo a "termina / se sale del minijuego").
//  - Al pulsar el botón se llama a onSkip: ahí pones lo que debe pasar al saltar (normalmente darlo por ganado).
public class MinigameSkip : MonoBehaviour
{
    [Tooltip("El botón de la interfaz (prefab BotonSaltar).")]
    public MinigameSkipUI ui;

    [Tooltip("Qué pasa al saltar este minijuego.")]
    public UnityEvent onSkip;

    public void Begin()
    {
        if (ui != null) ui.Show(this);
    }

    public void End()
    {
        if (ui != null) ui.Hide(this);
    }

    public void Skip()
    {
        onSkip.Invoke();
    }
}
