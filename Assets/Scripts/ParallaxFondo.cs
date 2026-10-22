using UnityEngine;

// Mueve una capa del fondo mas lento que la camara para dar profundidad. Factor 0 = queda fija, 1 = se mueve igual que la camara
[DefaultExecutionOrder(500)] // corre despues de que Cinemachine mueve la camara
public class ParallaxFondo : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float factorX = 0.5f;
    [SerializeField, Range(0f, 1f)] private float factorY = 0.2f;

    private Transform cam;
    private Vector3 inicioCam, inicio;

    void Start()
    {
        cam = (Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>()).transform;
        inicioCam = cam.position;
        inicio = transform.position;
    }

    void LateUpdate()
    {
        Vector3 d = cam.position - inicioCam;
        transform.position = inicio + new Vector3(d.x * factorX, d.y * factorY, 0f);
    }
}
