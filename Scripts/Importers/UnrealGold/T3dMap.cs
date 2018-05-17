#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sabresaurus.SabreCSG.Importers.UnrealGold
{
    /// <summary>
    /// Represents an Unreal Editor 1 map.
    /// </summary>
    public class T3dMap
    {
        /// <summary>
        /// Gets the actors in the map.
        /// </summary>
        /// <value>The actors in the map.</value>
        public List<T3dActor> Actors /*{ get; }*/ = new List<T3dActor>();

        /// <summary>
        /// Gets the brush models in the map.
        /// </summary>
        /// <value>The brush models in the map.</value>
        public List<T3dBrush> BrushModels /*{ get; }*/ = new List<T3dBrush>();

        /// <summary>
        /// Gets the brush actors in the level.
        /// </summary>
        /// <value>The brush actors in the level.</value>
        public List<T3dActor> Brushes { get { return Actors.Where(a => a.Class == "Brush" && a.BrushName != "Brush").ToList(); } }

        /// <summary>
        /// Gets the map title.
        /// </summary>
        /// <value>The map title.</value>
        public string Title
        {
            get
            {
                if (LevelInfo == null) return "";
                object value;
                if (LevelInfo.Properties.TryGetValue("Title", out value))
                    return value as string;
                return "";
            }
        }

        /// <summary>
        /// Gets the map author.
        /// </summary>
        /// <value>The map author.</value>
        public string Author
        {
            get
            {
                if (LevelInfo == null) return "";
                object value;
                if (LevelInfo.Properties.TryGetValue("Author", out value))
                    return value as string;
                return "";
            }
        }

        /// <summary>
        /// Gets the level information actor.
        /// </summary>
        /// <value>The level information actor.</value>
        public T3dActor LevelInfo { get { return Actors.FirstOrDefault(a => a.Class == "LevelInfo"); } }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "Unreal Engine 1 Map \"" + Title + "\"";
        }
    }
}

#endif