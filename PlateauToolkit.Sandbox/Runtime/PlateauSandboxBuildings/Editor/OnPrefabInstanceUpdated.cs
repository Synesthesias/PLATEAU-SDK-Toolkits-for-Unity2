using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Editor
{
    public class OnPrefabInstanceUpdatedParameter : ScriptableSingleton<OnPrefabInstanceUpdatedParameter>
    {
        public bool canUpdate = true;
        public bool prefabStageDirtied;
    }

    [InitializeOnLoad]
    public class OnPrefabInstanceUpdated
    {
        static OnPrefabInstanceUpdated()
        {
            PrefabStage.prefabStageOpened += PrefabStageOnPrefabStageOpened;
            PrefabStage.prefabStageDirtied += PrefabStageOnPrefabStageDirtied;
            PrefabStage.prefabStageClosing += PrefabStageOnPrefabStageClosing;
            OnPrefabInstanceUpdatedParameter.instance.prefabStageDirtied = false;

            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdate;
            OnPrefabInstanceUpdatedParameter.instance.canUpdate = true;
        }

        /// <summary>
        /// Prefab ModeでPrefabを開いたときに呼び出される
        /// </summary>
        private static void PrefabStageOnPrefabStageOpened(PrefabStage obj)
        {
            OnPrefabInstanceUpdatedParameter.instance.prefabStageDirtied = false;
        }

        /// <summary>
        /// Prefabを編集して差分が出た際に呼び出される
        /// </summary>
        private static void PrefabStageOnPrefabStageDirtied(PrefabStage obj)
        {
            OnPrefabInstanceUpdatedParameter.instance.prefabStageDirtied = true;
        }

        /// <summary>
        /// Prefab Modeを閉じたときに呼び出される
        /// </summary>
        private static void PrefabStageOnPrefabStageClosing(PrefabStage obj)
        {
            if (!OnPrefabInstanceUpdatedParameter.instance.prefabStageDirtied)
            {
                return;
            }

            if (!obj.prefabContentsRoot.TryGetComponent(out Runtime.PlateauSandboxBuilding buildingGeneratorComponent))
            {
                return;
            }

            SaveAssets(obj.prefabContentsRoot, buildingGeneratorComponent);
        }

        private static void OnPrefabInstanceUpdate(GameObject instance)
        {
            GameObject selectedGameObject = Selection.activeGameObject;
            if (instance != selectedGameObject)
            {
                return;
            }

            if (OnPrefabInstanceUpdatedParameter.instance.canUpdate == false)
            {
                OnPrefabInstanceUpdatedParameter.instance.canUpdate = true;
                return;
            }

            if (!instance.TryGetComponent(out Runtime.PlateauSandboxBuilding buildingGeneratorComponent))
            {
                return;
            }

            SaveAssets(instance, buildingGeneratorComponent);
        }

        private static void SaveAssets(GameObject obj, Runtime.PlateauSandboxBuilding buildingGeneratorComponent)
        {
            string meshAssetsFolderPath = BuildingMeshUtility.GetMeshAssetsFolderPath();
            if (!Directory.Exists(meshAssetsFolderPath))
            {
                Directory.CreateDirectory(meshAssetsFolderPath);
            }

            string prefabAssetsFolderPath = BuildingMeshUtility.GetPrefabAssetsFolderPath();
            if (!Directory.Exists(prefabAssetsFolderPath))
            {
                Directory.CreateDirectory(prefabAssetsFolderPath);
            }

            var lsFacadeMeshFilter = obj.transform.GetComponentsInChildren<MeshFilter>().ToList();
            if (BuildingMeshUtility.SaveMesh(lsFacadeMeshFilter, buildingGeneratorComponent.buildingName))
            {
                OnPrefabInstanceUpdatedParameter.instance.canUpdate = false;
                string prefabPath = Path.Combine(prefabAssetsFolderPath, buildingGeneratorComponent.buildingName + ".prefab").Replace("\\", "/");
                PrefabUtility.SaveAsPrefabAssetAndConnect(obj, prefabPath, InteractionMode.AutomatedAction);
                return;
            }

            EditorUtility.DisplayDialog("建築物のメッシュを保存", "建築物の保存に失敗しました。建築物を再生成して下さい。", "はい");
        }
    }
}
