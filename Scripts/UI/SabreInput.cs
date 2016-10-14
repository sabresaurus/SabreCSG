#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
	public static class SabreInput
	{
		public static bool AnyModifiersSet(Event e)
		{
			if(e.modifiers == EventModifiers.None 
				|| e.modifiers == EventModifiers.CapsLock) // Ignore CapsLock
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public static bool IsModifier(Event e, EventModifiers modifier)
		{
			EventModifiers modifiers1 = e.modifiers;
			EventModifiers modifiers2 = modifier;

			// Ignore capslock from either modifier
			modifiers1 &= (~EventModifiers.CapsLock);
			modifiers2 &= (~EventModifiers.CapsLock);

			if(modifiers1 == modifiers2)
			{
				return true;
			}
			else
			{
				return false;
			}
		}


	}
}
#endif