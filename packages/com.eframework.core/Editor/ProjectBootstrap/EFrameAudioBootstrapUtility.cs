#if UNITY_EDITOR
using System.IO;
using EFrameWork.Runtime.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace EFrameWork.Editor.ProjectBootstrap
{
    internal static class EFrameAudioBootstrapUtility
    {
        private const string MixerAssetName = "EFrameAudioMixerSettings";
        private const string SourceMixerAssetPath = "Packages/com.eframework.core/Editor/Settings/EFrameAudioMixerSettings.mixer";
        private const string ResourcesFolderPath = AudioResourcePaths.ResourcesAudioFolder;
        private const string TargetMixerAssetPath = AudioResourcePaths.MixerSettingsAssetPath;

        internal static bool IsAudioSetupReady()
        {
            var mixer = LoadProjectMixer();
            return mixer != null &&
                   HasGroup(mixer, "MUSIC") &&
                   HasGroup(mixer, "SFX") &&
                   HasVolumeControl(mixer, AudioManager.K_masterVolumeParam) &&
                   HasVolumeControl(mixer, AudioManager.K_musicVolumeParam) &&
                   HasVolumeControl(mixer, AudioManager.K_sfxVolumeParam);
        }

        internal static bool EnsureAudioSetup(out string message)
        {
            if (IsAudioSetupReady())
            {
                message = "Audio mixer is already initialized.";
                return true;
            }

            if (!File.Exists(SourceMixerAssetPath))
            {
                message = $"Audio mixer template was not found: {SourceMixerAssetPath}";
                return false;
            }

            EnsureFolderExists(ResourcesFolderPath);

            if (!File.Exists(TargetMixerAssetPath))
            {
                if (!AssetDatabase.CopyAsset(SourceMixerAssetPath, TargetMixerAssetPath))
                {
                    message = $"Failed to copy audio mixer template to {TargetMixerAssetPath}.";
                    return false;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var mixer = LoadProjectMixer();
            if (mixer == null)
            {
                message = $"Audio mixer asset was not available after initialization: {TargetMixerAssetPath}";
                return false;
            }

            if (!HasGroup(mixer, "MUSIC") || !HasGroup(mixer, "SFX"))
            {
                message = "Audio mixer was created, but required groups MUSIC/SFX are missing.";
                return false;
            }

            if (!HasVolumeControl(mixer, AudioManager.K_masterVolumeParam) ||
                !HasVolumeControl(mixer, AudioManager.K_musicVolumeParam) ||
                !HasVolumeControl(mixer, AudioManager.K_sfxVolumeParam))
            {
                message = "Audio mixer is missing exposed volume controls MasterVolume/MusicVolume/SfxVolume.";
                return false;
            }

            message = $"Initialized audio mixer under {TargetMixerAssetPath}.";
            return true;
        }

        private static AudioMixer LoadProjectMixer()
        {
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(TargetMixerAssetPath);
        }

        private static bool HasGroup(AudioMixer mixer, string groupName)
        {
            return mixer != null && mixer.FindMatchingGroups(groupName).Length > 0;
        }

        private static bool HasVolumeControl(AudioMixer mixer, string parameterName)
        {
            return mixer != null && mixer.GetFloat(parameterName, out _);
        }

        private static void EnsureFolderExists(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var segments = assetPath.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
#endif
