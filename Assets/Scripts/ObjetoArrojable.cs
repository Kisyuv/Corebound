using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ObjetoArrojable : MonoBehaviour
{
    [Header("Lanzamiento")]
    [SerializeField] private float multiplicadorVelocidad = 1f;
    [SerializeField] private float gravedadAlLanzar = 1f;
    [SerializeField] private float giroAlLanzar = 0f; // grados por segundo; 0 = no gira

    [Header("Impacto")]
    [SerializeField] private int danio = 1;
    [SerializeField] private string tagEnemigo = "Enemigo";
    [SerializeField] private bool destruirAlGolpearEnemigo = true;
    [SerializeField] private int rebotesAntesDeAterrizar = 0; // 0 = se queda quieto al primer toque con el suelo

    private Rigidbody2D rb;
    private Collider2D col;
    private Collider2D[] collidersJugador;
    private PlayerManos jugador;
    private bool enSuelo = true;
    private bool lanzado = false;
    private bool soltado = false; // true si se dejo caer con E: cae sin hacer dano
    private int rebotes;

    public bool PuedeRecogerse => enSuelo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D otro) => Avisar(otro, true);
    void OnTriggerStay2D(Collider2D otro) { if (enSuelo) Avisar(otro, true); }
    void OnTriggerExit2D(Collider2D otro) => Avisar(otro, false);

    // Solo avisa al jugador que el objeto esta a su alcance; el jugador decide con E si lo recoge
    private void Avisar(Collider2D otro, bool cerca)
    {
        if (!otro.CompareTag("Player")) return;
        var manos = otro.GetComponent<PlayerManos>();
        if (manos != null) manos.ObjetoCerca(this, cerca && enSuelo);
    }

    // Mientras esta en la mano, PlayerManos lo coloca cada frame en la mano del sprite
    public void ConfigurarAgarre(Transform portador)
    {
        jugador = portador.GetComponent<PlayerManos>();
        collidersJugador = portador.GetComponents<Collider2D>();
        enSuelo = false;
        lanzado = false;
        soltado = false;
        transform.SetParent(portador);
        transform.localRotation = Quaternion.identity;
        var sprite = GetComponent<SpriteRenderer>();
        var spritePortador = portador.GetComponent<SpriteRenderer>();
        if (sprite != null && spritePortador != null) sprite.sortingOrder = spritePortador.sortingOrder + 1;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.isTrigger = true;
        rb.linearVelocity = Vector2.zero;
    }

    public void Lanzar(Vector2 direccion, float fuerza)
    {
        Desprender();
        soltado = false;
        rb.linearVelocity = direccion * fuerza * multiplicadorVelocidad;
        rb.angularVelocity = giroAlLanzar;
    }

    // Lo deja caer sin impulso y sin hacer dano
    public void Soltar()
    {
        Desprender();
        soltado = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void Desprender()
    {
        lanzado = true;
        rebotes = 0;
        if (jugador != null) jugador.ObjetoCerca(this, false);
        transform.SetParent(null);
        IgnorarJugador(true); // mientras vuela o cae, no choca con el jugador
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravedadAlLanzar;
        col.isTrigger = false;
    }

    void OnCollisionEnter2D(Collision2D colision)
    {
        if (!lanzado) return;
        var otro = colision.gameObject;
        if (!soltado && otro.CompareTag(tagEnemigo))
        {
            otro.SendMessage("RecibirDano", danio, SendMessageOptions.DontRequireReceiver);
            if (destruirAlGolpearEnemigo) Destroy(gameObject);
            return;
        }
        // Solo se detiene si toca algo por debajo (el suelo); si choca con una pared sigue cayendo
        bool contactoAbajo = colision.GetContact(0).point.y < transform.position.y - 0.05f;
        if (contactoAbajo && rebotes++ >= rebotesAntesDeAterrizar) Aterrizar();
    }

    private void Aterrizar()
    {
        lanzado = false;
        soltado = false;
        enSuelo = true;
        IgnorarJugador(false); // vuelve a detectar al jugador para que pueda recogerlo
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.isTrigger = true;
        transform.rotation = Quaternion.identity;
    }

    private void IgnorarJugador(bool ignorar)
    {
        if (collidersJugador == null) return;
        foreach (var c in collidersJugador) Physics2D.IgnoreCollision(col, c, ignorar);
    }
}
