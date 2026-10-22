using UnityEngine;
using UnityEngine.Events;

// Objeto que se rompe (cajas, blancos, barriles). Lo rompe un arrojable si el objeto tiene el tag Enemigo, o un Ataque en caida desde arriba.
// Necesita dos colliders: uno solido y otro trigger un poco mas arriba para detectar el Ataque en caida.
public class ObjetoRompible : MonoBehaviour
{
    [SerializeField] private int vida = 1;
    [SerializeField] private int puntos = 50;
    [SerializeField] private bool romperConAtaqueCaida = true;
    [SerializeField] private AudioClip sonido;
    [SerializeField] private UnityEvent alRomperse;

    private bool roto;

    // La llama el arrojable al chocar (con SendMessage)
    public void RecibirDano(int cantidad)
    {
        vida -= cantidad;
        if (vida <= 0) Romper();
    }

    // El trigger esta encima del objeto: el jugador lo toca antes de aterrizar, cuando el Ataque en caida sigue activo
    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!romperConAtaqueCaida || roto || !otro.CompareTag("Player")) return;
        var jugador = otro.GetComponent<PlayerMovimiento>();
        if (jugador != null && jugador.HaciendoAtaqueCaida && otro.transform.position.y > transform.position.y) Romper();
    }

    private void Romper()
    {
        if (roto) return;
        roto = true;
        var jugador = FindAnyObjectByType<PlayerProgreso>();
        if (jugador != null) jugador.SumarPuntos(puntos);
        Sonido.Reproducir(sonido);
        alRomperse.Invoke();
        Destroy(gameObject);
    }
}
