#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Sabresaurus.SabreCSG.Importers.Quake1
{
    /// <summary>
    /// Importer for Quake 1 Map Format (*.map) format.
    /// </summary>
    /// <remarks>Created by Henry de Jongh for SabreCSG.</remarks>
    public class MapImporter
    {
        /// <summary>
        /// Imports the specified Quake 1 Map Format file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A <see cref="MapWorld"/> containing the imported world data.</returns>
        public MapWorld Import(string path)
        {
            // create a new world.
            MapWorld world = new MapWorld();

            // open the file for reading. we use streams for additional performance.
            // it's faster than File.ReadAllLines() as that requires two iterations.
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(stream))
            {
                // read all the lines from the file.
                int depth = 0;
                string line;
                bool justEnteredClosure = false;
                string key;
                object value;
                MapBrush brush = null;
                MapEntity entity = null;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine().Trim();
                    if (line.Length == 0) continue;

                    // skip comments.
                    if (line[0] == '/') continue;

                    // parse closures and keep track of them.
                    if (line[0] == '{') { depth++; justEnteredClosure = true; continue; }
                    if (line[0] == '}') { depth--; continue; }

                    // parse entity.
                    if (depth == 1)
                    {
                        // create a new entity and add it to the world.
                        if (justEnteredClosure)
                        {
                            entity = new MapEntity();
                            world.Entities.Add(entity);
                        }

                        // parse entity properties.
                        if (TryParsekeyValue(line, out key, out value))
                        {
                            switch (key)
                            {
                                case "classname": entity.ClassName = (string)value; break;
                            }
                        }
                    }

                    // parse entity brush.
                    if (depth == 2)
                    {
                        // create a new brush and add it to the entity.
                        if (justEnteredClosure)
                        {
                            brush = new MapBrush();
                            entity.Brushes.Add(brush);
                        }

                        // parse brush sides.
                        MapBrushSide mapBrushSide;
                        if (TryParseBrushSide(line, out mapBrushSide))
                        {
                            brush.Sides.Add(mapBrushSide);
                        }
                    }

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
            // detect floating point value.
            if (rawvalue.Contains('.') && float.TryParse(rawvalue, out vf))
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

        /// <summary>
        /// Tries the parse a brush side line.
        /// </summary>
        /// <param name="line">The line to be parsed.</param>
        /// <param name="mapBrushSide">The map brush side or null.</param>
        /// <returns>True if successful else false.</returns>
        private bool TryParseBrushSide(string line, out MapBrushSide mapBrushSide)
        {
            mapBrushSide = new MapBrushSide();

            // detect brush side definition.
            if (line[0] == '(')
            {
                string[] values = line.Replace("(", "").Replace(")", "").Replace("  ", " ").Replace("  ", " ").Trim().Split(' ');
                if (values.Length != 15) return false;

                try
                {
                    MapVector3 p1 = new MapVector3(float.Parse(values[0], CultureInfo.InvariantCulture), float.Parse(values[1], CultureInfo.InvariantCulture), float.Parse(values[2], CultureInfo.InvariantCulture));
                    MapVector3 p2 = new MapVector3(float.Parse(values[3], CultureInfo.InvariantCulture), float.Parse(values[4], CultureInfo.InvariantCulture), float.Parse(values[5], CultureInfo.InvariantCulture));
                    MapVector3 p3 = new MapVector3(float.Parse(values[6], CultureInfo.InvariantCulture), float.Parse(values[7], CultureInfo.InvariantCulture), float.Parse(values[8], CultureInfo.InvariantCulture));
                    mapBrushSide.Plane = new MapPlane(p1, p2, p3);
                    mapBrushSide.Material = values[9];
                    mapBrushSide.Offset = new MapVector2(float.Parse(values[10], CultureInfo.InvariantCulture), float.Parse(values[11], CultureInfo.InvariantCulture));
                    mapBrushSide.Rotation = float.Parse(values[12], CultureInfo.InvariantCulture);
                    mapBrushSide.Scale = new MapVector2(float.Parse(values[13], CultureInfo.InvariantCulture), float.Parse(values[14], CultureInfo.InvariantCulture));
                }
                catch (Exception)
                {
                    throw new Exception("Encountered invalid brush side. The format of the map file must be slightly different, please open an issue on github if you think you did everything right.");
                }

                return true;
            }

            return false;
        }
    }
}

#endif