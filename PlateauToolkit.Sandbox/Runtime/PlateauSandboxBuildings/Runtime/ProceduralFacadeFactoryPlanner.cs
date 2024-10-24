using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings.Interfaces;
using ProceduralToolkit;
using ProceduralToolkit.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime
{
    [CreateAssetMenu(menuName = "ProceduralToolkit/Buildings/Procedural Facade Planner/Factory", order = 6)]
    public class ProceduralFacadeFactoryPlanner : FacadePlanner
    {
        private const float k_MaxBuildingHeight = 100f;
        private const float k_MinFloorHeight = 2.75f;
        private const float k_MaxFloorHeight = 3.25f;
        private const float k_BufferWidth = 2;
        private const float k_SocleHeight = 0.3f;
        private const float k_SocleTopHeight = 0.05f;
        private const string k_SocleTopTexturedDraftName = "SocleTopTextured";
        private const float k_WindowBottomOffset = 1.5f;
        private const float k_WindowTopOffset = 0.8f;
        private const float k_WindowFrameRodHeight = 0.05f;
        private const float k_NarrowPanelSize = 2.5f;
        private const float k_MinWallWidthOffset = 1.25f;
        private const float k_ShadowWallWidthOffset = 0.3f; // 影壁の幅(窓ガラスを突き抜ける分を抑制)
        private const float k_ShadowWallHeightOffset = 0f;
        private const float k_BalconyConcaveDepth = 0.6f;
        private const float k_BalconyConvexDepth = 1f;
        private const float k_BalconyWindowDepth = -0.15f;
        private const float k_EntranceWindowHeight = 2.5f;
        private const float k_EntranceTopOffset = 1.0f;

        private readonly Dictionary<PanelType, List<Func<ILayoutElement>>> m_Constructors = new();
        private readonly Dictionary<PanelSize, float> m_SizeValues = new()
        {
            {PanelSize.k_Narrow, k_NarrowPanelSize}
        };

        public override List<ILayout> Plan(List<Vector2> foundationPolygon, BuildingGenerator.Config config)
        {
            if (k_MaxBuildingHeight < config.buildingHeight)
            {
                config.buildingHeight = k_MaxBuildingHeight;
            }

            SetupConstructors(config);

            // Supports only rectangular buildings
            var layouts = new List<ILayout>();
            for (int i = 0; i < foundationPolygon.Count; i++)
            {
                Vector2 a = foundationPolygon.GetLooped(i + 1);
                Vector2 aNext = foundationPolygon.GetLooped(i + 2);
                Vector2 b = foundationPolygon[i];
                Vector2 bPrevious = foundationPolygon.GetLooped(i - 1);
                float width = (b - a).magnitude;
                bool leftIsConvex = Geometry.GetAngle(b, a, aNext) <= 180;
                bool rightIsConvex = Geometry.GetAngle(bPrevious, b, a) <= 180;
                List<PanelSize> panelSizes = DivideFacade(width, leftIsConvex, rightIsConvex, out float remainderWidth);
                float wallWidthOffset = Mathf.Max(k_MinWallWidthOffset, remainderWidth);
                float wallAveWidthOffset = wallWidthOffset / panelSizes.Count;
                float floorWidthOffset = remainderWidth / panelSizes.Count;
                float shadowWallDepth = (config.skyscraperCondominiumParams.convexBalcony ? k_BalconyConvexDepth : k_BalconyConcaveDepth) - k_BalconyWindowDepth;
                float shadowWallWidthOffset = k_ShadowWallWidthOffset + (-floorWidthOffset + wallAveWidthOffset) * panelSizes.Count;
                float shadowWidth = width - k_ShadowWallWidthOffset + (floorWidthOffset - wallAveWidthOffset) * panelSizes.Count;

                // 小数点が最も小さい（フロア数を求めた時に最も正確に割り切れるかを表す）フロア数から最大のフロア高を求める
                float floorHeight = 0;
                float floorHeightRemaining = 1f;
                for (float tempFloorHeight = k_MinFloorHeight; tempFloorHeight < k_MaxFloorHeight;)
                {
                    float numFloor = config.buildingHeight / tempFloorHeight;
                    if (numFloor - Mathf.Floor(numFloor) < floorHeightRemaining)
                    {
                        floorHeight = tempFloorHeight;
                        floorHeightRemaining = numFloor - Mathf.Floor(numFloor);
                    }
                    tempFloorHeight += 0.05f;
                }

                var vertical = new VerticalLayout();
                switch (i)
                {
                    case 0:
                        config.faceDirection = BuildingGenerator.Config.FaceDirection.k_Back;
                        // shadowWallDepth += config.skyscraperCondominiumParams.hasBalconyBack ? 0 : -k_BalconyConcaveDepth;
                        vertical.Add(PlanNormalFacade(panelSizes, floorHeight, remainderWidth, config));
                        break;
                    case 1:
                        config.faceDirection = BuildingGenerator.Config.FaceDirection.k_Right;
                    //     shadowWallDepth += config.skyscraperCondominiumParams.hasBalconyRight ? 0 : -k_BalconyConcaveDepth;
                        vertical.Add(PlanNormalFacade(panelSizes, floorHeight, remainderWidth, config));
                        break;
                    case 2:
                        config.faceDirection = BuildingGenerator.Config.FaceDirection.k_Front;
                        // shadowWallDepth += config.skyscraperCondominiumParams.hasBalconyFront ? 0 : -k_BalconyConcaveDepth;
                        vertical.Add(PlanEntranceFacade(panelSizes, floorHeight, remainderWidth, config));
                        break;
                    case 3:
                        config.faceDirection = BuildingGenerator.Config.FaceDirection.k_Left;
                        // shadowWallDepth += config.skyscraperCondominiumParams.hasBalconyLeft ? 0 : -k_BalconyConcaveDepth;
                        vertical.Add(PlanNormalFacade(panelSizes, floorHeight, remainderWidth, config));
                        break;
                    default:
                        return layouts;
                }

                // vertical.AddElement(Construct(() => new ProceduralFacadeCompoundElements.ProceduralWall(config)
                // {
                //     m_IsShadowWall = true,
                //     m_MoveShadowWallDepth = shadowWallDepth,
                //     m_ShadowWallWidthOffset = shadowWallWidthOffset,
                //     m_ShadowWallHeightOffset = 0
                // }, shadowWidth, config.buildingHeight - k_ShadowWallHeightOffset));
                // vertical.Add(config.faceDirection == BuildingGenerator.Config.FaceDirection.k_Front
                //     ? PlanEntranceFacade(panelSizes, floorHeight, remainderWidth, config)
                //     : PlanNormalFacade(panelSizes, floorHeight, remainderWidth, config));
                layouts.Add(vertical);
            }

            return layouts;
        }

        private void SetupConstructors(BuildingGenerator.Config config)
        {
            m_Constructors[PanelType.k_Socle] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralSocle(
                    config,
                    socleColor: config.factoryVertexColorPalette.socleColor,
                    socleMat: config.factoryMaterialPalette.socle
                )
            };
            m_Constructors[PanelType.k_SocleTop] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralSocle(
                    config,
                    socleName: k_SocleTopTexturedDraftName,
                    socleColor: config.factoryVertexColorPalette.socleTopColor,
                    socleMat: config.factoryMaterialPalette.socleTop
                )
            };
            m_Constructors[PanelType.k_Entrance] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralEntrance(config),
            };
            m_Constructors[PanelType.k_Wall] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralWall(config)
            };
            m_Constructors[PanelType.k_ShadowWall] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralWall(config)
                {
                    m_IsShadowWall = true,
                    m_MoveShadowWallDepth = 0.8f,
                    m_ShadowWallWidthOffset = k_ShadowWallHeightOffset,
                    m_ShadowWallHeightOffset = 0
                }
            };
            m_Constructors[PanelType.k_Window] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralWindow(config)
                {
                    m_NumCenterRods = 0,
                    m_HasWindowsill = false,
                    m_RectangleWindow = true,
                    m_RectangleWindowOffsetScale = 0.33f
                }
            };
            m_Constructors[PanelType.k_WallOrWindow] = new List<Func<ILayoutElement>>
            {
                m_Constructors[PanelType.k_Wall][0],
                m_Constructors[PanelType.k_Window][0]
            };
        }

        private ILayout PlanNormalFacade(List<PanelSize> panelSizes, float floorHeight, float remainderWidth, BuildingGenerator.Config config)
        {
            int numFloorWithoutEntrance = (int)Mathf.Floor(config.buildingHeight / floorHeight) - 1;
            float entranceHeight = config.buildingHeight - numFloorWithoutEntrance * floorHeight;
            float floorWidthOffset = remainderWidth / panelSizes.Count;
            var vertical = new VerticalLayout
            {
                CreateHorizontal(panelSizes, 0, panelSizes.Count, k_SocleHeight, floorWidthOffset, m_Constructors[PanelType.k_Socle]),
                CreateHorizontal(panelSizes, 0, panelSizes.Count, k_SocleTopHeight, floorWidthOffset, m_Constructors[PanelType.k_SocleTop]),
                CreateHorizontalWindow(panelSizes, 0, panelSizes.Count, entranceHeight - k_SocleHeight - k_SocleTopHeight, floorWidthOffset)
            };

            // エントランスを除いた階数分の壁を生成
            float remainingHeight = config.buildingHeight - entranceHeight;
            if (0 < remainingHeight)
            {
                vertical.Add(CreateNormalFacadeVertical(panelSizes, floorHeight, remainderWidth, 0, panelSizes.Count, config));
            }

            return vertical;
        }

        private VerticalLayout CreateNormalFacadeVertical(List<PanelSize> panelSizes, float floorHeight, float remainderWidth, int from, int to, BuildingGenerator.Config config)
        {
            int numFloorWithoutEntrance = (int)Mathf.Floor(config.buildingHeight / floorHeight) - 1;
            float floorWidthOffset = remainderWidth / (to - from);
            var vertical = new VerticalLayout();
            for (int i = 0; i < numFloorWithoutEntrance; i++)
            {
                vertical.Add(CreateHorizontal(panelSizes, from, to, floorHeight, floorWidthOffset, m_Constructors[PanelType.k_Wall]));
            }

            return vertical;
        }

        private ILayout PlanEntranceFacade(List<PanelSize> panelSizes, float floorHeight, float remainderWidth, BuildingGenerator.Config config)
        {
            int numFloorWithoutEntrance = (int)Mathf.Floor(config.buildingHeight / floorHeight) - 1;
            float entranceHeight = config.buildingHeight - numFloorWithoutEntrance * floorHeight;
            float wallWidthOffset = Mathf.Max(k_MinWallWidthOffset, remainderWidth);
            float wallAveWidthOffset = wallWidthOffset / panelSizes.Count;
            float floorWidthOffset = remainderWidth / panelSizes.Count;

            // 1階部分
            var vertical = new VerticalLayout();
            float remainingHeight = config.buildingHeight - entranceHeight;
            float entranceTopOffset = 0 < remainingHeight ? 0 : k_EntranceTopOffset;
            var horizontal = new HorizontalLayout
            {
                Construct(m_Constructors[PanelType.k_Wall], wallWidthOffset * 0.5f, entranceHeight),
                CreateHorizontalEntrance(panelSizes, 0, panelSizes.Count, entranceHeight, floorWidthOffset - wallAveWidthOffset, entranceTopOffset, config),
                Construct(m_Constructors[PanelType.k_Wall], wallWidthOffset * 0.5f, entranceHeight)
            };
            vertical.Add(horizontal);

            // 2階以降は壁を生成
            if (0 < remainingHeight)
            {
                vertical.Add(CreateNormalFacadeVertical(panelSizes, floorHeight, remainderWidth, 0, panelSizes.Count, config));
            }

            return vertical;
        }

        private List<PanelSize> DivideFacade(float facadeWidth, bool leftIsConvex, bool rightIsConvex, out float remainder)
        {
            float availableWidth = facadeWidth;
            if (!leftIsConvex)
            {
                availableWidth -= k_BufferWidth;
            }
            if (!rightIsConvex)
            {
                availableWidth -= k_BufferWidth;
            }

            Dictionary<PanelSize, int> knapsack = PTUtils.Knapsack(m_SizeValues, availableWidth);
            var sizes = new List<PanelSize>();
            remainder = facadeWidth;
            foreach (KeyValuePair<PanelSize, int> pair in knapsack)
            {
                for (int i = 0; i < pair.Value; i++)
                {
                    sizes.Add(pair.Key);
                    remainder -= m_SizeValues[pair.Key];
                }
            }
            sizes.Shuffle();
            return sizes;
        }

        private HorizontalLayout CreateHorizontal(List<PanelSize> panelSizes, int from, int to, float height, float floorWidthOffset, List<Func<ILayoutElement>> constructors)
        {
            var horizontal = new HorizontalLayout();
            for (int i = from; i < to; i++)
            {
                float panelWidth = m_SizeValues[panelSizes[i]] + floorWidthOffset;
                horizontal.Add(Construct(constructors, panelWidth, height));
            }
            return horizontal;
        }

        private HorizontalLayout CreateHorizontalWindow(List<PanelSize> panelSizes, int from, int to, float height, float floorWidthOffset)
        {
            var horizontal = new HorizontalLayout();
            for (int i = from; i < to - 1;)
            {
                float panelWidth = m_SizeValues[panelSizes[i]] + floorWidthOffset;
                horizontal.Add(Construct(m_Constructors[PanelType.k_Wall][0], panelWidth, height));
                i++;

                if (i + 1 < to - 1)
                {
                    // 窓を2つ連続して格納
                    horizontal.Add(Construct(m_Constructors[PanelType.k_Window][0], panelWidth, height));
                    horizontal.Add(Construct(m_Constructors[PanelType.k_Window][0], panelWidth, height));
                    i += 2;
                }
                else if (i < to - 1)
                {
                    horizontal.Add(Construct(m_Constructors[PanelType.k_Window][0], panelWidth, height));
                    i++;
                }
            }
            float lastPanelWidth = m_SizeValues[panelSizes[to - 1]] + floorWidthOffset;
            horizontal.Add(Construct(m_Constructors[PanelType.k_Wall][0], lastPanelWidth, height));

            return horizontal;
        }

        private HorizontalLayout CreateHorizontalEntrance(List<PanelSize> panelSizes, int from, int to, float height, float floorWidthOffset, float entranceTopOffset, BuildingGenerator.Config config)
        {
            var horizontal = new HorizontalLayout();
            // Debug.Log($"{from} - {to}");
            int[] array = new int[to];
            int onesCount = to / 3;
            int i = 0;
            int oneCount = 0;
            while (onesCount > 0)
            {
                // 1を2回連続で配置
                array[i] = 1;
                array[i + 1] = 1;
                onesCount -= 1;
                oneCount += 1;

                // 次の配置位置までスキップ（1つは0を挟む）
                i += 3;
            }

            if (i + 1 <= to)
            {
                array[i] = 1;
                oneCount += 1;
            }

            // 最後の壁は追加せず、そのスペースをエントランスに振り分ける。
            float panelWidthOffset = 0;
            if (array[to - 1] == 0)
            {
                panelWidthOffset = m_SizeValues[panelSizes[to - 1]] + floorWidthOffset;
                panelWidthOffset /= oneCount;
            }

            for (int j = 0; j < to;)
            {
                float panelWidth = m_SizeValues[panelSizes[j]] + floorWidthOffset ;
                if (j + 1 < to && array[j] == 1 && array[j + 1] == 1)
                {
                    // Debug.Log($"{array[j]}");
                    // Debug.Log($"{array[j + 1]}");
                    horizontal.Add(Construct(() => new ProceduralFacadeCompoundElements.ProceduralEntrance(config)
                    {
                        m_EntranceTopOffset = entranceTopOffset
                    }, panelWidth * 2 + panelWidthOffset, height));
                    // horizontal.Add(Construct(m_Constructors[PanelType.k_Entrance][0], panelWidth * 2 + panelWidthOffset, height));
                    j += 2;
                }
                else if (array[j] == 1)
                {
                    // Debug.Log($"{array[j]}");
                    horizontal.Add(Construct(() => new ProceduralFacadeCompoundElements.ProceduralEntrance(config)
                    {
                        m_EntranceTopOffset = entranceTopOffset
                    }, panelWidth + panelWidthOffset, height));
                    // horizontal.Add(Construct(m_Constructors[PanelType.k_Entrance][0], panelWidth + panelWidthOffset, height));
                    j++;
                }
                else if (array[j] == 0 && j != to - 1)
                {
                    // Debug.Log($"{array[j]}");
                    horizontal.Add(Construct(m_Constructors[PanelType.k_Wall][0], panelWidth, height));
                    j++;
                }
                else
                {
                    // Debug.Log($"{array[j]}");
                    j++;
                }
            }

            return horizontal;
        }

        private static ILayoutElement Construct(List<Func<ILayoutElement>> constructors, float width, float height)
        {
            return Construct(constructors.GetRandom(), width, height);
        }

        private static ILayoutElement Construct(Func<ILayoutElement> constructor, float width, float height)
        {
            ILayoutElement element = constructor();
            element.width = width * element.widthScale;
            element.height = height * element.heightScale;
            return element;
        }

        private enum PanelSize : byte
        {
            k_Narrow,
        }

        private enum PanelType : byte
        {
            k_Socle,
            k_SocleTop,
            k_Entrance,
            k_Wall,
            k_ShadowWall,
            k_Window,
            k_WallOrWindow
        }
    }
}
