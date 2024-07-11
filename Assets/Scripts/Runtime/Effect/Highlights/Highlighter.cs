using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Highlighter : MonoBehaviour
{
    [SerializeField]
    private Renderer[] m_Renderers;
    public Renderer[] renderers { get { return m_Renderers; } }

    private void OnEnable()
    {
        HighlighterManager.instance.AddHighlighter(this);
    }

    private void OnDisable()
    {
        HighlighterManager.instance.RemoveHighlighter(this);
    }
}
