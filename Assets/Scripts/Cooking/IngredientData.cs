using UnityEngine;

[CreateAssetMenu(menuName = "Cooking/Ingredient")]
public class IngredientData : ScriptableObject
{
    public string ingredientName;
    public Sprite sprite;

    [Range(0, 100)]
    public int rotLevel = 0;

    public bool IsSafe    => rotLevel < 50;
    public bool IsSpoiled => rotLevel >= 50;

    public enum ProcessType { None, Chop, Fry, Boil }

    public IngredientData cookedVersion;
    public ProcessType requiredProcess = ProcessType.None;

    public int baseValue = 5;
    public Color tintColor = Color.white;
    public GameObject rotParticlePrefab;
    public GameObject modelPrefab; // 3D модель или специфичный префаб для этого ингредиента
    public Vector3 visualOffset;   // Смещение модели (например, для булочки Z = -0.00927)
    public Vector3 visualRotation; // Поворот модели (в градусах)
}
