#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Sabresaurus.SabreCSG.Importers.ValveMapFormat2006
{
    /// <summary>
    /// Importer for Valve Map Format (*.vmf) format.
    /// </summary>
    /// <remarks>Created by Henry de Jongh for SabreCSG.</remarks>
    public class VmfImporter
    {
        /// <summary>
        /// Imports the specified Valve Map Format file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A <see cref="VmfWorld"/> containing the imported world data.</returns>
        public VmfWorld Import(string path)
        {
            // create a new world.
            VmfWorld world = new VmfWorld();

            // open the file for reading. we use streams for additional performance.
            // it's faster than File.ReadAllLines() as that requires two iterations.
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(stream))
            {
                // read all the lines from the file.
                //bool inActor = false; T3dActor actor = null;
                //bool inBrush = false; T3dBrush brush = null;
                //bool inPolygon = false; T3dPolygon polygon = null;
                string[] closures = new string[64];
                int depth = 0;
                string line;
                string previousLine = "";
                bool justEnteredClosure = false;
                string key;
                object value;
                VmfSolid solid = null;
                VmfSolidSide solidSide = null;
                VmfEntity entity = null;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine().Trim();
                    if (line.Length == 0) continue;

                    // parse closures and keep track of them.
                    if (line[0] == '{') { closures[depth] = previousLine; depth++; justEnteredClosure = true; continue; }
                    if (line[0] == '}') { depth--; closures[depth] = null; continue; }

                    // parse version info.
                    if (closures[0] == "versioninfo")
                    {
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "editorversion": world.VersionInfoEditorVersion = (int)value; break;
                                case "editorbuild": world.VersionInfoEditorBuild = (int)value; break;
                                case "mapversion": world.VersionInfoMapVersion = (int)value; break;
                                case "formatversion": world.VersionInfoFormatVersion = (int)value; break;
                                case "prefab": world.VersionInfoPrefab = (int)value; break;
                            }
                        }
                    }

                    // parse view settings.
                    if (closures[0] == "viewsettings")
                    {
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "bSnapToGrid": world.ViewSettingsSnapToGrid = (int)value; break;
                                case "bShowGrid": world.ViewSettingsShowGrid = (int)value; break;
                                case "bShowLogicalGrid": world.ViewSettingsShowLogicalGrid = (int)value; break;
                                case "nGridSpacing": world.ViewSettingsGridSpacing = (int)value; break;
                                case "bShow3DGrid": world.ViewSettingsShow3DGrid = (int)value; break;
                            }
                        }
                    }

                    // parse world properties.
                    if (closures[0] == "world" && closures[1] == null)
                    {
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "id": world.Id = (int)value; break;
                                case "mapversion": world.MapVersion = (int)value; break;
                                case "classname": world.ClassName = (string)value; break;
                                case "detailmaterial": world.DetailMaterial = (string)value; break;
                                case "detailvbsp": world.DetailVBsp = (string)value; break;
                                case "maxpropscreenwidth": world.MaxPropScreenWidth = (int)value; break;
                                case "skyname": world.SkyName = (string)value; break;
                            }
                        }
                    }

                    // parse world solid.
                    if (closures[0] == "world" && closures[1] == "solid" && closures[2] == null)
                    {
                        // create a new solid and add it to the world.
                        if (justEnteredClosure)
                        {
                            solid = new VmfSolid();
                            world.Solids.Add(solid);
                        }

                        // parse solid properties.
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "id": solid.Id = (int)value; break;
                            }
                        }
                    }

                    // parse world solid side.
                    if (closures[0] == "world" && closures[1] == "solid" && closures[2] == "side" && closures[3] == null)
                    {
                        // create a new solid side and add it to the solid.
                        if (justEnteredClosure)
                        {
                            solidSide = new VmfSolidSide();
                            solid.Sides.Add(solidSide);
                        }

                        // parse solid side properties.
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "id": solidSide.Id = (int)value; break;
                                case "plane": solidSide.Plane = (VmfPlane)value; break;
                                case "material": solidSide.Material = (string)value; break;
                                //case "rotation": solidSide.Rotation = (float)value; break;
                                case "uaxis": solidSide.UAxis = (VmfAxis)value; break;
                                case "vaxis": solidSide.VAxis = (VmfAxis)value; break;
                                case "lightmapscale": solidSide.LightmapScale = (int)value; break;
                                case "smoothing_groups": solidSide.SmoothingGroups = (int)value; break;
                            }
                        }
                    }

                    // HACK: detect displacements.
                    if (closures[0] == "world" && closures[1] == "solid" && closures[2] == "side" && closures[3] == "dispinfo")
                    {
                        solidSide.HasDisplacement = true;
                    }

                    // parse entity.
                    if (closures[0] == "entity" && closures[1] == null)
                    {
                        // create a new entity and add it to the world.
                        if (justEnteredClosure)
                        {
                            entity = new VmfEntity();
                            world.Entities.Add(entity);
                        }

                        // parse entity properties.
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "id": entity.Id = (int)value; break;
                                case "classname": entity.ClassName = (string)value; break;
                            }
                        }
                    }

                    // parse entity solid.
                    if (closures[0] == "entity" && closures[1] == "solid" && closures[2] == null)
                    {
                        // create a new solid and add it to the entity.
                        if (justEnteredClosure)
                        {
                            solid = new VmfSolid();
                            entity.Solids.Add(solid);
                        }

                        // parse solid properties.
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "id": solid.Id = (int)value; break;
                            }
                        }
                    }

                    // parse entity solid side.
                    if (closures[0] == "entity" && closures[1] == "solid" && closures[2] == "side" && closures[3] == null)
                    {
                        // create a new solid side and add it to the solid.
                        if (justEnteredClosure)
                        {
                            solidSide = new VmfSolidSide();
                            solid.Sides.Add(solidSide);
                        }

                        // parse solid side properties.
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "id": solidSide.Id = (int)value; break;
                                case "plane": solidSide.Plane = (VmfPlane)value; break;
                                case "material": solidSide.Material = (string)value; break;
                                //case "rotation": solidSide.Rotation = (float)value; break;
                                case "uaxis": solidSide.UAxis = (VmfAxis)value; break;
                                case "vaxis": solidSide.VAxis = (VmfAxis)value; break;
                                case "lightmapscale": solidSide.LightmapScale = (int)value; break;
                                case "smoothing_groups": solidSide.SmoothingGroups = (int)value; break;
                            }
                        }
                    }

                    previousLine = line;
                    justEnteredClosure = false;
                }
            }

            return world;
        }

        /// <summary>
        /// Tries to parse a key value line.
        /// </summary>
        /// <param name="line">The line (e.g. '"editorversion" "400"').</param>
        /// <param name="key">The key that was found.</param>
        /// <param name="value">The value that was found.</param>
        /// <returns>True if successful else false.</returns>
        private bool TryParsekeyValue(string line, out string key, out object value)
        {
            key = "";
            value = null;

            if (!line.Contains('"')) return false;
            int idx = line.IndexOf('"', 1);

            key = line.Substring(1, idx - 1);
            string rawvalue = line.Substring(idx + 3, line.Length - idx - 4);
            if (rawvalue.Length == 0) return false;

            int vi;
            float vf;
            // detect plane definition.
            if (rawvalue[0] == '(')
            {
                string[] values = rawvalue.Replace("(", "").Replace(")", "").Split(' ');
                VmfVector3 p1 = new VmfVector3(float.Parse(values[0], CultureInfo.InvariantCulture), float.Parse(values[1], CultureInfo.InvariantCulture), float.Parse(values[2], CultureInfo.InvariantCulture));
                VmfVector3 p2 = new VmfVector3(float.Parse(values[3], CultureInfo.InvariantCulture), float.Parse(values[4], CultureInfo.InvariantCulture), float.Parse(values[5], CultureInfo.InvariantCulture));
                VmfVector3 p3 = new VmfVector3(float.Parse(values[6], CultureInfo.InvariantCulture), float.Parse(values[7], CultureInfo.InvariantCulture), float.Parse(values[8], CultureInfo.InvariantCulture));
                value = new VmfPlane(p1, p2, p3);
                return true;
            }
            // detect uv definition.
            else if (rawvalue[0] == '[')
            {
                string[] values = rawvalue.Replace("[", "").Replace("]", "").Split(' ');
                value = new VmfAxis(new VmfVector3(float.Parse(values[0], CultureInfo.InvariantCulture), float.Parse(values[1], CultureInfo.InvariantCulture), float.Parse(values[2], CultureInfo.InvariantCulture)), float.Parse(values[3], CultureInfo.InvariantCulture), float.Parse(values[4], CultureInfo.InvariantCulture));
                return true;
            }
            // detect floating point value.
            else if (rawvalue.Contains('.') && float.TryParse(rawvalue, out vf))
            {
                value = vf;
                return true;
            }
            // detect integer value.
            else if (Int32.TryParse(rawvalue, out vi))
            {
                value = vi;
                return true;
            }
            // probably a string value.
            else
            {
                value = rawvalue;
                return true;
            }
        }
    }
}

#endif