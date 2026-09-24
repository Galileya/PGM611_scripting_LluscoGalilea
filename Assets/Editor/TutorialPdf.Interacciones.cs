using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static partial class TutorialPdf
{
    const string PrefabAbeja = "Assets/Prefab/Abejita.prefab";
    const string PrefabJabali = "Assets/Prefab/Puerquito.prefab";
    const string PrefabCaracol = "Assets/Prefab/Caracol.prefab";

    static void CrearInteracciones()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play antes de configurar las interacciones.");
        if (!AsegurarRecursosTMP()) return;
        var scene = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
        if (GameObject.Find("TextoAbejas") != null && GameObject.Find("Caracol") != null &&
            GameObject.Find("Puerquito") != null && GameObject.Find("Abejita 3") != null &&
            UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Count(go => go.name == "Puerquito") == 1 &&
            UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Count(go => go.name == "Caracol") == 1)
            throw new InvalidOperationException("Las interacciones ya estan preparadas.");
        if (scene.isDirty && !HayRestosDeInteracciones()) throw new InvalidOperationException("Guardar la escena activa antes de continuar.");
        LimpiarRestosDeInteracciones();

        CrearPrefabAbeja();
        CrearPrefabJabali();
        CrearPrefabCaracol();
        CrearMarcador();

        var player = GameObject.Find("Idle");
        var jugador = player.GetComponent<Jugador>();
        jugador.textoAbejas = GameObject.Find("TextoAbejas").GetComponent<TMP_Text>();
        PrefabUtility.RecordPrefabInstancePropertyModifications(jugador);

        CrearInstancia(PrefabAbeja, "Abejita 2", new Vector3(14.1f, 0.42f, 0));
        CrearInstancia(PrefabAbeja, "Abejita 3", new Vector3(31.2f, 0.42f, 0));

        var vacio = new GameObject("Limite de caida", typeof(BoxCollider2D));
        vacio.tag = "puerquito";
        vacio.transform.position = new Vector3(0, -7.2f, 0);
        var trigger = vacio.GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(100, 1);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Resultado, "OK interacciones: 3 abejas, marcador, jabali, limite de caida y caracol pisoteable.");
    }

    static void VincularMarcador()
    {
        var player = UnityEngine.Object.FindFirstObjectByType<Jugador>();
        var text = GameObject.Find("TextoAbejas").GetComponent<TMP_Text>();
        player.textoAbejas = text;
        PrefabUtility.RecordPrefabInstancePropertyModifications(player);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        File.WriteAllText(Resultado, "OK referencia del marcador guardada como override de escena del prefab.");
    }

    static bool AsegurarRecursosTMP()
    {
        if (BuscarFuenteTMP() != null) return true;
        string packageCache = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PackageCache"));
        string paquete = Directory.Exists(packageCache)
            ? Directory.GetFiles(packageCache, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).FirstOrDefault()
            : null;
        if (paquete == null) throw new InvalidOperationException("No se encontro el paquete TMP Essentials incluido con Unity.");
        AssetDatabase.ImportPackage(paquete, false);
        File.WriteAllText(Resultado, "TMP Essentials importado. Ejecutar de nuevo la orden interacciones.");
        return false;
    }

    static TMP_FontAsset BuscarFuenteTMP()
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) return font;
        return AssetDatabase.FindAssets("t:TMP_FontAsset").Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>).FirstOrDefault(asset => asset != null);
    }

    static bool HayRestosDeInteracciones()
    {
        return GameObject.Find("Canvas") != null || GameObject.Find("Abejita 1") != null ||
            GameObject.Find("Abejita 2") != null || GameObject.Find("Abejita 3") != null ||
            GameObject.Find("Puerquito") != null || GameObject.Find("Caracol") != null ||
            GameObject.Find("Limite de caida") != null;
    }

    static void LimpiarRestosDeInteracciones()
    {
        var roots = new[] { "Abejita 1", "Abejita 2", "Abejita 3", "Puerquito", "Caracol", "Limite de caida" };
        foreach (string name in roots)
        {
            foreach (var gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Where(go => go.name == name).ToArray())
                UnityEngine.Object.DestroyImmediate(gameObject);
        }
        var marker = GameObject.Find("Marcador");
        if (marker != null) UnityEngine.Object.DestroyImmediate(marker.transform.root.gameObject);
        foreach (string path in new[] { PrefabAbeja, PrefabJabali, PrefabCaracol,
                     "Assets/Animaciones/Jugador/Caracol.controller", "Assets/Animaciones/Jugador/Aplastado.anim",
                     "Assets/Animaciones/Jugador/Marcador-Arial SDF.asset" })
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
    }

    static GameObject CrearDesdeAseprite(string source, string objectName, string prefabPath, string tag, int order, Vector2 colliderSize)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Recursos + source);
        if (asset == null) throw new InvalidOperationException("Falta recurso: " + source);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        instance.name = objectName;
        instance.tag = tag;
        var renderer = instance.GetComponent<SpriteRenderer>();
        if (renderer == null) throw new InvalidOperationException(objectName + " no tiene SpriteRenderer.");
        renderer.sortingOrder = order;
        var collider = instance.GetComponent<Collider2D>();
        if (collider == null) collider = instance.AddComponent<CapsuleCollider2D>();
        collider.isTrigger = true;
        if (collider is CapsuleCollider2D capsule) capsule.size = colliderSize;
        else if (collider is BoxCollider2D box) box.size = colliderSize;
        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.AutomatedAction);
        if (prefab == null) throw new InvalidOperationException("No se pudo crear " + prefabPath);
        return instance;
    }

    static void CrearPrefabAbeja()
    {
        var instance = CrearDesdeAseprite("Mob/Small Bee/Fly/Fly.aseprite", "Abejita 1", PrefabAbeja, "abejita", 3, new Vector2(0.25f, 0.23f));
        instance.transform.position = new Vector3(-1.5f, 0.15f, 0);
    }

    static void CrearPrefabJabali()
    {
        var instance = CrearDesdeAseprite("Mob/Boar/Run/Run.aseprite", "Puerquito", PrefabJabali, "puerquito", 2, new Vector2(0.38f, 0.32f));
        instance.transform.position = new Vector3(5.7f, -0.32f, 0);
    }

    static void CrearPrefabCaracol()
    {
        var instance = CrearDesdeAseprite("Mob/Snail/walk-Sheet.aseprite", "Caracol", PrefabCaracol, "caracol", 2, new Vector2(0.42f, 0.24f));
        var animator = instance.GetComponent<Animator>();
        if (animator == null) animator = instance.AddComponent<Animator>();
        var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animaciones/Jugador/Caracol.controller");
        var importedClip = AssetDatabase.LoadAllAssetsAtPath(Recursos + "Mob/Snail/Dead-Sheet.aseprite").OfType<AnimationClip>().FirstOrDefault();
        AnimationClip deadClip = null;
        if (importedClip != null)
        {
            deadClip = UnityEngine.Object.Instantiate(importedClip);
            deadClip.name = "Aplastado";
            var settings = AnimationUtility.GetAnimationClipSettings(deadClip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(deadClip, settings);
            AssetDatabase.CreateAsset(deadClip, "Assets/Animaciones/Jugador/Aplastado.anim");
        }
        if (deadClip == null)
        {
            deadClip = new AnimationClip { name = "Aplastado" };
            AssetDatabase.CreateAsset(deadClip, "Assets/Animaciones/Jugador/Aplastado.anim");
        }
        var state = controller.layers[0].stateMachine.AddState("Aplastado");
        state.motion = deadClip;
        controller.layers[0].stateMachine.defaultState = state;
        animator.runtimeAnimatorController = controller;
        var caracol = instance.AddComponent<Caracol>();
        var serialized = new SerializedObject(caracol);
        serialized.FindProperty("animator").objectReferenceValue = animator;
        serialized.FindProperty("esperaDesaparicion").floatValue = Mathf.Max(0.35f, deadClip.length);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        instance.transform.position = new Vector3(14.8f, 0.08f, 0);
        PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
    }

    static void CrearInstancia(string path, string name, Vector3 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.position = position;
    }

    static void CrearMarcador()
    {
        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1;
        canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);

        var panel = new GameObject("Marcador", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(28, -24);
        rect.sizeDelta = new Vector2(150, 58);
        panel.GetComponent<Image>().color = new Color(0.05f, 0.12f, 0.10f, 0.82f);

        var beeSprite = AssetDatabase.LoadAllAssetsAtPath(Recursos + "Mob/Small Bee/Fly/Fly.aseprite").OfType<Sprite>().FirstOrDefault();
        var icon = new GameObject("IconoAbeja", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(panel.transform, false);
        var iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f); iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f); iconRect.anchoredPosition = new Vector2(10, 0); iconRect.sizeDelta = new Vector2(36, 36);
        icon.GetComponent<Image>().sprite = beeSprite;
        icon.GetComponent<Image>().preserveAspect = true;

        var textObject = new GameObject("TextoAbejas", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0); textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(58, 2); textRect.offsetMax = new Vector2(-8, -2);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "0"; text.fontSize = 32; text.color = Color.white; text.alignment = TextAlignmentOptions.MidlineLeft;
        text.font = BuscarFuenteTMP();
    }
}
