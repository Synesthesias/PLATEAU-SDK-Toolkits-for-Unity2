using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlateauToolkit.Sandbox.Runtime.ElectricPost
{
    [ExecuteAlways]
    public class PlateauSandboxElectricPost : PlateauSandboxStreetFurniture
    {
        // ワイヤー
        private PlateauSandboxElectricPostWireHandler m_ElectricPostWireHandler;
        public (bool isShowing, PlateauSandboxElectricPost post) ShowingFrontWire => m_ElectricPostWireHandler.FrontShowing;
        public (bool isShowing, PlateauSandboxElectricPost post) ShowingBackWire => m_ElectricPostWireHandler.BackShowing;

        // 接続部分
        private PlateauSandboxElectricPostConnectPoints m_ElectricPostConnectPoints;

        // 自動で接続される範囲
        private const float m_SearchDistance = 50.0f;

        private PlateauSandboxElectricPostContext m_Context;

        // メッシュ操作
        private PlateauSandboxElectricPostMesh m_Mesh;

        private PlateauSandboxElectricPostInfo m_Info;
        public List<(PlateauSandboxElectricPost target, bool isFront)> FrontConnectedPosts => m_Info?.FrontConnectedPosts;
        public List<(PlateauSandboxElectricPost target, bool isFront)> BackConnectedPosts => m_Info?.BackConnectedPosts;

        private void Start()
        {
            m_Context = PlateauSandboxElectricPostContext.GetCurrent();
            m_Context.OnCancel.AddListener(() => SetHighLight(false));

            m_ElectricPostWireHandler = new PlateauSandboxElectricPostWireHandler(gameObject);
            m_ElectricPostConnectPoints = new PlateauSandboxElectricPostConnectPoints(gameObject);
            m_Mesh = new PlateauSandboxElectricPostMesh(gameObject);
            m_Info = new PlateauSandboxElectricPostInfo();

            if (hideFlags == HideFlags.None)
            {
                // 配置完了したら実行
                SearchPost();
            }
        }

        private void OnDestroy()
        {
            if (m_Info == null)
            {
                return;
            }

            m_Info.FrontConnectedPosts.ForEach(x => x.target?.RemoveConnectedPost(this));
            m_Info.BackConnectedPosts.ForEach(x => x.target?.RemoveConnectedPost(this));
        }

        private void SearchPost()
        {
            // 他の配置されている一番近い電柱を取得
            var nearestPost = GetNearestPost();
            if (nearestPost != null)
            {
                // 向きで接続部を決定
                bool isOwnFront = IsTargetFacingForward(nearestPost.transform.position);
                bool isOtherFront = nearestPost.IsTargetFacingForward(transform.position);

                string wireID = m_Info.GetNextWireID();
                AddConnectPoint(nearestPost, isOwnFront, isOtherFront);
                nearestPost.AddConnectPoint(this, isOtherFront, isOwnFront);
            }
        }

        private void Update()
        {
            if (m_Info == null)
            {
                return;
            }

            TryShowFrontWire();
            TryShowBackWire();
        }

        private void TryShowFrontWire()
        {
            if (m_Context.IsSelectingPost(this, true))
            {
                // 選択中であれば処理しない
                return;
            }

            if (m_Info.CanShowWire(out int targetCount, true, this))
            {
                var targetPost = FrontConnectedPosts[targetCount];
                if (targetPost.target != null)
                {
                    m_ElectricPostWireHandler.ShowToTarget(
                        true,
                        targetPost.target,
                        targetPost.isFront);
                    return;
                }
            }
            m_ElectricPostWireHandler.Hide(true);
        }

        private void TryShowBackWire()
        {
            if (m_Context.IsSelectingPost(this, false))
            {
                // 選択中であれば処理しない
                return;
            }

            if (m_Info.CanShowWire(out int targetCount, false, this))
            {
                var targetPost = BackConnectedPosts[targetCount];
                if (targetPost.target != null)
                {
                    m_ElectricPostWireHandler.ShowToTarget(
                        false,
                        targetPost.target,
                        targetPost.isFront);
                    return;
                }
            }
            m_ElectricPostWireHandler.Hide(false);
        }

        private PlateauSandboxElectricPost GetNearestPost()
        {
            var electricPosts = FindObjectsOfType<PlateauSandboxElectricPost>();
            PlateauSandboxElectricPost nearestPost = null;
            float nearestDistance = float.MaxValue;
            foreach (var electricPost in electricPosts)
            {
                if (electricPost == this)
                {
                    continue;
                }

                // 障害物チェック
                if (TryIsObstacleBetween(electricPost))
                {
                    continue;
                }

                // 範囲チェック
                float distance = Vector3.Distance(electricPost.transform.position, transform.position);
                if (distance > m_SearchDistance)
                {
                    continue;
                }

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPost = electricPost;
                }
            }

            return nearestPost;
        }

        public bool IsTargetFacingForward(Vector3 position)
        {
            Vector3 toTarget = position - gameObject.transform.position;
            float angle = Vector3.Angle(gameObject.transform.forward, toTarget);

            // 90度未満の場合は正面
            return angle < 90f;
        }

        private bool TryIsObstacleBetween(PlateauSandboxElectricPost target)
        {
            var startPoint = GetTopCenterPoint();
            Vector3 direction = startPoint - target.GetTopCenterPoint();
            float distance = direction.magnitude;
            direction.Normalize();

            // レイキャストを行い、障害物があるかどうかを確認
            if (Physics.Raycast(startPoint, direction, out RaycastHit hit, distance))
            {
                // 障害物がターゲットではない場合、障害物があると判断
                if (hit.collider.gameObject != target.gameObject)
                {
                    return true;
                }
            }

            return false;
        }

        public void AddConnectionSpace(bool isFront)
        {
            m_Info.AddConnectionSpace(isFront);
        }

        public void RemoveConnection(bool isFront, int count)
        {
            m_Info.RemoveConnection(isFront, count);
        }

        public void AddConnectPoint(PlateauSandboxElectricPost other, bool isFront, bool isOtherFront, string wireID)
        {
            if (isFront)
            {
                m_Info.AddFrontConnect(other, isOtherFront, wireID);
            }
            else
            {
                m_Info.AddBackConnect(other, isOtherFront, wireID);
            }
        }

        public void SetConnectPoint(PlateauSandboxElectricPost other, bool isFront, bool isOtherFront, int index)
        {
            if (isFront)
            {
                m_Info.SetFrontConnect(other, isOtherFront, index);
            }
            else
            {
                m_Info.SetBackConnect(other, isOtherFront, index);
            }
        }

        public void RemoveConnectedPost(PlateauSandboxElectricPost targetPost)
        {
            var postIndex = m_Info.GetConnectedPostIndex(targetPost);
            if (postIndex.index < 0)
            {
                return;
            }

            if (postIndex.isFront)
            {
                m_Info.RemoveConnection(true, postIndex.index);
                m_ElectricPostWireHandler.Hide(true);
            }
            else
            {
                m_Info.RemoveConnection(false, postIndex.index);
                m_ElectricPostWireHandler.Hide(false);
            }
        }

        public void OnHoverConnectionPoint(bool isOwnFront, PlateauSandboxElectricPost other, bool isOtherFront)
        {
            // ホバー時にセットせず線のみ表示
            m_ElectricPostWireHandler.ShowToTarget(
                isOwnFront,
                other,
                isOtherFront);
        }

        public void OnLeaveConnectionPoint(bool isOwnFront)
        {
            // ホバー終了したら非表示
            m_ElectricPostWireHandler.Hide(isOwnFront);
        }

        public Vector3 GetConnectPoint(PlateauSandboxElectricPostWireType wireType, bool isFront)
        {
            switch (wireType)
            {
                case PlateauSandboxElectricPostWireType.k_TopA:
                    return m_ElectricPostConnectPoints.GetConnectPoint(PlateauSandboxElectricPostWireType.k_TopA, isFront);
                case PlateauSandboxElectricPostWireType.k_TopB:
                    return m_ElectricPostConnectPoints.GetConnectPoint(PlateauSandboxElectricPostWireType.k_TopB, isFront);
                case PlateauSandboxElectricPostWireType.k_TopC:
                    return m_ElectricPostConnectPoints.GetConnectPoint(PlateauSandboxElectricPostWireType.k_TopC, isFront);
                case PlateauSandboxElectricPostWireType.k_BottomA:
                    return m_ElectricPostConnectPoints.GetConnectPoint(PlateauSandboxElectricPostWireType.k_BottomA, isFront);
                case PlateauSandboxElectricPostWireType.k_BottomB:
                    return m_ElectricPostConnectPoints.GetConnectPoint(PlateauSandboxElectricPostWireType.k_BottomB, isFront);
            }
            return Vector3.zero;
        }

        public void SetHighLight(bool isSelecting)
        {
            m_Mesh.SetHighLight(isSelecting);
        }

        public Vector3 GetTopCenterPoint()
        {
            return m_ElectricPostConnectPoints.GetConnectPoint(PlateauSandboxElectricPostWireType.k_TopB, true);
        }

        public bool IsConnectedFront(PlateauSandboxElectricPost target)
        {
            var indexInfo = m_Info.GetConnectedPostIndex(target);
            if (indexInfo.index < 0)
            {
                return false;
            }
            return indexInfo.isFront;
        }

        public void TryReleaseWire(bool isFront, int index)
        {
            if (isFront)
            {
                if (m_Info.FrontConnectedPosts[index].target != null)
                {
                    m_Info.FrontConnectedPosts[index].target.TryReleaseWire(this);
                    m_Info.ResetConnection(true, index);
                    m_ElectricPostWireHandler.Hide(true);
                }
            }
            else
            {
                if (m_Info.BackConnectedPosts[index].target != null)
                {
                    m_Info.BackConnectedPosts[index].target.TryReleaseWire(this);
                    m_Info.ResetConnection(false, index);
                    m_ElectricPostWireHandler.Hide(false);
                }
            }
        }

        public void TryReleaseWire(PlateauSandboxElectricPost target)
        {
            var indexInfo = m_Info.GetConnectedPostIndex(target);
            if (indexInfo.index < 0)
            {
                return;
            }
            if (indexInfo.isFront)
            {
                m_Info.ResetConnection(true, indexInfo.index);
                m_ElectricPostWireHandler.Hide(true);
            }
            else
            {
                m_Info.ResetConnection(false, indexInfo.index);
                m_ElectricPostWireHandler.Hide(false);
            }
        }
    }
}