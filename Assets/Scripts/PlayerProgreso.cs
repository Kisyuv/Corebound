using UnityEngine;
using UnityEngine.Events;

// Cuenta los nucleos y los puntos del nivel. El total de nucleos es la cantidad que hay en la escena
[RequireComponent(typeof(PlayerMovimiento), typeof(PlayerVida), typeof(PlayerAnimacion))]
public class PlayerProgreso : MonoBehaviour
{
    [SerializeField, Min(0)] private int nucleosRecolectados = 0;
    [SerializeField, Min(0)] private int vidaPorNucleo = 1;
    [SerializeField] private int puntosPorNucleo = 100;
    [SerializeField] private int puntos = 0;
    [SerializeField] private UnityEvent alPoderTerminarNivel; // se llama cuando no quedan nucleos por recoger (por ejemplo, para abrir la Puerta Nexo)

    private PlayerMovimiento mov;
    private PlayerVida vida;
    private PlayerAnimacion animacion;
    private bool nivelCompletado;

    public int NucleosRecolectados => nucleosRecolectados;
    public int NucleosTotales => nucleosRecolectados + NucleoEnergia.Pendientes;
    public int Puntos => puntos;
    public bool NivelPuedeTerminarse => nucleosRecolectados > 0 && NucleoEnergia.Pendientes == 0;

    void Awake()
    {
        mov = GetComponent<PlayerMovimiento>();
        vida = GetComponent<PlayerVida>();
        animacion = GetComponent<PlayerAnimacion>();
    }

    // Se revisa cada frame por si se agregan o quitan nucleos de la escena durante la partida
    void Update()
    {
        if (NivelPuedeTerminarse == nivelCompletado) return;
        nivelCompletado = NivelPuedeTerminarse;
        if (nivelCompletado) alPoderTerminarNivel.Invoke();
    }

    // Devuelve false si el jugador esta muerto; asi el nucleo no se recoge y se queda en su lugar
    public bool RecolectarNucleo()
    {
        if (!mov.ControlActivo) return false;
        nucleosRecolectados++;
        vida.Curar(vidaPorNucleo);
        SumarPuntos(puntosPorNucleo);
        animacion.Recoger();
        return true;
    }

    public void SumarPuntos(int cantidad) => puntos += cantidad;
}
