using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinsDsiplayer : MonoBehaviour {


    private UILabel label;
	// Use this for initialization
	void Start () {
        label = GetComponent<UILabel>();
        label.text = PlayerPurchaseManager.instance.coinsAmount.ToString();

        PlayerPurchaseManager.instance.onCoinsAmountChange += OnCoinsAmountChange;
    }

    private void OnDestroy()
    {
        PlayerPurchaseManager.instance.onCoinsAmountChange -= OnCoinsAmountChange;
    }

    void OnCoinsAmountChange()
    {
        label.text = PlayerPurchaseManager.instance.coinsAmount.ToString();
    }
}
