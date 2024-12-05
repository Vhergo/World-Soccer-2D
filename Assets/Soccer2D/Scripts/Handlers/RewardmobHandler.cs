using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RewardMobSDK;

public class RewardmobHandler : MonoBehaviour
{

    public static RewardmobHandler instance;

    private int gamesPlayedInRow = 0;
    private int gamesWinnedInRow = 0;

    private const string ON_GOAL = "Goal";
    private const string PERFECT_GAME = "Perfect game";
    private const string WIN_3_GAMES = "Win 3 games";
    private const string GAMES_10 = "Play 10 games in a row";
    public const string FIVE_STAR = "5 star review";
    public const string SOCIAL_SHARING = "Social sharing";

    void Start()
    {
        Debug.Assert(instance == null);
        instance = this;

        GameHandler.GameController.OnGameStart += GameController_OnGameStart;
        GameHandler.GameController.OnGameEnd += GameController_OnGameEnd;
        RewardMob.instance.ShowButton();
    }


    void GameController_OnGameStart()
    {
        RewardMob.instance.HideButton();
    }

    public void OnGoal()
    {
        if (GameHandler.GameController.gameMode == GameController.GameMode.OnePlayer)
            SendReward(ON_GOAL, 1);
    }

    void GameController_OnGameEnd()
    {
        if ((GameHandler.GameController.BlueScore >= GameHandler.GameController.endScore || GameHandler.GameController.RedScore >= GameHandler.GameController.endScore) && GameHandler.GameController.gameMode == GameController.GameMode.OnePlayer)
        {
            gamesPlayedInRow++;

            if (gamesPlayedInRow % 10 == 0)
                SendReward(GAMES_10, 3);

            if (GameHandler.GameController.BlueScore > GameHandler.GameController.RedScore)
            {
                gamesWinnedInRow++;

                if (GameHandler.GameController.RedScore == 0)
                    SendReward(PERFECT_GAME, 1);

                if (gamesWinnedInRow % 3 == 0)
                    SendReward(WIN_3_GAMES, 2);

            }
        }

        if(GameHandler.GameController.isOnMenu)
            RewardMob.instance.ShowButton();
    }

    public void SendReward(string name, int cost, bool rewardOnce = false)
    {
        if (PlayerPrefs.GetInt(name, 0) == 0)
        {
            RewardMob.instance.SendReward(name, cost);
            if (rewardOnce)
                PlayerPrefs.SetInt(name, 1);
        }

    }


    public void OpenRewardnobVideo()
    {
        Application.OpenURL("https://www.youtube.com/watch?v=Nds9E_AjMEk");
    }

    public void OnRewardMobButtonClick()
    {
#if !UNITY_EDITOR
            //if it's not visible, either load the logged in flow, or login screen based on the token's presence
            if (!SampleWebView.webViewObject.GetVisibility())
            {
                SampleWebView.Init();
                try
                {
                    if (RewardMob.instance.Token != null)
                    {
                        SampleWebView.webViewObject.LoadURL
                        (
                            "https://rewardmob.com/webview?token=" + RewardMob.instance.Token
                            + "&game=" + RewardMob.instance.gameID
                            + "&preventcache=" + System.DateTime.Now.Millisecond
                            + "&sdk_version=" + "1.0.0"
                        );
                    }
                    else
                    {
                        SampleWebView.webViewObject.LoadURL
                        (
                            "https://rewardmob.com/webview?game=" + RewardMob.instance.gameID
                            + "&sessionreward=" + RewardMob.instance.TotalRewardCount.ToString()
                            + "&preventcache=" + System.DateTime.Now.Millisecond
                            + "&sdk_version=" + "1.0.0"
                        );
                    }
                } catch {
                    Debug.LogError("Fill out the Game ID section inside of the RewardMobData object at RewardMobSDK/Resources.");
                }
            }
            //otherwise, if it was previously visible, close it
            else
            {
                RewardMob.instance.OnClosedWebView();
            }

            RewardMob.instance.ToggleLoadingScreen(true);
#endif
    }
}
