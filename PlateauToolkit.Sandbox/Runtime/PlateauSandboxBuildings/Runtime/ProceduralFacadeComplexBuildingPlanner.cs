using System;
using System.Collections.Generic;
using UnityEngine;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings.Interfaces;
using ProceduralToolkit;
using ProceduralToolkit.Buildings;

namespace PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildings.Runtime
{
    [CreateAssetMenu(menuName = "ProceduralToolkit/Buildings/Procedural Facade Planner/Complex Building", order = 7)]
    public class ProceduralFacadeComplexBuildingPlanner : FacadePlanner
    {
        private const float k_MaxBuildingHeight = 100f;
        private const float k_MinFloorHeight = 2.75f;
        private const float k_MaxFloorHeight = 3.25f;
        private const float k_SmallWallHeight = 1.0f;
        private const float k_DepressionWallHeight = 1.0f;
        private const float k_SmallWindowHeight = 1.5f;
        private const float k_BufferWidth = 2;
        private const float k_ShadowWallOffset = 0.65f;
        private const float k_EntranceWindowHeight = 2.5f;

        private readonly Dictionary<PanelType, List<Func<ILayoutElement>>> m_Constructors = new();
        private readonly Dictionary<PanelSize, float> m_SizeValues = new()
        {
            {PanelSize.k_Narrow, 2.5f},
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

                config.m_ComplexBuildingPlannerParams.m_AddedBoundaryWall = true;
                switch (i)
                {
                    case 0:
                    {
                        config.faceDirection = BuildingGenerator.Config.FaceDirection.k_Back;
                        var vertical = new VerticalLayout();
                        vertical.AddElement(Construct(m_Constructors[PanelType.k_ShadowWall], width - k_ShadowWallOffset, config.buildingHeight - k_ShadowWallOffset));
                        vertical.Add(PlanNormalFacade(width, floorHeight,　config, leftIsConvex, rightIsConvex));
                        layouts.Add(vertical);
                        break;
                    }
                    case 1:
                    {
                        config.faceDirection = BuildingGenerator.Config.FaceDirection.k_Right;
                        var vertical = new VerticalLayout();
                        vertical.AddElement(Construct(m_Constructors[PanelType.k_ShadowWall], width - k_ShadowWallOffset, config.buildingHeight - k_ShadowWallOffset));
                        vertical.Add(PlanNormalFacade(width, floorHeight,　config, leftIsConvex, rightIsConvex));
                        layouts.Add(vertical);
                        break;
                    }
                    case 2:
                    {
                        config.faceDirection = BuildingGenerator.Config.FaceDirection.k_Front;
                        var vertical = new VerticalLayout();
                        vertical.AddElement(Construct(m_Constructors[PanelType.k_ShadowWall], width - k_ShadowWallOffset, config.buildingHeight - k_ShadowWallOffset));
                        vertical.Add(PlanEntranceFacade(width, floorHeight,　config, leftIsConvex, rightIsConvex));
                        layouts.Add(vertical);
                        break;
                    }
                    case 3:
                    {
                        config.faceDirection = BuildingGenerator.Config.FaceDirection.k_Left;
                        var vertical = new VerticalLayout();
                        vertical.AddElement(Construct(m_Constructors[PanelType.k_ShadowWall], width - k_ShadowWallOffset, config.buildingHeight - k_ShadowWallOffset));
                        vertical.Add(PlanNormalFacade(width, floorHeight,　config, leftIsConvex, rightIsConvex));
                        layouts.Add(vertical);
                        break;
                    }
                }
            }

            return layouts;
        }

        private void SetupConstructors(BuildingGenerator.Config config)
        {
            m_Constructors[PanelType.k_ShadowWall] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralWall(config)
                {
                    m_IsShadowWall = true,
                    m_MoveShadowWallDepth = 0.7f,
                    m_ShadowWallWidthOffset = k_ShadowWallOffset,
                    m_ShadowWallHeightOffset = 0
                }
            };

            m_Constructors[PanelType.k_FullWindow] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralFullWindow(config)
                {
                    m_WindowFrameRodWidth = 0.2f
                }
            };

            m_Constructors[PanelType.k_CommercialSmallFullWindow] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralFullWindow(config)
                {
                    m_NumCenterRods = 1,
                    m_WindowFrameRodType = ProceduralFacadeElement.WindowFrameRodType.k_Vertical,
                    m_WindowFrameRodWidth = 0.05f
                }
            };

            m_Constructors[PanelType.k_CommercialWallWithFrame] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralWallWithFrame(config)
                {
                    m_NumCenterRods = 0
                }
            };

            m_Constructors[PanelType.k_OfficeSmallFullWindow] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralFullWindow(
                    config,
                    "OfficeBuildingSmallFullWindowTextured",
                    config.complexBuildingMaterialPalette.officeBuildingSpandrel
                )
                {
                    m_WindowFrameRodWidth = 0.2f,
                }
            };
        }

        private ILayout PlanNormalFacade(float facadeWidth, float floorHeight, BuildingGenerator.Config config, bool leftIsConvex, bool rightIsConvex)
        {
            List<PanelSize> panelSizes = DivideFacade(facadeWidth, leftIsConvex, rightIsConvex, out float remainderWidth);
            return CreateNormalFacadeVertical(panelSizes, remainderWidth, floorHeight, 0, panelSizes.Count, config);
        }

        private ILayout PlanEntranceFacade(float facadeWidth, float floorHeight, BuildingGenerator.Config config, bool leftIsConvex, bool rightIsConvex)
        {
            List<PanelSize> panelSizes = DivideFacade(facadeWidth, leftIsConvex, rightIsConvex, out float remainderWidth);

            var horizontal = new HorizontalLayout();

            const int entranceCount = 1;
            int entranceIndexInterval = (panelSizes.Count - entranceCount)/(entranceCount + 1);

            float floorWidthOffset = remainderWidth / panelSizes.Count;
            horizontal.Add(CreateNormalFacadeVertical(panelSizes, floorWidthOffset * entranceIndexInterval, floorHeight, 0, entranceIndexInterval, config));
            horizontal.Add(CreateEntranceVertical(panelSizes, floorWidthOffset, floorHeight, entranceIndexInterval, 0 == entranceIndexInterval, entranceIndexInterval + 1 == panelSizes.Count, config));
            horizontal.Add(CreateNormalFacadeVertical(panelSizes, floorWidthOffset * (panelSizes.Count - entranceIndexInterval - 1), floorHeight, entranceIndexInterval + 1, panelSizes.Count, config));

            return horizontal;
        }

        private VerticalLayout CreateNormalFacadeVertical(List<PanelSize> panelSizes, float remainderWidth, float floorHeight, int from, int to, BuildingGenerator.Config config)
        {
            int numFloorWithoutEntrance = (int)Mathf.Floor(config.buildingHeight / floorHeight) - 1;
            float entranceHeight = config.buildingHeight - numFloorWithoutEntrance * floorHeight;
            float floorWidthOffset = remainderWidth / (to - from);

            var vertical = new VerticalLayout { CreateHorizontal(panelSizes, from, to, entranceHeight, floorWidthOffset, m_Constructors[PanelType.k_FullWindow]) };

            float remainingHeight = config.buildingHeight - entranceHeight - k_SmallWallHeight;
            if (0 < remainingHeight)
            {
                vertical.Add(CreateHorizontal(panelSizes, from, to, k_SmallWallHeight, floorWidthOffset, m_Constructors[PanelType.k_CommercialWallWithFrame]));
                remainingHeight = config.buildingHeight - entranceHeight - k_SmallWallHeight - k_DepressionWallHeight;
                if (0 < remainingHeight)
                {
                    vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, k_DepressionWallHeight, floorWidthOffset, config));
                }
            }

            vertical.Add(CreateNormalFacadeVerticalIter(panelSizes, floorWidthOffset, entranceHeight, from, to, config));

            return vertical;
        }

        private VerticalLayout CreateEntranceVertical(List<PanelSize> panelSizes, float floorWidthOffset, float floorHeight, int entranceIndexInterval, bool noLeftLayout, bool noRightLayout, BuildingGenerator.Config config)
        {
            ProceduralFacadeElement.PositionType positionType = noLeftLayout switch
            {
                true when noRightLayout => ProceduralFacadeElement.PositionType.k_NoLeftRight,
                true => ProceduralFacadeElement.PositionType.k_NoLeft,
                false when noRightLayout => ProceduralFacadeElement.PositionType.k_NoRight,
                _ => ProceduralFacadeElement.PositionType.k_Middle
            };

            int numFloorWithoutEntrance = (int)Mathf.Floor(config.buildingHeight / floorHeight) - 1;
            float entranceHeight = config.buildingHeight - numFloorWithoutEntrance * floorHeight;
            var vertical = new VerticalLayout
            {
                Construct(new List<Func<ILayoutElement>>
                {
                    () => new ProceduralFacadeCompoundElements.ProceduralFullWindow(config)
                    {
                        m_WindowBottomOffset = 0,
                        m_WindowWidthOffset = 0,
                        m_WindowDepthOffset = 0,
                        m_WindowFrameRodHeight = 0.2f,
                        m_WindowFrameRodWidth = 0.2f,
                        m_NumCenterRods = 1,
                        m_HasWindowsill = false
                    }
                }, m_SizeValues[panelSizes[entranceIndexInterval]] + floorWidthOffset, k_EntranceWindowHeight),
                Construct(m_Constructors[PanelType.k_FullWindow], m_SizeValues[panelSizes[entranceIndexInterval]] + floorWidthOffset, entranceHeight - k_EntranceWindowHeight),
            };

            float remainingHeight = config.buildingHeight - entranceHeight - k_SmallWallHeight;
            if (0 < remainingHeight)
            {
                vertical.Add(Construct(m_Constructors[PanelType.k_CommercialWallWithFrame], m_SizeValues[panelSizes[entranceIndexInterval]] + floorWidthOffset, k_SmallWallHeight));
                remainingHeight = config.buildingHeight - entranceHeight - k_SmallWallHeight - k_DepressionWallHeight;
                if (0 < remainingHeight)
                {
                    vertical.Add(Construct(() => new ProceduralFacadeCompoundElements.ProceduralDepressionWall(config, positionType), m_SizeValues[panelSizes[entranceIndexInterval]] + floorWidthOffset, k_DepressionWallHeight));
                }
            }

            vertical.Add(CreateNormalFacadeVerticalIter(panelSizes, floorWidthOffset, entranceHeight, entranceIndexInterval, entranceIndexInterval + 1, config));

            return vertical;
        }

        private VerticalLayout CreateNormalFacadeVerticalIter(List<PanelSize> panelSizes, float floorWidthOffset, float entranceHeight, int from, int to, BuildingGenerator.Config config)
        {
            int i = 0;
            var vertical = new VerticalLayout();
            int switchIndex = 0;
            bool addedBoundaryWall = false;
            float remainingHeight = config.buildingHeight - entranceHeight - k_SmallWallHeight - k_DepressionWallHeight;
            float currentHeight = entranceHeight + k_SmallWallHeight + k_DepressionWallHeight;
            if (0 < remainingHeight)
            {
                while (0 < remainingHeight)
                {
                    if (entranceHeight <= remainingHeight)
                    {
                        if (addedBoundaryWall)
                        {
                            switch (switchIndex++ % 2)
                            {
                                case 0:
                                    remainingHeight -= config.complexBuildingParams.spandrelHeight;
                                    vertical.Add(remainingHeight < 0
                                        ? CreateHorizontal(panelSizes, from, to, config.complexBuildingParams.spandrelHeight + remainingHeight, floorWidthOffset, m_Constructors[PanelType.k_OfficeSmallFullWindow])
                                        : CreateHorizontal(panelSizes, from, to, config.complexBuildingParams.spandrelHeight, floorWidthOffset, m_Constructors[PanelType.k_OfficeSmallFullWindow]));
                                    break;
                                case 1:
                                    remainingHeight -= entranceHeight;
                                    vertical.Add(remainingHeight < 0
                                        ? CreateHorizontal(panelSizes, from, to, entranceHeight + remainingHeight, floorWidthOffset, m_Constructors[PanelType.k_FullWindow])
                                        : CreateHorizontal(panelSizes, from, to, entranceHeight, floorWidthOffset, m_Constructors[PanelType.k_FullWindow]));
                                    break;
                            }
                        }
                        else if (i++ % 4 == 3)
                        {
                            // 繋ぎ目未作成で境界線を超える場合
                            if (config.complexBuildingParams.buildingBoundaryHeight < currentHeight + k_SmallWindowHeight)
                            {
                                config.m_ComplexBuildingPlannerParams.m_AddedBoundaryWall = false;
                                addedBoundaryWall = true;
                                float smallWindowHeight = config.complexBuildingParams.buildingBoundaryHeight - currentHeight;
                                vertical.Add(CreateHorizontal(panelSizes, from, to, smallWindowHeight, floorWidthOffset, m_Constructors[PanelType.k_CommercialSmallFullWindow]));
                                if (smallWindowHeight + k_DepressionWallHeight <= remainingHeight)
                                {
                                    vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, k_DepressionWallHeight, floorWidthOffset, config));
                                    remainingHeight -= smallWindowHeight + k_DepressionWallHeight;
                                    currentHeight += smallWindowHeight + k_DepressionWallHeight;
                                }
                                else
                                {
                                    // DepressionWallの高さよりもremainingHeightが小さい
                                    vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, remainingHeight - smallWindowHeight, floorWidthOffset, config));
                                    remainingHeight = -1;
                                    currentHeight += remainingHeight;
                                }
                            }
                            // 複合ビル下部
                            else
                            {
                                vertical.Add(CreateHorizontal(panelSizes, from, to, k_SmallWindowHeight, floorWidthOffset, m_Constructors[PanelType.k_CommercialSmallFullWindow]));
                                remainingHeight -= k_SmallWindowHeight;
                                currentHeight += k_SmallWindowHeight;
                            }
                        }
                        else
                        {
                            // 繋ぎ目未作成で境界線を超える場合
                            if (config.complexBuildingParams.buildingBoundaryHeight < currentHeight + entranceHeight)
                            {
                                config.m_ComplexBuildingPlannerParams.m_AddedBoundaryWall = false;
                                addedBoundaryWall = true;
                                float wallWithFrameHeight = config.complexBuildingParams.buildingBoundaryHeight - currentHeight;
                                vertical.Add(CreateHorizontal(panelSizes, from, to, wallWithFrameHeight, floorWidthOffset, m_Constructors[PanelType.k_CommercialWallWithFrame]));
                                if (wallWithFrameHeight + k_DepressionWallHeight <= remainingHeight)
                                {
                                    vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, k_DepressionWallHeight, floorWidthOffset, config));
                                    remainingHeight -= wallWithFrameHeight + k_DepressionWallHeight;
                                    currentHeight += wallWithFrameHeight + k_DepressionWallHeight;
                                }
                                else
                                {
                                    // DepressionWallの高さよりもremainingHeightが小さい
                                    vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, remainingHeight - wallWithFrameHeight, floorWidthOffset, config));
                                    remainingHeight = -1;
                                    currentHeight += remainingHeight;
                                }
                            }
                            // 複合ビル下部
                            else
                            {
                                vertical.Add(CreateHorizontal(panelSizes, from, to, entranceHeight, floorWidthOffset, m_Constructors[PanelType.k_CommercialWallWithFrame]));
                                remainingHeight -= entranceHeight;
                                currentHeight += entranceHeight;
                            }
                        }
                    }
                    else
                    {
                        if (Geometry.Epsilon <= remainingHeight)
                        {
                            if (addedBoundaryWall)
                            {
                                switch (switchIndex++ % 2)
                                {
                                    case 0:
                                        remainingHeight -= config.complexBuildingParams.spandrelHeight;
                                        vertical.Add(remainingHeight < 0
                                            ? CreateHorizontal(panelSizes, from, to, config.complexBuildingParams.spandrelHeight + remainingHeight, floorWidthOffset, m_Constructors[PanelType.k_OfficeSmallFullWindow])
                                            : CreateHorizontal(panelSizes, from, to, config.complexBuildingParams.spandrelHeight, floorWidthOffset, m_Constructors[PanelType.k_OfficeSmallFullWindow]));
                                        break;
                                    case 1:
                                        remainingHeight -= entranceHeight;
                                        vertical.Add(remainingHeight < 0
                                            ? CreateHorizontal(panelSizes, from, to, entranceHeight + remainingHeight, floorWidthOffset, m_Constructors[PanelType.k_FullWindow])
                                            : CreateHorizontal(panelSizes, from, to, entranceHeight, floorWidthOffset, m_Constructors[PanelType.k_FullWindow]));
                                        break;
                                }
                            }
                            else if (config.complexBuildingParams.buildingBoundaryHeight < currentHeight + remainingHeight)
                            {
                                float wallWithFrameHeight = config.complexBuildingParams.buildingBoundaryHeight - currentHeight;
                                vertical.Add(CreateHorizontal(panelSizes, from, to, wallWithFrameHeight, floorWidthOffset, m_Constructors[PanelType.k_CommercialWallWithFrame]));
                                if (wallWithFrameHeight + k_DepressionWallHeight <= remainingHeight)
                                {
                                    vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, k_DepressionWallHeight, floorWidthOffset, config));
                                    vertical.Add(CreateHorizontal(panelSizes, from, to, remainingHeight - wallWithFrameHeight - k_DepressionWallHeight, floorWidthOffset, m_Constructors[PanelType.k_CommercialWallWithFrame]));
                                }
                                else
                                {
                                    // DepressionWallの高さよりもremainingHeightが小さい
                                    vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, remainingHeight - wallWithFrameHeight, floorWidthOffset, config));
                                }

                                // 上記処理で残りの高さを全て埋めている
                                remainingHeight = -1;
                                currentHeight += remainingHeight;
                            }
                            else
                            {
                                vertical.Add(CreateHorizontal(panelSizes, from, to, remainingHeight, floorWidthOffset, m_Constructors[PanelType.k_CommercialWallWithFrame]));

                                // 上記処理で残りの高さを全て埋めている
                                remainingHeight = -1;
                                currentHeight += remainingHeight;
                            }
                        }
                    }
                }
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

        private HorizontalLayout CreateDepressionWallNormalFacadeHorizontal(List<PanelSize> panelSizes, int from, int to, float height, float floorWidthOffset, BuildingGenerator.Config config)
        {
            var horizontal = new HorizontalLayout();
            for (int i = from; i < to; i++)
            {
                ProceduralFacadeElement.PositionType positionType;
                if (from == to - 1 && panelSizes.Count == 1)
                {
                    positionType = ProceduralFacadeElement.PositionType.k_NoLeftRight;
                }
                else if (i == 0)
                {
                    positionType = ProceduralFacadeElement.PositionType.k_NoLeft;
                }
                else if (i == to - 1 && to == panelSizes.Count)
                {
                    positionType = ProceduralFacadeElement.PositionType.k_NoRight;
                }
                else
                {
                    positionType = ProceduralFacadeElement.PositionType.k_Middle;
                }

                float panelWidth = m_SizeValues[panelSizes[i]] + floorWidthOffset;
                var balcony = new List<Func<ILayoutElement>>
                {
                    () => new ProceduralFacadeCompoundElements.ProceduralDepressionWall(config, positionType)
                };
                horizontal.Add(Construct(balcony, panelWidth, height));
            }
            return horizontal;
        }

        private ILayoutElement Construct(PanelType panelType, float width, float height)
        {
            return Construct(m_Constructors[panelType], width, height);
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
            k_Narrow
        }

        private enum PanelType : byte
        {
            k_ShadowWall,

            k_CommercialWallWithFrame,
            k_CommercialSmallFullWindow,

            k_OfficeSmallFullWindow,

            k_FullWindow,
        }
    }
}
