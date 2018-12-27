#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// Helper class for <see cref="EditorWindow"/> to implement a custom undo/redo stack that does
    /// not interfere with the default unity editor undo/redo stack.
    /// </summary>
    public class UndoRedoBarrier
    {
#if !UNITY_2018_1_OR_NEWER
        /// <summary>
        /// The unity editor undo/redo group index. This value is used to reset unity editor's
        /// history once the editor window loses focus or is closed.
        /// </summary>
        private int undoRedoGroupIndex;

        /// <summary>
        /// The singleton focus ensures that <see cref="OnFocus"/> and <see cref="OnLostFocus"/> are
        /// not executed multiple times in the wrong order (this tends to happen after script
        /// recompilations and when editor windows get closed).
        /// </summary>
        private bool singletonFocus;

        /// <summary>
        /// The ignore on undo/redo performed flag allows us to ignore an event that we caused
        /// ourselves by manipulating the undo history in unity editor.
        /// </summary>
        private bool ignoreOnUndoRedoPerformed = false;

        /// <summary>
        /// The undo/redo detector is a simple scriptable object that can be serialized and stored on
        /// the undo/redo stack. We use it to detect whether an undo or a redo operation occured in
        /// the editor.
        /// </summary>
        private UndoRedoDetector undoRedoDetector;
#endif

        /// <summary>
        /// The editor window that this class is actively monitoring.
        /// </summary>
        private EditorWindow editorWindow;

        /// <summary>
        /// The undo group name is used in unity editor's undo history stack and should be unique.
        /// </summary>
        private string undoGroupName;

        /// <summary>
        /// Occurs when the user undoes (CTRL+Z) something on the custom editor window.
        /// </summary>
        public event EventHandler OnUndo;

        /// <summary>
        /// Occurs when the user redoes (CTRL+Y) something on the custom editor window.
        /// </summary>
        public event EventHandler OnRedo;

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoBarrier"/> class.
        /// <para>
        /// You must call <see cref="OnGUI"/>, <see cref="OnFocus"/>, <see cref="OnLostFocus"/> and
        /// <see cref="OnDestroy"/> from the editor window in order to use this undo/redo barrier.
        /// </para>
        /// </summary>
        /// <param name="editorWindow">The editor window to monitor.</param>
        /// <param name="undoGroupName">Name of the undo group (should be unique).</param>
        public UndoRedoBarrier(EditorWindow editorWindow, string undoGroupName)
        {
            this.editorWindow = editorWindow;
            this.undoGroupName = undoGroupName;
        }

        /// <summary>
        /// Must be called when OnGUI occurs in the <see cref="EditorWindow"/>.
        /// </summary>
        public void OnGUI()
        {
#if UNITY_2018_1_OR_NEWER
            // unity editor 2018 (tested with 2018.3.0f2) allows us to simply intercept and cancel CTRL+Z.
            if (EditorWindow.focusedWindow == editorWindow && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Z && SabreInput.IsCommandModifier(Event.current))
            {
                if (OnUndo != null) OnUndo(this, null);
                Event.current.Use(); // cancel the real unity editor undo.
            }

            // unity editor 2018 (tested with 2018.3.0f2) allows us to simply intercept and cancel CTRL+Y.
            if (EditorWindow.focusedWindow == editorWindow && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Y && SabreInput.IsCommandModifier(Event.current))
            {
                if (OnRedo != null) OnRedo(this, null);
                Event.current.Use(); // cancel the real unity editor redo.
            }
#else
            // subscribe to editor undo/redo events.
            if (!EditorHelper.HasDelegate(Undo.undoRedoPerformed, (Action)OnUndoRedoPerformed))
                Undo.undoRedoPerformed += OnUndoRedoPerformed;

            // recreate the fake undo operation if it's not active.
            if (singletonFocus && Undo.GetCurrentGroupName() != undoGroupName)
                ForceCreateUndoEntry();
#endif
        }

        /// <summary>
        /// Must be called when OnFocus occurs in the <see cref="EditorWindow"/>.
        /// </summary>
        public void OnFocus()
        {
#if !UNITY_2018_1_OR_NEWER
            if (!singletonFocus && EditorWindow.focusedWindow == editorWindow)
            {
                undoRedoGroupIndex = Undo.GetCurrentGroup();
                ForceCreateUndoEntry();
                singletonFocus = true;
            }
#endif
        }

        /// <summary>
        /// Must be called when OnLostFocus occurs in the <see cref="EditorWindow"/>.
        /// </summary>
        public void OnLostFocus()
        {
#if !UNITY_2018_1_OR_NEWER
            singletonFocus = false;
            ignoreOnUndoRedoPerformed = true;
            Undo.RevertAllDownToGroup(undoRedoGroupIndex + 1);
#endif
        }

        /// <summary>
        /// Must be called when OnDestroy occurs in the <see cref="EditorWindow"/>.
        /// </summary>
        public void OnDestroy()
        {
#if !UNITY_2018_1_OR_NEWER
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
#endif
        }

#if !UNITY_2018_1_OR_NEWER
        /// <summary>
        /// Forces an undo-able operation onto unity editor's undo/redo stack.
        /// </summary>
        private void ForceCreateUndoEntry()
        {
            // create the undo/redo detector instance.
            if (undoRedoDetector == null)
                undoRedoDetector = ScriptableObject.CreateInstance<UndoRedoDetector>();
            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(undoRedoDetector, undoGroupName);
        }

        /// <summary>
        /// Called when an undo or redo was performed in unity editor.
        /// </summary>
        private void OnUndoRedoPerformed()
        {
            if (ignoreOnUndoRedoPerformed) { ignoreOnUndoRedoPerformed = false; return; }
            if (EditorWindow.focusedWindow != editorWindow) return;

            if (Undo.GetCurrentGroupName() == undoGroupName)
            {
                if (OnRedo != null) OnRedo(this, null);
            }
            else
            {
                if (OnUndo != null) OnUndo(this, null);

                // this sleep is required!
                // without it holding CTRL+Z will be so fast that it starts to bypass
                // our undo barrier and ends up affecting the scene itself.
                System.Threading.Thread.Sleep(50);
            }

            editorWindow.Repaint();
        }
#endif
    }
}

#endif