using UnityEngine;

// Punto débil en la espalda de la persona. Solo se ve y se puede golpear cuando está caída.
// Necesita un Collider. Ponlo como hijo del modelo de la persona para que se mueva con ella.
public class BossWeakPoint : MonoBehaviour
{
    [Tooltip("Lo que se ve del punto (se oculta cuando no está expuesto).")]
    public Renderer visual;
    public Collider hitCollider;

    public bool IsBroken { get; private set; }
    public bool IsExposed { get; private set; }

    HuggerBoss boss;

    public void Init(HuggerBoss owner)
    {
        boss = owner;
        SetExposed(false);
    }

    public void SetExposed(bool exposed)
    {
        IsExposed = exposed && !IsBroken;
        if (visual != null) visual.enabled = IsExposed;
        if (hitCollider != null) hitCollider.enabled = IsExposed;
    }

    // Lo llama la pelota al chocar.
    public void Hit()
    {
        if (!IsExposed || IsBroken) return;
        IsBroken = true;
        SetExposed(false);
        if (boss != null) boss.OnWeakPointHit();
    }

    public void ResetState()
    {
        IsBroken = false;
        SetExposed(false);
    }
}
