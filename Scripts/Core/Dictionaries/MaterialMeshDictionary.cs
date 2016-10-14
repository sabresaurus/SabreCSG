#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
	[System.Serializable]
	public class MaterialMeshDictionary
	{
		[System.Serializable]
		public class MeshObjectMapping
		{
			[SerializeField]
			public Mesh Mesh;
			[SerializeField]
			public GameObject GameObject;
		}

		[System.Serializable]
		public class Mapping
		{
			[SerializeField]
			public Material Key;
			[SerializeField]
			public List<MeshObjectMapping> MeshObjectMappings;
			
			public Mapping(Material key, Mesh firstMesh, GameObject firstObject)
			{
				this.Key = key;
				this.MeshObjectMappings = new List<MeshObjectMapping>()
				{
					new MeshObjectMapping()
					{
						Mesh = firstMesh,
						GameObject = firstObject,
					}
				};
			}
		}
		
		[SerializeField]
		List<Mapping> mappings = new List<Mapping>();

		public void Clear()
		{
			mappings.Clear();
		}

		public void Add(Material key, Mesh newMesh, GameObject newObject)
		{
			// If already contained the key add the mesh
			for (int i = 0; i < mappings.Count; i++) 
			{
				if(mappings[i].Key == key)
				{
					mappings[i].MeshObjectMappings.Add(new MeshObjectMapping()
						{
							Mesh = newMesh,
							GameObject = newObject,
						});
					return;
				}
			}	
			// Add a new key value pair and add the mesh to it
			mappings.Add(new Mapping(key, newMesh, newObject));
		}

		public bool Contains(Material key)
		{
			for (int i = 0; i < mappings.Count; i++) 
			{
				if(mappings[i].Key == key)
				{
					return true;
				}
			}	

			return false;
		}

		public List<MeshObjectMapping> this[Material key]
		{
			get
			{
				for (int i = 0; i < mappings.Count; i++) 
				{
					if(mappings[i].Key == key)
					{
						return mappings[i].MeshObjectMappings;
					}
				}	
				
				// None found
				return new List<MeshObjectMapping>();
			}
			//		set
			//		{
			//
			//		}
		}
	}
}
#endif