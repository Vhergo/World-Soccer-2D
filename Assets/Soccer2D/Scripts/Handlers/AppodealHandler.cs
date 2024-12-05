using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AppodealAds.Unity.Api;
using AppodealAds.Unity.Common;

public class AppodealHandler : MonoBehaviour, IRewardedVideoAdListener
{

    private int gamesPlayed = 0;

    bool adfree;
    void Awake()
    {
       // Debug.Log("yayaya");
        int af = PlayerPrefs.GetInt("adfree",0);
        if (af == 1)
        {
            adfree = true;
        }

        else
        {
            adfree = false;
        }
    }

    // Use this for initialization
    void Start()
    {
        GameHandler.GameController.OnGameEnd +=GameController_OnGameEnd;
        GameHandler.GameController.OnFiveGoal += GameController_OnFiveGoal;

        string appKey = "dff18bcbcbfdeeb863a65e1e73d275b02a018890d2e1b99d";
        Appodeal.disableLocationPermissionCheck();

        Appodeal.initialize(appKey, Appodeal.INTERSTITIAL | Appodeal.REWARDED_VIDEO);
        Appodeal.setRewardedVideoCallbacks(this);
    }

    void OnDisable()
    {
        GameHandler.GameController.OnGameEnd -= GameController_OnGameEnd;
        GameHandler.GameController.OnFiveGoal -= GameController_OnFiveGoal;
    }

    void GameController_OnFiveGoal()
    {
        if (GameHandler.GameController.isEndless)
        {
            gamesPlayed++;
            if (!adfree)
                ShowInterstitial();
        }
    }

    void GameController_OnGameEnd()
    {
        if(!adfree && !GameHandler.GameController.isOnMenu) {
            gamesPlayed++;
            ShowInterstitial();
        }
    

    }

    private void ShowInterstitial()
    {
        if (gamesPlayed % 3 != 0)
            return;

        if(Appodeal.isLoaded(Appodeal.REWARDED_VIDEO))
            Appodeal.show(Appodeal.REWARDED_VIDEO);
        else if (Appodeal.isLoaded(Appodeal.INTERSTITIAL))
            Appodeal.show(Appodeal.INTERSTITIAL);
    }

    public void ShowRewardedVideo()
    {
        if (Appodeal.isLoaded(Appodeal.REWARDED_VIDEO))
        {
            rewardedVideo = true;
            Appodeal.show(Appodeal.REWARDED_VIDEO);
        }
    }

    //Rewarded video functions

    private bool rewardedVideo = false;

    public void onRewardedVideoLoaded() { Debug.Log("onRewardedVideoLoaded"); }

    public void onRewardedVideoFailedToLoad() { Debug.Log("onRewardedVideoFailedToLoad"); }

    public void onRewardedVideoShown() { Debug.Log("onRewardedVideoShown"); }

    public void onRewardedVideoClosed() {
        Debug.Log("onRewardedVideoClosed");
        StartCoroutine(Timer());
    }

    public void onRewardedVideoFinished(int amount, string name)
    {
        Debug.Log("onRewardedVideoFinished" + name + " " + amount);
    }

    IEnumerator Timer()
    {
        yield return new WaitForSeconds(0.5f);
        if (rewardedVideo)
        {
            rewardedVideo = false;
            PlayerPurchaseManager.instance.AddCoins(50);
        }
    }
}
