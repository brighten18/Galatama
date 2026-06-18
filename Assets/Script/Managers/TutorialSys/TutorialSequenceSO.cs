using System;
using UnityEngine;

/// <summary>
/// Data tutorial berisi urutan langkah yang bisa dipakai ulang oleh satu panel UI.
/// </summary>
[CreateAssetMenu(fileName = "TutorialSequence", menuName = "GALATAMA/Tutorial Sequence")]
public class TutorialSequenceSO : ScriptableObject
{
    [Serializable]
    public class TutorialStep
    {
        [SerializeField] private string title;
        [SerializeField, TextArea(3, 8)] private string description;
        [SerializeField] private Sprite illustration;

        public string Title => title;
        public string Description => description;
        public Sprite Illustration => illustration;
    }

    [Header("Metadata")]
    [SerializeField] private string tutorialId = "tutorial.default";
    [SerializeField] private string displayName = "Tutorial";
    [SerializeField] private bool playOnce = true;
    [SerializeField] private bool lockPlayerWhileOpen = true;

    [Header("Steps")]
    [SerializeField] private TutorialStep[] steps;

    public string TutorialId => string.IsNullOrWhiteSpace(tutorialId) ? name : tutorialId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public bool PlayOnce => playOnce;
    public bool LockPlayerWhileOpen => lockPlayerWhileOpen;
    public TutorialStep[] Steps => steps;
    public int StepCount => steps != null ? steps.Length : 0;
}
