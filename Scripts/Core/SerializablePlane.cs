using UnityEngine;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
	[System.Serializable]
	public struct SerializablePlane
	{
		[SerializeField]
		Vector3 normal;

		[SerializeField]
		float distance;

		public SerializablePlane (Plane unityPlane)
		{
			this.normal = unityPlane.normal;
			this.distance = unityPlane.distance;
		}

		public SerializablePlane (Vector3 normal, float distance)
		{
			this.normal = normal;
			this.distance = distance;
		}

		public Vector3 Normal 
		{
			get 
			{
				return normal;
			}
			set 
			{
				normal = value;
			}
		}

		public float Distance 
		{
			get 
			{
				return distance;
			}
			set 
			{
				distance = value;
			}
		}

		public Plane UnityPlane
		{
			get
			{
				// Use parameterless constructor then set the normal and distance as this avoids a sqrt
				Plane plane = new Plane();
				plane.normal = normal;
				plane.distance = distance;
				return plane;
			}
		}
	}
}