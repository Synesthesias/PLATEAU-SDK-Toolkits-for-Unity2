using ProceduralToolkit;
using System;
using UnityEngine;

namespace PlateauToolkit.Sandbox.Runtime.PlateauSandboxBuildingsLib.Buildings.Configs
{
    public abstract class ComplexBuildingConfig
    {
        public class BuildingPlannerParams
        {
            public BuildingType m_LowerFloorBuildingType = BuildingType.k_CommercialBuilding;
            public BuildingType m_HigherFloorBuildingType = BuildingType.k_OfficeBuilding;
            public bool m_AddedBoundaryWall = false;
        }

        [Serializable]
        public class Params
        {
            public float buildingBoundaryHeight = 15.0f;
            public float spandrelHeight = 1.25f;
        }

        [Serializable]
        public class VertexColorPalette
        {
            public Color commercialBuildingWallColor = ColorE.white;
            public Color commercialBuildingDepressionWallColor = ColorE.white;
            public Color commercialBuildingWindowFrameColor = ColorE.gray;
            public Color commercialBuildingWindowGlassColor = ColorE.white;

            public Color commercialBuildingRoofColor = (ColorE.gray/4).WithA(1);
            public Color commercialBuildingRoofSideColor = (ColorE.gray/4).WithA(1);

            public Color officeBuildingWallColor = ColorE.white;
            public Color officeBuildingWindowFrameColor = ColorE.gray;
            public Color officeBuildingWindowGlassColor = ColorE.white;
            public Color officeBuildingSpandrelColor = ColorE.white;

            public Color officeBuildingRoofColor = (ColorE.gray/4).WithA(1);
            public Color officeBuildingRoofSideColor = (ColorE.gray/4).WithA(1);
        }


        [Serializable]
        public class VertexColorMaterialPalette
        {
            public Material commercialBuildingVertexWall;
            public Material commercialBuildingVertexWindow;
            public Material commercialBuildingVertexRoof;

            public Material officeBuildingVertexWall;
            public Material officeBuildingVertexWindow;
            public Material officeBuildingVertexRoof;
        }

        [Serializable]
        public class MaterialPalette
        {
            public Material commercialBuildingWall;
            public Material commercialBuildingDepressionWall;
            public Material commercialBuildingWindowFrame;
            public Material commercialBuildingWindowGlass;

            public Material commercialBuildingRoof;
            public Material commercialBuildingRoofSide;

            public Material officeBuildingWall;
            public Material officeBuildingWindowFrame;
            public Material officeBuildingWindowGlass;
            public Material officeBuildingSpandrel;

            public Material officeBuildingRoof;
            public Material officeBuildingRoofSide;
        }
    }
}
