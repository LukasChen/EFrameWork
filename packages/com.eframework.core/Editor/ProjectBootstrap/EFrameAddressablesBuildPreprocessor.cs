#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EFrameWork.Editor.ProjectBootstrap
{
    internal sealed class EFrameAddressablesBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            var generatedResPathWasCurrent = EFrameAddressablesBootstrapUtility.IsGeneratedResPathCurrent(out var generatedResPathMessage);

            if (!EFrameAddressablesBootstrapUtility.SyncProjectAddressablesAndGenerateResPath(out var syncMessage))
            {
                throw new BuildFailedException($"EFrame Addressables sync failed before build.{System.Environment.NewLine}{syncMessage}");
            }

            if (!EFrameAddressablesBootstrapUtility.ValidateManagedAddressables(out var validationMessage))
            {
                throw new BuildFailedException(validationMessage);
            }

            if (!generatedResPathWasCurrent)
            {
                throw new BuildFailedException(
                    $"EFrame regenerated ResPath before build. Let Unity recompile, then build again.{System.Environment.NewLine}{generatedResPathMessage}");
            }

            Debug.Log($"[EFrame Addressables] Build preflight complete. {syncMessage}{System.Environment.NewLine}{validationMessage}");
        }
    }
}
#endif
