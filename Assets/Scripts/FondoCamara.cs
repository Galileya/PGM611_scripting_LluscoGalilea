using UnityEngine;

// Conserva el fondo original del PDF cubriendo la pantalla al seguir al jugador.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(SpriteRenderer))]
public class FondoCamara : MonoBehaviour
{
    public Camera camara;
    SpriteRenderer fondo;
    void Awake() { fondo = GetComponent<SpriteRenderer>(); }
    void LateUpdate()
    {
        if (camara == null || fondo.sprite == null) return;
        var bounds = fondo.sprite.bounds;
        float height = camara.orthographicSize * 2;
        float scale = Mathf.Max(height / bounds.size.y, height * camara.aspect / bounds.size.x);
        transform.localScale = new Vector3(scale, scale, 1);
        var center = bounds.center * scale;
        transform.position = new Vector3(camara.transform.position.x - center.x, camara.transform.position.y - center.y, 1);
    }
}
