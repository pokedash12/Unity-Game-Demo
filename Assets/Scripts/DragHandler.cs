using UnityEngine;
using UnityEngine.EventSystems;
public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Transform ogParent;
    CanvasGroup canvasGroup;
    public void OnBeginDrag(PointerEventData eventData)
    {
        ogParent = transform.parent;
        transform.SetParent(transform.root);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        Slot dropSlot = eventData.pointerEnter?.GetComponent<Slot>();
        Slot originalSlot = ogParent.GetComponent<Slot>();
        if (dropSlot == null)
                {
                    GameObject item = eventData.pointerEnter;

                    if (item != null)
                    {
                        dropSlot = item.GetComponentInParent<Slot>();
                    }
                }
        if (dropSlot != null)
        {
        if (dropSlot.currentItem != null)
            {
                dropSlot.currentItem.transform.SetParent(originalSlot.transform);
                originalSlot.currentItem = dropSlot.currentItem;
                dropSlot.currentItem.GetComponent<RectTransform>().anchoredPosition = UnityEngine.Vector2.zero;
            }
            else
            {
                originalSlot.currentItem = null;
            }

        transform.SetParent(dropSlot.transform);
        dropSlot.currentItem = gameObject;
        }
        else
        {
            transform.SetParent(ogParent);
        }

        GetComponent<RectTransform>().anchoredPosition = UnityEngine.Vector2.zero;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
