#if UNITY_EDITOR && !UNITY_2018_1_OR_NEWER

using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// The undo/redo detector is a simple scriptable object that can be serialized and stored on
    /// the undo/redo stack. We use it to detect whether an undo or a redo operation occured in
    /// the editor. Used by <see cref="UndoRedoBarrier"/>.
    /// </summary>
    /// <seealso cref="UnityEngine.ScriptableObject" />
    internal class UndoRedoDetector : ScriptableObject
    {
        [SerializeField]
        public int CurrentValue;
    }
}

#endif