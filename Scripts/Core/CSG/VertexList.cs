#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	public class VertexList
	{
	    List<Vertex> vertices = new List<Vertex>();

	    public List<Vertex> Vertices
	    {
	        get { return vertices; }
	    }
		
		public void Clear()
		{
			vertices.Clear();
		}

	    /// <summary>
	    /// This will find if the vertex is already contained and return its index, otherwise it adds it as new
	    /// </summary>
	    public int AddOrGet(Vertex vertex)
	    {
			// Find if the vertex is already contained
	        for (int i = 0; i < vertices.Count; i++)
	        {
	            if (vertices[i] == vertex)
	            {
	                return i;
	            }
	        }

	        // None found, so add it and return the new index
	        vertices.Add(vertex);
	        return vertices.Count - 1;
	    }

		public int Add(Vertex vertex)
		{
			// None found, so add it and return the new index
			vertices.Add(vertex);
			return vertices.Count - 1;
		}
	}
}
#endif