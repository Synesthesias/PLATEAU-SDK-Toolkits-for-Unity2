using PlateauToolkit.Sandbox.Runtime.ElectricPost;
using System.Collections.Generic;
using UnityEngine;

namespace PlateauToolkit.Sandbox.Runtime
{
    /// <summary>
    /// 電柱のワイヤーのハンドラー
    /// </summary>
    public class PlateauSandboxElectricPostWireHandler
    {
        private GameObject m_WireRoot;

        private const string k_ElectricWireRootName = "Wires";

        private readonly List<PlateauSandboxElectricPostWire> m_FrontPostWires = new();
        private readonly List<PlateauSandboxElectricPostWire> m_BackPostWires = new();

        private (bool isShowing, PlateauSandboxElectricPost post) m_FrontShowing = new();
        public (bool isShowing, PlateauSandboxElectricPost post) FrontShowing => m_FrontShowing;

        private (bool isShowing, PlateauSandboxElectricPost post) m_BackShowing = new();
        public (bool isShowing, PlateauSandboxElectricPost post) BackShowing => m_BackShowing;

        public PlateauSandboxElectricPostWireHandler(GameObject post)
        {
            m_WireRoot = post.transform.Find(k_ElectricWireRootName).gameObject;
            InitializeWires();
        }

        private void InitializeWires()
        {
            foreach (Transform child in m_WireRoot.transform)
            {
                var wire = new PlateauSandboxElectricPostWire(child.gameObject);
                if (wire.WireType == PlateauSandboxElectricPostWireType.k_InValid)
                {
                    continue;
                }

                wire.Show(false);
                if (wire.IsFrontWire)
                {
                    m_FrontPostWires.Add(wire);
                }
                else
                {
                    m_BackPostWires.Add(wire);
                }
            }
        }

        public void ShowToTarget(bool isOwnFront, PlateauSandboxElectricPost targetPost, bool isTargetFront)
        {
            if (targetPost == null)
            {
                return;
            }

            foreach (var postWire in isOwnFront ? m_FrontPostWires : m_BackPostWires)
            {
                // 複製して使用する
                var wire = GameObject.Instantiate(postWire.ElectricWire, m_WireRoot.transform);
                var createWire = new PlateauSandboxElectricPostWire(wire);

                var targetConnectPosition = targetPost.GetConnectPoint(createWire.WireType, isTargetFront);
                createWire.SetElectricNode(targetConnectPosition);
            }

            if (isOwnFront)
            {
                m_FrontShowing = (true, targetPost);
            }
            else
            {
                m_BackShowing = (true, targetPost);
            }
        }

        public void Hide(bool isFront)
        {
            foreach (var postWire in m_PostWires)
            {
                if (postWire.IsFrontWire == isFront)
                {
                    postWire.Hide();
                }
            }
            if (isFront)
            {
                m_FrontShowing = (false, null);
            }
            else
            {
                m_BackShowing = (false, null);
            }
        }
    }
}