using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Maneja los objetos arrojables: E recoge o suelta; mantener F carga la fuerza y soltarla lanza el objeto
[DefaultExecutionOrder(10)]
[RequireComponent(typeof(PlayerMovimiento), typeof(PlayerAnimacion))]
public class PlayerManos : MonoBehaviour
{
    [SerializeField, Min(1)] private int capacidad = 2;
    [SerializeField] private AnclajesMano anclajes; // posicion de las manos en cada sprite del jugador
    [SerializeField] private float retrasoLanzamiento = 0.18f; // espera para que el objeto salga cuando el brazo se estira en la animacion

    [Header("Carga del lanzamiento")]
    [SerializeField] private float tiempoMaxCarga = 1f;
    [SerializeField] private float fuerzaMinima = 10f;
    [SerializeField] private float fuerzaMaxima = 25f;
    [SerializeField] private Slider barraPotencia;
    [SerializeField] private Image rellenoBarraPotencia;

    private PlayerMovimiento mov;
    private PlayerAnimacion animacion;
    private SpriteRenderer sprite;

    private readonly List<ObjetoArrojable> agarrados = new List<ObjetoArrojable>();
    private readonly List<ObjetoArrojable> cercanos = new List<ObjetoArrojable>();

    private bool cargando;
    private float inicioCarga;
    private bool lanzamientoPendiente;
    private Vector2 dirPendiente;
    private float fuerzaPendiente;
    private float tiempoLanzamiento;

    void Awake()
    {
        mov = GetComponent<PlayerMovimiento>();
        animacion = GetComponent<PlayerAnimacion>();
        sprite = GetComponent<SpriteRenderer>();
        MostrarBarra(false);
    }

    // Los arrojables la llaman cuando el jugador entra o sale de su alcance
    public void ObjetoCerca(ObjetoArrojable objeto, bool cerca)
    {
        if (!cerca) cercanos.Remove(objeto);
        else if (!cercanos.Contains(objeto)) cercanos.Add(objeto);
    }

    void Update()
    {
        var teclado = Keyboard.current;
        if (teclado == null || !mov.ControlActivo) return;

        // E: si hay un objeto cerca y espacio en las manos, lo recoge; si no, suelta el ultimo que agarro
        if (teclado.eKey.wasPressedThisFrame)
        {
            var cerca = cercanos.Find(o => o != null && o.PuedeRecogerse);
            if (cerca != null && agarrados.Count < capacidad) Recoger(cerca);
            else if (agarrados.Count > 0 && !lanzamientoPendiente) SacarUltimo().Soltar();
        }

        // F: empieza a cargar. La barra de potencia solo se ve mientras se carga
        if (teclado.fKey.wasPressedThisFrame && agarrados.Count > 0 && !lanzamientoPendiente)
        {
            inicioCarga = Time.time;
            cargando = true;
            MostrarBarra(true);
        }
        if (cargando && agarrados.Count == 0) // soltaron el objeto con E mientras cargaba
        {
            cargando = false;
            MostrarBarra(false);
        }
        if (cargando && teclado.fKey.isPressed) PintarBarra(Mathf.Clamp01((Time.time - inicioCarga) / tiempoMaxCarga));

        if (lanzamientoPendiente && Time.time >= tiempoLanzamiento)
        {
            lanzamientoPendiente = false;
            if (agarrados.Count > 0) SacarUltimo().Lanzar(dirPendiente, fuerzaPendiente);
        }

        if (teclado.fKey.wasReleasedThisFrame && cargando && agarrados.Count > 0)
            PrepararLanzamiento(teclado);
    }

    // Coloca los objetos agarrados en las manos del sprite actual. El ultimo va en la mano delantera y es el proximo en lanzarse
    void LateUpdate()
    {
        int n = agarrados.Count;
        if (n == 0 || anclajes == null || sprite == null) return;
        if (!anclajes.Buscar(sprite.sprite, out var a)) return;
        float lado = sprite.flipX ? -1f : 1f;
        for (int i = 0; i < n; i++)
        {
            Vector2 desfase = i == n - 1 ? a.frente : a.atras;
            var o = agarrados[i].transform;
            o.position = new Vector3(transform.position.x + desfase.x * lado, transform.position.y + desfase.y, o.position.z);
        }
    }

    private void Recoger(ObjetoArrojable objeto)
    {
        cercanos.Remove(objeto);
        agarrados.Add(objeto);
        objeto.ConfigurarAgarre(transform);
        animacion.Recoger();
    }

    private ObjetoArrojable SacarUltimo()
    {
        var ultimo = agarrados[agarrados.Count - 1];
        agarrados.RemoveAt(agarrados.Count - 1);
        return ultimo;
    }

    private void PrepararLanzamiento(Keyboard teclado)
    {
        cargando = false;
        MostrarBarra(false);
        float carga = Mathf.Clamp01((Time.time - inicioCarga) / tiempoMaxCarga);
        float y = teclado.wKey.isPressed || teclado.upArrowKey.isPressed ? 1f : 0f; // W o flecha arriba apuntan hacia arriba; S no apunta porque es el Ataque en caida
        Vector2 dir = new Vector2(mov.EntradaX, y).normalized;
        bool mirandoIzquierda = sprite != null && sprite.flipX;
        if (dir == Vector2.zero) dir = mirandoIzquierda ? Vector2.left : Vector2.right;
        if (dir.x != 0f) mov.Voltear(dir.x < 0f); // el jugador se gira hacia donde lanza
        dirPendiente = dir;
        fuerzaPendiente = Mathf.Lerp(fuerzaMinima, fuerzaMaxima, carga);
        tiempoLanzamiento = Time.time + retrasoLanzamiento;
        lanzamientoPendiente = true;
        animacion.Reproducir(mov.EnSuelo ? "Throw" : "ThrowAir");
    }

    private void MostrarBarra(bool visible)
    {
        if (barraPotencia == null) return;
        barraPotencia.value = 0f;
        barraPotencia.gameObject.SetActive(visible);
    }

    private void PintarBarra(float porcentaje)
    {
        if (barraPotencia != null) barraPotencia.value = porcentaje;
        if (rellenoBarraPotencia == null) return;
        rellenoBarraPotencia.color =
            porcentaje <= 0.35f ? Color.green :
            porcentaje <= 0.65f ? new Color(1f, 0.5f, 0f) :
            Color.red;
    }
}
