using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CompleteProject;

public class ShopItemScript : MonoBehaviour {

    public PlayerPurchaseManager.PurchasableItemsForCoins[] items;
    public int  coinsCost;
    public bool purchaseble = true;

    private bool _owned;
    public bool owned
    {
        set
        {
            _owned = value;
            priceContainer.alpha = _owned ? 0f : 1f;
            owndContainer.alpha = _owned ? 1f : 0f;
            uiButton.enabled = !owned;
        }
        get
        {
            return _owned;
        }
    }
    private UIWidget priceContainer;
    private UIWidget owndContainer;
    private UIButton uiButton;
    private UIWidget itemContainer;

    private void OnEnable()
    {
        priceContainer = _.Find<UIWidget>(transform, "Price Container");
        owndContainer = _.Find<UIWidget>(transform, "Sold Container");
        itemContainer = _.Find<UIWidget>(transform, "Container");
        uiButton = GetComponent<UIButton>();
    }

    void OnClick()
    {
        if (PlayerPurchaseManager.instance.coinsAmount < coinsCost)
        {
            GetComponentInParent<BuyEffectManager>().noMoneyEffect.PlayReverse();
            return;
        }

        if (!purchaseble || owned)
            return;

        Effectrplayer.instance.PlayBuyEffect();
        PlayerPurchaseManager.instance.BuyItemForCoins(items, coinsCost);
        for(int i = 0; i < items.Length; i++)
        {
            Debug.Log(items[i].ToString("g"));
        }
        GetComponentInParent<ShopItemsContainer>().UpdateItemsState();
        GetComponentInParent<BuyEffectManager>().ShowEffect(itemContainer);
    }
}
