using UnityEngine;
using UnityEngine.InputSystem;

// Lee el teclado y mueve al jugador: caminar, correr, saltar, dash, Ataque en caida, salto en pared y nado
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(PlayerAnimacion))]
public class PlayerMovimiento : MonoBehaviour
{
    [Header("Movimiento y salto")]
    [SerializeField] private float velocidadMovimiento = 6f;
    [SerializeField] private float velocidadCorrer = 9f; // velocidad si se sigue sosteniendo Shift cuando termina el Dash
    [SerializeField] private float alturaSalto = 3f;

    [Header("Impulso aereo (Dash)")]
    [SerializeField] private float velocidadDash = 12f;
    [SerializeField] private float duracionDash = 0.25f;
    [SerializeField] private float recargaDash = 1.5f;
    [SerializeField] private AudioClip sonidoDash;

    [Header("Ataque en caida")]
    [SerializeField] private float velocidadAtaqueCaida = 15f;

    [Header("Salto en la pared")]
    [SerializeField] private float alturaSaltoPared = 3.5f;
    [SerializeField] private float distanciaSaltoPared = 4f;
    [SerializeField] private float bloqueoInputPared = 0.2f;
    [SerializeField] private float velocidadDeslizamientoPared = 2f;

    [Header("Ambiente (para la Zona Subacuatica: 5)")]
    [SerializeField] private float velocidadMaxCaida = 50f;
    [SerializeField] private float gravedadAgua = 0.5f;
    [SerializeField] private float velocidadAgua = 3f;

    [Header("Deteccion")]
    [SerializeField] private LayerMask capaSuelo;
    [SerializeField] private LayerMask capaPared;
    [SerializeField] private Transform puntoDeteccion;
    [SerializeField] private float radioDeteccion = 0.2f;
    [SerializeField] private float margen = 0.05f;

    private Rigidbody2D rb;
    private CapsuleCollider2D col;
    private BoxCollider2D cuerpo; // collider de los pies; sirve para detectar el suelo en los bordes de plataformas
    private SpriteRenderer sprite;
    private PlayerAnimacion animacion;
    private float gravedadOriginal;

    private float entradaX;
    private float mirando = 1f;
    private bool enSuelo;
    private bool enAgua;
    private int paredDir; // -1 pared a la izquierda, 1 a la derecha, 0 ninguna
    private float ultimoSuelo;

    private bool corriendo;
    private bool haciendoDash;
    private float finDash;
    private float proximoDash;
    private float dirDash;

    private bool ataqueCaida;
    private float bloqueoHasta;

    public bool ControlActivo { get; set; } = true;
    public float EntradaX => entradaX;
    public bool EnSuelo => enSuelo;
    public bool EnAgua => enAgua;
    public bool Corriendo => corriendo;
    public bool HaciendoDash => haciendoDash;
    public bool HaciendoAtaqueCaida => ataqueCaida;
    public bool EnWallSlide => paredDir != 0 && !enSuelo && rb.linearVelocity.y < 0f;
    // Sigue contando como suelo 0.08 s despues de dejarlo, para que la animacion no salte a Fall por un frame
    public bool CercaDelSuelo => enSuelo || Time.time - ultimoSuelo < 0.08f;
    public Vector2 Velocidad => rb.linearVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        cuerpo = GetComponent<BoxCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
        animacion = GetComponent<PlayerAnimacion>();
        gravedadOriginal = rb.gravityScale;
    }

    void Update()
    {
        var teclado = Keyboard.current;
        if (teclado == null || !ControlActivo) return;

        entradaX = 0f;
        if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) entradaX -= 1f;
        if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) entradaX += 1f;
        if (entradaX != 0f)
        {
            mirando = Mathf.Sign(entradaX);
            Voltear(entradaX < 0f);
        }

        DetectarSuelo();
        if (enSuelo)
        {
            ultimoSuelo = Time.time;
            ataqueCaida = false;
        }
        paredDir = enSuelo ? 0 : DetectarPared();

        // En la pared, el sprite mira segun la pared y no segun la tecla.
        // El sprite de WallSlide tiene la mano hacia la derecha, asi que con la pared a la izquierda se voltea.
        if (EnWallSlide) Voltear(paredDir < 0);

        animacion.ComprobarAterrizaje();

        if (teclado.spaceKey.wasPressedThisFrame)
        {
            if (enAgua) // en el agua se puede impulsar hacia arriba sin limite, como brazadas
            {
                Saltar();
                animacion.Brazada();
            }
            else if (enSuelo) Saltar();
            else if (paredDir != 0) SaltarPared();
        }

        bool shift = teclado.leftShiftKey.wasPressedThisFrame || teclado.rightShiftKey.wasPressedThisFrame;
        if (shift && !haciendoDash && Time.time >= proximoDash) IniciarDash();
        if (corriendo && !ShiftPresionado(teclado)) corriendo = false;

        if (teclado.sKey.wasPressedThisFrame && !enSuelo && !haciendoDash && !enAgua)
            ataqueCaida = true;
    }

    void FixedUpdate()
    {
        if (!ControlActivo)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (haciendoDash)
        {
            if (Time.time >= finDash) TerminarDash();
            else
            {
                rb.linearVelocity = new Vector2(dirDash * velocidadDash, 0f);
                return;
            }
        }

        if (ataqueCaida)
        {
            rb.linearVelocity = new Vector2(0f, -Mathf.Min(velocidadAtaqueCaida, velocidadMaxCaida));
            return;
        }

        float vx = rb.linearVelocity.x;
        if (Time.time >= bloqueoHasta)
            vx = entradaX * (enAgua ? velocidadAgua : corriendo ? velocidadCorrer : velocidadMovimiento);

        float vy = rb.linearVelocity.y;
        vy = EnWallSlide ? Mathf.Max(vy, -velocidadDeslizamientoPared) : Mathf.Max(vy, -velocidadMaxCaida);

        rb.linearVelocity = new Vector2(vx, vy);
    }

    public void Voltear(bool izquierda)
    {
        if (sprite != null) sprite.flipX = izquierda;
    }

    private static bool ShiftPresionado(Keyboard teclado) => teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;

    // Formula v = raiz(2 * g * h): da la velocidad justa para subir exactamente 'altura' unidades.
    // Durante el dash la gravedad es 0, asi que se usa la gravedad original para no dividir por cero.
    private float GravedadSalto() => Mathf.Abs(Physics2D.gravity.y * (rb.gravityScale > 0f ? rb.gravityScale : gravedadOriginal));

    private float VelocidadParaAltura(float altura) => Mathf.Sqrt(2f * GravedadSalto() * altura);

    private void Saltar()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, VelocidadParaAltura(alturaSalto));
    }

    private void SaltarPared()
    {
        float vy = VelocidadParaAltura(alturaSaltoPared);
        float tiempoSubida = vy / GravedadSalto();
        float vx = distanciaSaltoPared / tiempoSubida;

        rb.linearVelocity = new Vector2(-paredDir * vx, vy);
        mirando = -paredDir;
        Voltear(mirando < 0f);
        bloqueoHasta = Time.time + bloqueoInputPared;
        animacion.Reproducir("WallJump", tiempoSubida); // la animacion dura lo mismo que la subida del salto
    }

    private void IniciarDash()
    {
        haciendoDash = true;
        ataqueCaida = false;
        dirDash = mirando;
        finDash = Time.time + duracionDash;
        proximoDash = Time.time + recargaDash;
        rb.gravityScale = 0f;
        Sonido.Reproducir(sonidoDash);
    }

    private void TerminarDash()
    {
        haciendoDash = false;
        rb.gravityScale = enAgua ? gravedadAgua : gravedadOriginal;
        var teclado = Keyboard.current;
        corriendo = teclado != null && ShiftPresionado(teclado);
    }

    private void DetectarSuelo()
    {
        enSuelo = Physics2D.OverlapCircle(puntoDeteccion.position, radioDeteccion, capaSuelo);
        // Si el circulo del centro no toca suelo, revisa bajo todo el ancho del cuerpo (para cuando esta parado en un borde)
        if (!enSuelo && cuerpo != null)
        {
            Bounds b = cuerpo.bounds;
            enSuelo = Physics2D.OverlapBox(new Vector2(b.center.x, b.min.y - 0.03f), new Vector2(b.size.x - 0.04f, 0.08f), 0f, capaSuelo);
        }
    }

    private int DetectarPared()
    {
        Bounds b = col.bounds;
        Vector2 tam = new Vector2(0.05f, b.size.y * 0.8f);
        if (Physics2D.BoxCast(b.center, tam, 0f, Vector2.left, b.extents.x + margen, capaPared)) return -1;
        if (Physics2D.BoxCast(b.center, tam, 0f, Vector2.right, b.extents.x + margen, capaPared)) return 1;
        return 0;
    }

    private void OnCollisionStay2D(Collision2D colision) => ataqueCaida = false;

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (!otro.CompareTag("Agua")) return;
        enAgua = true;
        rb.gravityScale = gravedadAgua;
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        if (!otro.CompareTag("Agua")) return;
        enAgua = false;
        rb.gravityScale = gravedadOriginal;
    }

    // Dibuja en la vista Scene la deteccion de suelo (rojo) y de paredes (azul)
    private void OnDrawGizmos()
    {
        if (puntoDeteccion != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(puntoDeteccion.position, radioDeteccion);
        }

        if (col == null) col = GetComponent<CapsuleCollider2D>();
        if (col == null) return;
        Bounds b = col.bounds;
        Vector2 tam = new Vector2(0.05f, b.size.y * 0.8f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(b.center + Vector3.left * (b.extents.x + margen), tam);
        Gizmos.DrawWireCube(b.center + Vector3.right * (b.extents.x + margen), tam);
    }
}
