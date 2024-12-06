using PlateauToolkit.Editor;
using PlateauToolkit.Sandbox.Runtime.ElectricPost;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using EditorGUILayout = UnityEditor.EditorGUILayout;

namespace PlateauToolkit.Sandbox.Editor
{
    /// <summary>
    /// 電柱コンポーネントのGUI表示
    /// </summary>
    public class PlateauSandboxElectricPostConnectionGUI
    {
        public PlateauSandboxElectricPostConnectionGUI(PlateauSandboxElectricPost own, bool isFront)
        {
            m_Own = own;
            m_IsFront = isFront;
        }
        private PlateauSandboxElectricPost m_Own;
        private bool m_IsFront;
        private bool m_IsPostSelecting; // 選択中かどうか

        private Dictionary<int, bool> m_IsOpen = new ();

        public UnityEvent<PlateauSandboxElectricPost> OnDirectSelect = new ();
        public UnityEvent<int> OnClickSelect = new ();
        public UnityEvent<int> OnClickRelease = new ();

        public void DrawLayout(List<(PlateauSandboxElectricPost target, bool isFront)> connectedPosts)
        {
            GUILayout.Space(5);
            PlateauToolkitEditorGUILayout.BorderLine();
            GUILayout.Space(5);

            // タイトル
            DrawTitle();

            // 接続先の電柱
            int count = 0;
            foreach (var connectedPost in connectedPosts)
            {
                GUILayout.Space(5);
                count++;

                PlateauSandboxElectricPost selectingPost = null;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(10);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Space(2);
                        DrawFoldout(count);
                    }

                    GUILayout.Space(20);
                    selectingPost = DrawConnectedPost(count, connectedPost.target);
                }

                if (!m_IsOpen[count])
                {
                    continue;
                }


                GUILayout.Space(5);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(10);
                    DrawIsConnectedFront(selectingPost, connectedPost.isFront);
                }

                GUILayout.Space(5);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    DrawSelectButton(count);
                    GUILayout.Space(5);
                    DrawReleaseButton(selectingPost != null, count);
                }
                GUILayout.Space(5);
            }

            // 追加ボタン
            GUILayout.Space(15);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                DrawAddButton();

                GUILayout.Space(10);

                DrawDeleteButton(connectedPosts.Count == 0);

                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(10);
        }

        public void DrawTitle()
        {
            // タイトル
            EditorGUILayout.LabelField(m_IsFront ? "前方接続部" : "後方接続部", EditorStyles.boldLabel);
        }

        public bool DrawFoldout(int count)
        {
            // 折りたたみ
            bool isOpen = false;
            m_IsOpen.TryGetValue(count, out isOpen);
            bool opened = EditorGUILayout.Foldout(isOpen, $"接続先 {count}");
            m_IsOpen[count] = opened;

            return opened;
        }

        public PlateauSandboxElectricPost DrawConnectedPost(int count, PlateauSandboxElectricPost target)
        {
            // 接続先の電柱
            var selectedPost = EditorGUILayout.ObjectField(
                            "",
                             target,
                             typeof(PlateauSandboxElectricPost), true) as PlateauSandboxElectricPost;

            if (selectedPost != null && selectedPost != m_Own)
            {
                OnDirectSelect.Invoke(selectedPost);
            }
            return selectedPost;
        }

        bool m_IsFrontSelect = false;
        public void DrawIsConnectedFront(PlateauSandboxElectricPost target, bool isFront)
        {
            // 接続先が正面かどうか
            bool isFrontActive = false;
            if (isFront)
            {
                if (target == null)
                {
                    return;
                }
                isFrontActive = target.FrontConnectedPost.isFront;
            }
            else
            {
                if (target == null)
                {
                    return;
                }
                isFrontActive = target.BackConnectedPost.isFront;
            }
            m_IsFrontSelect = EditorGUILayout.Toggle("接続部：前方", m_IsFrontSelect);
        }

        public void DrawSelectButton(int count)
        {
            if (new PlateauToolkitImageButtonGUI(
                    100,
                    20,
                    m_IsPostSelecting ? PlateauToolkitGUIStyles.k_ButtonCancelColor : PlateauToolkitGUIStyles.k_ButtonNormalColor,
                    false)
                .Button("選択する"))
            {
                // OnClickSelect.Invoke(count);
            }
        }

        public void DrawReleaseButton(bool isConnected, int count)
        {
            if (new PlateauToolkitImageButtonGUI(
                    100,
                    20,
                    isConnected ? PlateauToolkitGUIStyles.k_ButtonPrimaryColor : PlateauToolkitGUIStyles.k_ButtonDisableColor,
                    false)
                .Button("解除する"))
            {
                if (!isConnected)
                {
                    return;
                }
                // OnClickRelease.Invoke(count);
            }
        }

        private void DrawAddButton()
        {
            if (new PlateauToolkitImageButtonGUI(
                    150,
                    20,
                    PlateauToolkitGUIStyles.k_ButtonPrimaryColor,
                    false)
                .Button("追加する"))
            {
                // 追加
                m_Own.AddConnection(m_IsFront);
            }
        }

        private void DrawDeleteButton(bool isDisable)
        {
            if (new PlateauToolkitImageButtonGUI(
                    150,
                    20,
                    isDisable ? PlateauToolkitGUIStyles.k_ButtonDisableColor : PlateauToolkitGUIStyles.k_ButtonPrimaryColor,
                    false)
                .Button("削除する"))
            {
                if (isDisable)
                {
                    return;
                }
                // 削除
                m_Own.RemoveConnection(m_IsFront);
            }
        }

        public void Reset()
        {
            m_IsPostSelecting = false;
        }
    }
}