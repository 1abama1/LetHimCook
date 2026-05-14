using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// HUD-виджет текущего заказа: показывает нужный рецепт
/// и чекбоксы ингредиентов. Подключается к RecipeManager.
/// Разместите в Canvas (Screen Space Overlay).
/// </summary>
public class RecipeUI : MonoBehaviour
{
    [Header("References")]
    public GameObject recipePanel;
    public TextMeshProUGUI dishNameText;
    public Transform ingredientListRoot;
    public GameObject ingredientRowPrefab;   // TextMeshProUGUI + Image checkbox

    [Header("Colors")]
    public Color pendingColor = new Color(0.9f, 0.9f, 0.9f);
    public Color doneColor    = new Color(0.3f, 0.9f, 0.4f);
    public Color spoiledColor = new Color(1f,  0.4f, 0.1f);

    private List<GameObject>       _rows       = new List<GameObject>();
    private List<TextMeshProUGUI>  _rowLabels  = new List<TextMeshProUGUI>();
    private List<Image>            _rowChecks  = new List<Image>();

    void Start()
    {
        if (RecipeManager.Instance != null)
        {
            RecipeManager.Instance.onRecipeAssigned.AddListener(OnRecipeAssigned);
            RecipeManager.Instance.onIngredientAdded.AddListener(OnIngredientAdded);
            RecipeManager.Instance.onDishCompleted.AddListener(OnDishCompleted);
        }

        if (recipePanel != null) recipePanel.SetActive(false);
    }

    // ─── Callbacks ────────────────────────────────────────────────────────────
    void OnRecipeAssigned(RecipeData recipe)
    {
        if (recipePanel != null) recipePanel.SetActive(true);
        if (dishNameText != null) dishNameText.text = recipe.dishName;

        // Очищаем строки
        foreach (var row in _rows) Destroy(row);
        _rows.Clear(); _rowLabels.Clear(); _rowChecks.Clear();

        if (ingredientListRoot == null || recipe.requiredIngredientNames == null) return;

        foreach (var ingName in recipe.requiredIngredientNames)
        {
            GameObject row;

            if (ingredientRowPrefab != null)
            {
                row = Instantiate(ingredientRowPrefab, ingredientListRoot);
            }
            else
            {
                // Создаём базовую строку вручную если нет префаба
                row = new GameObject($"Row_{ingName}");
                row.transform.SetParent(ingredientListRoot, false);
                RectTransform rt = row.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200f, 28f);
                row.AddComponent<HorizontalLayoutGroup>().spacing = 8f;

                // Чекбокс
                GameObject checkGo = new GameObject("Check");
                checkGo.transform.SetParent(row.transform, false);
                RectTransform chRt = checkGo.AddComponent<RectTransform>();
                chRt.sizeDelta = new Vector2(20f, 20f);
                checkGo.AddComponent<Image>().color = pendingColor;

                // Текст
                GameObject textGo = new GameObject("Label");
                textGo.transform.SetParent(row.transform, false);
                RectTransform txtRt = textGo.AddComponent<RectTransform>();
                txtRt.sizeDelta = new Vector2(160f, 28f);
                TextMeshProUGUI label = textGo.AddComponent<TextMeshProUGUI>();
                label.text    = ingName;
                label.color   = pendingColor;
                label.fontSize = 14f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
            }

            row.SetActive(true);
            _rows.Add(row);
            _rowLabels.Add(row.GetComponentInChildren<TextMeshProUGUI>());
            _rowChecks.Add(row.GetComponentInChildren<Image>());
        }
    }

    void OnIngredientAdded(string ingredientName)
    {
        if (RecipeManager.Instance?.currentRecipe == null) return;
        string[] required = RecipeManager.Instance.currentRecipe.requiredIngredientNames;

        for (int i = 0; i < required.Length && i < _rowLabels.Count; i++)
        {
            if (required[i] != ingredientName) continue;
            if (_rowLabels[i] != null) _rowLabels[i].color = doneColor;
            if (_rowChecks[i] != null) _rowChecks[i].color  = doneColor;
            break;
        }
    }

    void OnDishCompleted(RecipeData recipe, bool usedSpoiled)
    {
        if (recipePanel != null) recipePanel.SetActive(false);
    }
}
