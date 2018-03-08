using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Sabresaurus.SabreCSG {

	public class MapEntityData {
		public Dictionary<string, string> properties;
		public List<MapBrushData> brushes;

		public MapEntityData() {
			brushes = new List<MapBrushData>();
			properties = new Dictionary<string, string>();
		}
	}

	public class MapBrushData {
		public List<MapPlaneData> planes;

		public MapBrushData() {
			planes = new List<MapPlaneData>();
		}

		Plane[] GetPlanes() {
			Plane[] output = new Plane[planes.Count];
			for (int i = 0; i < planes.Count; i++) {
				output[i] = planes[i].ToPlane();
			}
			return output;
		}

		public Polygon[] ToBrushPolygons() {
			return BrushFactory.GenerateBrushFromPlanes(GetPlanes());
		}
	}

	public class MapPlaneData {
		public List<Vector3> points;
		public string texture;
		public Vector2 offset;
		public float rotation;
		public Vector2 scale; 

		public MapPlaneData() {
			points = new List<Vector3>();
		}

		public Plane ToPlane() {
			return new Plane(points[0], points[1], points[2]);
		}
	}

	public class QuakeMapParser {

		public static List<MapEntityData> Parse(string path) {
			StreamReader reader = File.OpenText(path);
			List<MapEntityData> output = new List<MapEntityData>();;

			/*
				Parser states:
					TOPLEVEL - Expecting Entity `{` or End Of File
					ENTITY - Expecting Property `"`, Brush `{`, or block close `}`
					PROPERTY - Expecting String `"*" "*"`
					BRUSH - `( x y z ) ( x y z ) ( x y z ) string n n n n n`
				
				Comments `//` should cause parse to skip til the end of the line then continue as normal
			*/

			string parseState = "TOPLEVEL";
			string[] line = {};
			int pos = 1;
			MapEntityData currentEntity = new MapEntityData();
			MapBrushData currentBrush = new MapBrushData();

			int lifeSaver = 10000000;

			while (!reader.EndOfStream && lifeSaver > 0) 
			{
				lifeSaver --;
				if (pos >= line.Length) {
					if (reader.Peek() >= 0) {
						line = reader.ReadLine().Split(' ');
						pos = 0;
					} else {
						parseState = "EOF";
					}
				}

				if (line[pos] == "//") {
					pos = line.Length;
				} else {

					if (parseState == "TOPLEVEL")
					{
						if (line[pos] == "{") 
						{
							// Create Entity
							pos += 1;
							parseState = "ENTITY";
							currentEntity = new MapEntityData();
						}
						else 
						{
							// ERROR
							Debug.LogError("Parse error: TOPLEVEL");
							return null;
						}
					}
					else if (parseState == "ENTITY")
					{
						if (line[pos][0] == '"') 
						{
							// Add property to entity
							string key = line[pos].Substring(1, line[pos].Length-2);
							string value = "";
							int p = 1;
							while (pos + p < line.Length) {
								value += line[pos + p];
								if (line[pos+p].EndsWith("\"")) {
									break;
								}
								p++;
							}
							value = value.Substring(1, value.Length-2);
							currentEntity.properties.Add(key, value);
							pos += 1 + p;
						} 
						else if (line[pos] == "{")
						{
							// Add brushes to entity
							pos += 1;
							parseState = "BRUSH";
						}
						else if (line[pos] == "}")
						{
							// Close and add entity
							output.Add(currentEntity);
							parseState = "TOPLEVEL";
							pos += 1;
						}
						else
						{
							// ERROR
							Debug.LogError("Parse error: ENTITY");
							Debug.Log("Unexpected: "+line[pos]);
							return null;
						}

					}
					else if (parseState == "BRUSH")
					{
						if (line[pos] == "}") 
						{
							// Close and add brush
							pos += 1;
							currentEntity.brushes.Add(currentBrush);
							parseState = "ENTITY";
						}
						else if (line[pos] == "(")
						{
							MapPlaneData plane = new MapPlaneData();
							plane.points.Add(
								new Vector3(
									float.Parse(line[pos+1]),
									float.Parse(line[pos+2]),
									float.Parse(line[pos+3])
								)
							);

							plane.points.Add(
								new Vector3(
									float.Parse(line[pos+6]),
									float.Parse(line[pos+7]),
									float.Parse(line[pos+8])
								)
							);

							plane.points.Add(
								new Vector3(
									float.Parse(line[pos+11]),
									float.Parse(line[pos+12]),
									float.Parse(line[pos+13])
								)
							);

							plane.texture = line[pos + 15];

							plane.offset = new Vector2(
								float.Parse(line[pos + 16]),
								float.Parse(line[pos + 17])
							);

							plane.rotation = float.Parse(line[pos + 18]);

							plane.scale = new Vector2(
								float.Parse(line[pos + 19]),
								float.Parse(line[pos + 20])
							);

							pos += 21;

							currentBrush.planes.Add(plane);
						} 
						else {
							Debug.LogError("PARSE ERROR!! BRUSH!");
							Debug.Log("Unexpected: "+line[pos]);
							return null;
						}
					}
				}
			}

			if (lifeSaver == 0) {
				Debug.LogError(parseState);
			}

			return output;
		}

	}
}