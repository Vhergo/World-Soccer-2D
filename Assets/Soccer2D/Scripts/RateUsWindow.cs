using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RateUsWindow : MonoBehaviour {

    [SerializeField]
    private Transform starContainer;

	void Start () {
		
	}
	
	public void OnRateClick(int rate)
    {
        for(int i = 0; i < starContainer.childCount; i++)
        {
            RateUsStar rateUsStar = starContainer.GetChild(i).GetComponent<RateUsStar>();
            rateUsStar.SetStarState(i <= rate);
        }

        if(rate == 4)
        {
            RewardmobHandler.instance.SendReward(RewardmobHandler.FIVE_STAR, 3, true);
        }

        string urlString = "market://details?id=" + Application.identifier;
        Application.OpenURL(urlString);
        PlayerPrefs.SetInt("canRate", 1);

        transform.parent.GetComponent<TweenAlpha>().PlayReverse();
    }


}
