using System.Collections.Generic;
using UnityEngine;

// Guarda donde estan las manos del jugador en cada sprite (en unidades, desde el pivote del Player y con el sprite sin voltear).
// Los valores se calcularon buscando los guantes naranjas en cada frame.
public class AnclajesMano : ScriptableObject
{
    [System.Serializable]
    public struct Anclaje
    {
        public Sprite sprite;
        public Vector2 frente; // mano delantera: sostiene el proximo objeto que se lanza
        public Vector2 atras;
    }

    public Anclaje[] anclajes;
    private Dictionary<Sprite, Anclaje> mapa;

    public bool Buscar(Sprite s, out Anclaje a)
    {
        if (mapa == null)
        {
            mapa = new Dictionary<Sprite, Anclaje>();
            foreach (var x in anclajes) if (x.sprite != null) mapa[x.sprite] = x;
        }
        a = default;
        return s != null && mapa.TryGetValue(s, out a);
    }
}
