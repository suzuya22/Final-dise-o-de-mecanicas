using UnityEngine;

// Aplausos que guían por el laberinto a oscuras. Suenan a intervalos irregulares desde este punto.
// El sonido es 3D: se oye más fuerte cerca y del lado en que está.
[RequireComponent(typeof(AudioSource))]
public class ClapBeacon : MonoBehaviour
{
    [Tooltip("Sonidos de aplausos; se elige uno al azar cada vez.")]
    public AudioClip[] clapClips;
    [Tooltip("Segundos entre aplausos (mínimo y máximo).")]
    public Vector2 interval = new Vector2(1.5f, 3.5f);
    public Vector2 volume = new Vector2(0.7f, 1f);
    public Vector2 pitch = new Vector2(0.9f, 1.1f);

    [Header("Distancia")]
    [Tooltip("Hasta esta distancia se oye a todo volumen.")]
    public float minDistance = 2f;
    [Tooltip("Más allá de esta distancia deja de oírse.")]
    public float maxDistance = 30f;

    AudioSource source;
    float nextClap;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.playOnAwake = false;
        source.loop = false;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        nextClap = Time.time + Random.Range(interval.x, interval.y);
    }

    void Update()
    {
        if (Time.time < nextClap || clapClips == null || clapClips.Length == 0) return;

        source.pitch = Random.Range(pitch.x, pitch.y);
        source.PlayOneShot(clapClips[Random.Range(0, clapClips.Length)], Random.Range(volume.x, volume.y));
        nextClap = Time.time + Random.Range(interval.x, interval.y);
    }
}
