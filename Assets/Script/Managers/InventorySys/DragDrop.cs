using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    public static GameObject itemBeingDragged;
    Vector3 startPosition;
    Transform startParent;

private void Awake()
{
    rectTransform = GetComponent<RectTransform>();
    canvasGroup = GetComponent<CanvasGroup>();
    if (canvasGroup == null)
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
}

    private void Start()
    {
        // Cache canvas saat item masih di dalam hierarki Canvas
        canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
            Debug.LogError("Canvas tidak ditemukan! Pastikan item berada di dalam Canvas.");
    }

    public void OnBeginDrag(PointerEventData eventData)
{
    if (canvas == null)
        canvas = GetComponentInParent<Canvas>();

    // ✏️ DITAMBAH: Debug jika canvas masih null
    if (canvas == null)
    {
        Debug.LogError("[DragDrop] Canvas null saat OnBeginDrag pada: " + gameObject.name);
        return;
    }

    itemBeingDragged = gameObject;
    startPosition = transform.position;
    startParent = transform.parent;
    transform.SetParent(canvas.transform);
    canvasGroup.alpha = .6f;
    canvasGroup.blocksRaycasts = false;
}

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        itemBeingDragged = null;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Jika item tidak di-drop ke slot manapun, kembalikan ke posisi semula
        if (transform.parent == canvas.transform)
        {
            transform.position = startPosition;
            transform.SetParent(startParent);
        }

        Debug.Log("OnEndDrag");
    }
}