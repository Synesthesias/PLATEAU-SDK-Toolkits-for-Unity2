using System;
using System.Collections.Generic;
using UnityEngine;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings;
using PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings.Configs;
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

        private ComplexBuildingConfig.ComplexBuildingType GetComplexBuildingType (BuildingGenerator.Config config)
        {
            return config.m_ComplexBuildingPlannerParams.m_AddedBoundaryWall ? config.complexBuildingParams.higherFloorBuildingType : config.complexBuildingParams.lowerFloorBuildingType;
        }

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

                config.m_ComplexBuildingPlannerParams.m_AddedBoundaryWall = false;
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

            m_Constructors[PanelType.k_CommercialFullWindow] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralFullWindow(
                    config,
                    windowpaneFrameName: "CommercialBuildingFrameTextured")
                {
                    m_WindowFrameRodWidth = 0.2f
                }
            };

            m_Constructors[PanelType.k_CommercialSmallFullWindow] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralFullWindow(
                    config,
                    windowpaneFrameName: "CommercialBuildingFrameTextured")
                {
                    m_NumCenterRods = 1,
                    m_WindowFrameRodType = ProceduralFacadeElement.WindowFrameRodType.k_Vertical,
                    m_WindowFrameRodWidth = 0.05f
                }
            };

            m_Constructors[PanelType.k_CommercialWallWithFrame] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralWallWithFrame(
                    config,
                    windowpaneFrameName: "CommercialBuildingFrameTextured")
                {
                    m_NumCenterRods = 0
                }
            };

            m_Constructors[PanelType.k_OfficeFullWindow] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralFullWindow(
                    config,
                    windowpaneFrameName: "OfficeBuildingFrameTextured")
                {
                    m_WindowFrameRodWidth = 0.2f
                }
            };

            m_Constructors[PanelType.k_OfficeSmallFullWindow] = new List<Func<ILayoutElement>>
            {
                () => new ProceduralFacadeCompoundElements.ProceduralFullWindow(
                    config,
                    config.complexBuildingMaterialPalette.officeBuildingSpandrel,
                    "OfficeBuildingGlassSpandrelTextured",
                    "OfficeBuildingFrameTextured")
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

            // 列ごとに作成しているので初期化が必要
            config.m_ComplexBuildingPlannerParams.m_AddedBoundaryWall = false;
            horizontal.Add(CreateNormalFacadeVertical(panelSizes, floorWidthOffset * entranceIndexInterval, floorHeight, 0, entranceIndexInterval, config));

            // 列ごとに作成しているので初期化が必要
            config.m_ComplexBuildingPlannerParams.m_AddedBoundaryWall = false;
            horizontal.Add(CreateEntranceVertical(panelSizes, floorWidthOffset, floorHeight, entranceIndexInterval, 0 == entranceIndexInterval, entranceIndexInterval + 1 == panelSizes.Count, config));

            // 列ごとに作成しているので初期化が必要
            config.m_ComplexBuildingPlannerParams.m_AddedBoundaryWall = false;
            horizontal.Add(CreateNormalFacadeVertical(panelSizes, floorWidthOffset * (panelSizes.Count - entranceIndexInterval - 1), floorHeight, entranceIndexInterval + 1, panelSizes.Count, config));

            return horizontal;
        }

        private VerticalLayout CreateNormalFacadeVertical(List<PanelSize> panelSizes, float remainderWidth, float floorHeight, int from, int to, BuildingGenerator.Config config)
        {
            int numFloorWithoutEntrance = (int)Mathf.Floor(config.buildingHeight / floorHeight) - 1;
            float entranceHeight = config.buildingHeight - numFloorWithoutEntrance * floorHeight;
            float floorWidthOffset = remainderWidth / (to - from);
            ComplexBuildingConfig.ComplexBuildingType complexBuildingType = GetComplexBuildingType(config);
            var vertical = new VerticalLayout();
            switch (complexBuildingType)
            {
                case ComplexBuildingConfig.ComplexBuildingType.k_Apartment:
                    break;
                case ComplexBuildingConfig.ComplexBuildingType.k_OfficeBuilding:
                {
                    vertical.Add(CreateHorizontal(panelSizes, from, to, entranceHeight, floorWidthOffset, m_Constructors[PanelType.k_OfficeFullWindow]));
                    float remainingHeight = config.buildingHeight - entranceHeight;
                    float currentHeight = entranceHeight;
                    vertical.Add(CreateNormalFacadeVerticalIter(panelSizes, remainingHeight, currentHeight, floorWidthOffset, entranceHeight, from, to, config));
                    break;
                }
                case ComplexBuildingConfig.ComplexBuildingType.k_CommercialBuilding:
                {
                    vertical.Add(CreateHorizontal(panelSizes, from, to, entranceHeight, floorWidthOffset, m_Constructors[PanelType.k_CommercialFullWindow]));
                    float remainingHeight = config.buildingHeight - entranceHeight;
                    float currentHeight = entranceHeight;
                    if (0 < remainingHeight - k_SmallWallHeight)
                    {
                        remainingHeight -= k_SmallWallHeight;
                        currentHeight += k_SmallWallHeight;
                        vertical.Add(CreateHorizontal(panelSizes, from, to, k_SmallWallHeight, floorWidthOffset, m_Constructors[PanelType.k_CommercialWallWithFrame]));
                        if (0 < remainingHeight - k_DepressionWallHeight)
                        {
                            remainingHeight -= k_DepressionWallHeight;
                            currentHeight += k_DepressionWallHeight;
                            vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, k_DepressionWallHeight, floorWidthOffset, true, config));
                        }
                    }

                    vertical.Add(CreateNormalFacadeVerticalIter(panelSizes, remainingHeight, currentHeight, floorWidthOffset, entranceHeight, from, to, config));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return vertical;
        }

        private VerticalLayout CreateEntranceVertical(List<PanelSize> panelSizes, float floorWidthOffset, float floorHeight, int entranceIndexInterval, bool noLeftLayout, bool noRightLayout, BuildingGenerator.Config config)
        {
            ComplexBuildingConfig.ComplexBuildingType complexBuildingType = GetComplexBuildingType(config);
            var vertical = new VerticalLayout();
            switch (complexBuildingType)
            {
                case ComplexBuildingConfig.ComplexBuildingType.k_Apartment:
                    break;
                case ComplexBuildingConfig.ComplexBuildingType.k_OfficeBuilding:
                    vertical.Add(CreateOfficeEntranceVertical(panelSizes, floorWidthOffset, floorHeight, entranceIndexInterval, config));
                    break;
                case ComplexBuildingConfig.ComplexBuildingType.k_CommercialBuilding:
                    vertical.Add(CreateCommercialEntranceVertical(panelSizes, floorWidthOffset, floorHeight, entranceIndexInterval, noLeftLayout, noRightLayout, config));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return vertical;
        }

        private VerticalLayout CreateOfficeEntranceVertical(List<PanelSize> panelSizes, float floorWidthOffset, float floorHeight, int entranceIndexInterval, BuildingGenerator.Config config)
        {
            int numFloorWithoutEntrance = (int)Mathf.Floor(config.buildingHeight / floorHeight) - 1;
            float entranceHeight = config.buildingHeight - numFloorWithoutEntrance * floorHeight;
            var vertical = new VerticalLayout
            {
                Construct(new List<Func<ILayoutElement>>
                {
                    () => new ProceduralFacadeCompoundElements.ProceduralFullWindow(
                        config,
                        windowpaneFrameName: "OfficeBuildingFrameTextured")
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
                Construct(m_Constructors[PanelType.k_OfficeFullWindow], m_SizeValues[panelSizes[entranceIndexInterval]] + floorWidthOffset, entranceHeight - k_EntranceWindowHeight),
            };

            float remainingHeight = config.buildingHeight - entranceHeight;
            float currentHeight = entranceHeight;
            vertical.Add(CreateNormalFacadeVerticalIter(panelSizes, remainingHeight, currentHeight, floorWidthOffset, entranceHeight, entranceIndexInterval, entranceIndexInterval + 1, config));

            return vertical;
        }

        private VerticalLayout CreateCommercialEntranceVertical(List<PanelSize> panelSizes, float floorWidthOffset, float floorHeight, int entranceIndexInterval, bool noLeftLayout, bool noRightLayout, BuildingGenerator.Config config)
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
                    () => new ProceduralFacadeCompoundElements.ProceduralFullWindow(
                        config,
                        windowpaneFrameName: "CommercialBuildingFrameTextured")
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
                Construct(m_Constructors[PanelType.k_CommercialFullWindow], m_SizeValues[panelSizes[entranceIndexInterval]] + floorWidthOffset, entranceHeight - k_EntranceWindowHeight),
            };

            float remainingHeight = config.buildingHeight - entranceHeight;
            float currentHeight = entranceHeight;
            if (0 < remainingHeight - k_SmallWallHeight)
            {
                vertical.Add(Construct(m_Constructors[PanelType.k_CommercialWallWithFrame], m_SizeValues[panelSizes[entranceIndexInterval]] + floorWidthOffset, k_SmallWallHeight));
                remainingHeight -= k_SmallWallHeight;
                currentHeight += k_SmallWallHeight;
                if (0 < remainingHeight - k_DepressionWallHeight)
                {
                    remainingHeight -= k_DepressionWallHeight;
                    currentHeight += k_DepressionWallHeight;
                    vertical.Add(Construct(() => new ProceduralFacadeCompoundElements.ProceduralDepressionWall(config, positionType), m_SizeValues[panelSizes[entranceIndexInterval]] + floorWidthOffset, k_DepressionWallHeight));
                }
            }
            vertical.Add(CreateNormalFacadeVerticalIter(panelSizes, remainingHeight, currentHeight, floorWidthOffset, entranceHeight, entranceIndexInterval, entranceIndexInterval + 1, config));

            return vertical;
        }

        private VerticalLayout CreateNormalFacadeVerticalIter(List<PanelSize> panelSizes, float remainingHeight, float currentHeight, float floorWidthOffset, float entranceHeight, int from, int to, BuildingGenerator.Config config)
        {
            var vertical = new VerticalLayout();
            int apartmentFloorIndex = 0;
            int officeFloorIndex = 0;
            int commercialFloorIndex = 0;
            bool addedBoundaryWall = false;
            if (0 < remainingHeight)
            {
                while (0 < remainingHeight)
                {
                    ComplexBuildingConfig.ComplexBuildingType complexBuildingType = GetComplexBuildingType(config);
                    (int, bool, float, float) tupleData;
                    switch (complexBuildingType)
                    {
                        case ComplexBuildingConfig.ComplexBuildingType.k_Apartment:
                            tupleData = CreateBuildingPlanner(vertical, complexBuildingType, apartmentFloorIndex, addedBoundaryWall, currentHeight, remainingHeight, panelSizes, floorWidthOffset, entranceHeight, from, to, config);
                            apartmentFloorIndex = tupleData.Item1;
                            break;
                        case ComplexBuildingConfig.ComplexBuildingType.k_OfficeBuilding:
                            tupleData = CreateBuildingPlanner(vertical, complexBuildingType, officeFloorIndex, addedBoundaryWall, currentHeight, remainingHeight, panelSizes, floorWidthOffset, entranceHeight, from, to, config);
                            officeFloorIndex = tupleData.Item1;
                            break;
                        case ComplexBuildingConfig.ComplexBuildingType.k_CommercialBuilding:
                            tupleData = CreateBuildingPlanner(vertical, complexBuildingType, commercialFloorIndex, addedBoundaryWall, currentHeight, remainingHeight, panelSizes, floorWidthOffset, entranceHeight, from, to, config);
                            commercialFloorIndex = tupleData.Item1;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    addedBoundaryWall = tupleData.Item2;
                    currentHeight = tupleData.Item3;
                    remainingHeight = tupleData.Item4;

                    // (int, bool, float, float) tupleData = CreateCommercialBuildingPlanner(vertical, complexBuildingType, commercialFloorIndex, addedBoundaryWall, currentHeight, remainingHeight, panelSizes, floorWidthOffset, entranceHeight, from, to, config);
                    // commercialFloorIndex = tupleData.Item1;
                    // addedBoundaryWall = tupleData.Item2;
                    // currentHeight = tupleData.Item3;
                    // remainingHeight = tupleData.Item4;

                    // switch (complexBuildingType)
                    // {
                    //     case ComplexBuildingConfig.ComplexBuildingType.k_Apartment:
                    //         break;
                    //     case ComplexBuildingConfig.ComplexBuildingType.k_OfficeBuilding:
                    //     {
                    //         // (int, bool, float, float) tupleData = CreateOfficeBuildingPlanner(vertical, complexBuildingType,officeFloorIndex, addedBoundaryWall, currentHeight, remainingHeight, panelSizes, floorWidthOffset, entranceHeight, from, to, config);
                    //         (int, bool, float, float) tupleData = CreateBuildingPlanner(vertical, complexBuildingType, officeFloorIndex, addedBoundaryWall, currentHeight, remainingHeight, panelSizes, floorWidthOffset, entranceHeight, from, to, config);
                    //         officeFloorIndex = tupleData.Item1;
                    //         addedBoundaryWall = tupleData.Item2;
                    //         currentHeight = tupleData.Item3;
                    //         remainingHeight = tupleData.Item4;
                    //         break;
                    //     }
                    //     case ComplexBuildingConfig.ComplexBuildingType.k_CommercialBuilding:
                    //     {
                    //
                    //         break;
                    //     }
                    //     default:
                    //         throw new ArgumentOutOfRangeException();
                    // }
                }
            }

            return vertical;
        }
        //
        // private (int, bool, float, float) CreateOfficeBuildingPlanner(VerticalLayout vertical, int floorIndex, bool addedBoundaryWall, float currentHeight, float remainingHeight, List<PanelSize> panelSizes, float floorWidthOffset, float entranceHeight, int from, int to, BuildingGenerator.Config config)
        // {
        //     PanelType panelType = floorIndex % 2 == 0 ? PanelType.k_OfficeSmallFullWindow : PanelType.k_OfficeFullWindow;
        //     float panelHeight = floorIndex % 2 == 0 ? config.complexBuildingParams.spandrelHeight : entranceHeight;
        //
        //     if (entranceHeight <= remainingHeight)
        //     {
        //         if (addedBoundaryWall)
        //         {
        //             config.m_ComplexBuildingPlannerParams.m_AddedBoundaryWall = true;
        //             vertical.Add(CreateHorizontal(panelSizes, from, to, panelHeight, floorWidthOffset, m_Constructors[panelType]));
        //             remainingHeight -= panelHeight;
        //             currentHeight += panelHeight;
        //         }
        //         // 繋ぎ目未作成で境界線を超える場合
        //         else if (config.complexBuildingParams.buildingBoundaryHeight < currentHeight + panelHeight)
        //         {
        //             addedBoundaryWall = true;
        //             float remainingLowerBuildingHeight = config.complexBuildingParams.buildingBoundaryHeight - currentHeight;
        //             vertical.Add(CreateHorizontal(panelSizes, from, to, remainingLowerBuildingHeight, floorWidthOffset, m_Constructors[panelType]));
        //
        //             if (remainingLowerBuildingHeight + k_DepressionWallHeight <= remainingHeight)
        //             {
        //                 // 境界線の上に配置されるDepressionWallの高さよりもremainingHeightが大きい
        //                 vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, k_DepressionWallHeight, floorWidthOffset, false, config));
        //                 remainingHeight -= remainingLowerBuildingHeight + k_DepressionWallHeight;
        //                 currentHeight += remainingLowerBuildingHeight + k_DepressionWallHeight;
        //             }
        //             else
        //             {
        //                 // 境界線の上に配置されるDepressionWallの高さよりもremainingHeightが小さい
        //                 vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, remainingHeight - remainingLowerBuildingHeight, floorWidthOffset, false, config));
        //                 remainingHeight = -1;
        //                 currentHeight += remainingHeight;
        //             }
        //         }
        //         else
        //         {
        //             vertical.Add(CreateHorizontal(panelSizes, from, to, panelHeight, floorWidthOffset, m_Constructors[panelType]));
        //             remainingHeight -= panelHeight;
        //             currentHeight += panelHeight;
        //         }
        //     }
        //     else
        //     {
        //         if (addedBoundaryWall)
        //         {
        //             config.m_ComplexBuildingPlannerParams.m_AddedBoundaryWall = true;
        //             remainingHeight -= panelHeight;
        //             currentHeight += panelHeight;
        //             vertical.Add(remainingHeight < 0
        //                 ? CreateHorizontal(panelSizes, from, to, panelHeight + remainingHeight, floorWidthOffset, m_Constructors[panelType])
        //                 : CreateHorizontal(panelSizes, from, to, panelHeight, floorWidthOffset, m_Constructors[panelType]));
        //         }
        //         else if (config.complexBuildingParams.buildingBoundaryHeight < currentHeight + remainingHeight)
        //         {
        //             // float wallWithFrameHeight = config.complexBuildingParams.buildingBoundaryHeight - currentHeight;
        //             // vertical.Add(CreateHorizontal(panelSizes, from, to, wallWithFrameHeight, floorWidthOffset,
        //             //     m_Constructors[PanelType.k_CommercialWallWithFrame]));
        //             // if (wallWithFrameHeight + k_DepressionWallHeight <= remainingHeight)
        //             // {
        //             //     // DepressionWallの高さよりもremainingHeightが高い
        //             //     vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, k_DepressionWallHeight, floorWidthOffset,
        //             //         false, config));
        //             //
        //             //     // config.m_ComplexBuildingPlannerParams.m_AddedBoundaryWall = true;
        //             //     addedBoundaryWall = true;
        //             //     vertical.Add(CreateHorizontal(panelSizes, from, to, remainingHeight - wallWithFrameHeight - k_DepressionWallHeight,
        //             //         floorWidthOffset, m_Constructors[PanelType.k_OfficeSmallFullWindow]));
        //             // }
        //             // else
        //             // {
        //             //     // DepressionWallの高さよりもremainingHeightが低い
        //             //     vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, remainingHeight - wallWithFrameHeight,
        //             //         floorWidthOffset, false, config));
        //             // }
        //
        //             // 上記処理で残りの高さを全て埋めている
        //             // remainingHeight = -1;
        //             // currentHeight += remainingHeight;
        //         }
        //         else
        //         {
        //             // vertical.Add(CreateHorizontal(panelSizes, from, to, remainingHeight, floorWidthOffset,
        //             //     m_Constructors[PanelType.k_CommercialWallWithFrame]));
        //             //
        //             // // 上記処理で残りの高さを全て埋めている
        //             // remainingHeight = -1;
        //             // currentHeight += remainingHeight;
        //         }
        //     }
        //
        //     return (floorIndex, addedBoundaryWall, currentHeight, remainingHeight);
        // }

        private (int, bool, float, float) CreateBuildingPlanner(VerticalLayout vertical, ComplexBuildingConfig.ComplexBuildingType complexBuildingType, int floorIndex, bool addedBoundaryWall, float currentHeight, float remainingHeight, List<PanelSize> panelSizes, float floorWidthOffset, float entranceHeight, int from, int to, BuildingGenerator.Config config)
        {
            PanelType panelType;
            float panelHeight;
            switch (complexBuildingType)
            {
                case ComplexBuildingConfig.ComplexBuildingType.k_Apartment:
                    panelType = floorIndex % 4 == 3 ? PanelType.k_CommercialSmallFullWindow : PanelType.k_CommercialWallWithFrame;
                    panelHeight = floorIndex % 4 == 3 ? k_SmallWindowHeight : entranceHeight;
                    break;
                case ComplexBuildingConfig.ComplexBuildingType.k_OfficeBuilding:
                    panelType = floorIndex % 2 == 0 ? PanelType.k_OfficeSmallFullWindow : PanelType.k_OfficeFullWindow;
                    panelHeight = floorIndex % 2 == 0 ? config.complexOfficeBuildingParams.spandrelHeight : entranceHeight;
                    break;
                case ComplexBuildingConfig.ComplexBuildingType.k_CommercialBuilding:
                    panelType = floorIndex % 4 == 3 ? PanelType.k_CommercialSmallFullWindow : PanelType.k_CommercialWallWithFrame;
                    panelHeight = floorIndex % 4 == 3 ? k_SmallWindowHeight : entranceHeight;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(complexBuildingType), complexBuildingType, null);
            }

            if (entranceHeight < remainingHeight)
            {
                if (addedBoundaryWall)
                {
                    vertical.Add(CreateHorizontal(panelSizes, from, to, panelHeight, floorWidthOffset, m_Constructors[panelType]));
                    remainingHeight -= panelHeight;
                    currentHeight += panelHeight;
                }
                // 繋ぎ目未作成で境界線を超える場合
                else if (config.complexBuildingParams.buildingBoundaryHeight < currentHeight + panelHeight)
                {
                    addedBoundaryWall = true;
                    float remainingLowerBuildingHeight = config.complexBuildingParams.buildingBoundaryHeight - currentHeight;
                    vertical.Add(CreateHorizontal(panelSizes, from, to, remainingLowerBuildingHeight, floorWidthOffset, m_Constructors[panelType]));

                    if (remainingLowerBuildingHeight + k_DepressionWallHeight <= remainingHeight)
                    {
                        // 境界線の上に配置されるDepressionWallの高さよりもremainingHeightが大きい
                        vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, k_DepressionWallHeight, floorWidthOffset, false, config));
                        remainingHeight -= remainingLowerBuildingHeight + k_DepressionWallHeight;
                        currentHeight += remainingLowerBuildingHeight + k_DepressionWallHeight;
                    }
                    else
                    {
                        // 境界線の上に配置されるDepressionWallの高さよりもremainingHeightが小さい
                        vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, remainingHeight - remainingLowerBuildingHeight, floorWidthOffset, false, config));
                        remainingHeight = -1;
                        currentHeight += remainingHeight;
                    }
                }
                else
                {
                    vertical.Add(CreateHorizontal(panelSizes, from, to, panelHeight, floorWidthOffset, m_Constructors[panelType]));
                    remainingHeight -= panelHeight;
                    currentHeight += panelHeight;
                }
            }
            else
            {
                if (addedBoundaryWall)
                {
                    remainingHeight -= panelHeight;
                    currentHeight += panelHeight;
                    vertical.Add(remainingHeight < 0
                        ? CreateHorizontal(panelSizes, from, to, panelHeight + remainingHeight, floorWidthOffset, m_Constructors[panelType])
                        : CreateHorizontal(panelSizes, from, to, panelHeight, floorWidthOffset, m_Constructors[panelType]));
                }
                else if (config.complexBuildingParams.buildingBoundaryHeight < currentHeight + remainingHeight)
                {
                    // 境界線までの高さ分を埋める
                    float remainingLowerBuildingHeight = config.complexBuildingParams.buildingBoundaryHeight - currentHeight;
                    vertical.Add(CreateHorizontal(panelSizes, from, to, remainingLowerBuildingHeight, floorWidthOffset, m_Constructors[panelType]));

                    if (remainingLowerBuildingHeight + k_DepressionWallHeight <= remainingHeight)
                    {
                        // 境界線の上に配置されるDepressionWallの高さよりもremainingHeightが高い（残りの高さは上部のタイプで埋める）
                        addedBoundaryWall = true;
                        vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, k_DepressionWallHeight, floorWidthOffset, false, config));
                        remainingHeight -= k_DepressionWallHeight;
                        currentHeight += k_DepressionWallHeight;
                    }
                    else
                    {
                        // 境界線の上に配置されるDepressionWallの高さよりもremainingHeightが低い（残りの高さを全て埋める）
                        vertical.Add(CreateDepressionWallNormalFacadeHorizontal(panelSizes, from, to, remainingHeight - remainingLowerBuildingHeight, floorWidthOffset, false, config));
                        remainingHeight = -1;
                        currentHeight += remainingHeight;
                    }
                }
                else
                {
                    if (0 < remainingHeight - panelHeight)
                    {
                        vertical.Add(CreateHorizontal(panelSizes, from, to, panelHeight, floorWidthOffset, m_Constructors[panelType]));
                        remainingHeight -= panelHeight;
                        currentHeight += panelHeight;
                    }
                    else
                    {
                        // 残りの高さを埋める
                        vertical.Add(CreateHorizontal(panelSizes, from, to, remainingHeight, floorWidthOffset, m_Constructors[panelType]));
                        remainingHeight = -1;
                        currentHeight += remainingHeight;
                    }
                }
            }

            floorIndex++;
            config.m_ComplexBuildingPlannerParams.m_AddedBoundaryWall = addedBoundaryWall;
            return (floorIndex, addedBoundaryWall, currentHeight, remainingHeight);
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

        private HorizontalLayout CreateDepressionWallNormalFacadeHorizontal(List<PanelSize> panelSizes, int from, int to, float height, float floorWidthOffset, bool isEntrace, BuildingGenerator.Config config)
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
                if (isEntrace)
                {
                    var depressionWall = new List<Func<ILayoutElement>>
                    {
                        () => new ProceduralFacadeCompoundElements.ProceduralDepressionWall(config, positionType)
                        {
                            m_DepressionWallDepth = 0.3f
                        }
                    };
                    horizontal.Add(Construct(depressionWall, panelWidth, height));
                }
                else
                {
                    var depressionWall = new List<Func<ILayoutElement>>
                    {
                        () => new ProceduralFacadeCompoundElements.ProceduralDepressionWall(config, positionType)
                        {
                            m_DepressionWallName = "BoundaryWallTextured",
                            m_DepressionWallDepth = 0f,
                            m_DepressionWallMat = config.complexBuildingMaterialPalette.boundaryWall,
                            m_DepressionWallColor = config.complexBuildingVertexColorPalette.boundaryWallColor,
                        }
                    };
                    horizontal.Add(Construct(depressionWall, panelWidth, height));
                }
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

            k_CommercialFullWindow,
            k_CommercialWallWithFrame,
            k_CommercialSmallFullWindow,

            k_OfficeFullWindow,
            k_OfficeSmallFullWindow,
        }
    }
}
