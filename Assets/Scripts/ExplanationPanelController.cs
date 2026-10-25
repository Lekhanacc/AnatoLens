using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExplanationPanelController : MonoBehaviour
{
    public static ExplanationPanelController Instance { get; private set; }

    [Header("UI References")]
    public GameObject panelRoot;
    public TMP_Text explanationText;

    private RectTransform card;
    private RectTransform viewport;
    private RectTransform content;
    private TMP_Text titleText;
    private Button closeButton;
    private LayoutElement explanationLayout;
    private GameObject resourcesHeadingObject;
    private readonly List<GameObject> resourceButtons = new List<GameObject>();
    private bool panelBuilt;

    private struct Resource
    {
        public string title;
        public string url;
        public Resource(string title, string url) { this.title = title; this.url = url; }
    }

    private struct LearningContent
    {
        public string heading;
        public string body;
        public Resource[] resources;
        public LearningContent(string heading, string body, params Resource[] resources)
        {
            this.heading = heading;
            this.body = body;
            this.resources = resources;
        }
    }

    private void Awake()
    {
        Instance = this;
        if (panelRoot != null)
        {
            BuildPanel();
            panelRoot.SetActive(false);
        }
    }

    public void RequestExplanation()
    {
        if (Instance == null)
            Instance = this;
        if (panelRoot == null || explanationText == null)
        {
            Debug.LogError("[Learning] Explanation panel references are not assigned.");
            return;
        }
        if (!panelBuilt)
            BuildPanel();

        LabelToggleController.HideOpenLabels();
        ChatbotController.CloseOpenChat();
        MenuToggle.CloseOpenMenu();

        LearningContent lesson = GetLesson(ModelInfo.ActiveModel != null
            ? ModelInfo.ActiveModel.organName
            : "Anatomy");
        titleText.text = lesson.heading;
        explanationText.text = lesson.body;
        RebuildResourceButtons(lesson.resources);
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        LayoutBody();
    }

    public void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public static void CloseOpenExplanation()
    {
        if (Instance != null)
            Instance.HidePanel();
    }

    private void BuildPanel()
    {
        if (panelBuilt)
            return;
        RectTransform root = panelRoot.GetComponent<RectTransform>();
        if (root == null)
            return;
        panelBuilt = true;

        Image backdrop = panelRoot.GetComponent<Image>();
        if (backdrop == null)
            backdrop = panelRoot.AddComponent<Image>();
        backdrop.color = new Color(0.015f, 0.025f, 0.045f, 0.94f);
        backdrop.raycastTarget = true;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        card = CreateRect("LearningCard", root, new Color(0.045f, 0.065f, 0.095f, 1f));
        card.anchorMin = new Vector2(0.035f, 0.035f);
        card.anchorMax = new Vector2(0.965f, 0.965f);
        card.offsetMin = card.offsetMax = Vector2.zero;

        titleText = CreateText("LearningTitle", card, 34, FontStyles.Bold);
        titleText.rectTransform.anchorMin = new Vector2(0.06f, 0.84f);
        titleText.rectTransform.anchorMax = new Vector2(0.80f, 0.98f);
        titleText.rectTransform.offsetMin = titleText.rectTransform.offsetMax = Vector2.zero;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = new Color(0.42f, 0.9f, 0.85f);

        TMP_Text intro = CreateText("LearningSubtitle", card, 20, FontStyles.Normal);
        intro.text = "A quick guide and trusted resources";
        intro.color = new Color(0.78f, 0.83f, 0.9f);
        intro.rectTransform.anchorMin = new Vector2(0.06f, 0.79f);
        intro.rectTransform.anchorMax = new Vector2(0.84f, 0.85f);
        intro.rectTransform.offsetMin = intro.rectTransform.offsetMax = Vector2.zero;

        closeButton = panelRoot.transform.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton == null)
            closeButton = CreateCloseButton(card);
        else
        {
            closeButton.transform.SetParent(card, false);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.94f, 0.92f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.sizeDelta = new Vector2(68f, 64f);
            closeRect.anchoredPosition = Vector2.zero;
            closeButton.onClick.RemoveListener(HidePanel);
            closeButton.onClick.AddListener(HidePanel);
            Image closeImage = closeButton.GetComponent<Image>();
            if (closeImage != null) closeImage.color = new Color(0.13f, 0.2f, 0.29f, 1f);
            TMP_Text closeLabel = closeButton.GetComponentInChildren<TMP_Text>(true);
            if (closeLabel != null)
            {
                closeLabel.text = "×";
                closeLabel.fontSize = 40f;
                closeLabel.alignment = TextAlignmentOptions.Center;
                closeLabel.color = Color.white;
            }
        }

        GameObject viewportObject = new GameObject("LearningViewport",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(ScrollRect));
        viewportObject.transform.SetParent(card, false);
        viewport = viewportObject.GetComponent<RectTransform>();
        viewport.anchorMin = new Vector2(0.06f, 0.08f);
        viewport.anchorMax = new Vector2(0.94f, 0.77f);
        viewport.offsetMin = viewport.offsetMax = Vector2.zero;
        viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.025f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject("LearningContent",
            typeof(RectTransform), typeof(CanvasRenderer));
        contentObject.transform.SetParent(viewportObject.transform, false);
        content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(16, 16, 16, 16);
        contentLayout.spacing = 16f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter contentFitter = contentObject.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        explanationText.transform.SetParent(content, false);
        explanationText.rectTransform.anchorMin = new Vector2(0f, 1f);
        explanationText.rectTransform.anchorMax = new Vector2(1f, 1f);
        explanationText.rectTransform.pivot = new Vector2(0f, 1f);
        explanationText.rectTransform.anchoredPosition = Vector2.zero;
        explanationText.rectTransform.sizeDelta = Vector2.zero;
        explanationText.fontSize = 24f;
        explanationText.color = new Color(0.92f, 0.94f, 0.98f);
        explanationText.alignment = TextAlignmentOptions.TopLeft;
        explanationText.enableWordWrapping = true;
        explanationText.raycastTarget = false;
        explanationLayout = explanationText.gameObject.GetComponent<LayoutElement>();
        if (explanationLayout == null)
            explanationLayout = explanationText.gameObject.AddComponent<LayoutElement>();
        explanationLayout.flexibleHeight = 0f;

        ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

    }

    private void LayoutBody()
    {
        Canvas.ForceUpdateCanvases();
        float width = Mathf.Max(200f, viewport.rect.width - 48f);
        Vector2 preferred = explanationText.GetPreferredValues(explanationText.text, width, 0f);
        explanationLayout.preferredHeight = Mathf.Max(120f, preferred.y + 8f);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private void RebuildResourceButtons(Resource[] resources)
    {
        if (resourcesHeadingObject != null)
            Destroy(resourcesHeadingObject);
        foreach (GameObject button in resourceButtons)
            if (button != null) Destroy(button);
        resourceButtons.Clear();

        if (resources == null || resources.Length == 0)
            return;

        resourcesHeadingObject = new GameObject("ResourcesHeading",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        resourcesHeadingObject.transform.SetParent(content, false);
        TMP_Text resourcesHeading = resourcesHeadingObject.GetComponent<TMP_Text>();
        resourcesHeading.font = explanationText.font;
        resourcesHeading.fontSharedMaterial = explanationText.fontSharedMaterial;
        resourcesHeading.fontSize = 23f;
        resourcesHeading.fontStyle = FontStyles.Bold;
        resourcesHeading.enableWordWrapping = true;
        resourcesHeading.raycastTarget = false;
        resourcesHeading.text = "WATCH & READ";
        resourcesHeading.color = new Color(0.42f, 0.9f, 0.85f);
        resourcesHeading.rectTransform.sizeDelta = Vector2.zero;
        LayoutElement headingLayout = resourcesHeading.gameObject.AddComponent<LayoutElement>();
        headingLayout.minHeight = headingLayout.preferredHeight = 42f;

        float rowHeight = 58f;

        for (int i = 0; i < resources.Length; i++)
        {
            Resource item = resources[i];
            GameObject buttonObject = new GameObject("Resource_" + i,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(content, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, rowHeight);
            LayoutElement buttonLayout = buttonObject.AddComponent<LayoutElement>();
            buttonLayout.minHeight = buttonLayout.preferredHeight = rowHeight;
            buttonLayout.flexibleHeight = 0f;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.2f, 0.3f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            string url = item.url;
            button.onClick.AddListener(() => Application.OpenURL(url));

            TMP_Text label = CreateText("ResourceLabel", buttonObject.transform, 19, FontStyles.Bold);
            label.text = "↗  " + item.title;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(18f, 2f);
            label.rectTransform.offsetMax = new Vector2(-14f, -2f);
            label.raycastTarget = false;

            resourceButtons.Add(buttonObject);
        }
    }

    private RectTransform CreateRect(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    private TMP_Text CreateText(string name, Transform parent, float size, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.font = explanationText.font;
        text.fontSharedMaterial = explanationText.fontSharedMaterial;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateCloseButton(Transform parent)
    {
        GameObject go = new GameObject("CloseLearningButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.94f, 0.92f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(68f, 64f);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        button.onClick.AddListener(HidePanel);
        TMP_Text label = CreateText("CloseLabel", go.transform, 40f, FontStyles.Normal);
        label.text = "×";
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static LearningContent GetLesson(string rawName)
    {
        string name = (rawName ?? "Anatomy").ToLowerInvariant();
        if (name.Contains("neuron"))
            return new LearningContent("Neuron",
                "WHAT A NEURON DOES\nNeurons are specialized cells that receive information, combine signals, and pass messages to other neurons, muscles, or glands. They are the basic signaling units of the nervous system.\n\nMAIN PARTS\nDendrites receive incoming signals and carry them toward the cell body. The cell body contains the nucleus and maintains the cell. The axon carries an electrical impulse away from the cell body; its branches can contact many target cells.\n\nHOW A MESSAGE TRAVELS\nWhen incoming signals bring a neuron to threshold, an action potential travels along its axon. At most synapses, the electrical signal triggers release of chemical messengers called neurotransmitters. These cross the tiny synaptic gap and bind to receptors on the next cell.\n\nWHY THE SHAPE MATTERS\nLong axons can carry signals over long distances. Myelin, when present, insulates sections of an axon and helps impulses travel faster. Dendritic branches give a neuron many places to receive information. Real neurons differ greatly in shape depending on their jobs.",
                new Resource("Neuron anatomy video", "https://www.khanacademy.org/science/shs-earth-and-life-science/x901efce4a07b98a5%3Aunit-11/x901efce4a07b98a5%3Anervous-and-endocrine-system/v/anatomy-of-a-neuron"),
                new Resource("Neural cell types video", "https://www.khanacademy.org/science/health-and-medicine/human-anatomy-and-physiology/human-anatomy-and-physiology/nervous-system-introduction/v/introduction-to-neural-cell-types"));
        if (name.Contains("plant"))
            return new LearningContent("Plant cell",
                "A plant cell is a eukaryotic cell: its DNA is enclosed in a nucleus and it contains specialized organelles. The cell membrane controls exchange with the surroundings, while the cell wall outside it provides support and helps the cell keep its shape.\n\nCHLOROPLASTS AND ENERGY\nChloroplasts contain chlorophyll and capture light energy. During photosynthesis, the cell uses light, water, and carbon dioxide to make energy-rich sugars and releases oxygen. The sugars can be used in respiration or stored for later.\n\nVACUOLE AND SUPPORT\nA large central vacuole stores water and dissolved substances. When full, it presses outward against the cell wall, helping keep plant tissues firm. The vacuole can also hold pigments, nutrients, or waste products.\n\nSHARED ORGANELLES\nPlant cells also have mitochondria, which release usable energy from food; ribosomes, which build proteins; and an endoplasmic reticulum and Golgi apparatus, which help make and transport cell materials. Not every plant cell contains visible chloroplasts; for example, most root cells are not exposed to light.",
                new Resource("Plant and animal cells video", "https://www.khanacademy.org/science/ms-biology/x0c5bb03129646fd6%3Acells-and-organisms/x0c5bb03129646fd6%3Acell-parts-and-functions/v/comparing-plant-and-animal-cells"),
                new Resource("OpenStax: eukaryotic cells", "https://openstax.org/books/concepts-biology/pages/3-3-eukaryotic-cells"));
        if (name.Contains("animal cell"))
            return new LearningContent("Animal cell",
                "An animal cell is a eukaryotic cell with DNA enclosed inside a nucleus. Its flexible cell membrane separates the cell from its surroundings and regulates which substances enter and leave. The cytoplasm is the fluid-rich interior where many cell reactions take place.\n\nENERGY AND PROTEINS\nMitochondria convert energy from food into ATP, a form cells can use. Ribosomes build proteins from genetic instructions. The endoplasmic reticulum helps make proteins and lipids; the Golgi apparatus modifies, sorts, and packages materials for delivery.\n\nRECYCLING AND ORGANIZATION\nLysosomes contain enzymes that break down materials and worn-out cell parts. The cytoskeleton helps the cell maintain its shape, move, and organize internal transport. The centrosome helps organize structures involved in cell division.\n\nAnimal cells share many organelles with plant cells, including a nucleus, mitochondria, ribosomes, and a cell membrane. Animal cells do not have a cellulose cell wall or chloroplasts, so they obtain energy-rich nutrients by taking in food rather than photosynthesizing.",
                new Resource("Plant and animal cells video", "https://www.khanacademy.org/science/ms-biology/x0c5bb03129646fd6%3Acells-and-organisms/x0c5bb03129646fd6%3Acell-parts-and-functions/v/comparing-plant-and-animal-cells"),
                new Resource("OpenStax: eukaryotic cells", "https://openstax.org/books/concepts-biology/pages/3-3-eukaryotic-cells"));
        if (name.Contains("nephron"))
            return new LearningContent("Nephron",
                "The nephron is the kidney’s microscopic working unit. Each kidney contains many nephrons that filter blood and adjust the filtered fluid to help control water, salts, pH, and waste levels. Urine is the fluid that remains after these adjustments.\n\n1. FILTRATION\nBlood enters a tuft of capillaries called the glomerulus. Pressure moves water and small dissolved substances into Bowman’s capsule, forming filtrate. Blood cells and most large proteins normally remain in the bloodstream.\n\n2. REABSORPTION\nAs filtrate passes through the proximal tubule, loop of Henle, and distal tubule, the body takes back needed water, glucose, and ions. The loop of Henle helps establish a concentration gradient in the kidney, allowing the body to conserve water when needed.\n\n3. SECRETION AND OUTPUT\nSome substances move from blood into the tubule for removal. The collecting duct makes final adjustments to water and salt content. The resulting urine drains toward the renal pelvis, then travels down a ureter to the bladder. Hormones such as ADH influence how much water is reabsorbed.",
                new Resource("Kidney and nephron video", "https://www.khanacademy.org/science/health-and-medicine/renal-system/Introduction-to-the-kidneys/v/the-kidney-and-nephron"),
                new Resource("OpenStax: urine formation", "https://openstax.org/books/anatomy-and-physiology-2e/pages/25-5-physiology-of-urine-formation"));
        if (name.Contains("kidney"))
            return new LearningContent("Kidney",
                "The kidneys are paired organs that continuously filter blood and maintain a stable internal environment. They remove wastes such as urea and help regulate water volume, electrolytes, and blood acidity. The filtered waste and excess water leave the body as urine.\n\nFROM BLOOD TO URINE\nBlood enters each kidney through a renal artery and passes through millions of microscopic filtering units called nephrons. Nephrons filter fluid, return needed water and substances to the blood, and add some wastes to the forming urine.\n\nTHE URINE ROUTE\nUrine drains through collecting ducts into the renal pelvis, then travels through a ureter to the urinary bladder. The bladder stores urine until it is released through the urethra. The kidneys also release hormones that help regulate red blood cell production and blood pressure, and they activate vitamin D.\n\nThe kidneys do not simply remove everything from blood: filtration is followed by selective reabsorption, so useful substances are conserved while the body's composition is kept within healthy limits.",
                new Resource("Kidney and nephron video", "https://www.khanacademy.org/science/health-and-medicine/renal-system/Introduction-to-the-kidneys/v/the-kidney-and-nephron"),
                new Resource("OpenStax: urinary system", "https://openstax.org/books/anatomy-and-physiology-2e/pages/25-introduction"));
        if (name.Contains("digest") || name.Contains("stomach"))
            return new LearningContent("Digestive system",
                "The digestive system turns food into small molecules that the body can absorb and use. It also absorbs water and eliminates material that cannot be digested. Food moves through a continuous tract, while organs such as the liver and pancreas add substances that help digestion.\n\nTHE FOOD PATH\nDigestion begins in the mouth, where chewing breaks food into pieces and saliva starts chemical digestion. The esophagus moves swallowed food to the stomach. The stomach churns food and mixes it with acid and enzymes, producing a fluid mixture that passes into the small intestine.\n\nABSORPTION\nIn the small intestine, enzymes from the pancreas and bile from the liver help digest nutrients. The intestinal lining has folds and tiny projections that create a large surface for absorption. Sugars and amino acids enter blood capillaries; many fats enter lymphatic vessels.\n\nWATER AND WASTE\nThe large intestine absorbs additional water and salts. Helpful gut microbes also live there. Remaining material is compacted into stool and stored in the rectum before elimination. Digestion is coordinated by muscle contractions, secretions, and signals between the gut and nervous system.",
                new Resource("OpenStax: digestive system overview", "https://openstax.org/books/anatomy-and-physiology-2e/pages/23-1-overview-of-the-digestive-system"),
                new Resource("OpenStax: digestive system", "https://openstax.org/books/anatomy-and-physiology-2e/pages/23-introduction"));
        if (name.Contains("reproductive") || name.Contains("uter"))
            return new LearningContent("Female reproductive system",
                "The female reproductive system produces oocytes, supports fertilization, and, if pregnancy occurs, provides an environment for development. It also produces hormones that influence reproductive tissues and the menstrual cycle.\n\nOVARIES AND UTERINE TUBES\nThe ovaries contain developing follicles and release an oocyte during ovulation. The fimbriae near the opening of a uterine tube help guide the released oocyte into the tube. Fertilization most often occurs in a uterine tube. Tiny cilia and smooth-muscle contractions help move the oocyte or early embryo toward the uterus.\n\nUTERUS\nThe uterus has a muscular wall and an inner lining called the endometrium. During the menstrual cycle, the endometrium changes in response to hormones. If implantation does not occur, much of the lining is shed during menstruation. If implantation occurs, the uterus can support pregnancy.\n\nThe cervix is the lower opening of the uterus. The vagina connects the cervix to the exterior and also functions in menstruation and childbirth. The structures shown in an anatomical model are arranged in three dimensions, so left and right depend on the viewing direction.",
                new Resource("OpenStax: female reproductive anatomy", "https://openstax.org/books/anatomy-and-physiology/pages/27-2-anatomy-and-physiology-of-the-female-reproductive-system"));
        if (name.Contains("heart"))
            return new LearningContent("Heart",
                "The heart is a muscular pump that keeps blood moving through two linked circuits. The pulmonary circuit carries blood between the heart and lungs; the systemic circuit carries blood between the heart and the rest of the body.\n\nFOUR CHAMBERS\nThe right atrium receives blood returning from the body and passes it to the right ventricle. The right ventricle pumps it to the lungs, where carbon dioxide leaves the blood and oxygen enters. The left atrium receives this oxygen-rich blood and passes it to the left ventricle. The left ventricle pumps blood through the aorta to the body.\n\nVALVES AND HEART WALL\nFour valves open and close as pressure changes, helping prevent backward flow. The myocardium is the heart’s contracting muscle; the left ventricular wall is especially thick because it must generate pressure to supply the whole body.\n\nThe heart’s electrical conduction system coordinates each beat. Signals normally begin in the sinoatrial node, spread through the atria, pause briefly at the atrioventricular node, and then travel through pathways that activate the ventricles.",
                new Resource("OpenStax: heart anatomy", "https://openstax.org/books/anatomy-and-physiology-2e/pages/19-1-heart-anatomy"),
                new Resource("OpenStax: cardiac physiology", "https://openstax.org/books/anatomy-and-physiology-2e/pages/19-4-cardiac-physiology"));
        if (name.Contains("lung"))
            return new LearningContent("Lung",
                "The lungs are the main organs of gas exchange. Air travels through the trachea, into the bronchi, and through progressively smaller airways called bronchioles. These airways end in clusters of tiny air sacs called alveoli.\n\nGAS EXCHANGE\nAlveoli have thin walls and are surrounded by capillaries. Oxygen diffuses from inhaled air into the blood, while carbon dioxide diffuses from blood into the alveoli to be exhaled. The very large combined surface area of the alveoli makes this exchange efficient.\n\nBREATHING\nWhen the diaphragm contracts, it moves downward and the chest cavity expands, drawing air into the lungs. When it relaxes, air flows out. The lungs themselves do not actively pull air in; pressure changes in the chest move air through the airways.\n\nThe right lung usually has three lobes and the left lung two, leaving space for the heart. A thin double-layered membrane called the pleura surrounds each lung and reduces friction as the lungs move during breathing. The blood then carries oxygen to tissues and returns carbon dioxide to the lungs.",
                new Resource("OpenStax: the lungs", "https://openstax.org/books/anatomy-and-physiology/pages/22-2-the-lungs"),
                new Resource("OpenStax: gas exchange", "https://openstax.org/books/anatomy-and-physiology-2e/pages/22-4-gas-exchange"));
        if (name.Contains("eye"))
            return new LearningContent("Eye",
                "The eye gathers light and focuses it onto the retina, a light-sensitive layer lining the back of the eye. The retina converts light into electrical signals that the brain interprets as vision.\n\nFOCUSING LIGHT\nLight first passes through the transparent cornea, which bends much of the light entering the eye. It then passes through the pupil, whose size is controlled by the iris. The lens changes shape to focus on objects at different distances, a process called accommodation.\n\nTHE RETINA AND VISUAL SIGNAL\nRods are especially sensitive in dim light and help with peripheral vision. Cones work best in brighter light and provide color and fine detail. Signals pass from retinal cells into the optic nerve, which carries them to visual areas of the brain.\n\nThe sclera is the tough outer coat of the eye. The choroid supplies blood to many eye tissues, and the vitreous humor fills the large space behind the lens. The image formed on the retina is processed by the nervous system, which combines information from both eyes and interprets it with context.",
                new Resource("Eye structure video", "https://www.khanacademy.org/science/biology/human-biology/senses/a/vision-structure-of-the-eye"));
        if (name.Contains("brain"))
            return new LearningContent("Brain",
                "The brain is the central processing organ of the nervous system. It receives sensory information, helps control movement, regulates essential body functions, and supports learning, memory, language, emotion, and decision-making. Its regions specialize in different tasks but work together continuously.\n\nCEREBRUM\nThe cerebrum is the largest region. Its two hemispheres contain an outer cerebral cortex with folds that increase surface area. Broadly, frontal regions contribute to planning and voluntary movement; parietal regions process touch and spatial information; temporal regions support hearing and memory; and occipital regions process vision. These functions are distributed across networks rather than isolated to one small spot.\n\nCEREBELLUM AND BRAINSTEM\nThe cerebellum helps refine movement, balance, and motor learning. The brainstem connects the brain with the spinal cord and contains pathways and centers involved in breathing, heart rate, sleep, and other vital functions.\n\nNeurons communicate at synapses, while glial cells support, protect, and help maintain the brain’s environment. The brain’s activity depends on coordinated signaling across many connected areas.",
                new Resource("OpenStax: nervous system", "https://openstax.org/books/anatomy-and-physiology-2e/pages/12-introduction"));
        return new LearningContent(rawName ?? "Anatomy",
            "Explore the model and use the resources below to learn how its structures work together. Select a resource to open a trusted learning page.",
            new Resource("OpenStax: Anatomy and Physiology", "https://openstax.org/details/books/anatomy-and-physiology-2e"));
    }
}
