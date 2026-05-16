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
    [Header("Visual Adjustment")]
    public GameObject modelPrefab;
    public Vector3 countertopOffset;
    public Vector3 countertopRotation; // Поворот на столе
    public Vector3 plateOffset;
    public Vector3 plateRotation;      // Поворот на тарелке
    public Vector3 playerHoldOffset;
    public Vector3 playerHoldRotation; // Поворот в руках
    public float   visualScale = 1f;
}
