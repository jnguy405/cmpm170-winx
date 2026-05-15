using UnityEngine;
using UnityEngine.UI; // Fixed: Needed for LayoutElement and Image
using UnityEngine.EventSystems;

public class DraggableCraftItem :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public int itemID;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Image image; // Fixed: Declared image variable

    private Transform originalParent;
    private Vector3 originalPosition;

    private GameObject placeholder;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        image = GetComponent<Image>(); // Fixed: Initialized image variable

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = transform.position;
        originalParent = transform.parent;

        placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(originalParent);

        LayoutElement layout = placeholder.AddComponent<LayoutElement>();
        LayoutElement myLayout = GetComponent<LayoutElement>();

        if (myLayout != null)
        {
            layout.preferredWidth = myLayout.preferredWidth;
            layout.preferredHeight = myLayout.preferredHeight;
        }
        else
        {
            RectTransform rt = GetComponent<RectTransform>();
            layout.preferredWidth = rt.rect.width;
            layout.preferredHeight = rt.rect.height;
        }

        placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex());

        // Now this works because 'image' is defined
        if (image != null) image.raycastTarget = false; 

        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (image != null) image.raycastTarget = true;

        transform.SetParent(originalParent);
        transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
        transform.position = originalPosition;

        Destroy(placeholder);
    }
}