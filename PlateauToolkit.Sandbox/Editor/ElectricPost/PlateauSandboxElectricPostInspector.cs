using PlateauToolkit.Editor;
using PlateauToolkit.Sandbox.Runtime;
using PlateauToolkit.Sandbox.Runtime.ElectricPost;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace PlateauToolkit.Sandbox.Editor
{
    public struct PlateauSandboxElectricPostKeyEvent
    {
        private EventType keyEventType;
        private KeyCode keyCode;
        private (int id, PlateauSandboxElectricPost post) focusPost;

        public void SetFocusPost(int controlID, PlateauSandboxElectricPost post)
        {
            focusPost = (controlID, post);
        }

        private void SetKeyEvent()
        {
            keyEventType = Event.current.type;
            keyCode = Event.current.keyCode;
        }

        public bool IsFocusDelete(int controlID)
        {
            GUI.GetNameOfFocusedControl()

            if (focusPost.post == null)
            {
                return false;
            }

            SetKeyEvent();
            return focusControlID == GUIUtility.hotControl && IsDeleteKey();
        }

        public bool IsDeleteKey()
        {
            SetKeyEvent();
            return keyEventType == EventType.KeyDown && keyCode == KeyCode.Delete;
        }

        public bool IsEscapeKey()
        {
            SetKeyEvent();
            return keyEventType == EventType.KeyDown && keyCode == KeyCode.Escape;
        }
    }

    [CustomEditor(typeof(PlateauSandboxElectricPost))]
    public class PlateauSandboxElectricPostInspector : UnityEditor.Editor
    {
        private PlateauSandboxElectricPostContext m_Context;

        private const string k_FrontTargetNodeName = "前方接続部";
        private const string k_BackTargetNodeName = "後方接続部";

        // 接続先が正面かどうか
        private const string k_IsDestinationFrontName = "接続先が前方接続";

        private PlateauSandboxElectricPost m_Target;

        private PlateauSandboxElectricPostConnectionGUI m_FrontConnectionGUI;
        private PlateauSandboxElectricPostConnectionGUI m_BackConnectionGUI;

        private PlateauSandboxElectricPostKeyEvent m_KeyEvent;

        private void OnEnable()
        {
            m_Target = target as PlateauSandboxElectricPost;
            m_Context = PlateauSandboxElectricPostContext.GetCurrent();
            m_Context.OnSelected.AddListener(ResetSelect);
            m_KeyEvent = new PlateauSandboxElectricPostKeyEvent();

            SetGUI();
        }

        private void SetGUI()
        {
            if (m_FrontConnectionGUI == null)
            {
                m_FrontConnectionGUI = new PlateauSandboxElectricPostConnectionGUI(m_Target, true, m_KeyEvent);
                // m_FrontConnectionGUI.OnClickSelect.AddListener(() => SelectedPost(m_Target.FrontConnectedPost.target));
                // m_FrontConnectionGUI.OnClickRelease.AddListener(() => TryReleaseWire(true));
                // m_FrontConnectionGUI.OnDirectSelect.AddListener((post) => m_Target.SetFrontConnectPointToFacing(post));
            }
            if (m_BackConnectionGUI == null)
            {
                m_BackConnectionGUI = new PlateauSandboxElectricPostConnectionGUI(m_Target, false, m_KeyEvent);
                // m_BackConnectionGUI.OnClickSelect.AddListener(() => SelectedPost(m_Target.BackConnectedPost.target));
                // m_BackConnectionGUI.OnClickRelease.AddListener(() => TryReleaseWire(false));
                // m_BackConnectionGUI.OnDirectSelect.AddListener((post) => m_Target.SetBackConnectPointToFacing(post));
            }
        }

        public override void OnInspectorGUI()
        {
            if (m_Context == null || m_Target == null)
            {
                return;
            }

            serializedObject.Update();
            base.OnInspectorGUI();

            m_FrontConnectionGUI.DrawLayout(m_Target.FrontConnectedPosts);
            m_BackConnectionGUI.DrawLayout(m_Target.BackConnectedPosts);
            //
            // GUILayout.Space(5);
            //
            // var frontTarget = EditorGUILayout.ObjectField(
            //                       k_FrontTargetNodeName,
            //                       m_Target.FrontConnectedPost.target,
            //                       typeof(PlateauSandboxElectricPost), true) as PlateauSandboxElectricPost;
            //
            // if (frontTarget != null && frontTarget != m_Target)
            // {
            //     m_Target.SetFrontConnectPointToFacing(frontTarget);
            // }
            //
            // CreateDestinationCheckbox(true);
            // GUILayout.Space(5);
            //
            // // ボタン作成
            // using (new EditorGUILayout.HorizontalScope())
            // {
            //     GUILayout.FlexibleSpace();
            //     CreateSelectButton(true);
            //     GUILayout.Space(5);
            //     CreateReleaseButton(true);
            // }
            // GUILayout.Space(10);
            //
            // // 後ろの電線の設定
            // var backTarget = EditorGUILayout.ObjectField(
            //                      k_BackTargetNodeName,
            //                      m_Target.BackConnectedPost.target,
            //                      typeof(PlateauSandboxElectricPost), true) as PlateauSandboxElectricPost;
            //
            // if (backTarget != null && backTarget != m_Target)
            // {
            //     m_Target.SetBackConnectPointToFacing(backTarget);
            // }
            //
            // CreateDestinationCheckbox(false);
            // GUILayout.Space(5);
            //
            // // ボタン作成
            // using (new EditorGUILayout.HorizontalScope())
            // {
            //     GUILayout.FlexibleSpace();
            //     CreateSelectButton(false);
            //     GUILayout.Space(5);
            //     CreateReleaseButton(false);
            // }
            // GUILayout.Space(10);


            if (m_KeyEvent.IsEscapeKey())
            {
                ResetSelect();
            }

            if (m_KeyEvent.IsFocusDelete(GUIUtility.hotControl))
            {
                TryReleaseWire(true);
                TryReleaseWire(false);
                ResetSelect();
            }
        }

        private void ResetSelect()
        {
            GUIUtility.hotControl = 0;
            Event.current.Use();

            ToolManager.RestorePreviousPersistentTool();
            m_Context.SetSelectingPost(null, false);

            m_FrontConnectionGUI.Reset();
            m_BackConnectionGUI.Reset();

            m_Context.OnCancel.Invoke();
        }
        //
        // private void SelectedPost(PlateauSandboxElectricPost targetPost)
        // {
        //     ResetSelect();
        // }

        private void SetActiveTool()
        {
            if (ToolManager.activeToolType != typeof(PlateauSandboxElectricPostSelectTool))
            {
                ToolManager.SetActiveTool<PlateauSandboxElectricPostSelectTool>();
            }
            else
            {
                ToolManager.RestorePreviousPersistentTool();
            }
        }

        // private void CreateSelectButton(bool isFront)
        // {
        //     bool isSelecting = isFront ? m_IsFrontNodeSelecting : m_IsBackNodeSelecting;
        //
        //     if (new PlateauToolkitImageButtonGUI(
        //             100,
        //             20,
        //             isSelecting ? PlateauToolkitGUIStyles.k_ButtonCancelColor : PlateauToolkitGUIStyles.k_ButtonNormalColor,
        //             false)
        //         .Button("選択する"))
        //     {
        //         // 選択時
        //         if (!isSelecting)
        //         {
        //             // ワイヤーを外す
        //             TryReleaseWire(isFront);
        //             SetActiveTool();
        //
        //             // 選択中の状態にする
        //             m_Context.SetSelectingPost(m_Target, isFront);
        //
        //             if (isFront)
        //             {
        //                 m_IsFrontNodeSelecting = true;
        //             }
        //             else
        //             {
        //                 m_IsBackNodeSelecting = true;
        //             }
        //         }
        //         else
        //         {
        //             ResetSelect();
        //         }
        //     }
        // }
        //
        // private void CreateReleaseButton(bool isFront)
        // {
        //     bool isConnected = isFront ? m_Target.FrontConnectedPost.target != null : m_Target.BackConnectedPost.target != null;
        //
        //     if (new PlateauToolkitImageButtonGUI(
        //             100,
        //             20,
        //             isConnected ? PlateauToolkitGUIStyles.k_ButtonPrimaryColor : PlateauToolkitGUIStyles.k_ButtonDisableColor,
        //             false)
        //         .Button("解除する"))
        //     {
        //         if (!isConnected)
        //         {
        //             return;
        //         }
        //         TryReleaseWire(isFront);
        //         ResetSelect();
        //     }
        // }

        private void TryReleaseWire(bool isFront)
        {
            if (isFront)
            {
                if (m_Target.FrontConnectedPost.target != null)
                {
                    m_Target.FrontConnectedPost.target.RemoveConnectedPost(m_Target);
                    m_Target.RemoveConnectedPost(m_Target.FrontConnectedPost.target);
                }
            }
            else
            {
                if (m_Target.BackConnectedPost.target != null)
                {
                    m_Target.BackConnectedPost.target.RemoveConnectedPost(m_Target);
                    m_Target.RemoveConnectedPost(m_Target.BackConnectedPost.target);
                }
            }
        }
    }
}