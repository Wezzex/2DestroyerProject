using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-click setup for the enemy spawning systems (see Docs/EnemySpawningDesign.md).
// Safe to run more than once: it adds what's missing and re-applies the presets below.
// Wave assets and an existing scene wave list are only filled in the first time, so later tweaks survive.
public static class EnemySpawningSetup
{
    private const string StationPath = "Assets/Prefabs/EnemyStation/EnemyAIStation.prefab";
    private const string DestroyerPath = "Assets/Prefabs/EnemyDestroyer/EnemyAIDestroyer.prefab";
    private const string FighterPath = "Assets/Prefabs/EnemyFighter/EnemyAIFighter.prefab";
    private const string FighterTreePath = "Assets/Data/BehviourTreeData/BehaviourTreeDataFighter.asset";
    private const string DestroyerTreePath = "Assets/Data/BehviourTreeData/BehaviourTreeDataDestroyer.asset";
    private const string PlayerPath = "Assets/Prefabs/PlayerShipMotor.prefab";
    private const string FormationBranchName = "Formation";
    private const string BreakBranchName = "Break";
    private const string WaveFolder = "Assets/Data/Waves";
    private const string ScenePath = "Assets/Scenes/GameScene.unity";

    private const string ReturnBranchName = "Return";
    private const float HangarMargin = 10f;

    [MenuItem("Tools/Enemy Spawning/Run Full Setup")]
    public static void RunFullSetup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        GameObject fighterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FighterPath);
        GameObject destroyerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DestroyerPath);
        if (fighterPrefab == null || destroyerPrefab == null || AssetDatabase.LoadAssetAtPath<GameObject>(StationPath) == null)
        {
            Debug.LogError("[EnemySpawningSetup] Could not find the EnemyAI prefabs. Did they move?");
            return;
        }

        float destroyerSpacing = GetDestroyerSpacing();

        SetupFighterPrefab();
        SetupDestroyerPrefab();
        SetupStationPrefab(fighterPrefab, destroyerPrefab, destroyerSpacing);
        SetupFighterTree();
        SetupDestroyerTree();
        SetupPlayerPrefab();
        List<WaveDefinition> waves = CreateWaveDefinitions(destroyerPrefab, destroyerSpacing);
        SetupScene(waves);

        AssetDatabase.SaveAssets();
        Debug.Log("<color=#00ff00>[EnemySpawningSetup] Done.</color> Prefabs, fighter tree, wave assets and GameScene are set up.");
    }

    // ---------------- Prefabs ----------------

    [MenuItem("Tools/Enemy Spawning/Steps/1 Fighter Prefab")]
    private static void SetupFighterPrefab()
    {
        EditPrefab(FighterPath, root =>
        {
            SquadMember member = Ensure<SquadMember>(root);
            AIReturnToOwnerBehaviour returnBehaviour = Ensure<AIReturnToOwnerBehaviour>(root);

            Set(returnBehaviour, so =>
            {
                so.FindProperty("squadMember").objectReferenceValue = member;
                so.FindProperty("planer").objectReferenceValue = root.GetComponentInChildren<GlobalPathPlaner>(true);
                so.FindProperty("followPlannedPath").objectReferenceValue = root.GetComponentInChildren<FollowPlannedPath>(true);
                so.FindProperty("shipCPosition").objectReferenceValue = UnitSetup.GetBody(root);
            });

            // Boid steering sits on the moving body, next to FollowPlannedPath which applies it.
            Transform body = UnitSetup.GetBody(root);
            SquadFlocking flocking = Ensure<SquadFlocking>(body.gameObject);
            Set(flocking, so => so.FindProperty("member").objectReferenceValue = member);

            FollowPlannedPath follower = body.GetComponent<FollowPlannedPath>();
            if (follower != null) Set(follower, so => so.FindProperty("flocking").objectReferenceValue = flocking);

            AIFighterPatrolBehaviour patrol = root.GetComponent<AIFighterPatrolBehaviour>();
            if (patrol != null) Set(patrol, so => so.FindProperty("flocking").objectReferenceValue = flocking);

            Debug.Log("[EnemySpawningSetup] Fighter: SquadMember + AIReturnToOwnerBehaviour + SquadFlocking ready.");
        });
    }

    [MenuItem("Tools/Enemy Spawning/Steps/2 Destroyer Prefab")]
    private static void SetupDestroyerPrefab()
    {
        EditPrefab(DestroyerPath, root =>
        {
            // Needed when a station's garrison owns the destroyer. Wave destroyers simply have no owner.
            Ensure<SquadMember>(root);

            // Formation slots (StationDefense / WaveFleet) and the behaviour that holds them.
            FormationMember member = Ensure<FormationMember>(root);
            AIHoldFormationBehaviour hold = Ensure<AIHoldFormationBehaviour>(root);
            Transform body = UnitSetup.GetBody(root);
            Set(hold, so =>
            {
                so.FindProperty("member").objectReferenceValue = member;
                so.FindProperty("planer").objectReferenceValue = root.GetComponentInChildren<GlobalPathPlaner>(true);
                so.FindProperty("followPlannedPath").objectReferenceValue = root.GetComponentInChildren<FollowPlannedPath>(true);
                so.FindProperty("shipCPosition").objectReferenceValue = body;
            });

            // Wave destroyers travel by sub-light. It must sit next to ShipMover on the moving body.
            SubLightDrive drive = Ensure<SubLightDrive>(body.gameObject);
            Set(drive, so =>
            {
                so.FindProperty("obstacleMask").intValue = LayerMask.GetMask("Obsticles");
                so.FindProperty("cruiseSpeed").floatValue = 250f;
                so.FindProperty("chargeTime").floatValue = 4f;
                so.FindProperty("exitTime").floatValue = 1.5f;
                so.FindProperty("shipController").objectReferenceValue = root.GetComponent<ShipController>();
                so.FindProperty("unit").objectReferenceValue = body.GetComponent<UnitManager>();
            });

            // A destroyer carrying its own fighters would bring fighters along with every reinforcement wave.
            // Keep the component for later (carrier-style destroyers) but switch it off.
            foreach (DeploySpawner spawner in root.GetComponents<DeploySpawner>())
            {
                spawner.enabled = false;
            }

            Debug.Log("[EnemySpawningSetup] Destroyer: SquadMember, FormationMember, HoldFormation and SubLightDrive ready, own DeploySpawner disabled (destroyers launch no fighters).");
        });
    }

    [MenuItem("Tools/Enemy Spawning/Steps/3 Station Prefab")]
    private static void SetupStationPrefabMenu()
    {
        GameObject destroyerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DestroyerPath);
        SetupStationPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FighterPath), destroyerPrefab, GetDestroyerSpacing());
    }

    private static void SetupStationPrefab(GameObject fighterPrefab, GameObject destroyerPrefab, float destroyerSpacing)
    {
        EditPrefab(StationPath, root =>
        {
            UnitManager owner = root.GetComponentInChildren<StationManager>(true);
            if (owner == null)
            {
                Debug.LogError("[EnemySpawningSetup] Station prefab has no StationManager.");
                return;
            }
            AIDetector sensor = owner.GetComponentInChildren<AIDetector>(true);
            Transform hangar = GetOrCreateHangar(owner.transform);

            // Distress call -> WaveDirector (destroyer reinforcements).
            DistressBeacon beacon = Ensure<DistressBeacon>(root);
            Set(beacon, so =>
            {
                so.FindProperty("owner").objectReferenceValue = owner;
                so.FindProperty("sensor").objectReferenceValue = sensor;
            });

            List<DeploySpawner> spawners = root.GetComponents<DeploySpawner>().ToList();
            while (spawners.Count < 2) spawners.Add(root.AddComponent<DeploySpawner>());

            // [0] Fighters: launch on contact, dock and remember HP when the player leaves, full reset when idle.
            ConfigureDeploy(spawners[0], owner, sensor, hangar, fighterPrefab,
                squads: 1, perSquad: 5, FormationShape.Wedge, spacing: 6f,
                launchMin: 10f, launchMax: 20f, spread: 45f,
                DeployTrigger.OnSensorContact, RecallMode.DockAndStore, ReplenishMode.ResetWhenIdle,
                resetTime: 30f, respawnTime: 45f);

            // [1] Destroyer garrison: out from the start, held in formation by StationDefense, each loss respawns after 60 s.
            ConfigureDeploy(spawners[1], owner, sensor, null, destroyerPrefab,
                squads: 1, perSquad: 3, FormationShape.Line, spacing: destroyerSpacing,
                launchMin: 50f, launchMax: 75f, spread: 180f,
                DeployTrigger.OnGameStart, RecallMode.ReturnAndPatrol, ReplenishMode.ContinuousRespawn,
                resetTime: 30f, respawnTime: 60f);

            // Garrison formation: idle ring, defensive arc towards the player, reacts to other stations' alerts.
            StationDefense defense = Ensure<StationDefense>(root);
            Set(defense, so =>
            {
                so.FindProperty("owner").objectReferenceValue = owner;
                so.FindProperty("sensor").objectReferenceValue = sensor;
                so.FindProperty("garrison").objectReferenceValue = spawners[1];
            });

            if (spawners.Count > 2)
            {
                Debug.LogWarning($"[EnemySpawningSetup] Station has {spawners.Count} DeploySpawners; only the first two were configured.");
            }

            Debug.Log("[EnemySpawningSetup] Station: DistressBeacon, fighter DeploySpawner (5, on contact), destroyer garrison DeploySpawner (3, always) + StationDefense ready.");
        });
    }

    private static void ConfigureDeploy(DeploySpawner spawner, UnitManager owner, AIDetector sensor, Transform deployPoint, GameObject prefab,
        int squads, int perSquad, FormationShape formation, float spacing, float launchMin, float launchMax, float spread,
        DeployTrigger trigger, RecallMode recall, ReplenishMode replenish, float resetTime, float respawnTime)
    {
        spawner.enabled = true;
        Set(spawner, so =>
        {
            so.FindProperty("owner").objectReferenceValue = owner;
            so.FindProperty("sensor").objectReferenceValue = sensor;
            so.FindProperty("deployPoint").objectReferenceValue = deployPoint;

            SerializedProperty prefabs = so.FindProperty("unitPrefabs");
            prefabs.arraySize = 1;
            prefabs.GetArrayElementAtIndex(0).objectReferenceValue = prefab;

            so.FindProperty("maxSquads").intValue = squads;
            so.FindProperty("unitsPerSquad").intValue = perSquad;
            so.FindProperty("formation").enumValueIndex = (int)formation;
            so.FindProperty("formationSpacing").floatValue = spacing;
            so.FindProperty("launchDistanceMin").floatValue = launchMin;
            so.FindProperty("launchDistanceMax").floatValue = launchMax;
            so.FindProperty("launchSpreadAngle").floatValue = spread;
            so.FindProperty("trigger").enumValueIndex = (int)trigger;
            so.FindProperty("recallMode").enumValueIndex = (int)recall;
            so.FindProperty("replenishMode").enumValueIndex = (int)replenish;
            so.FindProperty("resetTime").floatValue = resetTime;
            so.FindProperty("respawnTime").floatValue = respawnTime;
        });
    }

    // A dock point just outside the station's hull and NavMesh hole, so fighters can actually reach it.
    private static Transform GetOrCreateHangar(Transform body)
    {
        Transform hangar = body.Find("Hangar");
        if (hangar != null) return hangar;

        hangar = new GameObject("Hangar").transform;
        hangar.SetParent(body, false);

        float radius = GetFootprintRadius(body.gameObject);
        hangar.position = body.position + Vector3.right * (radius + HangarMargin);
        return hangar;
    }

    // Formation spacing for destroyers: their width plus some breathing room.
    // Measured on loaded prefab contents; renderer bounds on an unloaded prefab asset read as zero.
    private static float GetDestroyerSpacing()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(DestroyerPath);
        try
        {
            return GetFootprintRadius(contents) * 2f + 10f;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    // Half-width of a ship on the XZ plane, from its renderers and NavMesh obstacle.
    private static float GetFootprintRadius(GameObject go)
    {
        float radius = 0f;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        }

        foreach (NavMeshObstacle obstacle in go.GetComponentsInChildren<NavMeshObstacle>(true))
        {
            Vector3 scale = obstacle.transform.lossyScale;
            float obstacleRadius = obstacle.shape == NavMeshObstacleShape.Capsule
                ? obstacle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z))
                : Mathf.Max(obstacle.size.x * Mathf.Abs(scale.x), obstacle.size.z * Mathf.Abs(scale.z)) * 0.5f;
            radius = Mathf.Max(radius, obstacleRadius);
        }

        return radius > 0f ? radius : 10f;
    }

    // ---------------- Behaviour tree ----------------

    [MenuItem("Tools/Enemy Spawning/Steps/4 Fighter Behaviour Tree")]
    private static void SetupFighterTree()
    {
        var tree = AssetDatabase.LoadAssetAtPath<BehaviourTreeData>(FighterTreePath);
        if (tree == null || tree.rootChildren == null || tree.rootChildren.Count == 0 || !(tree.rootChildren[0].node is SelectorData root))
        {
            Debug.LogError($"[EnemySpawningSetup] {FighterTreePath} doesn't start with a root Selector; add the Return branch by hand.");
            return;
        }

        if (root.children.Any(c => c.node != null && c.node.name == ReturnBranchName))
        {
            Debug.Log("[EnemySpawningSetup] Fighter tree already has the Return branch.");
            return;
        }

        var returnBranch = new SequenceData { name = ReturnBranchName };
        returnBranch.children.Add(new NodeEntry
        {
            node = new LeafData
            {
                name = "OrderedToReturn",
                strategyType = StrategyType.Condition,
                condition = "OrderedToReturn",
                strategyName = "SquadMember"
            }
        });
        returnBranch.children.Add(new NodeEntry
        {
            node = new LeafData
            {
                name = "ReturnToOwner",
                strategyType = StrategyType.Action,
                condition = "",
                strategyName = "ReturnToOwner"
            }
        });

        // First child = highest priority: a recall beats pursuit and patrol.
        root.children.Insert(0, new NodeEntry { node = returnBranch });

        EditorUtility.SetDirty(tree);
        Debug.Log("[EnemySpawningSetup] Fighter tree: Return branch added at the top of the root selector.");
    }

    [MenuItem("Tools/Enemy Spawning/Steps/6 Destroyer Behaviour Tree")]
    private static void SetupDestroyerTree()
    {
        var tree = AssetDatabase.LoadAssetAtPath<BehaviourTreeData>(DestroyerTreePath);
        if (tree == null || tree.rootChildren == null || tree.rootChildren.Count == 0 || !(tree.rootChildren[0].node is SelectorData root))
        {
            Debug.LogError($"[EnemySpawningSetup] {DestroyerTreePath} doesn't start with a root Selector; add the Break/Formation branches by hand.");
            return;
        }

        bool changed = false;

        // Highest priority first: a ship told to break off pursues, otherwise it holds its formation slot.
        // Ships without a formation owner fall through to the old pursuit/patrol branches.
        if (!root.children.Any(c => c.node != null && c.node.name == FormationBranchName))
        {
            var formation = new SequenceData { name = FormationBranchName };
            formation.children.Add(Leaf("HasFormationSlot", StrategyType.Condition, "HasFormationSlot", "FormationMember"));
            formation.children.Add(Leaf("HoldFormation", StrategyType.Action, "", "HoldFormation"));
            formation.children.Add(Leaf("Shoot", StrategyType.Action, "", "Shoot"));
            root.children.Insert(0, new NodeEntry { node = formation });
            changed = true;
        }

        if (!root.children.Any(c => c.node != null && c.node.name == BreakBranchName))
        {
            var breakBranch = new SequenceData { name = BreakBranchName };
            breakBranch.children.Add(Leaf("IsBreakingFormation", StrategyType.Condition, "IsBreakingFormation", "FormationMember"));
            breakBranch.children.Add(Leaf("Pursuit", StrategyType.Action, "", "Pursuit"));
            breakBranch.children.Add(Leaf("Shoot", StrategyType.Action, "", "Shoot"));
            root.children.Insert(0, new NodeEntry { node = breakBranch });
            changed = true;
        }

        if (changed) EditorUtility.SetDirty(tree);
        Debug.Log(changed
            ? "[EnemySpawningSetup] Destroyer tree: Break and Formation branches added at the top of the root selector."
            : "[EnemySpawningSetup] Destroyer tree already has the Break and Formation branches.");
    }

    private static NodeEntry Leaf(string name, StrategyType type, string condition, string strategyName)
    {
        return new NodeEntry
        {
            node = new LeafData { name = name, strategyType = type, condition = condition, strategyName = strategyName }
        };
    }

    // ---------------- Player ----------------

    [MenuItem("Tools/Enemy Spawning/Steps/7 Player Sub-light Drive")]
    private static void SetupPlayerPrefab()
    {
        EditPrefab(PlayerPath, root =>
        {
            ShipMover mover = root.GetComponentInChildren<ShipMover>(true);
            if (mover == null)
            {
                Debug.LogError("[EnemySpawningSetup] Player prefab has no ShipMover.");
                return;
            }

            SubLightDrive drive = Ensure<SubLightDrive>(mover.gameObject);
            Set(drive, so =>
            {
                // Never the NavMesh ground plane: the ship flies at its height, so it is always "ahead".
                so.FindProperty("obstacleMask").intValue = LayerMask.GetMask("Obsticles");
                so.FindProperty("shipController").objectReferenceValue = root.GetComponentInChildren<ShipController>(true);
                so.FindProperty("unit").objectReferenceValue = root.GetComponentInChildren<UnitManager>(true);
            });

            // Top-down flight: collisions must not pitch/roll the ship or move it off the plane,
            // or its forward vector tilts and it drifts into the ground plane.
            Rigidbody rb = mover.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.constraints |= RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }

            Debug.Log("[EnemySpawningSetup] Player: SubLightDrive ready (Left Shift: charge / drop out), rigidbody kept flat.");
        });
    }

    // ---------------- HUD ----------------

    [MenuItem("Tools/Enemy Spawning/Steps/8 Player HUD")]
    private static void SetupHudMenu()
    {
        Scene scene = OpenGameScene();
        if (!scene.IsValid()) return;

        SetupHud(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // Hull bar bottom-left, sub-light bar bottom-centre. Skipped if the scene already has a PlayerHUD.
    private static void SetupHud(Scene scene)
    {
        Canvas canvas = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Canvas>(true))
            .FirstOrDefault(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace);
        if (canvas == null)
        {
            Debug.LogError("[EnemySpawningSetup] No screen-space Canvas in the scene for the HUD.");
            return;
        }
        if (canvas.GetComponentInChildren<PlayerHUD>(true) != null)
        {
            Debug.Log("[EnemySpawningSetup] HUD already exists.");
            return;
        }

        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var hudObject = new GameObject("PlayerHUD", typeof(RectTransform));
        var hud = (RectTransform)hudObject.transform;
        hud.SetParent(canvas.transform, false);
        hud.anchorMin = Vector2.zero;
        hud.anchorMax = Vector2.one;
        hud.offsetMin = hud.offsetMax = Vector2.zero;
        hud.SetAsFirstSibling(); // behind pause / game over screens

        CreateBar(hud, "HullBar", new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(420f, 26f), sprite,
            new Color(0.3f, 0.9f, 0.4f), TextAlignmentOptions.Left, out Image hullFill, out TextMeshProUGUI hullText);
        RectTransform subLight = CreateBar(hud, "SubLightBar", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(480f, 18f), sprite,
            new Color(0.3f, 0.8f, 1f), TextAlignmentOptions.Center, out Image subLightFill, out TextMeshProUGUI subLightText);
        CanvasGroup group = subLight.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        PlayerHUD component = hudObject.AddComponent<PlayerHUD>();
        Set(component, so =>
        {
            so.FindProperty("healthFill").objectReferenceValue = hullFill;
            so.FindProperty("healthText").objectReferenceValue = hullText;
            so.FindProperty("subLightFill").objectReferenceValue = subLightFill;
            so.FindProperty("subLightText").objectReferenceValue = subLightText;
            so.FindProperty("subLightGroup").objectReferenceValue = group;
        });

        Debug.Log("[EnemySpawningSetup] HUD: hull and sub-light bars added to the Canvas.");
    }

    // A dark background with a horizontal fill and a label just above it. anchor is also the pivot.
    private static RectTransform CreateBar(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Sprite sprite,
        Color fillColor, TextAlignmentOptions alignment, out Image fill, out TextMeshProUGUI label)
    {
        var bar = (RectTransform)new GameObject(name, typeof(RectTransform), typeof(Image)).transform;
        bar.SetParent(parent, false);
        bar.anchorMin = bar.anchorMax = bar.pivot = anchor;
        bar.anchoredPosition = position;
        bar.sizeDelta = size;
        Image background = bar.GetComponent<Image>();
        background.sprite = sprite;
        background.type = Image.Type.Sliced;
        background.color = new Color(0f, 0f, 0f, 0.6f);
        background.raycastTarget = false;

        var fillRect = (RectTransform)new GameObject("Fill", typeof(RectTransform), typeof(Image)).transform;
        fillRect.SetParent(bar, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        fill = fillRect.GetComponent<Image>();
        fill.sprite = sprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        fill.color = fillColor;
        fill.raycastTarget = false;

        var labelRect = (RectTransform)new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).transform;
        labelRect.SetParent(bar, false);
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 4f);
        labelRect.sizeDelta = new Vector2(0f, 26f);
        label = labelRect.GetComponent<TextMeshProUGUI>();
        label.fontSize = 20f;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = name;

        return bar;
    }

    private static Scene OpenGameScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path == ScenePath) return scene;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return default;
        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    // ---------------- Waves ----------------

    private static List<WaveDefinition> CreateWaveDefinitions(GameObject destroyerPrefab, float spacing)
    {
        if (!AssetDatabase.IsValidFolder(WaveFolder))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Waves");
        }

        return new List<WaveDefinition>
        {
            GetOrCreateWave("Wave_DestroyerPair", destroyerPrefab, 2, FormationShape.Line, spacing, minThreat: 0f, weight: 3f),
            GetOrCreateWave("Wave_DestroyerWedge", destroyerPrefab, 3, FormationShape.Wedge, spacing, minThreat: 0.3f, weight: 2f),
            GetOrCreateWave("Wave_DestroyerBattleGroup", destroyerPrefab, 5, FormationShape.Wedge, spacing, minThreat: 0.6f, weight: 1f),
        };
    }

    private static WaveDefinition GetOrCreateWave(string assetName, GameObject prefab, int count, FormationShape formation, float spacing, float minThreat, float weight)
    {
        string path = $"{WaveFolder}/{assetName}.asset";
        var wave = AssetDatabase.LoadAssetAtPath<WaveDefinition>(path);
        if (wave != null) return wave; // keep any tuning done since

        wave = ScriptableObject.CreateInstance<WaveDefinition>();
        wave.ships = new List<WaveDefinition.Entry> { new WaveDefinition.Entry { prefab = prefab, count = count } };
        wave.formation = formation;
        wave.spacing = spacing;
        wave.minThreat = minThreat;
        wave.weight = weight;

        AssetDatabase.CreateAsset(wave, path);
        Debug.Log($"[EnemySpawningSetup] Created {path}");
        return wave;
    }

    // ---------------- Scene ----------------

    [MenuItem("Tools/Enemy Spawning/Steps/5 GameScene")]
    private static void SetupSceneMenu()
    {
        GameObject destroyerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DestroyerPath);
        SetupScene(CreateWaveDefinitions(destroyerPrefab, GetDestroyerSpacing()));
        AssetDatabase.SaveAssets();
    }

    private static void SetupScene(List<WaveDefinition> waves)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // Old spawner: keep it (stations still come from StationSpawner on the same object) but stop it
        // spawning ships. Fighters + garrison now come from the station's DeploySpawners, reinforcements from waves.
        EnemySpawner enemySpawner = Object.FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Include);
        if (enemySpawner != null)
        {
            Set(enemySpawner, so =>
            {
                so.FindProperty("spawnFighters").boolValue = false;
                so.FindProperty("spawnDestroyers").boolValue = false;
            });
        }

        WaveDirector director = Object.FindFirstObjectByType<WaveDirector>(FindObjectsInactive.Include);
        if (director == null)
        {
            var go = new GameObject("EnemyWaveDirector");
            SceneManager.MoveGameObjectToScene(go, scene);
            director = go.AddComponent<WaveDirector>();
        }

        ThreatMeter threat = Object.FindFirstObjectByType<ThreatMeter>(FindObjectsInactive.Include);
        if (threat == null) threat = director.gameObject.AddComponent<ThreatMeter>();

        Set(director, so =>
        {
            so.FindProperty("threatMeter").objectReferenceValue = threat;

            SerializedProperty list = so.FindProperty("waves");
            if (list.arraySize == 0)
            {
                list.arraySize = waves.Count;
                for (int i = 0; i < waves.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];
            }
        });

        SetupHud(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[EnemySpawningSetup] GameScene: EnemyWaveDirector + ThreatMeter ready, EnemySpawner ship spawning turned off.");
    }

    // ---------------- Helpers ----------------

    private static void EditPrefab(string path, System.Action<GameObject> edit)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static void Set(Object target, System.Action<SerializedObject> apply)
    {
        var so = new SerializedObject(target);
        apply(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
