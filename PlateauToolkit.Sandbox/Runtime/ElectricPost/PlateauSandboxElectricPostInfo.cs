using System.Collections.Generic;

namespace PlateauToolkit.Sandbox.Runtime.ElectricPost
{
    /// <summary>
    /// 電柱の情報
    /// </summary>
    public class PlateauSandboxElectricPostInfo
    {
        private PlateauSandboxElectricPost m_OwnPost;

        private (PlateauSandboxElectricPost target, bool isFront) m_FrontConnectedPost = (null, false);
        public (PlateauSandboxElectricPost target, bool isFront) FrontConnectedPost => m_FrontConnectedPost;

        private List<(PlateauSandboxElectricPost target, bool isFront)> m_FrontConnectedPosts = new();
        public List<(PlateauSandboxElectricPost target, bool isFront)> FrontConnectedPosts => m_FrontConnectedPosts;


        private (PlateauSandboxElectricPost target, bool isFront) m_BackConnectedPost = (null, false);
        public (PlateauSandboxElectricPost target, bool isFront) BackConnectedPost => m_BackConnectedPost;

        private List<(PlateauSandboxElectricPost target, bool isFront)> m_BackConnectedPosts = new();
        public List<(PlateauSandboxElectricPost target, bool isFront)> BackConnectedPosts => m_BackConnectedPosts;

        private PlateauSandboxElectricPostContext m_Context;

        public PlateauSandboxElectricPostInfo(PlateauSandboxElectricPost ownPost)
        {
            m_Context = PlateauSandboxElectricPostContext.GetCurrent();
            m_OwnPost = ownPost;
        }

        public bool CanConnect(bool isFront, PlateauSandboxElectricPost target)
        {
            if (isFront)
            {
                return m_FrontConnectedPost.target != target;
            }
            else
            {
                return m_BackConnectedPost.target != target;
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
            if (other == m_FrontConnectedPost.target)
            {
                return;
            }
            m_FrontConnectedPost = (other, isOtherFront);
            m_FrontConnectedPosts.Add((other, isOtherFront));
        }

        public void SetBackConnect(PlateauSandboxElectricPost other, bool isOtherFront)
        {
            if (other == m_BackConnectedPost.target)
            {
                return;
            }
            m_BackConnectedPost = (other, isOtherFront);
            m_BackConnectedPosts.Add((other, isOtherFront));
        }

        public bool CanShowFrontWire()
        {
            if (FrontConnectedPost.target == null)
            {
                return false;
            }

            // 相手が表示されてなければ表示
            if (FrontConnectedPost.isFront && !FrontConnectedPost.target.IsShowingFrontWire)
            {
                return true;
            }
            else if (!FrontConnectedPost.isFront && !FrontConnectedPost.target.IsShowingBackWire)
            {
                return true;
            }
            return false;
        }

        public bool CanShowBackWire()
        {
            if (BackConnectedPost.target == null)
            {
                return false;
            }

            // 相手が表示されてなければ表示
            if (BackConnectedPost.isFront && !BackConnectedPost.target.IsShowingFrontWire)
            {
                return true;
            }
            else if (!BackConnectedPost.isFront && !BackConnectedPost.target.IsShowingBackWire)
            {
                return true;
            }
            return false;
        }
    }
}