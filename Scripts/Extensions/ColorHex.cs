using UnityEngine;
using System;

namespace Sabresaurus.SabreCSG
{
	public struct ColorHex
	{
		byte r,g,b,a;

		public ColorHex(string hex)
		{
			r = 255;
			g = 255;
			b = 255;
			a = 255;
			if(hex.Length == 6)
			{
				r = Convert.ToByte(hex.Substring(0,2), 16);
				g = Convert.ToByte(hex.Substring(2,2), 16);
				b = Convert.ToByte(hex.Substring(4,2), 16);
			}
		}

		public static implicit operator Color32(ColorHex c)
		{
			return new Color32(c.r, c.g, c.b, c.a);
		}

		public static implicit operator Color(ColorHex c)
		{
			return new Color32(c.r, c.g, c.b, c.a);
		}
	}
}

