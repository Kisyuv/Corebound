using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Pantalla de "Has muerto". Empieza oculta; PlayerVida la muestra al morir (evento alMorir).
// Con cualquier tecla recarga el nivel actual.
public class PantallaGameOver : MonoBehaviour
{
    [SerializeField] private float esperaAntesDeReiniciar = 1f; // segundos antes de aceptar teclas, para ver la animacion de muerte
    [SerializeField] private AudioClip sonido;

    private float aceptarTeclasDesde;

    public void Mostrar()
    {
        gameObject.SetActive(true);
        Sonido.Reproducir(sonido);
        aceptarTeclasDesde = Time.time + esperaAntesDeReiniciar;
    }

    void Update()
    {
        var teclado = Keyboard.current;
        if (teclado == null || Time.time < aceptarTeclasDesde) return;
        if (teclado.anyKey.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
