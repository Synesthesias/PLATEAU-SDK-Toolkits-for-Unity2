﻿using PlateauToolkit.Editor;
using PlateauToolkit.Sandbox.Runtime;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlateauSandboxBuilding = PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime.PlateauSandboxBuilding;

namespace PlateauToolkit.Sandbox.Editor
{
    class PlateauSandboxPrefabCreationWizard : ScriptableWizard
    {
        enum Type
        {
            Human,
            Vehicle,
            Building,
            Plant,
            Advertisement,
            StreetFurniture,
            Sign,
            InteractiveTrafficLight,
            Miscellaneous,
        }

        PlateauSandboxPrefabCreationWizard m_Window;

        [SerializeField] GameObject m_BaseObject;
        [SerializeField] Type m_Type;

        public static void DisplayWizard()
        {
            DisplayWizard<PlateauSandboxPrefabCreationWizard>("PLATEAU プレハブ作成", "作成");
        }

        void OnEnable()
        {
            m_Window = GetWindow<PlateauSandboxPrefabCreationWizard>();
        }

        protected override bool DrawWizardGUI()
        {
            PlateauToolkitEditorGUILayout.HeaderLogo(m_Window.position.width);
            return base.DrawWizardGUI();
        }

        void OnWizardCreate()
        {
            if (m_BaseObject == null)
            {
                EditorUtility.DisplayDialog("PLATEAU アセット作成", "ベースとなるモデルを選択してください", "OK");
                return;
            }

            string saveFolderPath = EditorUtility.OpenFolderPanel("アセットの保存先を選択", "Assets", "");
            string savePrefabPath = $"{saveFolderPath}/{m_BaseObject.name} {m_Type}.prefab";

            string osPrefabPath = savePrefabPath.Replace('/', Path.DirectorySeparatorChar);
            if (File.Exists(osPrefabPath))
            {
                if (!EditorUtility.DisplayDialog(
                    "PLATEAU アセット作成",
                    $"{osPrefabPath}は既に存在します。上書きしますか？",
                    "はい", "いいえ"))
                {
                    // "No" was selected
                    return;
                }
            }

            var variant = (GameObject)PrefabUtility.InstantiatePrefab(m_BaseObject);
            using var _ = new CallbackDisposable(() => DestroyImmediate(variant));

            variant.name = m_BaseObject.name;
            switch (m_Type)
            {
                case Type.Human:
                    variant.AddComponent<PlateauSandboxHuman>();
                    break;
                case Type.Vehicle:
                    variant.AddComponent<PlateauSandboxVehicle>();
                    break;
                case Type.Building:
                    variant.AddComponent<PlateauSandboxBuilding>();
                    break;
                case Type.Plant:
                    variant.AddComponent<PlateauSandboxPlant>();
                    break;
                case Type.Advertisement:
                    variant.AddComponent<PlateauSandboxAdvertisement>();
                    break;
                case Type.StreetFurniture:
                    variant.AddComponent<PlateauSandboxStreetFurniture>();
                    break;
                case Type.Sign:
                    variant.AddComponent<PlateauSandboxSign>();
                    break;
                case Type.InteractiveTrafficLight:
                    PlateauSandboxInteractiveTrafficLight trafficLightComponent = variant.AddComponent<PlateauSandboxInteractiveTrafficLight>();
                    trafficLightComponent.CreateBulbDataAuto();
                    break;
                case Type.Miscellaneous:
                    variant.AddComponent<PlateauSandboxMiscellaneous>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Type));
            }

            GameObject savedAssetObject = PrefabUtility.SaveAsPrefabAsset(variant, savePrefabPath, out bool success);

            if (!success)
            {
                Debug.LogError("アセットの作成に失敗しました。");
            }

            if (EditorUtility.DisplayDialog(
                "PLATEAU アセット作成",
                "作成したアセットの編集画面を開きますか？",
                "開く", "キャンセル"))
            {
                string savedAssetPath = AssetDatabase.GetAssetPath(savedAssetObject);
                PrefabStageUtility.OpenPrefab(savedAssetPath);
            }
        }
    }
}
