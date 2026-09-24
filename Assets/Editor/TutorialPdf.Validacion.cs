using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using TMPro;

public static partial class TutorialPdf
{
    static void Comprobar(bool condition, string description, StringBuilder report)
    {
        if (!condition) throw new InvalidOperationException("Fallo: " + description);
        report.AppendLine("PASS " + description);
    }

    static void ValidarInteracciones()
    {
        if (Application.isPlaying) throw new InvalidOperationException("La comprobacion de escena requiere salir de Play.");
        var report = new StringBuilder();
        var player = UnityEngine.Object.FindFirstObjectByType<Jugador>();
        var playerCollider = player.GetComponent<CapsuleCollider2D>();
        var bees = UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None)
            .Where(c => c.CompareTag("abejita") && c.isTrigger).ToArray();
        Comprobar(bees.Length == 3, "tres abejas con trigger", report);
        Comprobar(player.textoAbejas != null && player.textoAbejas.GetComponent<TMP_Text>().text == "0", "marcador TextMeshPro enlazado e inicializado", report);
        var boar = GameObject.Find("Puerquito");
        Comprobar(boar != null && boar.CompareTag("puerquito") && boar.GetComponent<Collider2D>().isTrigger, "jabali peligroso con trigger", report);
        var voidTrigger = GameObject.Find("Limite de caida");
        Comprobar(voidTrigger != null && voidTrigger.CompareTag("puerquito") && voidTrigger.GetComponent<Collider2D>().isTrigger, "trigger de caida reinicia el nivel", report);
        var snail = GameObject.Find("Caracol");
        Comprobar(snail != null && snail.CompareTag("caracol") && snail.GetComponent<Collider2D>().isTrigger && snail.GetComponent<Caracol>() != null, "caracol con trigger y comportamiento de pisoton", report);
        var floor = GameObject.Find("Piso");
        Comprobar(floor.transform.localPosition == Vector3.zero, "Tilemap en el origen del Grid", report);
        var tileCollider = floor.GetComponent<TilemapCollider2D>();
        var composite = floor.GetComponent<CompositeCollider2D>();
        Comprobar(composite != null && floor.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Static && tileCollider.compositeOperation == Collider2D.CompositeOperation.Merge, "collider compuesto une los bordes de tiles", report);
        Physics2D.SyncTransforms();
        Comprobar(!playerCollider.Distance(tileCollider).isOverlapped, "posicion inicial sin solape con los tiles", report);
        Comprobar(Mathf.Abs(player.transform.position.x) < 0.01f && Mathf.Abs(player.transform.position.y + 0.55f) < 0.01f, "posicion inicial del jugador", report);
        var camera = Camera.main;
        Comprobar(camera != null && camera.GetComponent<Camara>().target == player.transform, "camara enlazada al jugador", report);
        Comprobar(GameObject.Find("Background").GetComponent<FondoCamara>().camara == camera, "fondo sigue y cubre la camara", report);
        Comprobar(SceneManager.GetActiveScene().path == Escena && EditorBuildSettings.scenes.Any(s => s.path == Escena && s.enabled), "NivelBosque es la escena de inicio", report);
        Directory.CreateDirectory("../artifacts");
        File.WriteAllText("../artifacts/validacion-interacciones.txt", report.ToString());
        File.WriteAllText(Resultado, report.ToString());
    }

    // Ejecuta fisica real de la escena en Play, controlando solo la entrada.
    // No modifica los assets ni guarda cambios de la prueba en la escena.
    static void ValidarBase()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("La prueba requiere Play.");
        var report = new StringBuilder();
        var player = UnityEngine.Object.FindFirstObjectByType<Jugador>();
        var rb = player.GetComponent<Rigidbody2D>();
        var oldPosition = rb.position;
        var mode = Physics2D.simulationMode;
        var movement = typeof(Jugador).GetField("movimiento", BindingFlags.Instance | BindingFlags.NonPublic);
        var jump = typeof(Jugador).GetField("saltoPendiente", BindingFlags.Instance | BindingFlags.NonPublic);
        var tick = typeof(Jugador).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            player.enabled = false;
            Physics2D.simulationMode = SimulationMode2D.Script;
            rb.position = new Vector2(0, -0.55f);
            rb.linearVelocity = Vector2.zero;
            movement.SetValue(player, 0f);
            jump.SetValue(player, false);
            Physics2D.SyncTransforms();
            void Step(int count) { for (int i = 0; i < count; i++) { tick.Invoke(player, null); Physics2D.Simulate(0.02f); } }
            Step(80);
            Comprobar(player.EstaEnPiso && rb.position.y > -1, "gravedad y colision con Tilemap", report);
            float ground = rb.position.y;
            float start = rb.position.x;
            movement.SetValue(player, 1f);
            Step(15);
            Comprobar(rb.position.x > start + 0.5f, "movimiento a la derecha (distancia " + (rb.position.x-start) + ")", report);
            movement.SetValue(player, -1f);
            Step(15);
            Comprobar(Mathf.Abs(rb.position.x - start) < 0.1f, "movimiento a la izquierda", report);
            movement.SetValue(player, 0f);
            Step(2);
            jump.SetValue(player, true);
            Step(1);
            Comprobar(rb.linearVelocity.y > 3 && !player.EstaEnPiso, "salto desde suelo", report);
            Step(5);
            float vertical = rb.linearVelocity.y;
            jump.SetValue(player, true);
            Step(1);
            Comprobar(rb.linearVelocity.y < vertical, "rechaza segundo salto en el aire", report);
            Step(80);
            Comprobar(player.EstaEnPiso && Mathf.Abs(rb.position.y - ground) < 0.08f, "aterrizaje estable", report);
            Comprobar(Mathf.Abs(rb.rotation) < 0.01f, "rotacion Z bloqueada", report);
            var cam = Camera.main;
            float z = cam.transform.position.z;
            cam.GetComponent<Camara>().SendMessage("LateUpdate");
            Comprobar(Vector2.Distance(cam.transform.position, player.transform.position) < 0.001f && cam.transform.position.z == z, "seguimiento de camara conserva profundidad", report);
            var animator = player.GetComponent<Animator>();
            animator.SetBool("estaEnPiso", true);
            animator.SetFloat("Velocidad", 0);
            animator.Play("Idle");
            animator.Update(0.1f);
            Comprobar(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "animacion Idle", report);
            animator.SetFloat("Velocidad", 1);
            animator.Update(0.1f); animator.Update(0.1f);
            Comprobar(animator.GetCurrentAnimatorStateInfo(0).IsName("Run"), "animacion Run", report);
            animator.SetBool("estaEnPiso", false);
            animator.SetFloat("VelocidadVertical", 3);
            animator.Update(0.1f); animator.Update(0.1f);
            Comprobar(animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"), "animacion de ascenso", report);
            animator.SetFloat("VelocidadVertical", -1);
            animator.Update(0.1f); animator.Update(0.1f);
            Comprobar(animator.GetCurrentAnimatorStateInfo(0).IsName("Jump-down"), "animacion de caida", report);
            Directory.CreateDirectory("../artifacts");
            File.WriteAllText("../artifacts/validacion-base.txt", report.ToString());
            File.WriteAllText(Resultado, report.ToString());
        }
        finally
        {
            movement.SetValue(player, 0f);
            jump.SetValue(player, false);
            rb.position = oldPosition;
            rb.linearVelocity = Vector2.zero;
            Physics2D.simulationMode = mode;
            player.enabled = true;
        }
    }
}
