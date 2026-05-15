using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform itemsContainer;
    public GameObject inventoryItemPrefab; // Префаб с иконкой (Image) и текстом (TextMeshProUGUI)

    [Header("Summary Texts")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI freshText;
    public TextMeshProUGUI spoiledText;
    public TextMeshProUGUI junkText;

    private void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.onInventoryChanged.AddListener(UpdateUI);
            PlayerEconomy.Instance.onMoneyChanged.AddListener(_ => UpdateUI());
            PlayerEconomy.Instance.onResourceChanged.AddListener((_, __) => UpdateUI());
            UpdateUI();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
                if (inventoryPanel.activeSelf)
                {
                    UpdateUI();
                }
            }
        }
    }

    private void UpdateUI()
    {
        if (PlayerEconomy.Instance == null) return;

        // Обновляем счётчики
        if (moneyText != null) moneyText.text = $"Money: {PlayerEconomy.Instance.Money}";
        if (freshText != null) freshText.text = $"Fresh: {PlayerEconomy.Instance.FreshIngredients}";
        if (spoiledText != null) spoiledText.text = $"Spoiled: {PlayerEconomy.Instance.SpoiledIngredients}";
        if (junkText != null) junkText.text = $"Junk: {PlayerEconomy.Instance.JunkLoot}";

        if (inventoryPanel != null && !inventoryPanel.activeSelf) return;

        // Очищаем старые элементы инвентаря
        if (itemsContainer != null)
        {
            foreach (Transform child in itemsContainer)
            {
                Destroy(child.gameObject);
            }

            // Создаем новые элементы для каждого стака в инвентаре
            foreach (var stack in PlayerEconomy.Instance.inventory)
            {
                if (stack.ingredient == null || stack.amount <= 0) continue;
                if (inventoryItemPrefab == null) break;

                GameObject obj = Instantiate(inventoryItemPrefab, itemsContainer);
                
                // Пытаемся найти Image для иконки
                Image icon = obj.transform.Find("Icon")?.GetComponent<Image>();
                if (icon == null) icon = obj.GetComponentInChildren<Image>();
                if (icon != null && stack.ingredient.sprite != null)
                    icon.sprite = stack.ingredient.sprite;

                // Пытаемся найти текст для названия/количества
                TextMeshProUGUI text = obj.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (text == null) text = obj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = $"{stack.ingredient.ingredientName} x{stack.amount}";
            }
        }
    }
}
