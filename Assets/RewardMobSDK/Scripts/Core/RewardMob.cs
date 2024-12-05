using System.Collections;
using System;
using UnityEngine;
using RewardMobSDK.Networking.WebRequests;
using RewardMobSDK.Networking.Connectivity;
using RewardMobSDK.Animations;
using RewardMobSDK.Gateway;
using System.IO;

namespace RewardMobSDK
{
    /// <summary>
    /// Main entry point for communications to the RewardMob API
    /// </summary>
    public sealed class RewardMob : MonoBehaviour
    {
        /// <summary>
        /// Actual modifiable reference of RewardMob singleton
        /// </summary>
        private static RewardMob _instance;

        /// <summary>
        /// Modifiable reference of RewardMob authorization token (DON'T TOUCH)
        /// </summary>
        private string _Token;

        /// <summary>
        /// Total amount of Rewards earned before opening WebView
        /// </summary>
        private int _RewardCount;

        /// <summary>
        /// Total amount of Rewards earned in session
        /// </summary>
        private int _TotalRewardCount;

        /// <summary>
        /// Whether the SDK is ready or not
        /// </summary>
        [HideInInspector]
        public bool startAcknowledged = false;

        /// <summary>
        /// Is user currently going through a login flow or not
        /// </summary>
        [HideInInspector]
        public bool isLoggingIn;

        /// <summary>
        /// Whether the SDK is in the process of reauthentication or not
        /// </summary>
        [HideInInspector]
        public bool isReauthenticating = false;

        /// <summary>
        /// Whether the user is in a supported country or not. This dictates whether RewardMob services should be available.
        /// </summary>
        [HideInInspector]
        public bool userInSupportedCountry;

        /// <summary>
        /// Is RewardMob ready to handle requests
        /// </summary>
        [HideInInspector]
        public bool isSetup;

        /// <summary>
        /// Should show a dropdown on next ShowButton()
        /// </summary>
        [HideInInspector]
        public bool shouldShowDropdown = false;

        /// <summary>
        /// The current state of Nonauthentication.
        /// Determines whether a WebView should be opened after earning the first Reward or not.
        /// </summary>
        public enum RewardMobAuthorizedState
        {
            NONE,
            NOT_EARNED_FIRST_REWARD,
            EARNED_FIRST_REWARD
        }

        /// <summary>
        /// The current state of Nonauthentication.
        /// </summary>
        private RewardMobAuthorizedState earnedState = RewardMobAuthorizedState.NONE;

        /// <summary>
        /// Reference to the Popup Bubble prefab.
        /// 
        /// Should be moved to the AnimationManager.
        /// </summary>
        [HideInInspector]
        public Transform popupBubble;

        /// <summary>
        /// Reference to the Countdown Timer prefab.
        /// </summary>
        [HideInInspector]
        public GameObject countdownTimer;

        /// <summary>
        /// Your game identifier as provided by in documentation or the RewardMob developer panel
        /// </summary>
        [HideInInspector]
        public string gameID = "";

        /// <summary>
        /// A reference to the RewardMob button prefab.
        /// </summary>
        [HideInInspector]
        public GameObject rewardMobButton;

        /// <summary>
        /// The amount in seconds to wait between showing latest winner dropdowns
        /// </summary>
        [HideInInspector]
        public int latestWinnersDropdownSecondsBetween = 300;

        /// <summary>
        /// This forces the developer to reauthenticate whenever the game is closed + opened to simulate a new user onboarding.
        /// </summary>
        public bool clearTokenOnLoad = false;

        /// <summary>
        /// The current platform identifier
        /// </summary>
        [HideInInspector]
        public int platformID;

        /// <summary>
        /// Has earned a Reward in this session
        /// </summary>
        [HideInInspector]
        public bool sessionEarnedReward;
        private bool initialOpen;

        /// <summary>
        /// Public accessor property for singleton instance
        /// </summary>
        public static RewardMob instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Public accessor property for total amount of rewards
        /// </summary>
        public int TotalRewardCount
        {
            get
            {
                return _TotalRewardCount;
            }
            set
            {
                _TotalRewardCount = value;
            }
        }

        /// <summary>
        /// Total amount of Rewards earned this session
        /// </summary>
        public int RewardCount
        {
            get
            {
                return _RewardCount;
            }

            set
            {
                _RewardCount = value;
            }
        }

        /// <summary>
        /// Acts as a proxy instance between what's stored in PlayerPreferences
        /// </summary>
        public string Token
        {
            set
            {
                if (value != null)
                    PlayerPrefs.SetString("RewardMobAuthenticationToken", value);
                else
                    PlayerPrefs.DeleteKey("RewardMobAuthenticationToken");
            }

            get
            {
                if (PlayerPrefs.HasKey("RewardMobAuthenticationToken"))
                    return PlayerPrefs.GetString("RewardMobAuthenticationToken");

                return null;
            }
        }

        /// <summary>
        /// Acts as a proxy instance between what's stored in PlayerPreferences
        /// </summary>
        public string TokenExpiration
        {
            set
            {
                if (value != null)
                    PlayerPrefs.SetString("RewardMobAuthenticationTokenExpirationDate", value);
                else
                    PlayerPrefs.DeleteKey("RewardMobAuthenticationTokenExpirationDate");
            }

            get
            {
                if (PlayerPrefs.HasKey("RewardMobAuthenticationTokenExpirationDate"))
                    return PlayerPrefs.GetString("RewardMobAuthenticationTokenExpirationDate");

                return null;
            }
        }

        /// <summary>
        /// Initialization for Singleton logic
        /// </summary>
        private void Awake()
        {
            //if there's already a manager in the scene, delete the one you just created
            if (_instance)
            {
                DestroyImmediate(gameObject);
            }
            //otherwise make it persistent
            else
            {
                DontDestroyOnLoad(gameObject);
                _instance = this;
            }
        }

        /// <summary>
        /// Configuration steps
        /// </summary>
        private void Start()
        {
            //load in the gameID
            var userConfigurations = Resources.LoadAll<RewardMobData>("")[0];
            gameID = userConfigurations.GameId;

            //check to see if user is from a supported country
            StartCoroutine(RewardMobRequestGateway.instance.GetRewardMobSupportedCountry((status) =>
            {
                userInSupportedCountry = status;
                isSetup = true;

                if(userInSupportedCountry)
                {
                    //get seconds to wait to display dropdowns
                    //Invoke("InvShowLatestWinners", latestWinnersDropdownSecondsBetween);
                }
            }));

            //determine the platform we're on
            ResolveValidPlatformIdentifier();

            //testing mode
            if (clearTokenOnLoad)
                ClearPersistentData();

            //check if we expired it
            if (RewardMobNativeMobileBridge.instance.ShouldRefreshToken())
                RewardMobNativeMobileBridge.instance.PerformReauthentication();

            startAcknowledged = true;
        }

        private void Update()
        {
            if(isSetup && !initialOpen)
            {
                initialOpen = true;
#if ! UNITY_EDITOR
                if (RewardMob.instance.Token == null)
                   StartCoroutine(ShowFirstLoginScreen());
#endif
            }    
        }

        private IEnumerator ShowFirstLoginScreen()
        {
            yield return new WaitWhile(() => SampleWebView.webViewObject == null);

            if (!SampleWebView.webViewObject.GetVisibility())
            {
                RewardMob.instance.ToggleLoadingScreen(true);
                SampleWebView.Init();

                SampleWebView.webViewObject.LoadURL(RewardMobEndpoints.GetWebViewURL(initialLoginScreen: true));
            }
        }

        /// <summary>
        /// Clears all token information from persistent storage
        /// 
        /// This system of storing in PlayerPrefs could be replaced by:
        /// 
        /// AccountManager in Android
        /// Keychain Services in iOS
        /// </summary>
        public void ClearPersistentData()
        {
            PlayerPrefs.DeleteKey("RewardMobAuthenticationToken");
            PlayerPrefs.DeleteKey("RewardMobAuthenticationTokenExpirationDate");
        }

        public void InvShowLatestWinners()
        {
            shouldShowDropdown = true;
        }

        /// <summary>
        /// Our API is using different numbers to represent platforms.
        /// 
        /// Find our current platform, and change it to what we use.
        /// </summary>
        private void ResolveValidPlatformIdentifier()
        {
            //grab our current platform
            RuntimePlatform unityPlatformIdentifier = Application.platform;

            switch(unityPlatformIdentifier)
            {
                case (RuntimePlatform.Android):
                    platformID = 2;
                    break;
                case (RuntimePlatform.IPhonePlayer):
                    platformID = 1;
                    break;
                default:
                    platformID = 0;
                    break;
            }
        }

        /// <summary>
        /// Primary method used to send a Reward request to the RewardMob API
        /// </summary>
        /// <param name="message">A short description of the Reward was earned</param>
        /// <param name="amount">The amount of Rewards the user should receive (typically 1-3)</param>
        public void SendReward(string message, int amount)
        {
            //if the user is from a country where RewardMob is supported
            if (this.userInSupportedCountry)
            {
                //increment logical count of rewards
                _RewardCount += amount;

                //send off the reward request
                RewardMobRequestGateway.instance.SendReward(amount, message,
                    //If successful, this callback will be fired
                    successReward =>
                    {
                        //increment physical count
                        _TotalRewardCount += amount;

                        sessionEarnedReward = true;

                        //play reward dropdown message
                        if (RewardMobAnimationManager.instance != null)
                        {
                            RewardMobAnimationManager.instance.PlayDropdownAnimation(
                                RewardMobAnimationManager.RewardMobDropdownType.REWARD,
                                successReward.comment, 
                                rewardCount: successReward.totalRewards    
                            );
                        }

                        //update reward count UI
                        rewardMobButton.GetComponent<RewardMobButton>().UpdateCount(_RewardCount);
                    },
                    //if failed, this callback will be fired
                    failedMessage =>
                    {
                        _RewardCount -= amount;

                        //play error dropdown animation
                        if (RewardMobAnimationManager.instance != null)
                        {
                            RewardMobAnimationManager.instance.PlayDropdownAnimation(
                                RewardMobAnimationManager.RewardMobDropdownType.WARNING, failedMessage
                            );
                        }
                    }
                );

                //check to see if it was the first reward earned to pop open webview
                if (earnedState != RewardMobAuthorizedState.EARNED_FIRST_REWARD)
                {
                    earnedState = RewardMobAuthorizedState.NOT_EARNED_FIRST_REWARD;
                }
            }
        }

        /// <summary>
        /// Send data to the user's sandbox
        /// </summary>
        /// <param name="data">Data to send</param>
        public void SetUserSandboxData(string data)
        {
            RewardMobRequestGateway.instance.SetUserSandboxData(data);
        }

        /// <summary>
        /// Get data stored inside of the user's sandbox
        /// </summary>
        /// <param name="callback">Method to called once fetch is complete</param>
        public void GetUserSandboxData(Action<string> callback)
        {
            RewardMobRequestGateway.instance.GetUserSandboxData((response) =>
            {
                callback(response);
            });
        }

        /// <summary>
        /// Shows the RewardMob button
        /// </summary>
        public void ShowButton()
        {
            StartCoroutine(ShowButtonCor());
        }

        /// <summary>
        /// Coroutine to set up Button
        /// </summary>
        /// <returns>Cor</returns>
        public IEnumerator ShowButtonCor()
        {
            yield return new WaitWhile(() => !isSetup);

            if (rewardMobButton != null && userInSupportedCountry)
            {
                rewardMobButton.SetActive(true);

                //if the users has earned a reward, show the bubble
                if (sessionEarnedReward && RewardCount > 0)
                {
                    rewardMobButton.GetComponent<RewardMobButton>().UpdateCount(RewardCount);
                    OnEarnedReward();
                }

                ShowFirstTimeEarnedRewardScreen();

                if (shouldShowDropdown)
                {
                    //StartCoroutine(ShowLatestWinners());

                    //shouldShowDropdown = false;
                    //Invoke("InvShowLatestWinners", latestWinnersDropdownSecondsBetween);
                }
            }

            yield return null;
        }

        /// <summary>
        /// Displays RewardMob latest winners, and information to the user.
        /// </summary>
        /// <returns>Cor</returns>
        private IEnumerator ShowLatestWinners()
        {
            //build URL + request
            var requestURL = RewardMobEndpoints.GetUsersWinningEndpoint() + gameID;

            WWW getLatestWinnersRequest = (Token != null)
                ? new WWW(requestURL, null, RewardMobWebRequestFactory.CreateAuthorizedHeaderRequest().Headers)
                : new WWW(requestURL);

            yield return getLatestWinnersRequest;

            //request successful FUTURE:(DONT CHECK FOR ERROR, INSTEAD CHECK FOR FALSE STATUS)
            if (string.IsNullOrEmpty(getLatestWinnersRequest.error))
            {
                var respObj = JsonUtility.FromJson<RewardMobResponseRoot>(getLatestWinnersRequest.text);
                var message = (respObj.status.message);

                print(message);

                RewardMobAnimationManager.instance.PlayDropdownAnimation(RewardMobAnimationManager.RewardMobDropdownType.REWARD, message, 10);
            }
        }

        /// <summary>
        /// Hides the RewardMob button
        /// </summary>
        public void HideButton()
        {
            rewardMobButton.SetActive(false);
        }

        /// <summary>
        /// Display bubble with Reward count
        /// </summary>
        private void OnEarnedReward()
        {
            popupBubble.gameObject.SetActive(true);
        }

        /// <summary>
        /// Called when user acknowledges rewards earned
        /// </summary>
        public void OnClosedWebView()
        {
            SampleWebView.webViewObject.SetVisibility(false);

            //hide the bubble
            popupBubble.gameObject.SetActive(false);

            sessionEarnedReward = false;

            _RewardCount = 0;

            if(countdownTimer != null)
            {
                if(countdownTimer.GetComponent(typeof(RewardMobCountdownTimer)) != null)
                {
                    //WIP
                    foreach(var cdt in countdownTimer.GetComponents<RewardMobCountdownTimer>())
                        Destroy(cdt);
                }

                countdownTimer.AddComponent<RewardMobCountdownTimer>();
                countdownTimer.SetActive(true);
            }
        }

        /// <summary>
        /// Reloads the WebView to load new content.
        /// 
        /// Could change this to be handled in the browser.
        /// </summary>
        [HideInInspector]
        public void RefreshWebview()
        {
            SampleWebView.webViewObject.LoadURL(RewardMobEndpoints.GetWebViewURL());
        }

        /// <summary>
        /// Turn the loading screen on, or off
        /// </summary>
        /// <param name="state">Whether it should be turned on, or off</param>
        public void ToggleLoadingScreen(bool state)
        {
            try
            {
                var canvas = GameObject.Find("RewardMobCanvas");
                canvas.GetComponentInChildren<RewardMobLoadingIcon>(true).gameObject.SetActive(state);
                canvas.GetComponentInChildren<RewardMobLoadingOverlay>(true).gameObject.SetActive(state);
            }
            catch
            {
                Debug.LogWarning("Can't find RewardMobCanvas in scene");
            }
        }

        /// <summary>
        /// Pop open a WebView for when an unauthorized user earns a Reward for the first time (Onboarding)
        /// </summary>
        private void ShowFirstTimeEarnedRewardScreen()
        {
#if !UNITY_EDITOR
            if ((earnedState == RewardMobAuthorizedState.NOT_EARNED_FIRST_REWARD) && (Token == null))
            {
                ToggleLoadingScreen(true);

                SampleWebView.Init();
                try
                {
                    SampleWebView.webViewObject.LoadURL(RewardMobEndpoints.GetWebViewURL(useLogicalRewards: true));

                    earnedState = RewardMobAuthorizedState.EARNED_FIRST_REWARD;
                }
                catch
                {
                    Debug.LogError("Fill out the Game ID section inside of the RewardMobData object at RewardMobSDK/Resources.");
                }
            }
#endif
        }

        /// <summary>
        /// TODO: MOVE TO FACTORY METHOD
        /// </summary>
        [Serializable]
        private class RewardMobResponseRoot
        {
            public RewardMobStatusObject status;
            public RewardMobWinnerObject winner;

            public RewardMobResponseRoot(RewardMobStatusObject status, RewardMobWinnerObject winner)
            {
                this.status = status;
                this.winner = winner;
            }
        }

        /// <summary>
        /// TODO: MOVE TO FACTORY METHOD
        /// </summary>
        [Serializable]
        private class RewardMobStatusObject
        {
            public string message;
            public bool success;

            public RewardMobStatusObject(string message, bool success)
            {
                this.message = message;
                this.success = success;
            }
        }

        /// <summary>
        /// TODO: MOVE TO FACTORY METHOD
        /// </summary>
        [Serializable]
        private class RewardMobWinnerObject
        {
            public string profileImage;
            public string countryFlag;
            public string countryName;

            public RewardMobWinnerObject(string profileImage, string countryFlag, string countryName)
            {
                this.profileImage = profileImage;
                this.countryFlag = countryFlag;
                this.countryName = countryName;
            }
        }

    }
}