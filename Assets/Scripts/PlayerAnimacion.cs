using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Decide que animacion mostrar segun lo que hace el jugador.
// Corre despues de PlayerMovimiento y PlayerManos para usar lo que ellos calcularon en el mismo frame.
[DefaultExecutionOrder(20)]
[RequireComponent(typeof(Animator), typeof(PlayerMovimiento))]
public class PlayerAnimacion : MonoBehaviour
{
    [Header("Reposo")]
    [SerializeField] private Vector2 esperaReposo = new Vector2(3f, 6f); // segundos en Idle (minimo y maximo) antes de una animacion de reposo al azar
    [SerializeField] private float tiempoAfk = 15f; // segundos quieto para que se siente a esperar (WaitSit)
    [SerializeField] private float velocidadAterrizajeFuerte = 6f; // si cae mas rapido que esto, al aterrizar muestra Land

    private static readonly string[] actividadesReposo = { "Idle2", "Stretching", "Sit" };

    private Animator anim;
    private PlayerMovimiento mov;
    private string estadoAnim;

    // Animaciones que se ven completas una vez (Hurt, Throw, Pick...). Mientras duran, no se cambia a otra automaticamente
    private readonly Dictionary<string, float> duracionesAnim = new Dictionary<string, float>();
    private float animBloqueoHasta;
    private bool bloqueoSuelo, bloqueoAire; // unas son solo de suelo y otras solo de aire; se cortan si eso cambia

    private float vyMinAire;
    private bool hizoAtaqueCaida;
    private float subidaAguaHasta; // SwimUp dura una brazada; moverse con A/D la corta
    private bool reiniciarSubida;

    private float tiempoQuieto, siguienteEn, reposoHasta, levantarseEn;
    private string reposoActual = "", ultimoReposo = "";
    private bool sentado;

    void Awake()
    {
        anim = GetComponent<Animator>();
        mov = GetComponent<PlayerMovimiento>();
        if (anim.runtimeAnimatorController != null)
            foreach (var clip in anim.runtimeAnimatorController.animationClips) duracionesAnim[clip.name] = clip.length;
    }

    void Update()
    {
        if (mov.ControlActivo) Actualizar();
    }

    private float Duracion(string estado, float porDefecto) => duracionesAnim.TryGetValue(estado, out float d) ? d : porDefecto;

    // Si se pasa duracionObjetivo, la animacion se acelera o se frena para durar justo ese tiempo
    public void Reproducir(string estado, float duracionObjetivo = 0f)
    {
        ReiniciarReposo();
        estadoAnim = estado;
        anim.Play(estado, 0, 0f);
        bloqueoSuelo = estado == "Throw" || estado == "PickGround" || estado == "Land" || estado == "GroundPoundLand" || estado == "Rising";
        bloqueoAire = estado == "WallJump" || estado == "ThrowAir" || estado == "PickAir";
        float d = Duracion(estado, 0f);
        anim.speed = 1f;
        if (duracionObjetivo > 0f && d > 0f)
        {
            anim.speed = d / duracionObjetivo;
            d = duracionObjetivo;
        }
        animBloqueoHasta = Time.time + d;
    }

    public void Recoger() => Reproducir(mov.EnSuelo ? "PickGround" : "PickAir");

    public void Morir()
    {
        Reproducir("Death");
        animBloqueoHasta = float.PositiveInfinity;
    }

    public void Brazada()
    {
        subidaAguaHasta = Time.time + Duracion("SwimUp", 0.5f);
        reiniciarSubida = true;
    }

    // Muestra la animacion de impacto al aterrizar despues de una caida rapida o de un Ataque en caida
    public void ComprobarAterrizaje()
    {
        if (mov.HaciendoAtaqueCaida) hizoAtaqueCaida = true;
        if (!mov.EnSuelo)
        {
            vyMinAire = Mathf.Min(vyMinAire, mov.Velocidad.y);
            return;
        }
        if (hizoAtaqueCaida) Reproducir("GroundPoundLand");
        else if (vyMinAire < -velocidadAterrizajeFuerte) Reproducir("Land");
        hizoAtaqueCaida = false;
        vyMinAire = 0f;
    }

    private void Actualizar()
    {
        if (float.IsPositiveInfinity(animBloqueoHasta)) return; // muerto: la animacion Death no se interrumpe
        bool suelo = mov.CercaDelSuelo;
        if (Time.time < animBloqueoHasta)
        {
            // El dash, el Ataque en caida, la pared o pasar de suelo a aire (o al reves) cortan la animacion en curso
            bool corta = mov.HaciendoDash || mov.HaciendoAtaqueCaida || mov.EnWallSlide ||
                         (bloqueoAire && mov.EnSuelo) || (bloqueoSuelo && !suelo);
            if (!corta) return;
            animBloqueoHasta = 0f;
        }
        if (anim.speed != 1f) anim.speed = 1f;
        string estado =
            mov.HaciendoAtaqueCaida ? "GroundPound" :
            mov.HaciendoDash ? (mov.EnAgua ? "SwimDash" : "Dash") :
            mov.EnAgua ? EstadoAgua() :
            mov.EnWallSlide ? "WallSlide" : // se queda en su ultimo frame mientras sigue en la pared
            !suelo ? (mov.Velocidad.y > 0.1f ? "Jump" : "Fall") : // en caidas largas, Fall se queda en su ultimo frame
            Mathf.Abs(mov.EntradaX) > 0.01f ? (mov.Corriendo ? "Run" : "Walk") : "Idle";

        if (estado == "Idle") estado = EstadoReposo();
        else if (tiempoQuieto > 0f) // cualquier accion cancela la animacion de reposo
        {
            bool sentadoFirme = sentado && !(reposoActual == "Sit" && Time.time < reposoHasta); // si aun se esta sentando, se corta sin animacion de levantarse
            ReiniciarReposo();
            if (sentadoFirme && (estado == "Walk" || estado == "Run"))
            {
                Reproducir("Rising");
                return;
            }
        }

        if (estado != "SwimUp") reiniciarSubida = false;
        if (estado == estadoAnim && !reiniciarSubida) return; // una brazada nueva reinicia SwimUp aunque ya se este viendo
        reiniciarSubida = false;
        estadoAnim = estado;
        anim.Play(estado, 0, 0f);
    }

    // En el agua: moverse con A/D muestra Swim. SwimUp se ve durante la brazada o mientras sube manteniendo Espacio
    private string EstadoAgua()
    {
        if (Mathf.Abs(mov.EntradaX) > 0.01f) return "Swim";
        float vy = mov.Velocidad.y;
        var teclado = Keyboard.current;
        bool subiendo = Time.time < subidaAguaHasta || (teclado != null && teclado.spaceKey.isPressed && vy > 0.5f);
        return subiendo ? "SwimUp" : vy < -0.5f ? "SwimDown" : "SwimIdle";
    }

    private void IniciarReposo(string estado)
    {
        reposoActual = estado;
        reposoHasta = Time.time + Duracion(estado, 1f);
    }

    private void ReiniciarReposo()
    {
        tiempoQuieto = siguienteEn = reposoHasta = 0f;
        reposoActual = "";
        sentado = false;
    }

    private string Sentarse()
    {
        sentado = true;
        ultimoReposo = "Sit";
        IniciarReposo("Sit");
        levantarseEn = reposoHasta + Random.Range(4f, 7f);
        return "Sit";
    }

    // Reposo: tras un rato en Idle hace una animacion al azar (distinta a la anterior) y vuelve a Idle.
    // Si pasa tiempoAfk segundos quieto, se sienta a esperar (WaitSit).
    private string EstadoReposo()
    {
        tiempoQuieto += Time.deltaTime;
        if (siguienteEn <= 0f) siguienteEn = tiempoQuieto + Random.Range(esperaReposo.x, esperaReposo.y);
        if (Time.time < reposoHasta) return reposoActual;
        if (sentado)
        {
            if (tiempoQuieto >= tiempoAfk) return "WaitSit";
            if (Time.time < levantarseEn) return "Sit"; // sigue sentado en el ultimo frame de Sit
            sentado = false;
            IniciarReposo("Rising");
            return "Rising";
        }
        if (reposoActual != "")
        {
            reposoActual = "";
            siguienteEn = tiempoQuieto + Random.Range(esperaReposo.x, esperaReposo.y);
        }
        if (tiempoQuieto >= tiempoAfk) return Sentarse();
        // Espera al inicio del ciclo de Idle para empezar, asi el cambio de animacion no da un salto
        if (tiempoQuieto >= siguienteEn && (estadoAnim != "Idle" || anim.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f < 0.12f))
        {
            string e;
            do e = actividadesReposo[Random.Range(0, actividadesReposo.Length)]; while (e == ultimoReposo);
            ultimoReposo = e;
            if (e == "Sit") return Sentarse();
            IniciarReposo(e);
            return e;
        }
        return "Idle";
    }
}
