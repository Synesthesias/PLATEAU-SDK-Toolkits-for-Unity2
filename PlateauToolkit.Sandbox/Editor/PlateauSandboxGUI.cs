﻿using PlateauToolkit.Editor;
using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlateauToolkit.Sandbox.Editor
{
    static class PlateauSandboxGUI
    {
        const int k_AssetButtonSelectedBorderSize = 3;
        const int k_AssetButtonSize = 75;
        const int k_AssetButtonBottomMargin = 3;

        static readonly Color k_AssetButtonNormal = new(0, 0, 0, 0);
        static readonly Color k_AssetButtonSelected = new(0.4f, 0.7f, 0.9f, 1f);

        static GUIStyle s_LoadingLabelStyle;

        static Texture2D s_WhiteTexture;
        static Texture2D WhiteTexture
        {
            get
            {
                if (s_WhiteTexture == null)
                {
                    Color[] pixels = { Color.white };
                    var texture = new Texture2D(1, 1);
                    texture.SetPixels(pixels);
                    texture.Apply();

                    s_WhiteTexture = texture;
                }

                return s_WhiteTexture;
            }
        }

        /// <summary>
        /// Draw single colored texture to a rect.
        /// </summary>
        /// <remarks>
        /// If you don't need <see cref="borderRadius" />, use <see cref="EditorGUI.DrawRect" />.
        /// </remarks>
        public static void DrawColorTexture(Rect rect, Color color, float borderRadius)
        {
            GUI.DrawTexture(rect, WhiteTexture, ScaleMode.StretchToFill, true, 0, color, 0, borderRadius);
        }

        public static void PlacementToolButton(PlateauSandboxContext context)
        {
            if (ToolManager.activeToolType != typeof(PlateauSandboxPlacementTool))
            {
                if (new PlateauToolkitImageButtonGUI(
                        220,
                        40,
                        PlateauToolkitGUIStyles.k_ButtonPrimaryColor).Button("配置ツールを起動"))
                {
                    ToolManager.SetActiveTool<PlateauSandboxPlacementTool>();
                }

                GUILayout.Space(5);
            }
            else
            {
                if (new PlateauToolkitImageButtonGUI(
                        220,
                        40,
                        PlateauToolkitGUIStyles.k_ButtonCancelColor).Button("配置ツールを終了"))
                {
                    ToolManager.RestorePreviousPersistentTool();
                }

                GUILayout.Space(5);

                if (context.IsSelectedObject(null))
                {
                    EditorGUILayout.HelpBox("リストから配置するオブジェクトを選択してください", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("配置モードを実行中", MessageType.Info);
                }
            }
        }

        public static void AssetButton<TAsset>(
            SandboxAsset<TAsset> asset, bool isSelected = false, Action onClick = null, bool isDragEnabled = false)
            where TAsset : Component
        {
            using (var scope = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                Rect buttonRect = EditorGUILayout.GetControlRect(GUILayout.Width(k_AssetButtonSize), GUILayout.Height(k_AssetButtonSize));
                EditorGUI.DrawRect(buttonRect, isSelected ? k_AssetButtonSelected : k_AssetButtonNormal);

                var textureRect = new Rect(
                    buttonRect.x + k_AssetButtonSelectedBorderSize,
                    buttonRect.y + k_AssetButtonSelectedBorderSize,
                    buttonRect.width - k_AssetButtonSelectedBorderSize * 2,
                    buttonRect.height - k_AssetButtonSelectedBorderSize * 2);

                if (asset.PreviewTexture != null)
                {
                    GUI.DrawTexture(textureRect, asset.PreviewTexture);
                }
                else
                {
                    // PlayMode 終了時にnullになるため再生成
                    Texture2D preview = AssetPreview.GetAssetPreview(asset.Asset.gameObject);
                    if (preview != null)
                    {
                        Texture2D cachedPreviewTexture = new(preview.width, preview.height);
                        cachedPreviewTexture.SetPixels(preview.GetPixels());
                        cachedPreviewTexture.Apply();
                        preview = cachedPreviewTexture;
                        asset.PreviewTexture = preview;
                        GUI.DrawTexture(textureRect, preview);
                    }
                    else
                    {
                        GUI.DrawTexture(textureRect, AssetPreview.GetMiniThumbnail(asset.Asset.gameObject));
                    }
                }

                GUILayout.FlexibleSpace();

                if (Event.current.type == EventType.MouseUp &&
                    buttonRect.Contains(Event.current.mousePosition))
                {
                    onClick?.Invoke();
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDrag &&
                    scope.rect.Contains(Event.current.mousePosition))
                {
                    if (isDragEnabled)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.paths = null;
                        DragAndDrop.objectReferences = new Object[] { asset.Asset.gameObject };
                        // Uncomment the following code if you need to have additional data for dragging.
                        // DragAndDrop.SetGenericData("data", data);
                        DragAndDrop.StartDrag(asset.Asset.name);
                    }
                }
            }

            EditorGUILayout.Space(k_AssetButtonBottomMargin);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(asset.Asset.gameObject, typeof(PlateauSandboxVehicle), false);
            EditorGUI.EndDisabledGroup();
        }
    }
}