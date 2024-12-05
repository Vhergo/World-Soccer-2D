using RewardMobSDK;
using UnityEngine;
using System;

namespace RewardMobSDK.Networking.WebRequests
{
    /// <summary>
    /// Class to handle parsing and building of
    /// URLs used inside of the SDK
    /// </summary>
    public static class RewardMobEndpoints
    {
        /// <summary>
        /// Enum to set which "mode" the game is currently in
        /// </summary>
        public enum SDKMode
        {
            PROD,
            DEV
        }

        /// <summary>
        /// Enum to set the current state of the WebView - whether the User is authenticated or not
        /// </summary>
        public enum WebViewState
        {
            UNAUTHENTICATED,
            AUTHENTICATED
        }

        /// <summary>
        /// The current mode of the SDK
        /// </summary>
        public static SDKMode CurrentMode = SDKMode.PROD;

        /// <summary>
        /// The prefix prepended to each RewardMob request for Dev-mode requests
        /// </summary>
        private const string DEV_PREFIX = "https://dev.rewardmob.com/API/RewardMobGamingAPI/";

        /// <summary>
        /// The prefix prepended to each RewardMob request for Production-mode requests (default)
        /// </summary>
        private const string PROD_PREFIX = "https://rewardmob.com/API/RewardMobGamingAPI/";

        /// <summary>
        /// The endpoint used to send multiple rewards simultaneously to the RewardMob API
        /// </summary>
        private const string SEND_MULTI_REWARD = "SendMultiReward";

        /// <summary>
        /// The endpoint used to send single rewards to the RewardMob API (default)
        /// </summary>
        private const string SEND_SINGLE_REWARD = "SendReward";

        /// <summary>
        /// The endpoint used to set user data for an authenticated RewardMob user
        /// </summary>
        private const string SEND_USER_DATA = "SendUserData";

        /// <summary>
        /// The endpoint used to grab latest winning users to display
        /// </summary>
        private const string GET_USERS_WINNING = "UsersWinning/";

        /// <summary>
        /// The endpoint used to get the amount of time (in seconds) remaining for the game's tournament
        /// 
        /// Note: Returns 0 if no tournament is running
        /// </summary>
        private const string GET_TOURNAMENT_TIME_REMAINING = "TimeRemaining/";

        /// <summary>
        /// Endpoint used to determine if a user is from a valid country or not
        /// </summary>
        private const string GET_REWARDMOB_SUPPORTED_COUNTRY = "RewardMobActive/";

        /// <summary>
        /// Endpoint used to grab an authenticated RewardMob user's sandbox data for the game
        /// </summary>
        private const string GET_USER_DATA = "GetUserData";

        /// <summary>
        /// Used to convert the environment type being returned by the 
        /// Mobile helper functions to a friendly format.
        /// </summary>
        /// <returns>Legible version of the current mode</returns>
        public static string CurrentModeToString()
        {
            switch (CurrentMode)
            {
                case (SDKMode.DEV):
                    return "dev";
                case (SDKMode.PROD):
                default:
                    return "prod";
            }
        }

        /// <summary>
        /// Grabs a valid WebView URL given the current state of the user 
        /// </summary>
        /// <param name="wvs">Current state of the User</param>
        /// <returns>The WebView URL</returns>
        public static string GetWebViewURL(bool useLogicalRewards = false, bool initialLoginScreen = false)
        {
            //determine data to display (could remove coupling)
            var sessionReward = useLogicalRewards ? RewardMob.instance.RewardCount : RewardMob.instance.TotalRewardCount;

            //build the path/arguments required for every webview request
            var data = ("sessionreward=" + sessionReward.ToString()
                        + "&game=" + RewardMob.instance.gameID
                        + "&preventcache=" + DateTime.Now.Millisecond
                        + "&sdk_version=" + "1.2.8"
                        + "&env=" + RewardMobEndpoints.CurrentModeToString());

            if (initialLoginScreen)
                data += "&initialLogin=true";

            //determine state
            var wvs = RewardMob.instance.Token != null ? WebViewState.AUTHENTICATED : WebViewState.UNAUTHENTICATED;

            //return the correct URL to load given the current state
            return ((wvs == WebViewState.AUTHENTICATED) ? BuildAuthenticatedWebviewURL(data) : BuildNonauthenticatedWebviewURL(data));
        }

        /// <summary>
        /// Grabs the correct Authentication URL given the current SDK mode
        /// </summary>
        /// <returns>Authentication URL string</returns>
        public static string GetAuthenticationURL()
        {
            string baseURL = (CurrentMode == SDKMode.PROD) 
                ? "https://rewardmob.com/" 
                : "https://dev.rewardmob.com";

            return (baseURL + "auth/oauth/authorize?response_type=token&client_id=" + RewardMob.instance.gameID + "&redirect_uri=rm" + RewardMob.instance.gameID + "://&scope=send_reward");
        }

        /// <summary>
        /// Builds the proper authenticated URL string
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Built string</returns>
        private static string BuildAuthenticatedWebviewURL(string data)
        {
            //build the URLs
            string productionURL = ("https://rewardmob.com/webview?token=" + RewardMob.instance.Token + "&" + data);
            string developmentURL = ("https://dev.rewardmob.com/webview?token=" + RewardMob.instance.Token + "&" + data);

            return ((CurrentMode == SDKMode.PROD) ? productionURL : developmentURL);
        }

        /// <summary>
        /// Builds the proper unauthenticated URL string
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Built string</returns>
        private static string BuildNonauthenticatedWebviewURL(string data)
        {
            //build the URLs
            string productionURL = ("https://rewardmob.com/webview?" + data);
            string developmentURL = ("https://dev.rewardmob.com/webview?" + data);

            return ((CurrentMode == SDKMode.PROD) ? productionURL : developmentURL);
        }

        /// <summary>
        /// Build proper multi reward endpoint
        /// </summary>
        /// <returns>Built URL string</returns>
        public static string GetMultiRewardEndpoint()
        {
            return BuildAPIURL(SEND_MULTI_REWARD);
        }

        /// <summary>
        /// Build proper single reward endpoint
        /// </summary>
        /// <returns>Built URL string</returns>
        public static string GetSingleRewardEndpoint()
        {
            return BuildAPIURL(SEND_SINGLE_REWARD);
        }

        /// <summary>
        /// Build proper tournament time remaining endpoint
        /// </summary>
        /// <returns>Built URL string</returns>
        public static string GetTournamentTimeRemainingEndpoint()
        {
            return BuildAPIURL(GET_TOURNAMENT_TIME_REMAINING);
        }

        /// <summary>
        /// Build proper "is user in supported country" endpoint
        /// </summary>
        /// <returns></returns>
        public static string GetRewardMobSupportedCountryEndpoint()
        {
            return BuildAPIURL(GET_REWARDMOB_SUPPORTED_COUNTRY);
        }

        /// <summary>
        /// Build SendUserData endpoint
        /// </summary>
        /// <returns></returns>
        public static string GetSendUserDataEndpoint()
        {
            return BuildAPIURL(SEND_USER_DATA);
        }

        /// <summary>
        /// Build GetUserData endpoint
        /// </summary>
        /// <returns></returns>
        public static string GetUserDataEndpoint()
        {
            return BuildAPIURL(GET_USER_DATA);
        }

        /// <summary>
        /// Build GetUsersWinning endpoint
        /// </summary>
        /// <returns></returns>
        public static string GetUsersWinningEndpoint()
        {
            return BuildAPIURL(GET_USERS_WINNING);
        }

        /// <summary>
        /// Determine which prefix to use given an SDK mode
        /// </summary>
        /// <returns>Built URL string</returns>
        private static string BuildAPIURL(string resource)
        {
            return (CurrentMode == SDKMode.PROD) ? PROD_PREFIX + resource : DEV_PREFIX + resource;
        }
    }
}