using System.Collections.Generic;

namespace PlateauToolkit.Sandbox.Runtime.ElectricPost
{
    /// <summary>
    /// 電柱の情報
    /// </summary>
    public class PlateauSandboxElectricPostInfo
    {
        // private PlateauSandboxElectricPost m_OwnPost;

        private List<(PlateauSandboxElectricPost target, bool isFront)> m_FrontConnectedPosts = new();
        public List<(PlateauSandboxElectricPost target, bool isFront)> FrontConnectedPosts => m_FrontConnectedPosts;

        private List<(PlateauSandboxElectricPost target, bool isFront)> m_BackConnectedPosts = new();
        public List<(PlateauSandboxElectricPost target, bool isFront)> BackConnectedPosts => m_BackConnectedPosts;

        // public PlateauSandboxElectricPostInfo(PlateauSandboxElectricPost ownPost)
        // {
        //     m_OwnPost = ownPost;
        // }
        //
        public bool CanConnect(bool isFront, PlateauSandboxElectricPost target)
        {
            if (isFront)
            {
                return !m_FrontConnectedPosts.Exists(x => x.target == target);
            }
            else
            {
                return !m_BackConnectedPosts.Exists(x => x.target == target);
            }
        }

        public void AddConnection(bool isFront)
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

        public void SetFrontConnect(PlateauSandboxElectricPost other, bool isOtherFront)
        {
            if (m_FrontConnectedPosts.Exists(x => x.target == other))
            {
                return;
            }
            m_FrontConnectedPosts.Add((other, isOtherFront));
        }

        public void SetBackConnect(PlateauSandboxElectricPost other, bool isOtherFront)
        {
            if (m_BackConnectedPosts.Exists(x => x.target == other))
            {
                return;
            }
            m_BackConnectedPosts.Add((other, isOtherFront));
        }

        public bool CanShowFrontWire()
        {
            if (m_FrontConnectedPosts.Count == 0)
            {
                return false;
            }

            // 相手が表示されてなければ表示
            foreach (var frontConnectedPost in m_FrontConnectedPosts)
            {
                if (frontConnectedPost.isFront && !frontConnectedPost.target.IsShowingFrontWire)
                {
                    return true;
                }
                else if (!frontConnectedPost.isFront && !frontConnectedPost.target.IsShowingBackWire)
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanShowBackWire()
        {
            if (m_BackConnectedPosts.Count == 0)
            {
                return false;
            }

            // 相手が表示されてなければ表示
            foreach (var backConnectedPost in m_BackConnectedPosts)
            {
                if (backConnectedPost.isFront && !backConnectedPost.target.IsShowingFrontWire)
                {
                    return true;
                }
                else if (!backConnectedPost.isFront && !backConnectedPost.target.IsShowingBackWire)
                {
                    return true;
                }
            }
            return false;
        }
    }
}