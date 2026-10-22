using UnityEngine;

// Reproduce efectos de sonido 2D (mismo volumen sin importar la distancia a la camara).
// El sonido sigue aunque se destruya el objeto que lo pidio (un nucleo recogido, una caja rota).
public static class Sonido
{
    private static AudioSource fuente;

    public static void Reproducir(AudioClip clip)
    {
        if (clip == null) return;
        if (fuente == null)
        {
            var objeto = new GameObject("Sonidos");
            Object.DontDestroyOnLoad(objeto);
            fuente = objeto.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
        }
        fuente.PlayOneShot(clip);
    }
}
