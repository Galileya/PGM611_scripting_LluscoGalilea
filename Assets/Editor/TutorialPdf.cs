using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Herramientas de editor para construir y verificar los pasos del PDF.
// Solo ejecuta una orden cuando se crea expresamente Library/PdfCommand.txt.
[InitializeOnLoad]
public static partial class TutorialPdf
{
    const string Recursos = "Assets/Sprites/Legacy-Fantasy - High Forest 2.3/";
    const string Orden = "Library/PdfCommand.txt";
    const string Resultado = "Library/PdfResult.txt";
    static TutorialPdf() { EditorApplication.update += ProcesarOrden; }

    static void ProcesarOrden()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Orden)) return;
        string orden = File.ReadAllText(Orden).Trim();
        File.Delete(Orden);
        try
        {
            if (orden == "inspeccionar") Inspeccionar();
            else if (orden == "estado") File.WriteAllText(Resultado, "scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + " dirty=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty + " playing=" + EditorApplication.isPlaying + " objects=" + string.Join(",", UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Select(go => go.name)));
            else if (orden == "guardarConRespaldo") { var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene(); Directory.CreateDirectory("../artifacts"); EditorSceneManager.SaveScene(scene, "../artifacts/NivelBosque-respaldo.unity", true); EditorSceneManager.SaveOpenScenes(); File.WriteAllText(Resultado, "OK escena guardada; respaldo previo: ../artifacts/NivelBosque-respaldo.unity"); }
            else if (orden == "refrescar") AssetDatabase.Refresh();
            else if (orden == "guardar") { EditorSceneManager.SaveOpenScenes(); File.WriteAllText(Resultado, "OK guardado"); }
            else if (orden == "play") EditorApplication.isPlaying = true;
            else if (orden == "stop") EditorApplication.isPlaying = false;
            else if (orden == "validarBase") ValidarBase();
            else if (orden == "validarInteracciones") ValidarInteracciones();
            else if (orden == "captura") Capturar();
            else if (orden == "ajustarBase") AjustarBase();
            else if (orden == "contornos") CorregirContornos();
            else if (orden == "escenario") CrearEscenario();
            else if (orden == "interacciones") CrearInteracciones();
            else if (orden == "vincularUI") VincularMarcador();
            else throw new InvalidOperationException("Orden desconocida: " + orden);
        }
        catch (Exception ex)
        {
            File.WriteAllText(Resultado, "ERROR: " + ex);
            Debug.LogException(ex);
        }
    }

    static void AjustarBase()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play.");
        var player = UnityEngine.Object.FindFirstObjectByType<Jugador>();
        player.transform.position = new Vector3(0, -0.55f, 0);
        player.GetComponent<CapsuleCollider2D>().offset = new Vector2(-0.035f, 0.385f);
        player.comprobadorPiso.localPosition = new Vector3(-0.035f, 0.165f, 0);
        PrefabUtility.ApplyPrefabInstance(player.gameObject, InteractionMode.AutomatedAction);
        var bg = GameObject.Find("Background");
        bg.transform.localScale = new Vector3(3, 1.6f, 1);
        bg.transform.position = new Vector3(1, -2.8f, 1);
        var fondo = bg.GetComponent<FondoCamara>();
        if (fondo == null) fondo = bg.AddComponent<FondoCamara>();
        fondo.camara = Camera.main;
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        File.WriteAllText(Resultado, "OK ajuste de pivote del personaje y fondo");
    }

    static void Capturar()
    {
        Directory.CreateDirectory("../artifacts");
        var cam = Camera.main;
        var rt = new RenderTexture(1280, 720, 24);
        var previous = cam.targetTexture;
        var active = RenderTexture.active;
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes("../artifacts/nivel-bosque.png", image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            File.WriteAllText(Resultado, "OK captura");
        }
        finally
        {
            cam.targetTexture = previous;
            RenderTexture.active = active;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }

    static void Inspeccionar()
    {
        var sb = new StringBuilder();
        foreach (string relative in new[] { "Character/Idle/Idle.aseprite", "Character/Run/Run.aseprite", "Character/Jump/Jump.aseprite", "Character/Jump-End/Jump-End.aseprite", "Background/Background.aseprite", "Mob/Small Bee/Fly/Fly.aseprite", "Mob/Boar/Run/Run.aseprite", "Mob/Snail/walk-Sheet.aseprite", "Mob/Snail/Dead-Sheet.aseprite" })
        {
            sb.AppendLine(relative);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Recursos + relative))
            {
                sb.AppendLine("  " + asset.GetType().Name + ": " + asset.name);
                if (asset is Sprite s) sb.AppendLine("    bounds " + s.bounds + " rect " + s.rect + " pivot " + s.pivot);
                if (asset is AnimationClip clip)
                    foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                        sb.AppendLine("    curva: " + b.path + " / " + b.propertyName);
                if (asset is GameObject go)
                    foreach (var t in go.GetComponentsInChildren<Transform>(true))
                        sb.AppendLine("    objeto: " + t.name + " componentes: " + string.Join(",", t.GetComponents<Component>().Select(c => c.GetType().Name)));
            }
        }
        File.WriteAllText(Resultado, sb.ToString());
    }
}
