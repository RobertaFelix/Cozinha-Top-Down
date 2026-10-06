using System.Collections.Generic;
using UnityEngine;

public class RecipeBookController : MonoBehaviour
{
    [Header("Página de Receitas")]
    [SerializeField]
    private GameObject recipePanel;


    [Header("Slots")]
    [SerializeField]
    private GameObject slotPrefab;

    [SerializeField]
    private int slotCount;


    [Header("Fonte das Receitas")]
    [SerializeField]
    private StoveController stoveController;


    private List<RecipeBookSlot> createdSlots =
        new List<RecipeBookSlot>();


    private void Start()
    {
        CreateSlots();
    }


    private void CreateSlots()
    {
        if (recipePanel == null)
        {
            Debug.LogError(
                "RecipeBookController: Recipe Panel não foi configurado."
            );

            return;
        }


        if (slotPrefab == null)
        {
            Debug.LogError(
                "RecipeBookController: Slot Prefab não foi configurado."
            );

            return;
        }


        if (stoveController == null)
        {
            Debug.LogError(
                "RecipeBookController: Stove Controller não foi configurado."
            );

            return;
        }


        CookingRecipe[] recipes =
            stoveController.recipes;


        if (recipes == null)
        {
            Debug.LogError(
                "RecipeBookController: O StoveController não possui receitas."
            );

            return;
        }


        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObject =
                Instantiate(
                    slotPrefab,
                    recipePanel.transform
                );


            RecipeBookSlot slot =
                slotObject.GetComponent<RecipeBookSlot>();


            if (slot == null)
            {
                Debug.LogError(
                    "RecipeBookController: O Slot Prefab não possui " +
                    "o componente RecipeBookSlot."
                );

                continue;
            }


            if (i < recipes.Length &&
                recipes[i] != null)
            {
                slot.Setup(recipes[i]);
            }
            else
            {
                Debug.LogWarning(
                    "RecipeBookController: Não existe receita " +
                    "para o slot " + i + "."
                );
            }


            createdSlots.Add(slot);
        }
    }


    public void UnlockRecipe(CookingRecipe recipe)
    {
        if (recipe == null)
        {
            Debug.LogWarning(
                "RecipeBookController: Receita nula."
            );

            return;
        }


        for (int i = 0;
             i < createdSlots.Count;
             i++)
        {
            RecipeBookSlot slot =
                createdSlots[i];


            if (slot == null)
                continue;


            if (slot.GetRecipe() == recipe)
            {
                slot.Unlock();


                Debug.Log(
                    "Receita desbloqueada no livro de receitas."
                );


                return;
            }
        }


        Debug.LogWarning(
            "RecipeBookController: A receita não foi encontrada " +
            "entre os slots."
        );
    }


    public bool HasUnlockedRecipe(
        CookingRecipe recipe)
    {
        if (recipe == null)
            return false;


        foreach (RecipeBookSlot slot
                 in createdSlots)
        {
            if (slot == null)
                continue;


            if (slot.GetRecipe() == recipe)
            {
                return slot.IsUnlocked();
            }
        }


        return false;
    }
}