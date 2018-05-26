#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sabresaurus.SabreCSG.Importers.ValveMapFormat2006
{
    /// <summary>
    /// Represents a Hammer World.
    /// </summary>
    public class VmfWorld
    {
        public int VersionInfoEditorVersion = -1;
        public int VersionInfoEditorBuild = -1;
        public int VersionInfoMapVersion = -1;
        public int VersionInfoFormatVersion = -1;
        public int VersionInfoPrefab = -1;

        public int ViewSettingsSnapToGrid = -1;
        public int ViewSettingsShowGrid = -1;
        public int ViewSettingsShowLogicalGrid = -1;
        public int ViewSettingsGridSpacing = -1;
        public int ViewSettingsShow3DGrid = -1;

        public int Id = -1;
        public int MapVersion = -1;
        public string ClassName = "";
        public string DetailMaterial = "";
        public string DetailVBsp = "";
        public int MaxPropScreenWidth = -1;
        public string SkyName = "";

        /// <summary>
        /// The solids in the world.
        /// </summary>
        public List<VmfSolid> Solids = new List<VmfSolid>();

        /// <summary>
        /// The entities in the world.
        /// </summary>
        public List<VmfEntity> Entities = new List<VmfEntity>();
    }
}

#endif