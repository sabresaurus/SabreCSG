#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sabresaurus.SabreCSG.Importers.UnrealGold
{
    /// <summary>
    /// Represents an Unreal Editor 1 Actor.
    /// </summary>
    public class T3dActor
    {
        /// <summary>
        /// Gets the class of the actor.
        /// </summary>
        /// <value>The class of the actor.</value>
        public string Class /*{ get; }*/;

        /// <summary>
        /// Gets the name of the actor.
        /// </summary>
        /// <value>The name of the actor.</value>
        public string Name /*{ get; }*/;

        /// <summary>
        /// Gets the actor properties.
        /// </summary>
        /// <value>The actor properties.</value>
        public Dictionary<string, object> Properties /*{ get; }*/ = new Dictionary<string, object>();

        /// <summary>
        /// The T3D map this actor is a part of.
        /// </summary>
        private T3dMap m_T3dMap;

        /// <summary>
        /// Gets the location of the actor.
        /// </summary>
        /// <value>The location of the actor.</value>
        public T3dVector3 Location
        {
            get
            {
                object value;
                if (Properties.TryGetValue("Location", out value))
                    return value as T3dVector3;
                return new T3dVector3();
            }
        }

        /// <summary>
        /// Gets the rotation of the actor.
        /// </summary>
        /// <value>The rotation of the actor.</value>
        public T3dRotator Rotation
        {
            get
            {
                object value;
                if (Properties.TryGetValue("Rotation", out value))
                    return value as T3dRotator;
                return new T3dRotator();
            }
        }

        /// <summary>
        /// Gets the main scale of the actor.
        /// </summary>
        /// <value>The main scale of the actor.</value>
        public T3dVector3 MainScale
        {
            get
            {
                object value;
                if (Properties.TryGetValue("MainScale", out value))
                    return value as T3dVector3;
                return new T3dVector3(1.0f, 1.0f, 1.0f);
            }
        }

        /// <summary>
        /// Gets the post scale of the actor.
        /// </summary>
        /// <value>The post scale of the actor.</value>
        public T3dVector3 PostScale
        {
            get
            {
                object value;
                if (Properties.TryGetValue("PostScale", out value))
                    return value as T3dVector3;
                return new T3dVector3(1.0f, 1.0f, 1.0f);
            }
        }

        /// <summary>
        /// Gets the pre pivot of the actor.
        /// </summary>
        /// <value>The pre pivot of the actor.</value>
        public T3dVector3 PrePivot
        {
            get
            {
                object value;
                if (Properties.TryGetValue("PrePivot", out value))
                    return value as T3dVector3;
                return new T3dVector3(0.0f, 0.0f, 0.0f);
            }
        }

        /// <summary>
        /// Gets the brush name.
        /// </summary>
        /// <value>
        /// The brush name.
        /// </value>
        public string BrushName
        {
            get
            {
                object value;
                if (Properties.TryGetValue("Brush", out value))
                {
                    string brush = (string)value;
                    int i = brush.IndexOf('.');
                    return brush.Substring(i + 1, brush.Length - i - 2);
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the brush associated with the actor.
        /// </summary>
        /// <value>
        /// The brush associated with the actor.
        /// </value>
        public T3dBrush Brush
        {
            get
            {
                return m_T3dMap.BrushModels.FirstOrDefault(b => b.Name == BrushName);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T3dActor"/> class.
        /// </summary>
        /// <param name="class">The class of the actor.</param>
        /// <param name="name">The name of the actor.</param>
        public T3dActor(T3dMap map, string @class, string name)
        {
            m_T3dMap = map;
            Class = @class;
            Name = name;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "Unreal Engine 1 " + Class + " \"" + Name + "\"";
        }
    }
}

#endif