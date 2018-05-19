#if UNITY_EDITOR || RUNTIME_CSG

using System;

namespace Sabresaurus.SabreCSG.Importers.ValveMapFormat2006
{
    /// <summary>
    /// Represents a Hammer UV Axis.
    /// </summary>
    public class VmfAxis
    {
        /// <summary>
        /// The x, y, z vector.
        /// </summary>
        public VmfVector3 Vector;

        /// <summary>
        /// The UV translation.
        /// </summary>
        public float Translation;

        /// <summary>
        /// The UV scale.
        /// </summary>
        public float Scale;

        /// <summary>
        /// Initializes a new instance of the <see cref="VmfAxis"/> class.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="translation">The translation.</param>
        /// <param name="scale">The scale.</param>
        public VmfAxis(VmfVector3 vector, float translation, float scale)
        {
            Vector = vector;
            Translation = translation;
            Scale = scale;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "VmfAxis " + Vector + ", T=" + Translation + ", S=" + Scale;
        }
    }
}

#endif