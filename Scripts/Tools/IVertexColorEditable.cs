using UnityEngine;
using System.Collections;

public interface IVertexColorEditable
{
    void SetSelectionColor(Color color);

    Color GetColor();
}

