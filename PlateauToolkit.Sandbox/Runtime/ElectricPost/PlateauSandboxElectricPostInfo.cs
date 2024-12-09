using System.Collections.Generic;

namespace PlateauToolkit.Sandbox.Runtime.ElectricPost
{
    /// <summary>
    /// 電柱の情報
    /// </summary>
    public class PlateauSandboxElectricPostInfo
    {
        private List<(PlateauSandboxElectricPost target, bool isFront)> m_FrontConnectedPosts = new();
        public List<(PlateauSandboxElectricPost target, bool isFront)> FrontConnectedPosts => m_FrontConnectedPosts;

        private List<(PlateauSandboxElectricPost target, bool isFront)> m_BackConnectedPosts = new();
        public List<(PlateauSandboxElectricPost target, bool isFront)> BackConnectedPosts => m_BackConnectedPosts;

        public void AddConnectionSpace(bool isFront)
        {
            if (isFront)
            {
                m_FrontConnectedPosts.Add((null, false));
            }
            else
            {
                m_BackConnectedPosts.Add((null, false));
            }
        }

        public void ResetConnection(bool isFront, int index)
        {
            if (isFront)
            {
                if (index >= 0 && index < m_FrontConnectedPosts.Count)
                {
                    m_FrontConnectedPosts[index] = (null, false);
                }
            }
            else
            {
                if (index >= 0 && index < m_BackConnectedPosts.Count)
                {
                    m_BackConnectedPosts[index] = (null, false);
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

        public void AddFrontConnect(PlateauSandboxElectricPost other, bool isOtherFront)
        {
            m_FrontConnectedPosts.Add((other, isOtherFront));
        }

        public void SetFrontConnect(PlateauSandboxElectricPost other, bool isOtherFront, int index)
        {
            TryResetConnect(false, other, isOtherFront);
            if (m_FrontConnectedPosts.Count > index)
            {
                m_FrontConnectedPosts[index] = (other, isOtherFront);
            }
        }

        public void AddBackConnect(PlateauSandboxElectricPost other, bool isOtherFront)
        {
            m_BackConnectedPosts.Add((other, isOtherFront));
        }

        public void SetBackConnect(PlateauSandboxElectricPost other, bool isOtherFront, int index)
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

        public bool CanShowFrontWire(out int connectedCount)
        {
            connectedCount = -1;
            if (m_FrontConnectedPosts.Count == 0)
            {
                return false;
            }

            // 相手が表示されてなければ表示
            int count = 0;
            foreach (var frontConnectedPost in m_FrontConnectedPosts)
            {
                if (frontConnectedPost.target == null)
                {
                    continue;
                }
                if (frontConnectedPost.isFront && !frontConnectedPost.target.IsShowingFrontWire)
                {
                    connectedCount = count;
                    return true;
                }
                else if (!frontConnectedPost.isFront && !frontConnectedPost.target.IsShowingBackWire)
                {
                    connectedCount = count;
                    return true;
                }
                count++;
            }
            return false;
        }

        public bool CanShowBackWire(out int connectedCount)
        {
            connectedCount = -1;
            if (m_BackConnectedPosts.Count == 0)
            {
                return false;
            }

            // 相手が表示されてなければ表示
            int count = 0;
            foreach (var backConnectedPost in m_BackConnectedPosts)
            {
                if (backConnectedPost.target == null)
                {
                    continue;
                }
                if (backConnectedPost.isFront && !backConnectedPost.target.IsShowingFrontWire)
                {
                    connectedCount = count;
                    return true;
                }
                else if (!backConnectedPost.isFront && !backConnectedPost.target.IsShowingBackWire)
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
    }
}