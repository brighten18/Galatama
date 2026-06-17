using UnityEngine;

/// <summary>
/// Holds a sequence of text panels for a single monologue event.
/// Supports **bold** markdown syntax which is converted to Unity rich text at runtime.
/// </summary>
[CreateAssetMenu(fileName = "MonologueData", menuName = "GALATAMA/Monologue Data")]
public class MonologueData : ScriptableObject
{
    [SerializeField, TextArea(2, 6)] private string[] panels;

    /// <summary>Raw panel strings (may contain **bold** markdown).</summary>
    public string[] Panels => panels;
}
