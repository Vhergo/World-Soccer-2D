using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RewardMobSDK;
using RewardMobSDK.Networking.WebRequests;
using System;
using RewardMobSDK.Networking.Connectivity;
using System.Runtime.InteropServices;
using RewardMobSDK.Gateway;

namespace RewardMobSDK
{
    public class RewardMobNativeMobileBridge : MonoBehaviour
    {
        /// <summary>
        /// Actual implementation of the Singleton
        /// </summary>
        private static RewardMobNativeMobileBridge _instance;

#if UNITY_IOS
	[DllImport ("__Internal")]
	private static extern void _CloseSafari ();
#endif

        [HideInInspector]
        public bool isReauthenticating = false;

        /// <summary>
        /// How many seconds should be shaved off of the process of reauthentication
        /// This accounts for making the network request, attempts, etc.
        /// </summary>
        private int reauthenticationOffsetInSeconds = 15;

        /// <summary>
        /// Singleton's static point-of-entry
        /// </summary>
        public static RewardMobNativeMobileBridge instance
        {
            //guard to protect resetting the singleton's instance
            get
            {
                return _instance;
            }
        }

        public void Awake()
        {
            //initialize singleton
            if (!_instance)
            {
                _instance = this;
            }
        }

        /// <summary>
        /// Method used for communication from iOS/Android -> Unity for passing a valid authentication token
        /// 
        /// NOT INTENDED TO BE MANUALLY CALLED WITHIN UNITY
        /// </summary>
        /// <param name="tokenFragment">The token</param>
        public void OnAccessToken(string tokenFragment)
        {
            string[] keyValues = tokenFragment.Split('&');

            foreach (string keyValuePair in keyValues)
            {
                string[] keyValue = keyValuePair.Split('=');

                //invalid KVP
                if (keyValue.Length != 2)
                    continue;

                string key = keyValue[0];
                string value = keyValue[1];

                switch (key)
                {
                    case "access_token":
                        RewardMob.instance.Token = value;
                        break;
                    case "expires_in":
                        double expirationInSeconds = (double)long.Parse(value);
                        RewardMob.instance.TokenExpiration = DateTime.UtcNow.AddSeconds(expirationInSeconds - reauthenticationOffsetInSeconds).ToString();
                        break;
                    default:
                        break;
                }
            }

            try
            {
                if (RewardMob.instance.countdownTimer != null)
                {
                    if (RewardMob.instance.countdownTimer.GetComponent(typeof(RewardMobCountdownTimer)) != null)
                        Destroy(RewardMob.instance.countdownTimer.GetComponent<RewardMobCountdownTimer>());

                    RewardMob.instance.countdownTimer.AddComponent<RewardMobCountdownTimer>();
                    RewardMob.instance.countdownTimer.SetActive(true);
                }
            }
            catch (NullReferenceException e)
            {
                Debug.LogError("Could not find RewardMob Countdown Timer.");
            }

            //if we're performing normal authentication: ensure all cached rewards are sent off, and show WebView
            if (!isReauthenticating)
            {
                RewardMobRequestGateway.instance.PreparePostCachedRewardRequest((cb) => { SampleWebView.webViewObject.LoadURL(RewardMobEndpoints.GetWebViewURL()); });
            }

#if UNITY_IOS
		// Tell iOS to Close the SafariViewController.
		_CloseSafari ();
#endif

            isReauthenticating = false;
            RewardMob.instance.TotalRewardCount = 0;


        }

        /// <summary>
        /// Method to perform the reauthentication step once a user bearer token has become expired.
        /// </summary>
        public void PerformReauthentication()
        {
            if (ConnectivityTester.HasInternetConnection())
            {
                PlayerPrefs.DeleteKey("RewardMobAuthenticationTokenExpirationDate");

                isReauthenticating = true;

                //go through the reauthentication steps
                Application.OpenURL(RewardMobEndpoints.GetAuthenticationURL());
            }
        }

        /// <summary>
        /// When the user leaves the game, and returns.
        /// 
        /// NOTE:
        /// This is a temporary implementation.
        /// 
        /// As to how this will occur in our native SDKs is still up for debate,
        /// but it will likely be filtered as a non-obtrusive background request without
        /// the WebView being rendered.
        /// 
        /// </summary>
        /// <param name="pause">State</param>
        public void OnApplicationPause(bool pause)
        {
            //if the user is coming back into the application, and it's time to refresh the token
            if (!pause)
            {
                //SPLIT UP LOGIC 
                //if it currently has a timer
                var countdownTimerRef = RewardMob.instance.countdownTimer;

                if (countdownTimerRef != null)
                {
                    //WIP
                    if (countdownTimerRef.GetComponent(typeof(RewardMobCountdownTimer)) != null)
                        Destroy(countdownTimerRef.GetComponent<RewardMobCountdownTimer>());

                    countdownTimerRef.AddComponent<RewardMobCountdownTimer>();
                    countdownTimerRef.SetActive(true);
                }

                if (RewardMob.instance.startAcknowledged && ShouldRefreshToken())
                    PerformReauthentication();

                //refresh the webview if it's already open
                try
                {
                    if (SampleWebView.webViewObject.GetVisibility())
                    {
                        if (RewardMob.instance.isLoggingIn)
                        {
                            RewardMob.instance.isLoggingIn = false;
                            return;
                        }

                        RewardMob.instance.OnClosedWebView();
                    }
                }
                catch (NullReferenceException e)
                {
                    Debug.LogWarning("WebView not Initalized yet! " + e.Message);
                }
            }
        }

        /// <summary>
        /// Checks to see if the token should be refreshed
        /// </summary>
        /// <returns>Whether it should be refreshed or not</returns>
        public bool ShouldRefreshToken()
        {
            //if there's no stored token, don't bother
            if (!PlayerPrefs.HasKey("RewardMobAuthenticationTokenExpirationDate"))
                return false;

            DateTime expirationDate = DateTime.Parse(RewardMob.instance.TokenExpiration);

            //if the token expires today, lets refresh it
            return (DateTime.UtcNow >= expirationDate);
        }

        /// <summary>
        /// Method used for communication from iOS/Android -> Unity for passing an environment mode
        ///
        /// This method is called by native iOS/Android calls.
        /// </summary>
        /// <param name="environment">Environment to use</param>
        public void SetEnvironment(string environment)
        {
            //token between dev and production are assigned differently
            //token stored wont work when passed to WebView will show "No tournament found"
            if (RewardMobEndpoints.CurrentModeToString() != environment)
                RewardMob.instance.Token = null;

            switch (environment)
            {
                case ("dev"):
                    RewardMobEndpoints.CurrentMode = RewardMobEndpoints.SDKMode.DEV;
                    break;
                case ("prod"):
                default:
                    RewardMobEndpoints.CurrentMode = RewardMobEndpoints.SDKMode.PROD;
                    break;
            }
        }
    }
}
