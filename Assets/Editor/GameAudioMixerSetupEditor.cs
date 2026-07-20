#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>建立 GameAudio.mixer 分軌，並寫入 GameAudioMixerRegistry。</summary>
public static class GameAudioMixerSetupEditor
{
    private const string AudioFolder = "Assets/Resources/Audio";
    private const string MixerPath = AudioFolder + "/GameAudio.mixer";
    private const string RegistryPath = AudioFolder + "/GameAudioMixerRegistry.asset";

    private static readonly (string groupName, string paramName, GameAudioChannel channel)[] Channels =
    {
        ("BGM", "BgmVolume", GameAudioChannel.Bgm),
        ("NPC Voice", "NpcVoiceVolume", GameAudioChannel.NpcVoice),
        ("Button SFX", "ButtonSfxVolume", GameAudioChannel.ButtonSfx),
        ("Battle SFX", "BattleSfxVolume", GameAudioChannel.BattleSfx),
    };

    private static Type ControllerType => Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor");

    private static Type GroupControllerType =>
        Type.GetType("UnityEditor.Audio.AudioMixerGroupController, UnityEditor");

    private static Type GroupParameterPathType =>
        Type.GetType("UnityEditor.Audio.AudioGroupParameterPath, UnityEditor");

    [MenuItem("Tools/Audio/Create or Refresh Game Audio Mixer")]
    public static void CreateOrRefresh()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(AudioFolder);

        UnityEngine.Object controller = LoadOrCreateController(MixerPath);
        if (controller == null)
        {
            Debug.LogError(
                "GameAudioMixerSetupEditor: 無法建立 Audio Mixer。請手動建立 " + MixerPath +
                " 後再執行一次。");
            return;
        }

        AudioMixer mixer = controller as AudioMixer;
        if (mixer == null)
            mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

        controller = ResolveController(controller, MixerPath);
        if (controller == null)
        {
            Debug.LogError("GameAudioMixerSetupEditor: 無法載入 AudioMixerController。");
            return;
        }

        if (mixer == null)
            mixer = controller as AudioMixer ?? AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

        UnityEngine.Object masterGroup = GetMasterGroup(controller, mixer);
        if (masterGroup == null)
        {
            Debug.LogError("GameAudioMixerSetupEditor: 找不到 Master 分軌。");
            return;
        }

        ExposeGroupVolume(controller, masterGroup, "MasterVolume");

        var channelGroups = new Dictionary<GameAudioChannel, AudioMixerGroup>();
        for (int i = 0; i < Channels.Length; i++)
        {
            (string groupName, string paramName, GameAudioChannel channel) = Channels[i];
            UnityEngine.Object groupObject = FindOrCreateGroup(mixer, controller, groupName);
            if (groupObject == null)
            {
                Debug.LogError("GameAudioMixerSetupEditor: 無法建立分軌 " + groupName + "。");
                return;
            }

            AttachGroupToMaster(controller, groupObject, masterGroup);
            ExposeGroupVolume(controller, groupObject, paramName);
            channelGroups[channel] = groupObject as AudioMixerGroup;
        }

        NotifyControllerChanged(controller);

        GameAudioMixerRegistry registry = AssetDatabase.LoadAssetAtPath<GameAudioMixerRegistry>(RegistryPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<GameAudioMixerRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
        }

        registry.EditorAssign(
            mixer,
            channelGroups[GameAudioChannel.Bgm],
            channelGroups[GameAudioChannel.NpcVoice],
            channelGroups[GameAudioChannel.ButtonSfx],
            channelGroups[GameAudioChannel.BattleSfx]);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = registry;
        EditorGUIUtility.PingObject(registry);
        Debug.Log(
            "GameAudioMixerSetupEditor: 已更新 " + MixerPath + " 與 " + RegistryPath +
            "。請在 Mixer 視窗確認各分軌已連到 Master → Output。");
    }

    private static UnityEngine.Object LoadOrCreateController(string path)
    {
        UnityEngine.Object existing = LoadControllerAsset(path);
        if (existing != null)
            return existing;

        Type controllerType = ControllerType;
        if (controllerType == null)
            return null;

        MethodInfo createAtPath = controllerType.GetMethod(
            "CreateMixerControllerAtPath",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(string) },
            null);

        if (createAtPath == null)
        {
            Debug.LogError("GameAudioMixerSetupEditor: 找不到 AudioMixerController.CreateMixerControllerAtPath。");
            return null;
        }

        createAtPath.Invoke(null, new object[] { path });
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return LoadControllerAsset(path);
    }

    private static UnityEngine.Object LoadControllerAsset(string path)
    {
        Type controllerType = ControllerType;
        if (controllerType == null)
            return AssetDatabase.LoadMainAssetAtPath(path);

        foreach (MethodInfo method in typeof(AssetDatabase).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name != "LoadAssetAtPath" || !method.IsGenericMethodDefinition)
                continue;

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                continue;

            MethodInfo typedLoad = method.MakeGenericMethod(controllerType);
            object loaded = typedLoad.Invoke(null, new object[] { path });
            if (loaded != null)
                return loaded as UnityEngine.Object;
        }

        UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && controllerType.IsInstanceOfType(all[i]))
                return all[i];
        }

        return null;
    }

    private static UnityEngine.Object ResolveController(UnityEngine.Object controller, string path)
    {
        Type controllerType = ControllerType;
        if (controller != null && controllerType != null && controllerType.IsInstanceOfType(controller))
            return controller;

        return LoadControllerAsset(path);
    }

    private static UnityEngine.Object GetMasterGroup(UnityEngine.Object controller, AudioMixer mixer)
    {
        if (controller != null)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty masterProperty = serializedController.FindProperty("m_MasterGroup");
            if (masterProperty?.objectReferenceValue != null)
                return masterProperty.objectReferenceValue;

            Type runtimeType = controller.GetType();
            FieldInfo masterField = runtimeType.GetField(
                "masterGroup",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (masterField == null)
            {
                masterField = runtimeType.GetField(
                    "m_MasterGroup",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            object masterValue = masterField?.GetValue(controller);
            if (masterValue != null)
                return masterValue as UnityEngine.Object;
        }

        if (mixer != null)
        {
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("Master");
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].name == "Master")
                    return groups[i];
            }
        }

        return null;
    }

    private static UnityEngine.Object FindOrCreateGroup(
        AudioMixer mixer,
        UnityEngine.Object controller,
        string name)
    {
        if (mixer != null)
        {
            AudioMixerGroup[] matches = mixer.FindMatchingGroups(name);
            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i].name == name)
                    return matches[i];
            }
        }

        MethodInfo createNewGroup = ControllerType?.GetMethod(
            "CreateNewGroup",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(string), typeof(bool) },
            null);

        if (createNewGroup == null)
        {
            Debug.LogError("GameAudioMixerSetupEditor: 找不到 CreateNewGroup(string, bool)。");
            return null;
        }

        return createNewGroup.Invoke(controller, new object[] { name, false }) as UnityEngine.Object;
    }

    private static void AttachGroupToMaster(
        UnityEngine.Object controller,
        UnityEngine.Object childGroup,
        UnityEngine.Object masterGroup)
    {
        if (controller == null || childGroup == null || masterGroup == null)
            return;

        if (ReferenceEquals(childGroup, masterGroup))
            return;

        MethodInfo addChildToParent = ControllerType?.GetMethod(
            "AddChildToParent",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        addChildToParent?.Invoke(controller, new[] { childGroup, masterGroup });
    }

    private static void ExposeGroupVolume(
        UnityEngine.Object controller,
        UnityEngine.Object group,
        string exposedName)
    {
        Type groupType = GroupControllerType;
        Type controllerType = ControllerType;
        Type pathType = GroupParameterPathType;
        if (groupType == null || controllerType == null || pathType == null || group == null)
            return;

        MethodInfo getGuidForVolume = groupType.GetMethod(
            "GetGUIDForVolume",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (getGuidForVolume == null)
            return;

        object volumeGuid = getGuidForVolume.Invoke(group, null);
        MethodInfo containsExposed = controllerType.GetMethod(
            "ContainsExposedParameter",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        bool alreadyExposed = containsExposed != null &&
                              (bool)containsExposed.Invoke(controller, new[] { volumeGuid });

        if (!alreadyExposed)
        {
            object path = Activator.CreateInstance(pathType, group, volumeGuid);
            MethodInfo addExposed = controllerType.GetMethod(
                "AddExposedParameter",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            addExposed?.Invoke(controller, new[] { path });
        }

        RenameExposedParameter(controller, volumeGuid, exposedName);
    }

    private static void RenameExposedParameter(
        UnityEngine.Object controller,
        object volumeGuid,
        string exposedName)
    {
        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty exposedParameters = serializedController.FindProperty("exposedParameters");
        if (exposedParameters == null || !exposedParameters.isArray)
            exposedParameters = serializedController.FindProperty("m_ExposedParameters");

        if (exposedParameters == null || !exposedParameters.isArray)
            return;

        for (int i = 0; i < exposedParameters.arraySize; i++)
        {
            SerializedProperty entry = exposedParameters.GetArrayElementAtIndex(i);
            SerializedProperty guidProp = entry.FindPropertyRelative("guid");
            SerializedProperty nameProp = entry.FindPropertyRelative("name");
            if (guidProp == null || nameProp == null)
                continue;

            if (!GuidPropertiesEqual(guidProp, volumeGuid))
                continue;

            nameProp.stringValue = exposedName;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            return;
        }
    }

    private static bool GuidPropertiesEqual(SerializedProperty guidProp, object guidObject)
    {
        if (guidProp == null || guidObject == null)
            return false;

        SerializedProperty data0 = guidProp.FindPropertyRelative("m_Data_0");
        SerializedProperty data1 = guidProp.FindPropertyRelative("m_Data_1");
        SerializedProperty data2 = guidProp.FindPropertyRelative("m_Data_2");
        SerializedProperty data3 = guidProp.FindPropertyRelative("m_Data_3");
        if (data0 == null || data1 == null || data2 == null || data3 == null)
            return false;

        Type guidType = guidObject.GetType();
        FieldInfo f0 = guidType.GetField("m_Data_0", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        FieldInfo f1 = guidType.GetField("m_Data_1", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        FieldInfo f2 = guidType.GetField("m_Data_2", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        FieldInfo f3 = guidType.GetField("m_Data_3", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f0 == null || f1 == null || f2 == null || f3 == null)
            return false;

        return data0.ulongValue == (ulong)f0.GetValue(guidObject) &&
               data1.ulongValue == (ulong)f1.GetValue(guidObject) &&
               data2.ulongValue == (ulong)f2.GetValue(guidObject) &&
               data3.ulongValue == (ulong)f3.GetValue(guidObject);
    }

    private static void NotifyControllerChanged(UnityEngine.Object controller)
    {
        MethodInfo onSubAssetChanged = ControllerType?.GetMethod(
            "OnSubAssetChanged",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        onSubAssetChanged?.Invoke(controller, null);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
            return;

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
