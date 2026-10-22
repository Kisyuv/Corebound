using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Al tocarlo, el jugador lo recoge: recupera vida, suma puntos y sube el contador de nucleos
[RequireComponent(typeof(Collider2D))]
public class NucleoEnergia : MonoBehaviour
{
    [SerializeField] private string tagJugador = "Player";
    [SerializeField] private float duracionAbsorcion = 0.25f; // tiempo que tarda en volar hacia el jugador y encogerse
    [SerializeField] private AudioClip sonido;
    [SerializeField] private UnityEvent alRecolectar;

    // Nucleos activos que quedan sin recoger en la escena. El HUD lo usa para calcular el total.
    // Un nucleo desactivado al empezar (por ejemplo, escondido en una caja) no cuenta hasta que se activa.
    public static int Pendientes { get; private set; }

    private bool recogido;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Awake() => GetComponent<Collider2D>().isTrigger = true;

    void OnEnable()
    {
        if (!recogido) Pendientes++;
    }

    void OnDisable()
    {
        if (!recogido) Pendientes--;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (recogido || !otro.CompareTag(tagJugador)) return;
        var jugador = otro.GetComponent<PlayerProgreso>();
        if (jugador == null || !jugador.RecolectarNucleo()) return;
        recogido = true;
        Pendientes--;
        GetComponent<Collider2D>().enabled = false;
        Sonido.Reproducir(sonido);
        alRecolectar.Invoke();
        StartCoroutine(Absorber(otro.transform));
    }

    private IEnumerator Absorber(Transform destino)
    {
        Vector3 inicio = transform.position, escala = transform.localScale;
        for (float t = 0f; t < duracionAbsorcion; t += Time.deltaTime)
        {
            float k = t / duracionAbsorcion;
            transform.position = Vector3.Lerp(inicio, destino.position, k);
            transform.localScale = escala * (1f - k);
            yield return null;
        }
        Destroy(gameObject);
    }
}
