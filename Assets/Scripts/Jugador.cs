using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
public class Jugador : MonoBehaviour
{
    public float velocidad = 2f;
    public float alturaSalto = 4f;
    public Transform comprobadorPiso;
    public float radioComprobadorPiso = 0.035f;
    public LayerMask layerPiso;
    public TMP_Text textoAbejas;

    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer sprite;
    float movimiento;
    bool saltoPendiente;
    public bool EstaEnPiso { get; private set; }
    int cantAbejas;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        ActualizarMarcador();
    }

    void Update()
    {
        float eje = 0;
        bool salto = false;
#if ENABLE_LEGACY_INPUT_MANAGER
        eje = Input.GetAxisRaw("Horizontal");
        salto = Input.GetButtonDown("Jump");
#elif ENABLE_INPUT_SYSTEM
        // La plantilla Universal 2D activa el sistema nuevo inicialmente.
        // Este respaldo permite jugar antes de reiniciar Unity al activar Both.
        var teclado = Keyboard.current;
        if (teclado != null)
        {
            eje = (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed ? 1 : 0)
                - (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed ? 1 : 0);
            salto = teclado.spaceKey.wasPressedThisFrame;
        }
#endif
        movimiento = eje;
        if (salto && EstaEnPiso) saltoPendiente = true;
        if (movimiento != 0) sprite.flipX = movimiento < 0;
        animator.SetFloat("Velocidad", Mathf.Abs(movimiento));
        animator.SetFloat("VelocidadVertical", rb.linearVelocity.y);
        animator.SetBool("estaEnPiso", EstaEnPiso);
    }

    void FixedUpdate()
    {
        EstaEnPiso = comprobadorPiso != null && rb.linearVelocity.y <= 0.1f &&
            Physics2D.OverlapCircle(comprobadorPiso.position, radioComprobadorPiso, layerPiso);
        float vertical = rb.linearVelocity.y;
        if (saltoPendiente && EstaEnPiso)
        {
            vertical = alturaSalto;
            EstaEnPiso = false;
        }
        saltoPendiente = false;
        // La entrada se lee en Update; la velocidad fisica se aplica en FixedUpdate.
        rb.linearVelocity = new Vector2(movimiento * velocidad, vertical);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("abejita"))
        {
            Destroy(collision.gameObject);
            cantAbejas++;
            ActualizarMarcador();
            return;
        }

        if (collision.CompareTag("caracol"))
        {
            bool caeDesdeArriba = rb.linearVelocity.y <= 0.1f &&
                GetComponent<CapsuleCollider2D>().bounds.min.y >= collision.bounds.max.y - 0.05f;
            var caracol = collision.GetComponent<Caracol>();
            if (caeDesdeArriba && caracol != null)
            {
                caracol.Pisar();
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, alturaSalto * 0.7f);
                EstaEnPiso = false;
                return;
            }
            ReiniciarNivel();
            return;
        }

        if (collision.CompareTag("puerquito")) ReiniciarNivel();
    }

    void ActualizarMarcador()
    {
        if (textoAbejas != null) textoAbejas.text = cantAbejas.ToString();
    }

    static void ReiniciarNivel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
