using System;
using UnityEngine;

namespace PlateauToolkit.Sandbox.Runtime.ElectricPost
{
    /// <summary>
    ///. 電柱の接続コライダー
    /// </summary>
    [ExecuteAlways]
    public class PlateauSandboxElectricPostConnectCollider : MonoBehaviour
    {
        [SerializeField]
        private bool isFront = true;

        private PlateauSandboxElectricPost m_ParentPost;

        private void Awake()
        {
            m_ParentPost = transform.parent.GetComponent<PlateauSandboxElectricPost>();
        }

        public void OnMouseHover(PlateauSandboxElectricPostSelectingInfo info)
        {
            if (info.post == null)
            {
                return;
            }

            if (info.post == m_ParentPost)
            {
                return;
            }

            m_ParentPost.SetHighLight(true);

            // 選択中の電柱から電線を表示してもらう
            info.post.OnHoverConnectionPoint(info.isFront, m_ParentPost, isFront);
        }

        public void OnMoveLeave(PlateauSandboxElectricPostSelectingInfo info)
        {
            if (info.post == null)
            {
                return;
            }

            info.post.OnLeaveConnectionPoint(info.isFront);
            m_ParentPost.SetHighLight(false);
        }

        public void OnSelect(PlateauSandboxElectricPostSelectingInfo info)
        {
            if (info.post == null)
            {
                return;
            }

            // 接続
            m_ParentPost.AddConnectPoint(info.post, isFront, info.isFront);
            info.post.SetConnectPoint(m_ParentPost, info.isFront, isFront, info.index);

            // ハイライト解除
            m_ParentPost.SetHighLight(false);
        }
    }
}