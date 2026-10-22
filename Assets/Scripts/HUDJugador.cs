using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Muestra en pantalla las vidas (un icono por cada vida maxima) y los nucleos recogidos sobre el total del nivel
public class HUDJugador : MonoBehaviour
{
    [SerializeField] private PlayerVida vida;
    [SerializeField] private PlayerProgreso progreso;

    [Header("Vidas")]
    [SerializeField] private Image plantillaVida; // icono que se copia hasta tener uno por cada vida maxima
    [SerializeField] private Color colorVidaLlena = new Color(0.9f, 0.15f, 0.2f);
    [SerializeField] private Color colorVidaVacia = new Color(0.2f, 0.2f, 0.2f, 0.6f);

    [Header("Nucleos")]
    [SerializeField] private Text textoNucleos;
    [SerializeField] private string formatoNucleos = "Nucleos: {0} / {1}";

    private readonly List<Image> iconosVida = new List<Image>();
    private int vidasMostradas = -1, maximoMostrado = -1, nucleosMostrados = -1, totalMostrado = -1;

    void Awake()
    {
        if (vida == null) vida = FindAnyObjectByType<PlayerVida>();
        if (progreso == null) progreso = FindAnyObjectByType<PlayerProgreso>();
        if (plantillaVida != null) iconosVida.Add(plantillaVida);
    }

    // Revisa los valores cada frame (asi tambien refleja cambios hechos en el Inspector) y solo redibuja si algo cambio
    void Update()
    {
        if (vida != null && (vida.Vidas != vidasMostradas || vida.VidasMaximas != maximoMostrado))
            PintarVidas(vida.Vidas, vida.VidasMaximas);

        if (progreso != null && textoNucleos != null &&
            (progreso.NucleosRecolectados != nucleosMostrados || progreso.NucleosTotales != totalMostrado))
        {
            nucleosMostrados = progreso.NucleosRecolectados;
            totalMostrado = progreso.NucleosTotales;
            textoNucleos.text = string.Format(formatoNucleos, nucleosMostrados, totalMostrado);
        }
    }

    private void PintarVidas(int actuales, int maximas)
    {
        vidasMostradas = actuales;
        maximoMostrado = maximas;
        if (plantillaVida == null) return;
        while (iconosVida.Count < maximas) iconosVida.Add(Instantiate(plantillaVida, plantillaVida.transform.parent));
        for (int i = 0; i < iconosVida.Count; i++)
        {
            iconosVida[i].gameObject.SetActive(i < maximas);
            iconosVida[i].color = i < actuales ? colorVidaLlena : colorVidaVacia;
        }
    }
}
