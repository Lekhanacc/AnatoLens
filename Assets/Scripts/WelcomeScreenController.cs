using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>Animated first-launch splash and guided onboarding for AnatoLens.</summary>
public sealed class WelcomeScreenController : MonoBehaviour
{
    private const float SplashDuration = 5f;

    private static readonly Color BackgroundColor = new Color32(7, 15, 30, 255);
    private static readonly Color AccentColor = new Color32(74, 224, 207, 255);
    private static readonly Color MutedTextColor = new Color32(169, 190, 207, 255);
    private static readonly Color SecondaryColor = new Color32(28, 54, 76, 255);

    private ARSession[] arSessions;
    private ARTrackedImageManager[] imageManagers;
    private bool[] sessionWasEnabled;
    private bool[] managerWasEnabled;
    private GameObject screenRoot;
    private Transform pageRoot;
    private Sprite roundedSprite;
    private Sprite glowSprite;
    private Sprite circleSprite;
    private Sprite ringSprite;
    private Sprite gradientButtonSprite;
    private CanvasGroup pageCanvasGroup;
    private Coroutine pageTransition;
    private RectTransform splashProgressFill;
    private float splashProgressWidth;
    private int currentInstructionStep;
    private Transform activeOrbit;
    private RectTransform activeHeroCore;
    private Vector3 activeHeroCoreScale = Vector3.one;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateWelcomeScreen()
    {
        if (FindObjectOfType<WelcomeScreenController>(true) != null)
            return;

        new GameObject("AnatoLens Welcome Screen").AddComponent<WelcomeScreenController>();
    }

    private void Awake()
    {
        CacheARState(pauseUntilScan: true);
        BuildCanvas();
    }

    private void Start()
    {
        if (screenRoot != null)
            StartCoroutine(AdvanceFromSplash());
    }

    private void Update()
    {
        if (activeOrbit != null)
            activeOrbit.Rotate(0f, 0f, -8f * Time.unscaledDeltaTime);

        if (activeHeroCore != null)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 1.8f) * 0.025f;
            activeHeroCore.localScale = activeHeroCoreScale * pulse;
        }
    }

    private IEnumerator AdvanceFromSplash()
    {
        float elapsed = 0f;
        while (elapsed < SplashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / SplashDuration);
            if (splashProgressFill != null)
                splashProgressFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    splashProgressWidth * progress);
            yield return null;
        }
        ShowWelcomePage();
    }

    private void CacheARState(bool pauseUntilScan)
    {
        arSessions = FindObjectsOfType<ARSession>(true);
        imageManagers = FindObjectsOfType<ARTrackedImageManager>(true);
        sessionWasEnabled = new bool[arSessions.Length];
        managerWasEnabled = new bool[imageManagers.Length];

        for (int i = 0; i < arSessions.Length; i++)
        {
            sessionWasEnabled[i] = arSessions[i].enabled;
            if (pauseUntilScan)
                arSessions[i].enabled = false;
        }

        for (int i = 0; i < imageManagers.Length; i++)
        {
            managerWasEnabled[i] = imageManagers[i].enabled;
            if (pauseUntilScan)
                imageManagers[i].enabled = false;
        }
    }

    private void BuildCanvas()
    {
        screenRoot = new GameObject("Onboarding Overlay", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        screenRoot.transform.SetParent(transform, false);

        Canvas canvas = screenRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = screenRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = screenRoot.GetComponent<RectTransform>();
        Stretch(canvasRect);
        Image background = screenRoot.AddComponent<Image>();
        background.sprite = CreateGradientSprite();
        background.color = Color.white;
        background.raycastTarget = true;

        roundedSprite = CreateRoundedSprite(18f);
        glowSprite = CreateGlowSprite();
        circleSprite = CreateCircleSprite();
        ringSprite = CreateRingSprite();
        gradientButtonSprite = CreateGradientButtonSprite();
        CreateGlow(screenRoot.transform, new Vector2(0.5f, 0.55f),
            new Vector2(1040f, 1040f), new Color32(17, 153, 167, 66));
        CreateGlow(screenRoot.transform, new Vector2(0.98f, 0.92f),
            new Vector2(620f, 620f), new Color32(39, 104, 255, 46));

        GameObject pages = new GameObject("Onboarding Pages", typeof(RectTransform));
        pages.transform.SetParent(screenRoot.transform, false);
        pageCanvasGroup = pages.AddComponent<CanvasGroup>();
        pageRoot = pages.transform;
        Stretch(pages.GetComponent<RectTransform>());

        ShowSplashPage();
    }

    private void ShowSplashPage()
    {
        ClearPage();
        splashProgressFill = null;
        CreateBlock("Splash Backdrop", pageRoot, new Color32(5, 9, 22, 255),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        CreateGlow(pageRoot, new Vector2(0.5f, 0.43f), new Vector2(1000f, 1000f),
            new Color32(88, 70, 210, 58));

        CreateText("Splash App Name", pageRoot,
            "Anato<color=#55DDF2><b>Lens</b></color>", 90f, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.905f),
            new Vector2(900f, 122f), FontStyles.Bold);
        CreateText("Splash Tagline", pageRoot, "See Beyond the Books", 38f,
            new Color32(236, 235, 247, 255), TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.835f), new Vector2(900f, 68f), FontStyles.Normal);
        RectTransform splashUnderline = CreateBlock("Splash Gradient Underline", pageRoot,
            new Color32(94, 178, 255, 255), new Vector2(0.5f, 0.79f),
            new Vector2(0.5f, 0.79f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(370f, 7f));
        splashUnderline.GetComponent<Image>().sprite = gradientButtonSprite;
        splashUnderline.GetComponent<Image>().type = Image.Type.Sliced;
        splashUnderline.GetComponent<Image>().color = Color.white;
        CreateSplashComposition(pageRoot);

        const float barWidth = 560f;
        const float barHeight = 18f;
        RectTransform track = CreateRoundedBlock("Splash Loading Track", pageRoot,
            new Color32(26, 31, 51, 255), new Vector2(0.5f, 0.125f),
            new Vector2(0.5f, 0.125f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(barWidth, barHeight));
        splashProgressWidth = barWidth;
        splashProgressFill = CreateRoundedBlock("Splash Loading Progress", track,
            new Color32(81, 220, 246, 255), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, barHeight));
        Image splashFillImage = splashProgressFill.GetComponent<Image>();
        splashFillImage.sprite = gradientButtonSprite;
        splashFillImage.type = Image.Type.Sliced;
        splashFillImage.color = Color.white;
        TMP_Text splashFooter = CreateText("Splash Footer", pageRoot,
            "EXPLORE     •     INTERACT     •     LEARN", 24f,
            new Color32(193, 201, 228, 255), TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.06f), new Vector2(900f, 48f), FontStyles.Normal);
        splashFooter.characterSpacing = 2f;
        PlayPageTransition();
    }

    private void ShowWelcomePage()
    {
        ClearPage();
        splashProgressFill = null;
        CreateBlock("Welcome Backdrop", pageRoot, new Color32(5, 9, 22, 255),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        CreateWelcomeDecoration(pageRoot);
        CreateText("Welcome Logo", pageRoot,
            "Anato<color=#55DDF2><b>Lens</b></color>", 84f, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.71f),
            new Vector2(900f, 112f), FontStyles.Bold);
        CreateText("Welcome Tagline", pageRoot, "Your Body. A whole new world.", 33f,
            new Color32(226, 229, 241, 255), TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.645f), new Vector2(900f, 58f), FontStyles.Normal);
        RectTransform welcomeUnderline = CreateBlock("Welcome Gradient Underline", pageRoot,
            new Color32(114, 132, 255, 255), new Vector2(0.5f, 0.61f),
            new Vector2(0.5f, 0.61f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(290f, 6f));
        welcomeUnderline.GetComponent<Image>().sprite = gradientButtonSprite;
        welcomeUnderline.GetComponent<Image>().type = Image.Type.Sliced;
        welcomeUnderline.GetComponent<Image>().color = Color.white;
        CreateText("Welcome Heading", pageRoot,
            "Meet your body\n<color=#55DDF2>in</color> <color=#B77CFF><b>3D!</b></color>",
            75f, Color.white, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.365f), new Vector2(970f, 190f), FontStyles.Normal);
        CreateText("Welcome Description", pageRoot,
            "Explore, interact and discover\nanatomy in a whole new way.", 31f,
            new Color32(205, 207, 215, 255), TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.255f), new Vector2(930f, 112f), FontStyles.Normal);
        CreatePrimaryButton("Get Started Button", "Get Started",
            new Vector2(0.5f, 0.18f), ShowInstructionsPage,
            new Color32(255, 255, 255, 255), null, useGradient: true);
        PlayPageTransition();
    }

    private void ShowInstructionsPage()
    {
        ShowInstructionStep(0);
    }

    private void ShowInstructionStep(int step)
    {
        currentInstructionStep = Mathf.Clamp(step, 0, 2);
        ClearPage();
        Color purple = new Color32(173, 91, 255, 255);
        Color panelColor = new Color32(20, 22, 29, 255);
        CreateBlock("Instruction Background", pageRoot, new Color32(8, 9, 13, 255),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform panel = CreateRoundedBlock("Instruction Card", pageRoot, panelColor,
            new Vector2(0.5f, 0.445f), new Vector2(0.5f, 0.445f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 1550f));
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(74, 77, 90, 220);
        outline.effectDistance = new Vector2(2f, -2f);

        UnityEngine.Events.UnityAction backAction;
        if (currentInstructionStep == 0)
            backAction = ShowWelcomePage;
        else
            backAction = () => ShowInstructionStep(currentInstructionStep - 1);
        CreateBackButton(backAction);

        CreateInstructionIllustration(panel, currentInstructionStep, purple);
        for (int i = 0; i < 3; i++)
        {
            RectTransform dot = CreateBlock("Instruction Page Dot " + (i + 1), panel,
                i == currentInstructionStep ? purple : new Color32(76, 78, 88, 255),
                new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f),
                new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 36f, 0f),
                new Vector2(i == currentInstructionStep ? 20f : 16f,
                    i == currentInstructionStep ? 20f : 16f));
            dot.GetComponent<Image>().sprite = circleSprite;
        }

        string[] numbers = { "01", "02", "03" };
        string[] titles = { "Find a supported image", "Point your camera at it", "Watch the 3D model appear" };
        string[] details =
        {
            "Use the AnatoLens reference image from your book or notes.",
            "Keep the image clear and steady while it scans.",
            "Explore, rotate, zoom and learn!"
        };
        CreateText("Instruction Step Number", panel, numbers[currentInstructionStep], 25f,
            purple, TextAlignmentOptions.Left, new Vector2(0.5f, 0.445f),
            new Vector2(790f, 46f), FontStyles.Bold);
        CreateText("Instruction Step Title", panel, titles[currentInstructionStep], 50f,
            Color.white, TextAlignmentOptions.Left, new Vector2(0.5f, 0.375f),
            new Vector2(790f, 132f), FontStyles.Bold);
        CreateText("Instruction Step Detail", panel, details[currentInstructionStep], 29f,
            new Color32(205, 207, 215, 255), TextAlignmentOptions.Left,
            new Vector2(0.5f, 0.285f), new Vector2(790f, 140f), FontStyles.Normal);

        if (currentInstructionStep < 2)
            CreateNextStepButton(panel, purple, () => ShowInstructionStep(currentInstructionStep + 1));
        else
            CreatePrimaryButton("Start Exploring Button", "START SCANNING",
                new Vector2(0.5f, 0.095f), BeginScanning, purple, panel);

        PlayPageTransition();
    }

    private void CreateInstructionIllustration(Transform parent, int step, Color purple)
    {
        RectTransform stage = CreateRoundedBlock("Instruction Illustration Stage", parent,
            new Color32(28, 29, 38, 255), new Vector2(0.5f, 0.745f),
            new Vector2(0.5f, 0.745f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(760f, 560f));
        CreateGlow(stage, new Vector2(0.5f, 0.5f), new Vector2(500f, 430f),
            new Color(purple.r, purple.g, purple.b, 0.2f));

        if (step == 0)
        {
            RectTransform backSheet = CreateRoundedBlock("Reference Image Back Sheet", stage,
                new Color32(198, 199, 210, 255), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-36f, -8f), new Vector2(360f, 390f));
            backSheet.localRotation = Quaternion.Euler(0f, 0f, -9f);
            RectTransform sheet = CreateRoundedBlock("Reference Image Sheet", stage,
                new Color32(243, 239, 236, 255), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(26f, 14f), new Vector2(360f, 410f));
            sheet.localRotation = Quaternion.Euler(0f, 0f, 8f);
            CreateBlock("Reference Heading Line", sheet, new Color32(177, 172, 181, 255),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-56f, 142f), new Vector2(178f, 12f));
            CreateText("Reference Anatomy Icon", sheet, "♥", 150f,
                new Color32(220, 71, 101, 255), TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(240f, 220f), FontStyles.Normal,
                new Vector2(0f, 14f));
            CreateText("Reference Image Caption", sheet, "ANATOMY IMAGE", 20f,
                new Color32(91, 91, 104, 255), TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(280f, 40f), FontStyles.Bold,
                new Vector2(0f, -150f));
        }
        else if (step == 1)
        {
            RectTransform phone = CreateRoundedBlock("Scan Phone", stage,
                new Color32(6, 8, 14, 255), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(320f, 520f));
            Outline phoneOutline = phone.gameObject.AddComponent<Outline>();
            phoneOutline.effectColor = new Color32(112, 113, 127, 255);
            phoneOutline.effectDistance = new Vector2(3f, -3f);
            CreateRoundedBlock("Scan Phone Screen", phone, new Color32(22, 20, 31, 255),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(260f, 410f));
            CreateRoundedBlock("Scan Phone Camera", phone, new Color32(3, 4, 9, 255),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(92f, 16f));
            CreateRoundedBlock("Scanned Reference Target", phone,
                new Color32(236, 230, 228, 255), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -12f), new Vector2(176f, 236f));
            CreateText("Scanned Target Icon", phone, "♥", 92f,
                new Color32(220, 71, 101, 255), TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(150f, 150f), FontStyles.Normal,
                new Vector2(0f, -10f));
            CreateScanBrackets(phone, purple, new Vector2(210f, 268f));
        }
        else
        {
            RectTransform halo = CreateBlock("Model Halo", stage, new Color32(40, 30, 62, 190),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330f, 330f));
            halo.GetComponent<Image>().sprite = circleSprite;
            halo.GetComponent<Image>().color = new Color32(111, 57, 195, 100);
            CreateText("3D Heart Model", stage, "♥", 230f,
                new Color32(245, 91, 125, 255), TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(330f, 330f), FontStyles.Normal);
            RectTransform orbit = CreateBlock("3D Model Orbit", stage,
                new Color32(185, 104, 255, 220), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(570f, 210f));
            orbit.GetComponent<Image>().sprite = ringSprite;
            orbit.localRotation = Quaternion.Euler(0f, 0f, -13f);
        }
    }

    private void CreateScanBrackets(Transform parent, Color color, Vector2 size)
    {
        const float length = 42f;
        const float thickness = 7f;
        Vector2 center = new Vector2(0.5f, 0.5f);
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        Vector2[] corners =
        {
            new Vector2(-halfWidth, halfHeight), new Vector2(halfWidth, halfHeight),
            new Vector2(-halfWidth, -halfHeight), new Vector2(halfWidth, -halfHeight)
        };
        for (int i = 0; i < corners.Length; i++)
        {
            bool right = i % 2 == 1;
            bool top = i < 2;
            float xDirection = right ? -1f : 1f;
            float yDirection = top ? -1f : 1f;
            CreateBlock("Scan Bracket Horizontal", parent, color, center, center, center,
                corners[i] + new Vector2(xDirection * length * 0.5f, 0f),
                new Vector2(length, thickness));
            CreateBlock("Scan Bracket Vertical", parent, color, center, center, center,
                corners[i] + new Vector2(0f, yDirection * length * 0.5f),
                new Vector2(thickness, length));
        }
    }

    private void CreateNextStepButton(Transform parent, Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject("Next Instruction Button",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.82f, 0.105f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(154f, 154f);
        Image image = buttonObject.GetComponent<Image>();
        image.sprite = circleSprite;
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        Shadow shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(color.r, color.g, color.b, 0.38f);
        shadow.effectDistance = new Vector2(0f, -5f);
        CreateText("Next Step Arrow", buttonObject.transform, "→", 68f, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f),
            new Vector2(130f, 130f), FontStyles.Normal);
    }

    private void CreateBrandHeader(Transform parent)
    {
        CreateBrandMark(parent, new Vector2(0.36f, 0.94f), 0.7f);
        CreateText("Brand Name", parent, "AnatoLens", 28f, Color.white,
            TextAlignmentOptions.MidlineLeft, new Vector2(0.53f, 0.94f),
            new Vector2(300f, 64f), FontStyles.Bold);
        CreateText("Brand Descriptor", parent, "ANATOMY  •  IN AUGMENTED REALITY", 14f,
            MutedTextColor, TextAlignmentOptions.MidlineLeft, new Vector2(0.56f, 0.902f),
            new Vector2(370f, 34f), FontStyles.Bold);
    }

    private void CreateSplashComposition(Transform parent)
    {
        CreateGlow(parent, new Vector2(0.5f, 0.46f), new Vector2(980f, 980f),
            new Color32(82, 79, 228, 68));

        Texture2D heroTexture = Resources.Load<Texture2D>("AnatoLensSplashHero");
        if (heroTexture == null)
        {
            Debug.LogWarning("AnatoLens splash hero texture was not found in Resources.");
            return;
        }

        Sprite heroSprite = Sprite.Create(heroTexture,
            new Rect(0f, 0f, heroTexture.width, heroTexture.height),
            new Vector2(0.5f, 0.5f), 100f);
        RectTransform hero = CreateBlock("Splash Anatomy Illustration", parent, Color.white,
            new Vector2(0.5f, 0.46f), new Vector2(0.5f, 0.46f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 1290f));
        Image heroImage = hero.GetComponent<Image>();
        heroImage.sprite = heroSprite;
        heroImage.preserveAspect = true;
        heroImage.raycastTarget = false;

        activeHeroCore = hero;
        activeHeroCoreScale = hero.localScale;
        activeOrbit = null;
    }

    private void CreateWelcomeDecoration(Transform parent)
    {
        CreateGlow(parent, new Vector2(0.5f, 0.42f), new Vector2(1000f, 1000f),
            new Color32(58, 73, 193, 37));
        CreateDecorativeCircle(parent, new Vector2(-0.06f, 1.02f), 500f,
            new Color32(59, 75, 207, 140));
        CreateDecorativeCircle(parent, new Vector2(1.06f, 0.76f), 250f,
            new Color32(91, 57, 194, 112));
        CreateDecorativeCircle(parent, new Vector2(-0.07f, 0.08f), 470f,
            new Color32(30, 58, 157, 145));
        CreateDecorativeCircle(parent, new Vector2(1.08f, -0.04f), 420f,
            new Color32(34, 47, 151, 135));
        CreateDecorativeOrbit(parent, new Vector2(1.05f, 0.98f), new Vector2(700f, 700f),
            new Color32(88, 105, 255, 200), -25f);
        CreateDecorativeOrbit(parent, new Vector2(-0.06f, 0.13f), new Vector2(500f, 500f),
            new Color32(91, 80, 227, 180), 35f);
        CreateDecorativeOrbit(parent, new Vector2(0.91f, 0.22f), new Vector2(600f, 600f),
            new Color32(96, 76, 224, 165), -32f);
        CreateDecorativeCircle(parent, new Vector2(0.13f, 0.87f), 16f,
            new Color32(67, 126, 255, 255));
        CreateDecorativeCircle(parent, new Vector2(0.86f, 0.91f), 18f,
            new Color32(133, 99, 255, 255));
        CreateDecorativeCircle(parent, new Vector2(0.16f, 0.09f), 24f,
            new Color32(117, 78, 242, 255));
        CreateDecorativeCircle(parent, new Vector2(0.91f, 0.25f), 17f,
            new Color32(132, 91, 255, 255));
    }

    private void CreateDecorativeCircle(Transform parent, Vector2 anchor, float size, Color color)
    {
        RectTransform circle = CreateBlock("Welcome Decorative Orb", parent, color,
            anchor, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
        circle.GetComponent<Image>().sprite = circleSprite;
    }

    private void CreateDecorativeOrbit(Transform parent, Vector2 anchor,
        Vector2 size, Color color, float rotation)
    {
        RectTransform orbit = CreateBlock("Welcome Decorative Orbit", parent, color,
            anchor, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        orbit.GetComponent<Image>().sprite = ringSprite;
        orbit.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private void CreateBrandMark(Transform parent, Vector2 anchor, float scale)
    {
        float side = 82f * scale;
        RectTransform mark = CreateRoundedBlock("AnatoLens Scan Mark", parent,
            new Color32(19, 48, 69, 255), anchor, anchor, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(side, side));
        float length = 17f * scale;
        float stroke = 4f * scale;
        Vector2 center = new Vector2(0.5f, 0.5f);
        CreateMarkCorner(mark, new Vector2(-15f * scale, 15f * scale), 1f, 1f, length, stroke, center);
        CreateMarkCorner(mark, new Vector2(15f * scale, 15f * scale), -1f, 1f, length, stroke, center);
        CreateMarkCorner(mark, new Vector2(-15f * scale, -15f * scale), 1f, -1f, length, stroke, center);
        CreateMarkCorner(mark, new Vector2(15f * scale, -15f * scale), -1f, -1f, length, stroke, center);
        CreateRoundedBlock("Scan Center", mark, AccentColor, center, center, center,
            Vector2.zero, new Vector2(9f * scale, 9f * scale));
    }

    private void CreateMarkCorner(Transform parent, Vector2 position, float xDirection,
        float yDirection, float length, float stroke, Vector2 anchor)
    {
        CreateBlock("Brand Corner Horizontal", parent, AccentColor, anchor, anchor, anchor,
            position + new Vector2(xDirection * length * 0.5f, 0f), new Vector2(length, stroke));
        CreateBlock("Brand Corner Vertical", parent, AccentColor, anchor, anchor, anchor,
            position + new Vector2(0f, yDirection * length * 0.5f), new Vector2(stroke, length));
    }

    private void CreateProgress(Transform parent, int currentStep)
    {
        CreateText("Intro Progress Label", parent, "GETTING STARTED     •     " + currentStep + " OF 2",
            17f, MutedTextColor, TextAlignmentOptions.Center, new Vector2(0.5f, 0.855f),
            new Vector2(560f, 42f), FontStyles.Bold);
        for (int i = 0; i < 2; i++)
        {
            RectTransform segment = CreateRoundedBlock("Intro Progress " + (i + 1), parent,
                i < currentStep ? AccentColor : SecondaryColor,
                new Vector2(0.5f, 0.825f), new Vector2(0.5f, 0.825f),
                new Vector2(0.5f, 0.5f), new Vector2(i == 0 ? -54f : 54f, 0f),
                new Vector2(96f, 8f));
            segment.name = "Intro Progress " + (i + 1);
        }
    }

    private void CreateHeroCard(Transform parent)
    {
        RectTransform card = CreateRoundedBlock("Welcome Hero Card", parent,
            new Color32(14, 32, 53, 244), new Vector2(0.5f, 0.445f),
            new Vector2(0.5f, 0.445f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(820f, 540f));
        CreateGlow(card, new Vector2(0.5f, 0.61f), new Vector2(560f, 440f),
            new Color32(35, 198, 200, 78));

        CreatePill(card, "Hero Category", "INTERACTIVE ANATOMY", new Vector2(0.5f, 0.91f),
            new Vector2(360f, 46f), new Color32(22, 57, 76, 255));

        Texture2D robotTexture = Resources.Load<Texture2D>("AnatoLensRobot");
        if (robotTexture != null)
        {
            GameObject robot = new GameObject("Welcome Robot Illustration",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            robot.transform.SetParent(card, false);
            RectTransform robotRect = robot.GetComponent<RectTransform>();
            robotRect.anchorMin = robotRect.anchorMax = new Vector2(0.5f, 0.5f);
            robotRect.pivot = new Vector2(0.5f, 0.5f);
            robotRect.anchoredPosition = new Vector2(0f, 26f);
            robotRect.sizeDelta = new Vector2(300f, 340f);
            Image robotImage = robot.GetComponent<Image>();
            robotImage.sprite = Sprite.Create(robotTexture,
                new Rect(0f, 0f, robotTexture.width, robotTexture.height),
                new Vector2(0.5f, 0.5f), 100f);
            robotImage.preserveAspect = true;
            robotImage.raycastTarget = false;
        }

        CreatePill(card, "Hero Feature One", "SCAN", new Vector2(0.25f, 0.12f),
            new Vector2(170f, 46f), new Color32(20, 48, 68, 255));
        CreatePill(card, "Hero Feature Two", "EXPLORE 3D", new Vector2(0.5f, 0.12f),
            new Vector2(230f, 46f), new Color32(20, 48, 68, 255));
        CreatePill(card, "Hero Feature Three", "LEARN", new Vector2(0.75f, 0.12f),
            new Vector2(170f, 46f), new Color32(20, 48, 68, 255));
    }

    private void CreatePill(Transform parent, string name, string label,
        Vector2 anchor, Vector2 size, Color color)
    {
        RectTransform pill = CreateRoundedBlock(name, parent, color, anchor, anchor,
            new Vector2(0.5f, 0.5f), Vector2.zero, size);
        CreateText(name + " Label", pill, label, 16f, AccentColor,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), size - new Vector2(14f, 8f),
            FontStyles.Bold);
    }

    private void CreateGlow(Transform parent, Vector2 anchor, Vector2 size, Color tint)
    {
        GameObject glowObject = new GameObject("Soft Accent Glow",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glowObject.transform.SetParent(parent, false);
        RectTransform rect = glowObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        Image image = glowObject.GetComponent<Image>();
        image.sprite = glowSprite;
        image.color = tint;
        image.raycastTarget = false;
    }

    private Sprite CreateGradientSprite()
    {
        const int height = 256;
        Texture2D texture = new Texture2D(2, height, TextureFormat.RGBA32, false);
        texture.name = "AnatoLens Welcome Background Gradient";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color top = new Color32(16, 34, 59, 255);
        Color bottom = BackgroundColor;
        for (int y = 0; y < height; y++)
        {
            Color color = Color.Lerp(bottom, top, y / (float)(height - 1));
            texture.SetPixel(0, y, color);
            texture.SetPixel(1, y, color);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 2f, height),
            new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateGlowSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "AnatoLens Soft Radial Glow";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - (size - 1) * 0.5f) / (size * 0.5f);
            float dy = (y - (size - 1) * 0.5f) / (size * 0.5f);
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            byte alpha = (byte)(Mathf.Clamp01(1f - radius) * Mathf.Clamp01(1f - radius) * 255f);
            texture.SetPixel(x, y, new Color32(255, 255, 255, alpha));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateCircleSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "AnatoLens UI Circle";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
            byte alpha = (byte)(Mathf.Clamp01(radius + 0.75f - distance) * 255f);
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRingSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "AnatoLens UI Orbit Ring";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = size * 0.43f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
            float edge = Mathf.Abs(distance - radius);
            byte alpha = (byte)(Mathf.Clamp01(3.25f - edge) / 3.25f * 255f);
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateGradientButtonSprite()
    {
        const int width = 128;
        const int height = 64;
        const float radius = 30f;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "AnatoLens Cyan Violet Button Gradient";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[width * height];
        Color left = new Color32(62, 222, 245, 255);
        Color right = new Color32(185, 105, 255, 255);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float dx = Mathf.Max(Mathf.Abs(x - (width - 1) * 0.5f) - (width * 0.5f - radius), 0f);
            float dy = Mathf.Max(Mathf.Abs(y - (height - 1) * 0.5f) - (height * 0.5f - radius), 0f);
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            float alpha = Mathf.Clamp01(radius + 0.75f - distance);
            Color color = Color.Lerp(left, right, x / (float)(width - 1));
            color.a *= alpha;
            pixels[y * width + x] = color;
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
    }

    private Sprite CreateRoundedSprite(float radius)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "AnatoLens Rounded UI Card";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(Mathf.Max(radius - x, 0f), x - (size - 1f - radius));
            float dy = Mathf.Max(Mathf.Max(radius - y, 0f), y - (size - 1f - radius));
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            byte alpha = distance <= radius - 1f ? (byte)255 :
                distance >= radius ? (byte)0 : (byte)128;
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
    }

    private RectTransform CreateRoundedBlock(string name, Transform parent, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateBlock(name, parent, color,
            anchorMin, anchorMax, pivot, position, size);
        Image image = rect.GetComponent<Image>();
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        return rect;
    }

    private void CreatePrimaryButton(string objectName, string label, Vector2 anchor,
        UnityEngine.Events.UnityAction onClick, Color? tint = null, Transform parent = null,
        bool useGradient = false)
    {
        Color buttonColor = tint ?? new Color32(12, 135, 145, 255);
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform),
            typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent != null ? parent : pageRoot, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(720f, 128f);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = useGradient ? gradientButtonSprite : roundedSprite;
        image.type = Image.Type.Sliced;
        image.color = useGradient ? Color.white : buttonColor;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = useGradient ? Color.white : buttonColor;
        colors.highlightedColor = useGradient ? new Color(0.92f, 0.96f, 1f, 1f) :
            Color.Lerp(buttonColor, Color.white, 0.12f);
        colors.pressedColor = useGradient ? new Color(0.78f, 0.82f, 0.9f, 1f) :
            Color.Lerp(buttonColor, Color.black, 0.15f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);
        Shadow shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
        shadow.effectDistance = new Vector2(0f, -8f);

        Color buttonTextColor = useGradient ? new Color32(8, 27, 56, 255) : Color.white;
        CreateText("Button Label", buttonObject.transform, label, 31f, buttonTextColor,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f),
            new Vector2(550f, 100f), FontStyles.Bold, new Vector2(-30f, 0f));
        CreateText("Button Arrow", buttonObject.transform, "→", 46f, buttonTextColor,
            TextAlignmentOptions.Center, new Vector2(1f, 0.5f),
            new Vector2(100f, 100f), FontStyles.Normal, new Vector2(-66f, 0f));
    }

    private void CreateBackButton(UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject("Back Button", typeof(RectTransform),
            typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(pageRoot, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = new Vector2(34f, -250f);
        buttonRect.sizeDelta = new Vector2(96f, 96f);

        Image background = buttonObject.GetComponent<Image>();
        background.sprite = roundedSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color32(18, 22, 31, 170);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(onClick);
        CreateText("Back Arrow", buttonObject.transform, "←", 60f, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f),
            new Vector2(90f, 90f), FontStyles.Normal, new Vector2(-2f, 2f));
    }

    private static RectTransform CreateBlock(string name, Transform parent, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject block = new GameObject(name, typeof(RectTransform), typeof(Image));
        block.transform.SetParent(parent, false);
        RectTransform rect = block.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = block.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value,
        float fontSize, Color color, TextAlignmentOptions alignment,
        Vector2 anchor, Vector2 size, FontStyles style, Vector2 position = default)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private void ClearPage()
    {
        if (pageRoot == null)
            return;

        activeOrbit = null;
        activeHeroCore = null;
        splashProgressFill = null;

        if (pageTransition != null)
        {
            StopCoroutine(pageTransition);
            pageTransition = null;
        }

        if (pageCanvasGroup != null)
        {
            pageCanvasGroup.alpha = 1f;
            pageCanvasGroup.interactable = true;
            pageCanvasGroup.blocksRaycasts = true;
        }

        for (int i = pageRoot.childCount - 1; i >= 0; i--)
            Destroy(pageRoot.GetChild(i).gameObject);
    }

    private void PlayPageTransition()
    {
        if (pageCanvasGroup == null)
            return;

        pageCanvasGroup.alpha = 0f;
        pageCanvasGroup.interactable = false;
        pageCanvasGroup.blocksRaycasts = false;
        pageRoot.localScale = Vector3.one * 0.985f;
        pageTransition = StartCoroutine(AnimatePageIn());
    }

    private IEnumerator AnimatePageIn()
    {
        const float duration = 0.32f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            pageCanvasGroup.alpha = eased;
            pageRoot.localScale = Vector3.Lerp(Vector3.one * 0.985f, Vector3.one, eased);
            yield return null;
        }

        pageCanvasGroup.alpha = 1f;
        pageRoot.localScale = Vector3.one;
        pageCanvasGroup.interactable = true;
        pageCanvasGroup.blocksRaycasts = true;
        pageTransition = null;
    }

    private void BeginScanning()
    {
        activeOrbit = null;
        activeHeroCore = null;
        splashProgressFill = null;
        screenRoot.SetActive(false);

        for (int i = 0; i < arSessions.Length; i++)
            if (arSessions[i] != null && sessionWasEnabled[i])
                arSessions[i].enabled = true;

        for (int i = 0; i < imageManagers.Length; i++)
            if (imageManagers[i] != null && managerWasEnabled[i])
                imageManagers[i].enabled = true;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
