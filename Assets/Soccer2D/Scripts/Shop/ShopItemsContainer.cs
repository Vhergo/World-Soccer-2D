using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopItemsContainer : MonoBehaviour {

    private UIGrid grid;
    private UIScrollView scrollView;

    private void Awake()
    {
        grid = GetComponent<UIGrid>();
        scrollView = GetComponentInParent<UIScrollView>();
    }


    public void UpdateItemsState()
    {
        foreach (Transform child in transform)
        {
            ShopItemScript shopItemScript = child.GetComponent<ShopItemScript>();

            if (shopItemScript == null)
                continue;

            bool allItemsSold = true;
            foreach (PlayerPurchaseManager.PurchasableItemsForCoins item in shopItemScript.items)
            {
                if (!PlayerPurchaseManager.instance.itemsIsBought(item))
                {
                    allItemsSold = false;
                    shopItemScript.owned = false;
                    break;
                }
            }

            if (allItemsSold)
                shopItemScript.owned = true;
        }
    }

    public void ShowContainer()
    {
        gameObject.SetActive(true);
        UpdateItemsState();
        grid.Reposition();
        scrollView.RestrictWithinBounds(true);
        scrollView.ResetPosition();
    }

    public void HideContainer()
    {
        gameObject.SetActive(false);
    }
}
