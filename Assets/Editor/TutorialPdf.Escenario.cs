using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEditor.U2D.Sprites;

public static partial class TutorialPdf
{
    const string Escena = "Assets/Scenes/NivelBosque.unity";

    [MenuItem("Tutorial PDF/1 - Crear escenario y jugador")]
    public static void CrearEscenario()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play antes de construir.");
        // Nunca reemplazar cambios de escena que el usuario no haya guardado.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Guardar la escena actual antes de construir el nivel.");
        if (File.Exists(Escena)) throw new InvalidOperationException("NivelBosque ya existe; editarlo sin reconstruirlo.");
        foreach (var folder in new[] { "Assets/Animaciones/Jugador", "Assets/Prefab", "Assets/Materiales", "Assets/Tiles", "Assets/Paletas" })
            Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        ConfigurarProyecto();
        PrepararTiles();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1;

        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData), typeof(Camara));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 0, -10);
        var cam = camera.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 1.5f;
        cam.backgroundColor = new Color32(148, 224, 226, 255);
        cam.clearFlags = CameraClearFlags.SolidColor;
        var background = Instanciar("Background/Background.aseprite", "Background");
        background.GetComponent<SpriteRenderer>().sortingOrder = -10;
        // El mismo fondo del PDF, ampliado para cubrir el recorrido de la camara.
        background.transform.localScale = new Vector3(3f, 1.6f, 1);
        background.transform.position = new Vector3(1f, -2.8f, 1);
        background.AddComponent<FondoCamara>().camara = cam;

        var grid = new GameObject("Grid", typeof(Grid));
        grid.GetComponent<Grid>().cellSize = new Vector3(0.16f, 0.16f, 1);
        var floor = new GameObject("Piso", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        floor.layer = LayerMask.NameToLayer("Pisito");
        floor.transform.SetParent(grid.transform, false);
        var map = floor.GetComponent<Tilemap>();
        floor.GetComponent<TilemapRenderer>().sortingOrder = 0;
        Plataforma(map, -8, -3, 17, 4);
        Plataforma(map, 12, 0, 5, 4);
        Plataforma(map, -16, 0, 5, 4);
        Plataforma(map, -2, 4, 5, 4);
        Plataforma(map, 21, 4, 5, 4);
        Plataforma(map, 30, 0, 12, 4);
        CrearPaleta();

        var player = Instanciar("Character/Idle/Idle.aseprite", "Idle");
        player.tag = "Player";
        player.transform.position = new Vector3(0, -0.55f, 0);
        player.GetComponent<SpriteRenderer>().sortingOrder = 2;
        var rb = player.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        var collider = player.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.16f, 0.43f);
        collider.offset = new Vector2(-0.035f, 0.385f);
        var material = new PhysicsMaterial2D("solido") { friction = 0, bounciness = 0 };
        AssetDatabase.CreateAsset(material, "Assets/Materiales/solido.physicsMaterial2D");
        collider.sharedMaterial = material;
        var feet = new GameObject("ComprobadorDePiso");
        feet.transform.SetParent(player.transform, false);
        feet.transform.localPosition = new Vector3(-0.035f, 0.165f, 0);
        var control = player.AddComponent<Jugador>();
        control.comprobadorPiso = feet.transform;
        control.layerPiso = LayerMask.GetMask("Pisito");
        player.GetComponent<Animator>().runtimeAnimatorController = CrearAnimador();
        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(player, "Assets/Prefab/Jugador.prefab", InteractionMode.AutomatedAction);
        camera.GetComponent<Camara>().target = player.transform;
        EditorSceneManager.SaveScene(scene, Escena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Escena, true) };
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = player;
        SceneView.lastActiveSceneView?.LookAt(Vector3.zero, Quaternion.identity, 2.5f, true, true);
        File.WriteAllText(Resultado, "OK escenario: " + Escena + "\nJugador, 6 plataformas, animaciones, camara y prefab.");
    }

    static void ConfigurarProyecto()
    {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        if (LayerMask.NameToLayer("Pisito") < 0) layers.GetArrayElementAtIndex(6).stringValue = "Pisito";
        var tagList = tags.FindProperty("tags");
        foreach (var name in new[] { "abejita", "puerquito", "caracol" })
        {
            bool exists = false;
            for (int i = 0; i < tagList.arraySize; i++) exists |= tagList.GetArrayElementAtIndex(i).stringValue == name;
            if (!exists) { tagList.InsertArrayElementAtIndex(tagList.arraySize); tagList.GetArrayElementAtIndex(tagList.arraySize - 1).stringValue = name; }
        }
        tags.ApplyModifiedPropertiesWithoutUndo();
        EditorSettings.serializationMode = SerializationMode.ForceText;
        PlayerSettings.companyName = "Alexcito";
        PlayerSettings.productName = "Juego Multimedia Principal";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        var settings = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
        var input = settings.FindProperty("activeInputHandler");
        if (input != null) { input.intValue = 2; settings.ApplyModifiedPropertiesWithoutUndo(); }
    }

    static GameObject Instanciar(string path, string name)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Recursos + path);
        if (asset == null) throw new InvalidOperationException("Falta recurso: " + path);
        var result = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        PrefabUtility.UnpackPrefabInstance(result, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        result.name = name;
        return result;
    }

    static void PrepararTiles()
    {
        string path = Recursos + "Assets/Tiles.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var rects = new System.Collections.Generic.List<SpriteRect>();
        for (int y = 0; y < tex.height / 16; y++)
            for (int x = 0; x < tex.width / 16; x++)
                rects.Add(new SpriteRect { name = $"Tile_{x}_{y}", rect = new Rect(x * 16, tex.height - (y + 1) * 16, 16, 16), alignment = SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f), spriteID = GUID.Generate() });
        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        AplicarContornos(provider, rects.ToArray());
        provider.Apply();
        importer.SaveAndReimport();
        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Sprite;
            AssetDatabase.CreateAsset(tile, "Assets/Tiles/" + sprite.name + ".asset");
        }
    }

    static void AplicarContornos(ISpriteEditorDataProvider provider, SpriteRect[] rects)
    {
        var physics = provider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
        foreach (var r in rects)
        {
            // La hierba ocupa los ultimos 6 pixeles de la primera fila.
            // Una superficie continua evita que la capsula tropiece en cada tile.
            float top = r.name.EndsWith("_0") ? -2 : 8;
            physics.SetOutlines(r.spriteID, new System.Collections.Generic.List<Vector2[]> {
                new[] { new Vector2(-8,-8), new Vector2(-8,top), new Vector2(8,top), new Vector2(8,-8) }
            });
        }
    }

    static void CorregirContornos()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Recursos + "Assets/Tiles.png");
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        AplicarContornos(provider, provider.GetSpriteRects());
        provider.Apply(); importer.SaveAndReimport();
        var floor = GameObject.Find("Piso");
        // El Tilemap debe compartir el origen del Grid; desplazarlo separaba el suelo del jugador.
        floor.transform.localPosition = Vector3.zero;
        var rigidbody = floor.GetComponent<Rigidbody2D>();
        if (rigidbody == null) rigidbody = floor.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Static;
        var composite = floor.GetComponent<CompositeCollider2D>();
        if (composite == null) composite = floor.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        floor.GetComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
        var player = GameObject.Find("Idle");
        if (player != null)
        {
            player.transform.position = new Vector3(0, -0.55f, 0);
            var playerCollider = player.GetComponent<CapsuleCollider2D>();
            if (playerCollider != null) playerCollider.offset = new Vector2(-0.035f, 0.385f);
            var feet = player.transform.Find("ComprobadorDePiso");
            if (feet != null) feet.localPosition = new Vector3(-0.035f, 0.165f, 0);
            PrefabUtility.ApplyPrefabInstance(player, InteractionMode.AutomatedAction);
        }
        EditorSceneManager.MarkSceneDirty(floor.scene);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        File.WriteAllText(Resultado, "OK contorno de suelo continuo");
    }

    static Tile ObtenerTile(int x, int y) => AssetDatabase.LoadAssetAtPath<Tile>($"Assets/Tiles/Tile_{x}_{y}.asset");

    static void Plataforma(Tilemap map, int left, int top, int width, int height)
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                int sx = x == 0 ? 0 : x == width - 1 ? 4 : 1 + x % 3;
                int sy = y == 0 ? 0 : y == height - 1 ? 4 : y;
                map.SetTile(new Vector3Int(left + x, top - y, 0), ObtenerTile(sx, sy));
            }
    }

    static void CrearPaleta()
    {
        var palette = new GameObject("PaletaBosque", typeof(Grid));
        palette.GetComponent<Grid>().cellSize = new Vector3(0.16f, 0.16f, 1);
        var child = new GameObject("Tiles", typeof(Tilemap), typeof(TilemapRenderer));
        child.transform.SetParent(palette.transform, false);
        var map = child.GetComponent<Tilemap>();
        for (int y = 0; y < 25; y++) for (int x = 0; x < 25; x++)
            map.SetTile(new Vector3Int(x, -y, 0), ObtenerTile(x, y));
        PrefabUtility.SaveAsPrefabAsset(palette, "Assets/Paletas/PaletaBosque.prefab");
        UnityEngine.Object.DestroyImmediate(palette);
    }

    static AnimationClip ExportarClip(string source, string name, bool loop)
    {
        var original = AssetDatabase.LoadAllAssetsAtPath(Recursos + source).OfType<AnimationClip>().First();
        var clip = UnityEngine.Object.Instantiate(original);
        clip.name = name;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, "Assets/Animaciones/Jugador/" + name + ".anim");
        return clip;
    }

    static AnimatorController CrearAnimador()
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animaciones/Jugador/PjController.controller");
        controller.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        controller.AddParameter("VelocidadVertical", AnimatorControllerParameterType.Float);
        controller.AddParameter("estaEnPiso", AnimatorControllerParameterType.Bool);
        var machine = controller.layers[0].stateMachine;
        var idle = machine.AddState("Idle", new Vector3(220, 0));
        idle.motion = ExportarClip("Character/Idle/Idle.aseprite", "Idle", true);
        var run = machine.AddState("Run", new Vector3(470, 0));
        run.motion = ExportarClip("Character/Run/Run.aseprite", "Run", true);
        var jump = machine.AddState("Jump", new Vector3(220, 160));
        jump.motion = ExportarClip("Character/Jump/Jump.aseprite", "Jump", true);
        var fall = machine.AddState("Jump-down", new Vector3(470, 160));
        fall.motion = ExportarClip("Character/Jump-End/Jump-End.aseprite", "Jump-down", false);
        machine.defaultState = idle;
        var t = Transicion(idle.AddTransition(run));
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidad");
        t.AddCondition(AnimatorConditionMode.If, 0, "estaEnPiso");
        t = Transicion(run.AddTransition(idle));
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");
        t.AddCondition(AnimatorConditionMode.If, 0, "estaEnPiso");
        t = Transicion(machine.AddAnyStateTransition(jump));
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocidadVertical");
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "estaEnPiso");
        // Incluye caer desde una plataforma sin pulsar salto.
        t = Transicion(machine.AddAnyStateTransition(fall));
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "VelocidadVertical");
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "estaEnPiso");
        t = Transicion(fall.AddTransition(run));
        t.AddCondition(AnimatorConditionMode.If, 0, "estaEnPiso");
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidad");
        t = Transicion(fall.AddTransition(idle));
        t.AddCondition(AnimatorConditionMode.If, 0, "estaEnPiso");
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");
        return controller;
    }

    static AnimatorStateTransition Transicion(AnimatorStateTransition t)
    {
        t.hasExitTime = false;
        t.duration = 0;
        t.canTransitionToSelf = false;
        return t;
    }
}
