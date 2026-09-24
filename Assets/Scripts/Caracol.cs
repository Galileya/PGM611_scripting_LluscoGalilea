using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Caracol : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float esperaDesaparicion = 0.55f;
    bool derrotado;

    public void Pisar()
    {
        if (derrotado) return;
        derrotado = true;
        GetComponent<Collider2D>().enabled = false;
        if (animator != null) animator.Play("Aplastado", 0, 0);
        StartCoroutine(Desaparecer());
    }

    IEnumerator Desaparecer()
    {
        yield return new WaitForSeconds(esperaDesaparicion);
        Destroy(gameObject);
    }
}
