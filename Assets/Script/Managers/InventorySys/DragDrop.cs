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
        canvas = GetComponentInParent<Canvas>();

        // FIX: Jika tidak ada Canvas di parent hierarchy,
        // komponen ini tidak relevan di konteks ini (misal saat di-clone ke ToolsHolder).
        // Disable saja diri sendiri daripada error.
        if (canvas == null)
        {
            Debug.LogWarning($"[DragDrop] Canvas tidak ditemukan pada '{gameObject.name}'. " +
                             $"DragDrop di-disable karena object berada di luar hierarki Canvas.");
            enabled = false;
            return;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // FIX: Coba cari canvas sekali lagi jika null (fallback)
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("[DragDrop] OnBeginDrag dibatalkan, Canvas tidak ditemukan pada: " + gameObject.name);
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

        if (canvas != null && transform.parent == canvas.transform)
        {
            transform.position = startPosition;
            transform.SetParent(startParent);
        }

        Debug.Log("OnEndDrag");
    }
}