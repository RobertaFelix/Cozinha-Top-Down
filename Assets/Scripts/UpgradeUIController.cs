
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUIController : MonoBehaviour
{
    [Header("Interface completa")]
    [SerializeField] private GameObject upgradePanel;

    [Header("Slots")]
    [SerializeField] private Transform leftSlots;
    [SerializeField] private Transform rightSlots;
    [SerializeField] private GameObject slotPrefab;

    [Header("Sprites dos 24 upgrades")]
    [SerializeField] private Sprite[] upgradeSprites = new Sprite[24];

    [Header("Preços individuais dos upgrades")]
    [Tooltip("O preço do elemento 0 pertence ao UpgradeSlot_1, e assim por diante.")]
    [SerializeField] private int[] upgradePrices = new int[24];

    [Header("Posições dos 12 slots da direita")]
    [Tooltip("Posições relativas ao RectTransform de Right Slots.")]
    [SerializeField] private Vector2[] rightSlotPositions =
    {
        new Vector2(-120, 100),
        new Vector2(-40, 100),
        new Vector2(40, 100),
        new Vector2(120, 100),

        new Vector2(-120, 30),
        new Vector2(-120, -30),

        new Vector2(120, 30),
        new Vector2(120, -30),

        new Vector2(-120, -100),
        new Vector2(-40, -100),
        new Vector2(40, -100),
        new Vector2(120, -100)
    };

    [Header("Preço total")]
    [SerializeField] private TMP_Text totalText;

    [Header("Botões")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private const int LeftSlotCount = 24;
    private const int RightSlotCount = 12;

    private Button[] leftButtons;
    private Image[] leftIcons;
    private TMP_Text[] leftPriceTexts;

    private Button[] rightButtons;
    private Image[] rightIcons;

    // Upgrades já comprados.
    private Sprite[] purchasedSprites = new Sprite[RightSlotCount];

    // Upgrades selecionados, ainda não comprados.
    private Sprite[] pendingSprites = new Sprite[RightSlotCount];

    public bool IsOpen =>
        upgradePanel != null && upgradePanel.activeSelf;

    private void Awake()
    {
        CreateSlots();

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmPurchase);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelPurchase);

        if (upgradePanel != null)
            upgradePanel.SetActive(false);

        UpdateTotal();
        UpdateRightSlots();
    }

    private void CreateSlots()
    {
        if (slotPrefab == null || leftSlots == null || rightSlots == null)
        {
            Debug.LogError(
                "Configure Slot Prefab, Left Slots e Right Slots."
            );
            return;
        }

        leftButtons = new Button[LeftSlotCount];
        leftIcons = new Image[LeftSlotCount];
        leftPriceTexts = new TMP_Text[LeftSlotCount];

        rightButtons = new Button[RightSlotCount];
        rightIcons = new Image[RightSlotCount];

        // Cria os 24 slots da esquerda.
        for (int i = 0; i < LeftSlotCount; i++)
        {
            GameObject slot = Instantiate(slotPrefab, leftSlots);
            slot.name = "UpgradeSlot_" + (i + 1);

            leftButtons[i] = slot.GetComponent<Button>();
            leftIcons[i] = GetIcon(slot.transform);
            leftPriceTexts[i] = GetPriceText(slot.transform);

            int index = i;

            if (leftButtons[i] != null)
            {
                leftButtons[i].onClick.AddListener(
                    () => SelectUpgrade(index)
                );
            }

            Sprite sprite =
                i < upgradeSprites.Length ? upgradeSprites[i] : null;

            if (leftIcons[i] != null)
            {
                leftIcons[i].sprite = sprite;
                leftIcons[i].enabled = sprite != null;
            }

            // Exibe o preço somente nos slots da esquerda.
            if (leftPriceTexts[i] != null)
            {
                if (sprite != null)
                {
                    leftPriceTexts[i].text = GetUpgradePrice(i).ToString();
                    leftPriceTexts[i].gameObject.SetActive(true);
                }
                else
                {
                    leftPriceTexts[i].text = "";
                    leftPriceTexts[i].gameObject.SetActive(false);
                }
            }
        }

        // Cria os 12 slots da direita.
        for (int i = 0; i < RightSlotCount; i++)
        {
            GameObject slot = Instantiate(slotPrefab, rightSlots);
            slot.name = "PurchaseSlot_" + (i + 1);

            RectTransform slotRect = slot.GetComponent<RectTransform>();

            if (slotRect != null)
            {
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.localScale = Vector3.one;

                if (rightSlotPositions != null &&
                    i < rightSlotPositions.Length)
                {
                    slotRect.anchoredPosition = rightSlotPositions[i];
                }
            }

            rightButtons[i] = slot.GetComponent<Button>();
            rightIcons[i] = GetIcon(slot.transform);

            // Desativa o texto de preço nos slots da direita.
            TMP_Text priceText = GetPriceText(slot.transform);

            if (priceText != null)
            {
                priceText.text = "";
                priceText.gameObject.SetActive(false);
            }

            if (rightButtons[i] != null)
                rightButtons[i].interactable = false;
        }
    }

    private Image GetIcon(Transform slot)
    {
        Transform iconTransform = slot.Find("Icon");

        if (iconTransform != null)
            return iconTransform.GetComponent<Image>();

        Image[] images = slot.GetComponentsInChildren<Image>(true);

        foreach (Image img in images)
        {
            if (img.transform != slot)
                return img;
        }

        Debug.LogWarning(
            "O slot " + slot.name +
            " precisa de um filho Image chamado Icon."
        );

        return null;
    }

    private TMP_Text GetPriceText(Transform slot)
    {
        Transform priceTransform = slot.Find("Price");

        if (priceTransform == null)
            return null;

        TMP_Text price = priceTransform.GetComponent<TMP_Text>();

        if (price == null)
        {
            Debug.LogWarning(
                "O objeto Price do slot " + slot.name +
                " precisa de um componente TextMeshPro."
            );
        }

        return price;
    }

    private int GetUpgradePrice(int index)
    {
        if (upgradePrices == null ||
            index < 0 ||
            index >= upgradePrices.Length)
        {
            return 0;
        }

        return Mathf.Max(0, upgradePrices[index]);
    }

    private int GetPriceForSprite(Sprite sprite)
    {
        for (int i = 0; i < upgradeSprites.Length; i++)
        {
            if (upgradeSprites[i] == sprite)
                return GetUpgradePrice(i);
        }

        return 0;
    }

    public void Open()
    {
        if (upgradePanel == null)
            return;

        ClearPending();
        upgradePanel.SetActive(true);

        UpdateRightSlots();
        UpdateTotal();
    }

    public void CancelPurchase()
    {
        ClearPending();

        UpdateRightSlots();
        UpdateTotal();

        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    private void SelectUpgrade(int index)
    {
        if (upgradeSprites == null ||
            index < 0 ||
            index >= upgradeSprites.Length)
        {
            return;
        }

        Sprite sprite = upgradeSprites[index];

        if (sprite == null)
            return;

        // Impede selecionar upgrades já comprados.
        foreach (Sprite purchased in purchasedSprites)
        {
            if (purchased == sprite)
            {
                Debug.Log("Este upgrade já foi comprado!");
                return;
            }
        }

        // Impede selecionar upgrades já pendentes.
        foreach (Sprite pending in pendingSprites)
        {
            if (pending == sprite)
            {
                Debug.Log("Este upgrade já está selecionado!");
                return;
            }
        }

        // Procura o primeiro espaço livre.
        for (int i = 0; i < RightSlotCount; i++)
        {
            if (purchasedSprites[i] == null &&
                pendingSprites[i] == null)
            {
                pendingSprites[i] = sprite;

                UpdateRightSlots();
                UpdateTotal();
                return;
            }
        }

        Debug.Log("Não há espaços disponíveis para novos upgrades.");
    }

    public void ConfirmPurchase()
    {
        if (!IsOpen)
            return;

        int total = CalculateTotal();

        if (total == 0)
        {
            Debug.Log("Nenhum upgrade foi selecionado.");
            return;
        }

        if (CoinManager.Instance == null)
        {
            Debug.LogError("CoinManager não encontrado!");
            return;
        }

        if (CoinManager.Instance.TotalCoins < total)
        {
            Debug.Log("Moedas insuficientes para comprar os upgrades.");
            return;
        }

        bool paymentSuccessful =
            CoinManager.Instance.SpendCoins(total);

        if (!paymentSuccessful)
        {
            Debug.LogWarning("O pagamento não foi realizado.");
            return;
        }

        // Transforma os upgrades pendentes em comprados.
        for (int i = 0; i < RightSlotCount; i++)
        {
            if (pendingSprites[i] == null)
                continue;

            purchasedSprites[i] = pendingSprites[i];
            pendingSprites[i] = null;
        }

        UpdateRightSlots();
        UpdateTotal();

        Debug.Log("Upgrades comprados com sucesso!");
    }

    private void ClearPending()
    {
        for (int i = 0; i < RightSlotCount; i++)
            pendingSprites[i] = null;
    }

    private int CalculateTotal()
    {
        int total = 0;

        for (int i = 0; i < RightSlotCount; i++)
        {
            if (pendingSprites[i] != null)
                total += GetPriceForSprite(pendingSprites[i]);
        }

        return total;
    }

    private void UpdateTotal()
    {
        if (totalText != null)
            totalText.text = CalculateTotal().ToString();
    }

    private void UpdateRightSlots()
    {
        if (rightIcons == null)
            return;

        for (int i = 0; i < RightSlotCount; i++)
        {
            Sprite sprite = purchasedSprites[i] != null
                ? purchasedSprites[i]
                : pendingSprites[i];

            if (rightIcons[i] != null)
            {
                rightIcons[i].sprite = sprite;
                rightIcons[i].enabled = sprite != null;
            }
        }
    }
}
