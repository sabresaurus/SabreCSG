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
    public class CustomUndoStackHelper
    {
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

        /// <summary>
        /// The undo/redo detector is a simple scriptable object that can be serialized and stored on
        /// the undo/redo stack. We use it to detect whether an undo or a redo operation occured in
        /// the editor.
        /// </summary>
        /// <seealso cref="UnityEngine.ScriptableObject" />
        private class UndoRedoDetector : ScriptableObject
        {
            [SerializeField]
            public int CurrentValue;
        }

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
        /// Initializes a new instance of the <see cref="CustomUndoStackHelper"/> class.
        /// <para>
        /// You must call <see cref="OnGUI"/>, <see cref="OnFocus"/>, <see cref="OnLostFocus"/> and
        /// <see cref="OnDestroy"/> from the editor window in order to use this custom history stack.
        /// </para>
        /// </summary>
        /// <param name="editorWindow">The editor window to monitor.</param>
        /// <param name="undoGroupName">Name of the undo group (should be unique).</param>
        public CustomUndoStackHelper(EditorWindow editorWindow, string undoGroupName)
        {
            this.editorWindow = editorWindow;
            this.undoGroupName = undoGroupName;
        }

        /// <summary>
        /// Must be called when OnGUI occurs in the <see cref="EditorWindow"/>.
        /// </summary>
        public void OnGUI()
        {
            // subscribe to editor undo/redo events.
            if (!EditorHelper.HasDelegate(Undo.undoRedoPerformed, (Action)OnUndoRedoPerformed))
                Undo.undoRedoPerformed += OnUndoRedoPerformed;

            if (singletonFocus && Undo.GetCurrentGroupName() != undoGroupName)
            {
                ForceCreateUndoEntry();
            }
        }

        /// <summary>
        /// Must be called when OnFocus occurs in the <see cref="EditorWindow"/>.
        /// </summary>
        public void OnFocus()
        {
            if (!singletonFocus && EditorWindow.focusedWindow == editorWindow)
            {
                undoRedoGroupIndex = Undo.GetCurrentGroup();
                //Debug.Log("OnFocus: Group Index: " + undoRedoGroupIndex);

                ForceCreateUndoEntry();
                //Debug.Log("Added 2DSE Undo Entry");

                singletonFocus = true;
            }
        }

        /// <summary>
        /// Must be called when OnLostFocus occurs in the <see cref="EditorWindow"/>.
        /// </summary>
        public void OnLostFocus()
        {
            singletonFocus = false;

            ignoreOnUndoRedoPerformed = true;
            Undo.RevertAllDownToGroup(undoRedoGroupIndex + 1);
            //Debug.Log("OnLostFocus: Reverted To Group Index: " + (undoRedoGroupIndex + 1));
        }

        /// <summary>
        /// Must be called when OnDestroy occurs in the <see cref="EditorWindow"/>.
        /// </summary>
        public void OnDestroy()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

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
                //Debug.Log("Redo!!");
                if (OnRedo != null)
                    OnRedo(this, null);
            }
            else
            {
                //Debug.Log("Undo!! " + Undo.GetCurrentGroup() + " " + undoRedoGroupIndex);
                if (OnUndo != null)
                    OnUndo(this, null);

                // this sleep is required!
                // without it holding CTRL+Z will be so fast that it starts to bypass
                // our undo barrier and ends up affecting the scene itself.
                System.Threading.Thread.Sleep(50);
            }

            editorWindow.Repaint();
        }
    }
}

#endif