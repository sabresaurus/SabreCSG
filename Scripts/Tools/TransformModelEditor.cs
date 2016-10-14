#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// Dummy tool that does nothing
    /// </summary>
    public class TransformModelEditor : Tool
    {
        public override void ResetTool()
        {
        }

        public override void Deactivated()
        {
        }
    }
}
#endif