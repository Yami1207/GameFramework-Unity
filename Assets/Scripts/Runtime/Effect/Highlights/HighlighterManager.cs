using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlighterManager : Singleton<HighlighterManager>
{
    public List<Highlighter> m_Highlighters = new List<Highlighter>(8);
    public List<Highlighter> highlighters { get { return m_Highlighters; } }

    public void AddHighlighter(Highlighter highlighter)
    {
        m_Highlighters.Add(highlighter);
    }

    public void RemoveHighlighter(Highlighter highlighter)
    {
        m_Highlighters.Remove(highlighter);
    }
}
