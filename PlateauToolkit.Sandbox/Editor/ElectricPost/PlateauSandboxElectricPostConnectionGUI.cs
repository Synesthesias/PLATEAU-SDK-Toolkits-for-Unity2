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
        public PlateauSandboxElectricPostConnectionGUI(PlateauSandboxElectricPost own, bool isFront, PlateauSandboxElectricPostKeyEvent keyEvent)
        {
            m_Own = own;
            m_IsFront = isFront;
            m_KeyEvent = keyEvent;
        }
        private PlateauSandboxElectricPost m_Own;
        private PlateauSandboxElectricPostKeyEvent m_KeyEvent;
        private bool m_IsFront;
        private bool m_IsPostSelecting; // 選択中かどうか

        private Dictionary<int, bool> m_IsOpen = new ();

        public UnityEvent<PlateauSandboxElectricPost> OnDirectSelect = new ();
        // public UnityEvent<int> OnClickSelect = new ();
        // public UnityEvent OnClickDelete = new ();

        public UnityEvent<int> OnFocusObject = new ();

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

                selectingPost = DrawConnectedPost(count, connectedPost.target);
                GUILayout.Space(5);
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawIsConnectedFront(selectingPost, connectedPost.isFront);
                }

                GUILayout.Space(5);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    DrawSelectButton(count);
                    GUILayout.Space(5);
                    DrawDeleteButton();
                }
                GUILayout.Space(5);
            }

            // 追加ボタン
            GUILayout.Space(15);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                DrawAddButton();
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

            if (m_KeyEvent.IsFocusDelete(GUIUtility.hotControl))
            {
                // Deleteされた時に該当のオブジェクト選択されたいたときは接続解除
                m_Own.RemoveConnectedPost(selectedPost);
            }

            return selectedPost;
        }

        bool m_IsFrontSelect = false;
        private void DrawIsConnectedFront(PlateauSandboxElectricPost target, bool isFront)
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

            GUI.enabled = false;
            EditorGUILayout.LabelField("接続部：前方");
            GUI.enabled = true;
        }

        private void DrawSelectButton(int count)
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

        private void DrawDeleteButton()
        {
            if (new PlateauToolkitImageButtonGUI(
                    100,
                    20,
                    PlateauToolkitGUIStyles.k_ButtonPrimaryColor,
                    false)
                .Button("削除する"))
            {
                // m_Own.RemoveConnectedPost();
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

        public void Reset()
        {
            m_IsPostSelecting = false;
        }
    }

}