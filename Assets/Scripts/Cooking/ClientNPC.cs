using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ClientNPC : MonoBehaviour
{
    public RecipeData wantedRecipe;
    public float patienceTime = 30f;
    public Slider patienceSlider;
    
    private float currentPatience;
    private bool isServed = false;

    public UnityEvent onClientAngry;

    void Start()
    {
        currentPatience = patienceTime;
        if (RecipeManager.Instance != null && RecipeManager.Instance.allRecipes.Count > 0)
        {
            int randomIndex = Random.Range(0, RecipeManager.Instance.allRecipes.Count);
            wantedRecipe = RecipeManager.Instance.allRecipes[randomIndex];
            RecipeManager.Instance.AssignRecipe(wantedRecipe);
        }
    }

    void Update()
    {
        if (isServed) return;

        currentPatience -= Time.deltaTime;
        
        if (patienceSlider != null)
        {
            patienceSlider.value = currentPatience / patienceTime;
        }

        if (currentPatience <= 0)
        {
            LeaveAngry();
        }
    }

    public void ReceiveDish(RecipeData cookedRecipe, bool usedSpoiled)
    {
        if (cookedRecipe == wantedRecipe)
        {
            isServed = true;
            if (usedSpoiled) 
            {
                DialogueSystem.Instance?.StartDialogue("client_complaint", this, null);
            }
            Destroy(gameObject, 2f);
        }
    }

    void LeaveAngry()
    {
        isServed = true;
        onClientAngry?.Invoke();
        PlayerEconomy.Instance?.SpendMoney(10);
        Destroy(gameObject);
    }
}
