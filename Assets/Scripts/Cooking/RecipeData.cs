using UnityEngine;

[CreateAssetMenu(menuName = "Cooking/Recipe")]
public class RecipeData : ScriptableObject
{
    public string recipeName;
    public Sprite icon;

    public string[] requiredIngredientNames;

    public int basePayment = 20;
    public int spoiledBonusPayment = 5;

    [Range(0, 100)] public int clientSatisfactionFresh   = 90;
    [Range(0, 100)] public int clientSatisfactionSpoiled = 40;

    public float cookTimeSeconds = 8f;
}
