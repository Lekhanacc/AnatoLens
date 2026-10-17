using UnityEngine;
using UnityEngine.EventSystems;

public class AssistantWidgetDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const string PositionXKey = "AnatoLens.AssistantWidget.X";
    private const string PositionYKey = "AnatoLens.AssistantWidget.Y";
    private const float ScreenInset = 8f;

    private RectTransform widgetRect;
    private RectTransform canvasRect;
    private Canvas canvas;
    private Vector2 pointerOffset;

    private void Awake()
    {
        widgetRect = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.transform as RectTransform;
    }

    private void Start()
    {
        if (widgetRect == null || canvasRect == null)
            return;

        if (PlayerPrefs.HasKey(PositionXKey) && PlayerPrefs.HasKey(PositionYKey))
        {
            widgetRect.anchoredPosition = new Vector2(
                PlayerPrefs.GetFloat(PositionXKey), PlayerPrefs.GetFloat(PositionYKey));
        }
        else
        {
            widgetRect.anchoredPosition = new Vector2(
                canvasRect.rect.width - widgetRect.rect.width - 24f, 176f);
        }

        ClampToCanvas();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (TryGetCanvasPoint(eventData, out Vector2 canvasPoint))
            pointerOffset = widgetRect.anchoredPosition - canvasPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (TryGetCanvasPoint(eventData, out Vector2 canvasPoint))
        {
            widgetRect.anchoredPosition = canvasPoint + pointerOffset;
            ClampToCanvas();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (widgetRect == null)
            return;

        PlayerPrefs.SetFloat(PositionXKey, widgetRect.anchoredPosition.x);
        PlayerPrefs.SetFloat(PositionYKey, widgetRect.anchoredPosition.y);
        PlayerPrefs.Save();
    }

    private bool TryGetCanvasPoint(PointerEventData eventData, out Vector2 canvasPoint)
    {
        canvasPoint = Vector2.zero;
        if (canvasRect == null || widgetRect == null)
            return false;

        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
            return false;

        canvasPoint = localPoint - canvasRect.rect.min;
        return true;
    }

    private void ClampToCanvas()
    {
        if (widgetRect == null || canvasRect == null)
            return;

        Vector2 position = widgetRect.anchoredPosition;
        Vector2 widgetSize = widgetRect.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;
        float maxX = Mathf.Max(ScreenInset, canvasSize.x - widgetSize.x - ScreenInset);
        float maxY = Mathf.Max(ScreenInset, canvasSize.y - widgetSize.y - ScreenInset);
        position.x = Mathf.Clamp(position.x, ScreenInset, maxX);
        position.y = Mathf.Clamp(position.y, ScreenInset, maxY);
        widgetRect.anchoredPosition = position;
    }
}
