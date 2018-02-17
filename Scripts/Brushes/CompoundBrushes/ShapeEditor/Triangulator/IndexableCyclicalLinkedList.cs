#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sabresaurus.SabreCSG.ShapeEditor.Triangulator
{
    /// <summary>
    /// Implements a LinkedList that is both indexable as well as cyclical. Thus
    /// indexing into the list with an out-of-bounds index will automatically cycle
    /// around the list to find a valid node.
    /// </summary>
    class IndexableCyclicalLinkedList<T> : LinkedList<T>
    {
        /// <summary>
        /// Gets the LinkedListNode at a particular index.
        /// </summary>
        /// <param name="index">The index of the node to retrieve.</param>
        /// <returns>The LinkedListNode found at the index given.</returns>
        public LinkedListNode<T> this[int index]
        {
            get
            {
                //perform the index wrapping
                while (index < 0)
                    index = Count + index;
                if (index >= Count)
                    index %= Count;

                //find the proper node
                LinkedListNode<T> node = First;
                for (int i = 0; i < index; i++)
                    node = node.Next;

                return node;
            }
        }

        /// <summary>
        /// Removes the node at a given index.
        /// </summary>
        /// <param name="index">The index of the node to remove.</param>
        public void RemoveAt(int index)
        {
            Remove(this[index]);
        }

        /// <summary>
        /// Finds the index of a given item.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>The index of the item if found; -1 if the item is not found.</returns>
        public int IndexOf(T item)
        {
            for (int i = 0; i < Count; i++)
                if (this[i].Value.Equals(item))
                    return i;

            return -1;
        }
    }
}
#endif