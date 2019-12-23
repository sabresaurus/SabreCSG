#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Sabresaurus.SabreCSG.Importers.UnrealGold
{
    /// <summary>
    /// Importer for Unreal Gold Text (*.t3d) format. Unreal Editor -&gt; File -&gt; Export.
    /// </summary>
    /// <remarks>Created by Henry de Jongh for SabreCSG.</remarks>
    public class T3dImporter
    {
        /// <summary>
        /// Imports the specified Unreal Text file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A <see cref="T3dMap"/> containing the imported map data.</returns>
        public T3dMap Import(string path)
        {
            // create a new map.
            T3dMap map = new T3dMap();

            // open the file for reading. we use streams for additional performance.
            // it's faster than File.ReadAllLines() as that requires two iterations.
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(stream))
            {
                // read all the lines from the file.
                bool inActor = false; T3dActor actor = null;
                bool inBrush = false; T3dBrush brush = null;
                bool inPolygon = false; T3dPolygon polygon = null;
                string line;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine().Trim();

                    // if we currently parsing an actor:
                    if (inActor)
                    {
                        if (!inBrush)
                        {
                            // if we done parsing the actor:
                            if (line == "End Actor")
                            {
                                inActor = false;
                                continue;
                            }

                            // try parsing an actor property.
                            string key;
                            object value;
                            if (TryParseProperty(line, out key, out value))
                                actor.Properties.Add(key, value);

                            // make sure we detect brush declarations:
                            if (line.StartsWith("Begin Brush"))
                            {
                                // read the properties of the brush.
                                var properties = ParseKeyValuePairs(line);
                                if (properties.ContainsKey("Name"))
                                {
                                    // this is a valid brush model that can be parsed further.
                                    inBrush = true;
                                    // create a new brush model and add it to the map.
                                    brush = new T3dBrush(properties["Name"]);
                                    map.BrushModels.Add(brush);
                                    continue;
                                }
                            }
                        }
                        // we are currently parsing a brush:
                        else
                        {
                            // if we done parsing the brush:
                            if (line == "End Brush")
                            {
                                inBrush = false;
                                continue;
                            }

                            if (!inPolygon)
                            {
                                // make sure we detect brush polygon declarations:
                                if (line.StartsWith("Begin Polygon"))
                                {
                                    inPolygon = true;
                                    // create a new brush polygon and add it to the brush.
                                    polygon = new T3dPolygon();
                                    brush.Polygons.Add(polygon);
                                    // read the properties of the brush polygon.
                                    var properties = ParseKeyValuePairs(line);
                                    if (properties.ContainsKey("Item"))
                                        polygon.Item = properties["Item"];
                                    if (properties.ContainsKey("Texture"))
                                        polygon.Texture = properties["Texture"];
                                    if (properties.ContainsKey("Flags"))
                                        polygon.Flags = (T3dPolygonFlags)Int32.Parse(properties["Flags"]);
                                }
                            }
                            // we are currently parsing a brush polygon:
                            else
                            {
                                // if we done parsing the brush polygon:
                                if (line == "End Polygon")
                                {
                                    inPolygon = false;
                                    continue;
                                }

                                if (line.StartsWith("Origin"))
                                    polygon.Origin = ParsePolygonVector(line);
                                if (line.StartsWith("Normal"))
                                    polygon.Normal = ParsePolygonVector(line);
                                if (line.StartsWith("TextureU"))
                                    polygon.TextureU = ParsePolygonVector(line);
                                if (line.StartsWith("TextureV"))
                                    polygon.TextureV = ParsePolygonVector(line);
                                if (line.StartsWith("Pan"))
                                {
                                    int u, v;
                                    ParsePolygonUV(line, out u, out v);
                                    polygon.PanU = u;
                                    polygon.PanV = v;
                                }
                                if (line.StartsWith("Vertex"))
                                    polygon.Vertices.Add(ParsePolygonVector(line));
                            }
                        }
                    }
                    // if we are looking for another begin declaration:
                    else
                    {
                        // if we are going to parse an actor:
                        if (line.StartsWith("Begin Actor"))
                        {
                            // read the properties of the actor.
                            var properties = ParseKeyValuePairs(line);
                            if (properties.ContainsKey("Class") && properties.ContainsKey("Name"))
                            {
                                // this is a valid actor that can be parsed further.
                                inActor = true;
                                // create a new actor and add it to the map.
                                actor = new T3dActor(map, properties["Class"], properties["Name"]);
                                map.Actors.Add(actor);
                                continue;
                            }
                        }
                    }
                }
            }

            // return the map data.
            return map;
        }

        /// <summary>
        /// Parses a polygon vector.
        /// </summary>
        /// <param name="line">The line to be parsed (e.g. 'Origin   -00128.000000,-00008.000000,+00008.000000').</param>
        /// <returns></returns>
        private T3dVector3 ParsePolygonVector(string line)
        {
            // remove the first word, any +123 values and trim spaces.
            string[] parts = line.Substring(line.IndexOf(' ')).Replace("+", "").Trim().Split(',');
            return new T3dVector3(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Parses the polygon uv.
        /// </summary>
        /// <param name="line">The line to be parsed (e.g. 'Pan      U=0 V=0').</param>
        /// <param name="u">The u-coordinate.</param>
        /// <param name="v">The v-coordinate.</param>
        private void ParsePolygonUV(string line, out int u, out int v)
        {
            int ui = line.IndexOf("U=");
            int vi = line.IndexOf("V=");
            string x = line.Substring(ui + 2, line.IndexOf(' ', ui) - ui - 2);
            string y = line.Substring(vi + 2);
            Int32.TryParse(x, out u);
            Int32.TryParse(y, out v);
        }

        /// <summary>
        /// Tries to parse a property.
        /// </summary>
        /// <param name="line">The line to be parsed (e.g. 'MaxCarcasses=50').</param>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>True if successful else false.</returns>
        private bool TryParseProperty(string line, out string key, out object value)
        {
            key = null;
            value = null;
            // find the first occurence of the '=' character.
            int splitIndex = line.IndexOf('=');
            if (splitIndex == -1) return false; // early exit.

            // get key and the value (as string).
            key = line.Substring(0, splitIndex);
            if (key.Contains(' ')) return false; // properties don't have spaces.
            string rawvalue = line.Substring(splitIndex + 1);

            // determine and parse the value type.
            int integer;
            float number;

            // string value has been detected.
            if (rawvalue[0] == '"')
            {
                // remove quotes.
                value = rawvalue.Substring(1, rawvalue.Length - 2);
                return true;
            }
            // complex struct has been detected.
            else if (rawvalue[0] == '(')
            {
                // rotator has been detected.
                if (rawvalue[1] == 'P' || (rawvalue[1] == 'Y' && rawvalue[2] == 'a') || rawvalue[1] == 'R')
                {
                    int pi = rawvalue.IndexOf("Pitch=");
                    int yi = rawvalue.IndexOf("Yaw=");
                    int ri = rawvalue.IndexOf("Roll=");
                    if (pi == -1 && yi == -1 && ri == -1) return false;

                    int v1 = 0;
                    int v2 = 0;
                    int v3 = 0;

                    // pitch.
                    if (pi != -1)
                    {
                        int len = rawvalue.IndexOf(',', pi);
                        if (len == -1) len = rawvalue.IndexOf(')', pi);
                        string xs = rawvalue.Substring(pi + 6, len - 1 - 6);
                        Int32.TryParse(xs, out v1);
                    }

                    // yaw.
                    if (yi != -1)
                    {
                        int len = rawvalue.IndexOf(',', yi);
                        if (len == -1) len = rawvalue.IndexOf(')', yi);
                        string ys = rawvalue.Substring(yi + 4, len - yi - 4);
                        Int32.TryParse(ys, out v2);
                    }

                    // roll.
                    if (ri != -1)
                    {
                        int len = rawvalue.IndexOf(',', ri);
                        if (len == -1) len = rawvalue.IndexOf(')', ri);
                        string zs = rawvalue.Substring(ri + 5, len - ri - 5);
                        Int32.TryParse(zs, out v3);
                    }

                    value = new T3dRotator(v1, v2, v3);
                    return true;
                }
                // vector has been detected.
                if (rawvalue[1] == 'X' || rawvalue[1] == 'Y' || rawvalue[1] == 'Z')
                {
                    int xi = rawvalue.IndexOf("X=");
                    int yi = rawvalue.IndexOf("Y=");
                    int zi = rawvalue.IndexOf("Z=");
                    if (xi == -1 && yi == -1 && zi == -1) return false;

                    float v1 = 0.0f;
                    float v2 = 0.0f;
                    float v3 = 0.0f;

                    // x-coordinate.
                    if (xi != -1)
                    {
                        int len = rawvalue.IndexOf(',', xi);
                        if (len == -1) len = rawvalue.IndexOf(')', xi);
                        string xs = rawvalue.Substring(xi + 2, len - 1 - 2);
                        float.TryParse(xs, out v1);
                    }

                    // y-coordinate.
                    if (yi != -1)
                    {
                        int len = rawvalue.IndexOf(',', yi);
                        if (len == -1) len = rawvalue.IndexOf(')', yi);
                        string ys = rawvalue.Substring(yi + 2, len - yi - 2);
                        float.TryParse(ys, out v2);
                    }

                    // z-coordinate.
                    if (zi != -1)
                    {
                        int len = rawvalue.IndexOf(',', zi);
                        if (len == -1) len = rawvalue.IndexOf(')', zi);
                        string zs = rawvalue.Substring(zi + 2, len - zi - 2);
                        float.TryParse(zs, out v3);
                    }

                    value = new T3dVector3(v1, v2, v3);
                    return true;
                }
                // scale has been detected.
                if (rawvalue[1] == 'S' && rawvalue[2] != 'h')
                {
                    // (Scale=(X=0.173205,Y=0.150000,Z=0.125000),SheerAxis=SHEER_ZX)
                    string x = rawvalue.Replace("(Scale=", "X=").Replace(",SheerAxis=SHEER_ZX)", "");
                    string k;
                    object v;
                    if (TryParseProperty(x, out k, out v))
                    {
                        T3dVector3 vec = (T3dVector3)v;
                        vec.X = vec.X == 0.0f ? 1.0f : vec.X;
                        vec.Y = vec.Y == 0.0f ? 1.0f : vec.Y;
                        vec.Z = vec.Z == 0.0f ? 1.0f : vec.Z;
                        value = vec; // vector.
                        return true;
                    }
                }
            }
            // floating point number has been detected.
            else if (rawvalue.Contains('.') && float.TryParse(rawvalue, out number))
            {
                value = number;
                return true;
            }
            // integer number has been detected.
            else if (int.TryParse(rawvalue, out integer))
            {
                value = integer;
                return true;
            }
            // constant enum has been detected.
            else
            {
                value = rawvalue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses the key value pairs on a single line.
        /// </summary>
        /// <param name="line">The line to be parsed (e.g. 'Begin Actor Class=Brush Name=Brush0').</param>
        /// <returns>The key value pairs that could be found on the line.</returns>
        private Dictionary<string, string> ParseKeyValuePairs(string line)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            string name = "";
            StringBuilder buffer = new StringBuilder(128);
            foreach (char c in line)
            {
                // spaces will clear the buffer and indicate a potential value.
                if (c == ' ')
                {
                    // do we have a valid key?
                    if (name != "")
                    {
                        // add a key/value pair to the dictionary.
                        results.Add(name, buffer.ToString());
                        name = "";
                    }
                    buffer.Remove(0, buffer.Length);
                    continue;
                }
                // equals character indicates we found a key.
                if (c == '=')
                {
                    name = buffer.ToString();
                    buffer.Remove(0, buffer.Length);
                    continue;
                }
                // add character to the buffer.
                buffer.Append(c);
            }

            // do we have a valid key?
            if (name != "")
            {
                // add a key/value pair to the dictionary.
                results.Add(name, buffer.ToString());
            }

            return results;
        }
    }
}

#endif