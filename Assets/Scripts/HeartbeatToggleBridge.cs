using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeartbeatToggleBridge : MonoBehaviour
{
    private static HeartbeatAnimator activeAnimator;
    private static GameObject cancelCanvasObject;

    public void OnToggleBeatPressed()
    {
        HeartbeatAnimator anim = FindFirstObjectByType<HeartbeatAnimator>();

        if (anim != null)
        {
            anim.ToggleBeating();
            if (anim.isBeating)
                ShowCancelButton(anim);
            else
                HideCancelButton();
        }
        else
        {
            Debug.Log("[Heartbeat] No heart model spawned yet — scan the marker first.");
            HideCancelButton();
        }

        MenuToggle.CloseOpenMenu();
    }

    private static void ShowCancelButton(HeartbeatAnimator animator)
    {
        activeAnimator = animator;
        if (cancelCanvasObject == null)
        {
            cancelCanvasObject = new GameObject("Heartbeat Cancel Overlay",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = cancelCanvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;

            CanvasScaler scaler = cancelCanvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject buttonObject = new GameObject("Cancel Beat", typeof(RectTransform),
                typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(cancelCanvasObject.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-24f, -24f);
            buttonRect.sizeDelta = new Vector2(64f, 64f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color32(14, 25, 38, 232);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject textObject = new GameObject("Close Icon",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.text = "×";
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 42f;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color32(85, 221, 207, 255);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            button.onClick.AddListener(CancelActiveBeat);
        }

        cancelCanvasObject.SetActive(true);
    }

    private static void CancelActiveBeat()
    {
        if (activeAnimator != null)
            activeAnimator.StopBeating();
        HideCancelButton();
    }

    private static void HideCancelButton()
    {
        activeAnimator = null;
        if (cancelCanvasObject != null)
            cancelCanvasObject.SetActive(false);
    }
}
