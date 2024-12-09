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
            m_Context = PlateauSandboxElectricPostContext.GetCurrent();
            m_Own = own;
            m_IsFront = isFront;
            m_KeyEvent = keyEvent;
        }
        private PlateauSandboxElectricPostContext m_Context;
        private PlateauSandboxElectricPost m_Own;
        private PlateauSandboxElectricPostKeyEvent m_KeyEvent;
        private bool m_IsFront;
        private int m_SelectingIndex = -1;

        public UnityEvent<PlateauSandboxElectricPost> OnDirectSelect = new ();
        public UnityEvent<bool> OnClickSelect = new ();
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
            if (connectedPosts != null)
            {
                var drawPosts = new List<(PlateauSandboxElectricPost target, bool isFront)>(connectedPosts);
                int count = 0;
                foreach (var connectedPost in drawPosts)
                {
                    PlateauSandboxElectricPost selectingPost = null;

                    GUILayout.Space(5);
                    selectingPost = DrawConnectedPost(count, connectedPost.target);
                    GUILayout.Space(5);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawIsConnectedFront(selectingPost);
                    }

                    GUILayout.Space(5);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        DrawSelectButton(count);
                        GUILayout.Space(5);
                        DrawDeleteButton(count, selectingPost);
                    }

                    GUILayout.Space(5);

                    count++;
                }
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

        public PlateauSandboxElectricPost DrawConnectedPost(int count, PlateauSandboxElectricPost target)
        {
            if (target != null)
            {
                m_KeyEvent.TryAddFocusPost(target, count);
            }

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

        private void DrawIsConnectedFront(PlateauSandboxElectricPost target)
        {
            // 接続先が正面かどうか
            if (target == null)
            {
                return;
            }
            bool isFrontActive = target.IsConnectedFront(m_Own);

            GUI.enabled = false;
            string text = isFrontActive ? "接続部：前方" : "接続部：後方";
            EditorGUILayout.LabelField(text);
            GUI.enabled = true;
        }

        private void DrawSelectButton(int count)
        {
            bool isSelect = count == m_SelectingIndex;
            if (new PlateauToolkitImageButtonGUI(
                    100,
                    20,
                    isSelect ? PlateauToolkitGUIStyles.k_ButtonCancelColor : PlateauToolkitGUIStyles.k_ButtonNormalColor,
                    false)
                .Button("選択する"))
            {
                // 選択時
                if (!isSelect)
                {
                    // ワイヤーを外す
                    m_Own.TryReleaseWire(m_IsFront, count);
                    m_SelectingIndex = count;
                    m_Context.SetSelectingPost(m_Own, m_IsFront, count);
                }
                else
                {
                    m_SelectingIndex = -1;
                    m_Context.ResetSelect();
                }
                OnClickSelect.Invoke(m_SelectingIndex >= 0);
            }
        }

        private void DrawDeleteButton(int count, PlateauSandboxElectricPost post)
        {
            if (new PlateauToolkitImageButtonGUI(
                    100,
                    20,
                    PlateauToolkitGUIStyles.k_ButtonPrimaryColor,
                    false)
                .Button("削除する"))
            {
                m_KeyEvent.RemoveFocusPost(post, count);
                m_Own.RemoveConnectedPost(post);
                post.RemoveConnectedPost(m_Own);
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
                m_Own.AddConnectionSpace(m_IsFront);
            }
        }

        public void Reset()
        {
            m_SelectingIndex = -1;
        }
    }

}