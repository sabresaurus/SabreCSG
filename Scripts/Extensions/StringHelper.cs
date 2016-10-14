using UnityEngine;
using System.Collections;
using System;
using System.Text;

namespace Sabresaurus.SabreCSG
{
	public static class StringHelper
	{
		public static bool TryParseScale(string inputString, out Vector3 outputScale)
		{
			// Try as a scale triplet (e.g. 3,2,3)
			string[] split = inputString.Split(',');	
			Vector3 tempScale = Vector3.one;
			if(split.Length == 3)
			{
				int componentsFilled = 0;
				for (int i = 0; i < 3; i++) 
				{
					float outValue;
					if(float.TryParse(split[i].Trim(), out outValue))
					{
						tempScale[i] = outValue;
						componentsFilled++;
					}
				}
				if(componentsFilled == 3)
				{
					outputScale = tempScale;
					return true;
				}
			}

			// There are commas present, but we don't know how to parse so fail
			if(split.Length > 1)
			{
				outputScale = Vector3.one;
				return false;
			}

			// No commas present so try to parse as a single uniform value (e.g. 3 => 3,3,3)
			float uniformValue;
			if(float.TryParse(inputString, out uniformValue))
			{
				if(uniformValue != 0)
				{
					outputScale = new Vector3(uniformValue,uniformValue,uniformValue);
					return true;
				}
			}

			// Still unable to parse, return false and just default output scale to 1
			outputScale = Vector3.one;
			return false;
		}

		public static bool TryParseScale(string inputString, out Vector2 outputScale)
		{
			// Try as a scale pair (e.g. 3,2)
			string[] split = inputString.Split(',');	
			Vector2 tempScale = Vector2.one;
			if(split.Length == 2)
			{
				int componentsFilled = 0;
				for (int i = 0; i < 2; i++) 
				{
					float outValue;
					if(float.TryParse(split[i].Trim(), out outValue))
					{
						tempScale[i] = outValue;
						componentsFilled++;
					}
				}
				if(componentsFilled == 2)
				{
					outputScale = tempScale;
					return true;
				}
			}

			// There are commas present, but we don't know how to parse so fail
			if(split.Length > 1)
			{
				outputScale = Vector3.one;
				return false;
			}

			// No commas present so try to parse as a single uniform value (e.g. 3 => 3,3,3)
			float uniformValue;
			if(float.TryParse(inputString, out uniformValue))
			{
				if(uniformValue != 0)
				{
					outputScale = new Vector2(uniformValue,uniformValue);
					return true;
				}
			}

			// Still unable to parse, return false and just default output scale to 1
			outputScale = Vector2.one;
			return false;
		}

		public static string ParseDisplayString(string input)
		{
			StringBuilder stringBuilder = new StringBuilder();

			for (int i = 0; i < input.Length; i++) 
			{
				// If we've just started an uppercase (not at the start of the string) then prepend a space
				if(i > 0 && Char.IsUpper(input[i]) && !Char.IsUpper(input[i-1]))
				{
					stringBuilder.Append(' ');
				}
				stringBuilder.Append(input[i]);
			}

			return stringBuilder.ToString();
		}
	}
}