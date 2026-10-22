using UnityEngine;
using UnityEngine.Events;

// Vidas del jugador. Tras recibir dano queda invulnerable un momento; con 0 vidas muere
[RequireComponent(typeof(PlayerMovimiento), typeof(PlayerAnimacion))]
public class PlayerVida : MonoBehaviour
{
    [SerializeField, Min(1)] private int vidasMaximas = 3;
    [SerializeField, Min(0)] private int vidas = 3;
    [SerializeField] private float tiempoInvulnerabilidad = 1.5f;
    [SerializeField] private UnityEvent alMorir; // se llama al morir (por ejemplo, para mostrar el Game Over)

    private PlayerMovimiento mov;
    private PlayerAnimacion animacion;
    private float finInvulnerabilidad;

    public int Vidas => vidas;
    public int VidasMaximas => vidasMaximas;

    void Awake()
    {
        mov = GetComponent<PlayerMovimiento>();
        animacion = GetComponent<PlayerAnimacion>();
    }

    // En el Inspector, las vidas no pueden pasar del maximo
    void OnValidate() => vidas = Mathf.Min(vidas, vidasMaximas);

    public void RecibirDano(int cantidad)
    {
        if (!mov.ControlActivo || cantidad <= 0 || Time.time < finInvulnerabilidad) return;

        vidas = Mathf.Max(vidas - cantidad, 0);
        finInvulnerabilidad = Time.time + tiempoInvulnerabilidad;
        if (vidas > 0)
        {
            animacion.Reproducir("Hurt");
            return;
        }
        mov.ControlActivo = false;
        animacion.Morir();
        alMorir.Invoke();
    }

    public void Curar(int cantidad) => vidas = Mathf.Clamp(vidas + cantidad, 0, vidasMaximas);
}
