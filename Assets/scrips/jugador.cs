using UnityEngine;
using logicaDeJugador;

namespace logicaDeJugador
{
    public class jugador : MonoBehaviour
    {
        [Header("Movimiento")]
        [Min(0f)]
        public float velocidad = 3f;

        private Rigidbody2D rb;
        private float movimiento;
        private Vector3 escalaInicial;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            escalaInicial = transform.localScale;
        }

        void Update()
        {
            movimiento = Input.GetAxisRaw("Horizontal");

            rb.linearVelocity = new Vector2(
                movimiento * velocidad,
                rb.linearVelocity.y
            );

            if (movimiento != 0f)
            {
                transform.localScale = new Vector3(
                    Mathf.Sign(movimiento) * Mathf.Abs(escalaInicial.x),
                    escalaInicial.y,
                    escalaInicial.z
                );
            }
        }
    }
}
public class Enemigo
{

}

#region Mundo
namespace herramientas
{
    namespace calculos
    {
        public class Ejemplo
        {
            public void metodoEjemplo ()
            {
                jugador J;
            }
        }
    }
    namespace conectividad
    {
        
    }
} 
#endregion

namespace skills
{
    namespace daño
    {
        using herramientas.calculos;
        public class golpes
        {
            public void metodoDeGolpe ()
            {
                Ejemplo e;
            }
        }
    }
    namespace conectividad
    {
        
    }
} 
