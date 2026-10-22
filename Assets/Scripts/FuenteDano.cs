using UnityEngine;

// Quita vida al jugador cuando lo toca (pinchos, abismo, lava, enemigos). Funciona con collider trigger o solido.
// Si tiene un punto de reaparicion, lleva al jugador ahi despues del golpe.
[RequireComponent(typeof(Collider2D))]
public class FuenteDano : MonoBehaviour
{
    [SerializeField, Min(1)] private int dano = 1;
    [SerializeField] private string tagJugador = "Player";
    [SerializeField] private Transform puntoReaparicion; // vacio = solo quita vida, sin mover al jugador

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    // Tambien revisa mientras el jugador sigue encima: cuando termina su invulnerabilidad, vuelve a recibir dano
    void OnTriggerEnter2D(Collider2D otro) => Golpear(otro);
    void OnTriggerStay2D(Collider2D otro) => Golpear(otro);
    void OnCollisionEnter2D(Collision2D colision) => Golpear(colision.collider);
    void OnCollisionStay2D(Collision2D colision) => Golpear(colision.collider);

    private void Golpear(Collider2D otro)
    {
        if (!otro.CompareTag(tagJugador)) return;
        var vida = otro.GetComponent<PlayerVida>();
        if (vida == null) return;
        vida.RecibirDano(dano);
        if (puntoReaparicion == null) return;
        var rb = otro.attachedRigidbody;
        rb.position = puntoReaparicion.position;
        rb.linearVelocity = Vector2.zero;
    }
}
