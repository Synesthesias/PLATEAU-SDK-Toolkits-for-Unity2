using System;
using System.Collections.Generic;

namespace PlateauToolkit.Sandbox.Runtime.ElectricPost
{
    public struct PlateauSandboxElectricConnectInfo
    {
        public PlateauSandboxElectricPost m_Target;
        public bool m_IsFront;
        public string m_WireID;
    }

    /// <summary>
    /// 電柱の情報
    /// </summary>
    public class PlateauSandboxElectricPostInfo
    {
        private List<PlateauSandboxElectricConnectInfo> m_FrontConnectedPosts = new();
        public List<PlateauSandboxElectricConnectInfo> FrontConnectedPosts => m_FrontConnectedPosts;

        private List<PlateauSandboxElectricConnectInfo> m_BackConnectedPosts = new();
        public List<PlateauSandboxElectricConnectInfo> BackConnectedPosts => m_BackConnectedPosts;

        public void AddConnectionSpace(bool isFront)
        {
            if (isFront)
            {
                m_FrontConnectedPosts.Add(new PlateauSandboxElectricConnectInfo());
            }
            else
            {
                m_BackConnectedPosts.Add(new PlateauSandboxElectricConnectInfo());
            }
        }

        public void ResetConnection(bool isFront, int index)
        {
            if (isFront)
            {
                if (index >= 0 && index < m_FrontConnectedPosts.Count)
                {
                    m_FrontConnectedPosts[index] = new PlateauSandboxElectricConnectInfo();
                }
            }
            else
            {
                if (index >= 0 && index < m_BackConnectedPosts.Count)
                {
                    m_BackConnectedPosts[index] = new PlateauSandboxElectricConnectInfo();
                }
            }
        }

        public void RemoveConnection(bool isFront, int index)
        {
            if (isFront)
            {
                if (index >= 0 && index < m_FrontConnectedPosts.Count)
                {
                    m_FrontConnectedPosts.RemoveAt(index);
                }
            }
            else
            {
                if (index >= 0 && index < m_BackConnectedPosts.Count)
                {
                    m_BackConnectedPosts.RemoveAt(index);
                }
            }
        }

        public void AddFrontConnect(PlateauSandboxElectricPost other, bool isOtherFront, string wireID)
        {
            m_FrontConnectedPosts.Add(new PlateauSandboxElectricConnectInfo()
            {
                m_Target = other,
                m_IsFront = isOtherFront,
                m_WireID = wireID
            });
        }

        public void AddBackConnect(PlateauSandboxElectricPost other, bool isOtherFront, string wireID)
        {
            m_BackConnectedPosts.Add(new PlateauSandboxElectricConnectInfo()
            {
                m_Target = other,
                m_IsFront = isOtherFront,
                m_WireID = wireID
            });
        }

        public void SetFrontConnect(PlateauSandboxElectricPost other, bool isOtherFront, string index)
        {
            TryResetConnect(false, other, isOtherFront);
            if (m_FrontConnectedPosts.Count > index)
            {
                m_FrontConnectedPosts[index] = (other, isOtherFront);
            }
        }

        public void SetBackConnect(PlateauSandboxElectricPost other, bool isOtherFront, string index)
        {
            TryResetConnect(false, other, isOtherFront);
            if (m_BackConnectedPosts.Count > index)
            {
                m_BackConnectedPosts[index] = (other, isOtherFront);
            }
        }

        private void TryResetConnect(bool isFront, PlateauSandboxElectricPost other, bool isOtherFront)
        {
            var connectedPosts = isFront ? m_FrontConnectedPosts : m_BackConnectedPosts;
            int index = connectedPosts.FindIndex(x => x.target == other && x.isFront == isOtherFront);
            if (index >= 0)
            {
                // すでに接続されているのでnullに
                connectedPosts[index] = (null, false);
            }
        }

        public bool CanShowWire(out int connectedCount, bool isFront, PlateauSandboxElectricPost own)
        {
            var connectedPosts = isFront ? m_FrontConnectedPosts : m_BackConnectedPosts;

            connectedCount = -1;
            if (connectedPosts.Count == 0)
            {
                return false;
            }

            int count = 0;
            foreach (var connectedPost in connectedPosts)
            {
                if (connectedPost.target == null)
                {
                    count++;
                    continue;
                }

                bool isShow = false;
                if (connectedPost.isFront)
                {
                    if (connectedPost.target.ShowingFrontWire.post != own)
                    {
                        // 相手のワイヤーが自身に向けられてなければ、自分からワイヤーを表示する
                        isShow = true;
                    }
                    else if (connectedPost.target.ShowingFrontWire.post == own &&
                             !connectedPost.target.ShowingFrontWire.isShowing)
                    {
                        // 自分に向けられていて、表示されていなければ表示
                        isShow = true;
                    }
                }
                else
                {
                    if (connectedPost.target.ShowingBackWire.post != own)
                    {
                        // 相手のワイヤーが自身に向けられてなければ、自分からワイヤーを表示する
                        isShow = true;
                    }
                    else if (connectedPost.target.ShowingBackWire.post == own &&
                             !connectedPost.target.ShowingBackWire.isShowing)
                    {
                        // 自分に向けられていて、表示されていなければ表示
                        isShow = true;
                    }
                }

                if (isShow)
                {
                    connectedCount = count;
                    return true;
                }

                count++;
            }
            return false;
        }

        public (bool isFront, int index) GetConnectedPostIndex(PlateauSandboxElectricPost target)
        {
            int frontIndex = FrontConnectedPosts.FindIndex(x => x.target == target);
            if (frontIndex >= 0)
            {
                return (true, frontIndex);
            }
            int backIndex = BackConnectedPosts.FindIndex(x => x.target == target);
            if (backIndex >= 0)
            {
                return (false, backIndex);
            }
            return (false, -1);
        }

        public string GetNextWireID(bool isFront)
        {
            return isFront ? "Front_" : "Back_" + Guid.NewGuid();
        }
    }
}