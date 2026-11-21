using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    public Image[] tabImages;
    public GameObject[] pages;
    
    public RectTransform FurniturePage { get; private set; }

    private void Awake()
    {
        if (pages.Length == 2 && pages[0].name == "InventoryPage")
            FurniturePage = FurnitureInventoryPage.Install(this);
        ActivateTab(0);
    }

    public void ActivateTab(int TabNo)
    {
        for(int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(false);
            tabImages[i].color = Color.grey;
        }
        pages[TabNo].SetActive(true);
        tabImages[TabNo].color = Color.white;
    }
}
