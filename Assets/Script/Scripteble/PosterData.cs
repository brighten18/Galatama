using UnityEngine;

[CreateAssetMenu(fileName = "PosterData", menuName = "Galatama/Poster Data")]
public class PosterData : ScriptableObject
{
    [Header("UI Content")]
    [SerializeField] private string posterName = "Poster";
    [SerializeField] private Sprite posterSprite;

    [Header("Popup Layout")]
    [SerializeField] private Vector2 anchoredPosition = Vector2.zero;
    [SerializeField] private Vector3 localScale = Vector3.one;
    [SerializeField] private Vector2 sizeDelta = new Vector2(900f, 540f);

    public string PosterName => posterName;
    public Sprite PosterSprite => posterSprite;
    public Vector2 AnchoredPosition => anchoredPosition;
    public Vector3 LocalScale => localScale;
    public Vector2 SizeDelta => sizeDelta;
}
