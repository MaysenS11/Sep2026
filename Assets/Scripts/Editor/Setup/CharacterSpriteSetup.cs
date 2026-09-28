#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CharacterSpriteSetup
{
    private struct CharacterConfig
    {
        public string charName;
        public string folder;
        public string frontFile;
        public string backFile;
        public string leftFile;
        public string rightFile;
        public Vector2 pivot;
        public string defAssetPath;
    }

    [MenuItem("Tools/Setup All Character Sprites")]
    public static void SetupAll()
    {
        var configs = new CharacterConfig[]
        {
            new CharacterConfig
            {
                charName = "Moose",
                folder = "Assets/Sprites/Player/Moose",
                frontFile = "Moose_Front.png",
                backFile = "Moose_Back.png",
                leftFile = "Moose_Left.png",
                rightFile = "Moose_Right.png",
                pivot = new Vector2(0.5f, 6.4f / 96f),
                defAssetPath = "Assets/ScriptableObjects/Player/Moose_Char.asset"
            },
            new CharacterConfig
            {
                charName = "Raven",
                folder = "Assets/Sprites/Player/Raven",
                frontFile = "Raven_Front.png",
                backFile = "Raven_Back.png",
                leftFile = "Raven_Left.png",
                rightFile = "Raven_Right.png",
                pivot = new Vector2(0.5f, 6.4f / 64f),
                defAssetPath = "Assets/ScriptableObjects/Player/Raven_Char.asset"
            },
            new CharacterConfig
            {
                charName = "Rat",
                folder = "Assets/Sprites/Player/Rat",
                frontFile = "Rat_Front.png",
                backFile = "Rat_Back.png",
                leftFile = "Rat_Left.png",
                rightFile = "Rat_Right.png",
                pivot = new Vector2(0.5f, 6.4f / 64f),
                defAssetPath = "Assets/ScriptableObjects/Player/Rat_Char.asset"
            }
        };

        var catController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Sprites/Player/Cat/AnimationCat.controller");
        if (catController == null)
        {
            Debug.LogError("Cat controller not found at Assets/Sprites/Player/Cat/AnimationCat.controller");
            return;
        }

        var catDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/ScriptableObjects/Player/Cat_Char.asset");
        if (catDef != null)
        {
            var catSo = new SerializedObject(catDef);
            catSo.FindProperty("animatorController").objectReferenceValue = catController;
            catSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(catDef);
        }

        foreach (var cfg in configs)
        {
            SetupCharacter(cfg, catController);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Successfully setup sprites, animations, override controllers, and character definitions for all characters!");
    }

    private static void SetupCharacter(CharacterConfig cfg, RuntimeAnimatorController baseController)
    {
        string[] files = new string[] { cfg.frontFile, cfg.backFile, cfg.leftFile, cfg.rightFile };
        foreach (var f in files)
        {
            string path = $"{cfg.folder}/{f}";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                
                TextureImporterSettings tis = new TextureImporterSettings();
                importer.ReadTextureSettings(tis);
                tis.spriteAlignment = (int)SpriteAlignment.Custom;
                tis.spritePivot = cfg.pivot;
                importer.SetTextureSettings(tis);
                importer.SaveAndReimport();
            }
        }

        Sprite frontSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{cfg.folder}/{cfg.frontFile}");
        Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{cfg.folder}/{cfg.backFile}");
        Sprite leftSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{cfg.folder}/{cfg.leftFile}");
        Sprite rightSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{cfg.folder}/{cfg.rightFile}");

        if (frontSprite == null || backSprite == null || leftSprite == null || rightSprite == null)
        {
            Debug.LogError($"Missing sprites for {cfg.charName} in {cfg.folder}");
            return;
        }

        AnimationClip clipFront = GetOrCreateClip($"{cfg.folder}/IdleFront_{cfg.charName}.anim", frontSprite);
        AnimationClip clipBack = GetOrCreateClip($"{cfg.folder}/IdleBack_{cfg.charName}.anim", backSprite);
        AnimationClip clipLeft = GetOrCreateClip($"{cfg.folder}/IdleLeft_{cfg.charName}.anim", leftSprite);
        AnimationClip clipRight = GetOrCreateClip($"{cfg.folder}/IdleRight_{cfg.charName}.anim", rightSprite);

        string overridePath = $"{cfg.folder}/Animation{cfg.charName}.overrideController";
        var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(baseController);
            AssetDatabase.CreateAsset(overrideController, overridePath);
        }
        else
        {
            overrideController.runtimeAnimatorController = baseController;
        }

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            var originalClip = overrides[i].Key;
            if (originalClip == null) continue;
            string name = originalClip.name;

            AnimationClip targetClip = clipFront;
            if (name.Contains("Back") || name.Contains("Up")) targetClip = clipBack;
            else if (name.Contains("Left")) targetClip = clipLeft;
            else if (name.Contains("Right")) targetClip = clipRight;
            else if (name.Contains("Front") || name.Contains("Down")) targetClip = clipFront;

            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, targetClip);
        }

        overrideController.ApplyOverrides(overrides);
        EditorUtility.SetDirty(overrideController);

        var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(cfg.defAssetPath);
        if (charDef != null)
        {
            var so = new SerializedObject(charDef);
            so.FindProperty("animatorController").objectReferenceValue = overrideController;
            so.FindProperty("fullBodySprite").objectReferenceValue = frontSprite;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(charDef);
        }
    }

    private static AnimationClip GetOrCreateClip(string clipPath, Sprite sprite)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            clip.frameRate = 60;
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[2];
        keyframes[0] = new ObjectReferenceKeyframe { time = 0f, value = sprite };
        keyframes[1] = new ObjectReferenceKeyframe { time = 0.5f, value = sprite };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }
}
#endif
