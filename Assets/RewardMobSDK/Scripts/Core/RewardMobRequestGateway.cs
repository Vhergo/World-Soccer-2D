using RewardMobSDK;
using RewardMobSDK.Networking.Connectivity;
using RewardMobSDK.Animations;
using RewardMobSDK.Networking.WebRequests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Text;

namespace RewardMobSDK.Gateway
{
    /// <summary>
    /// Acts as an intermediary between Unity's coroutine functionality, and 
    /// RewardMob business logic. Primarily handles sending Rewards, and caching functionality.
    /// </summary>
    public class RewardMobRequestGateway : MonoBehaviour
    {
        /// <summary>
        /// EventHandler blueprint for when a Reward is successfully credited to the user
        /// </summary>
        /// <param name="source">Object making the request</param>
        /// <param name="args">The details of the Reward</param>
        public delegate void RewardSuccessfullySentEventHandler(object source, RewardMobEventArgs args);

        /// <summary>
        /// EventHandler blueprint for when a Reward fails to be credited to the user
        /// </summary>
        /// <param name="source">Object making the request</param>
        /// <param name="args">The details of the Reward</param>
        public delegate void RewardSendFailedEventHandler(object source, RewardMobEventArgs args);

        /// <summary>
        /// Event hook for when a Reward send fails
        /// </summary>
        public event RewardSendFailedEventHandler RewardSendFailed;

        /// <summary>
        /// Event hook for when a Reward send succeeds
        /// </summary>
        public event RewardSuccessfullySentEventHandler RewardSuccessfullySent;

        /// <summary>
        /// Actual implementation of the Singleton
        /// </summary>
        private static RewardMobRequestGateway _instance;

        /// <summary>
        /// Singleton's static point-of-entry
        /// </summary>
        public static RewardMobRequestGateway instance
        {
            //guard to protect resetting the singleton's instance
            get
            {
                return _instance;
            }

            private set
            {
            }
        }

        /// <summary>
        /// Collection of Rewards intended to be sent when conditionals are met
        /// </summary>
        private List<Reward> cachedRewards { get; set; }

        /// <summary>
        /// Flag to control if IP was checked or not
        /// </summary>
        private bool checkedSinceOffline = true;

        /// <summary>
        /// Ensure RewardMob GO persists between scene changes
        /// </summary>
        public void Awake()
        {
            //initialize singleton
            if (!_instance)
            {
                _instance = this;
            }
        }

        /// <summary>
        /// Check to see when a player loses internet connection/isn't logged in after earning a Reward
        /// </summary>
        public void Update()
        {
            if(Application.internetReachability == NetworkReachability.NotReachable)
            {
                checkedSinceOffline = false;
            }

            //if the token is expired, clear it from playerprefs
            if ((RewardMob.instance.TokenExpiration != null) && (DateTime.UtcNow > DateTime.Parse(RewardMob.instance.TokenExpiration)))
            {
                if (PlayerPrefs.HasKey("RewardMobAuthenticationToken"))
                {
                    PlayerPrefs.DeleteKey("RewardMobAuthenticationToken");
                }
            }

            //if connected to the internet
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                //if we have a token in memory
                if (RewardMob.instance.Token != null)
                {
                    PreparePostCachedRewardRequest();
                }

                //if the RewardMob SDK is properly configured & 
                if (RewardMob.instance.isSetup && !checkedSinceOffline)
                {
                    checkedSinceOffline = true;

                    StartCoroutine(GetRewardMobSupportedCountry((status) =>
                    {
                        RewardMob.instance.userInSupportedCountry = status;
                    }));
                }
            }
        }

        /// <summary>
        /// Checks to see whether user is from a valid country, or the contrary
        /// </summary>
        /// <param name="onSuccess">Callback</param>
        /// <returns>Cor</returns>
        public IEnumerator GetRewardMobSupportedCountry(Action<bool> onSuccess)
        {
            //Build request
            using (var req = UnityWebRequest.Get(RewardMobEndpoints.GetRewardMobSupportedCountryEndpoint()))
            {
                yield return req.Send();

                try { onSuccess(JsonUtility.FromJson<RewardMobResponseRoot>(req.downloadHandler.text).status.success); }
                catch { onSuccess(false); }
            }
        }

        /// <summary>
        /// Pre-async dispatcher of sending a Reward off to the RewardMob API
        /// </summary>
        /// <param name="rewardAmount">Amount of rewards to send</param>
        /// <param name="rewardMessage">Short description explaining why the rewards were earned</param>
        /// <param name="onSuccess">Method to call when successful</param>
        /// <param name="onFailed">Method to call when unsuccessful</param>
        public void SendReward(int rewardAmount, string rewardMessage, Action<Reward> onSuccess, Action<string> onFailed)
        {
            if (ConnectivityTester.HasInternetConnection())
            {
                StartCoroutine(SendRewardCor(rewardAmount, rewardMessage, onSuccess, onFailed));
                return;
            }

            RewardMobAnimationManager.instance.PlayDropdownAnimation(
                RewardMobAnimationManager.RewardMobDropdownType.WARNING, "No Internet Connection!"
            );
        }

        /// <summary>
        /// Asynchronous method to send Reward off to API
        /// 
        /// TODO: Should be split into more defined methods.
        /// </summary>
        /// <param name="rewardAmount">The amount of rewards to send.</param>
        /// <param name="rewardMessage">A short description explaining why the rewards were earned.</param>
        /// <returns>Cor</returns>
        private IEnumerator SendRewardCor(int rewardAmount, string rewardMessage, Action<Reward> onSuccess, Action<string> onFailed)
        {
            //check time remaining
            using (UnityWebRequest checkTimeRemaining = UnityWebRequest.Get(RewardMobEndpoints.GetTournamentTimeRemainingEndpoint() + RewardMob.instance.gameID))
            {
                if (RewardMob.instance.Token != null)
                    checkTimeRemaining._SetRequestHeaders(RewardMobWebRequestFactory.CreateAuthorizedHeaderRequest().Headers);

                //halt execution until request returns
                yield return checkTimeRemaining.Send();

                float secondsRemaining;
                float.TryParse(checkTimeRemaining.downloadHandler.text, out secondsRemaining);

                //if there's no time remaining, don't bother
                if (secondsRemaining <= 0)
                    yield break;

                //check if the reward is "cacheworthy"
                if (ShouldCache())
                {
                    Cache(new Reward(RewardMob.instance.platformID, rewardAmount, rewardMessage, System.DateTime.UtcNow.ToString()));

                    //update the user
                    onSuccess(new Reward(RewardMob.instance.platformID, rewardAmount, rewardMessage));
                    yield break;
                }

                //prepare the request
                var sendRewardRequestData = RewardMobWebRequestFactory.CreateRewardRequest(rewardAmount, rewardMessage);

                using (UnityWebRequest sendReward = UnityWebRequest.Put(RewardMobEndpoints.GetSingleRewardEndpoint(), sendRewardRequestData.Payload))
                {
#if UNITY_IOS
                    sendReward.chunkedTransfer = false;
#endif
                    sendReward.method = UnityWebRequest.kHttpVerbPOST;
                    sendReward._SetRequestHeaders(sendRewardRequestData.Headers);

                    yield return sendReward.Send();

                    HandleRewardRequestClientsideUpdate(sendReward, new Reward(RewardMob.instance.platformID, rewardAmount, rewardMessage), onSuccess, onFailed);

                    //if we got an authorization error, cache it!
                    if (!string.IsNullOrEmpty(sendReward.error))
                    {
                        Cache(new Reward(RewardMob.instance.platformID, rewardAmount, rewardMessage, System.DateTime.UtcNow.ToString()));
                    }
                }
            }
        }

        /// <summary>
        /// Handles the response returned from the RewardMob API for sending a reward.
        /// Authorization errors, and OK messages are considered valid for a reward to be sent.
        /// 
        /// Note: This only handles updating the frontend.
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="reward"></param>
        private void HandleRewardRequestClientsideUpdate(UnityWebRequest requestObject, Reward reward, Action<Reward> onSuccess, Action<string> onFailed)
        {
            //grab a reference to the headers
            var statusCode = requestObject.responseCode;

            if (statusCode == 200 || statusCode == 401)
            {
                //publish a message to all subscribers of a RewardSuccessfullySent event
                onSuccess(reward);
                return;
            }

            onFailed(JsonUtility.FromJson<RewardMobResponseRoot>(requestObject.downloadHandler.text).status.message);
        }

        /// <summary>
        /// Determine if a Reward is cacheworthy
        /// </summary>
        /// <returns></returns>
        private bool ShouldCache()
        {
            return (RewardMob.instance.Token == null);
        }

        /// <summary>
        /// Send off POST request for cached rewards
        /// </summary>
        public void PreparePostCachedRewardRequest(Action<string> callback = null)
        {
            //ensure there are rewards to be sent
            if (cachedRewards != null)
            {
                if (cachedRewards.Count > 0)
                {
                    List<Reward> rewardContainer = new List<Reward>();

                    //convert cached requests to rewards, and add them to the container
                    foreach (Reward reward in cachedRewards)
                    {
                        rewardContainer.Add(reward);
                    }

                    //send WWW request, and clear our cache
                    List<Reward> backupContainer = cachedRewards;

                    //send off requests
                    StartCoroutine(PostCachedRewards(backupContainer, callback));

                    cachedRewards.Clear();
                    return;
                }
            }

            if (callback != null)
                callback("Refresh");
        }

        /// <summary>
        /// Send off POST request for cached rewards
        /// </summary>
        private IEnumerator PostCachedRewards(List<Reward> backupContainer = null, Action<string> callback = null)
        {
            //create the headers/payload
            var requestData = RewardMobWebRequestFactory.CreateCachedRewardsRequest(cachedRewards.ToArray());

            using (var bulkPostRewardsRequest = UnityWebRequest.Put(RewardMobEndpoints.GetMultiRewardEndpoint(), requestData.Payload))
            {
#if UNITY_IOS
                bulkPostRewardsRequest.chunkedTransfer = false;
#endif
                bulkPostRewardsRequest.method = UnityWebRequest.kHttpVerbPOST;
                bulkPostRewardsRequest._SetRequestHeaders(requestData.Headers);

                //halt for request to finish
                yield return bulkPostRewardsRequest.Send();

                //check if the request failed and handle it
                if (!string.IsNullOrEmpty(bulkPostRewardsRequest.error) && backupContainer != null)
                {
                    cachedRewards = backupContainer;
                }

                if (callback != null)
                    callback("Refresh");
            }
        }

        /// <summary>
        /// Cache a reward that was considered cacheworthy
        /// </summary>
        /// <param name="reward">The reward to cache</param>
        private void Cache(Reward reward)
        {
            //instantiate a new List of rewards if null or 0
            if (cachedRewards == null || cachedRewards.Count == 0)
            {
                cachedRewards = new List<Reward>();
            }

            cachedRewards.Add(reward);
        }

        /// <summary>
        /// Send an arbitrary string to the user's sandbox
        /// </summary>
        /// <param name="data"></param>
        public void SetUserSandboxData(string data)
        {
            StartCoroutine(SetUserSandboxDataCor(data));
        }

        /// <summary>
        /// Send an arbitrary string to the user's sandbox
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private IEnumerator SetUserSandboxDataCor(string data)
        {
            var requestData = RewardMobWebRequestFactory.CreateSendSandboxDataRequest(data);

            WWW postUserDataRequest = new WWW(RewardMobEndpoints.GetSendUserDataEndpoint(), requestData.Payload, requestData.Headers);

            yield return postUserDataRequest;
        }

        /// <summary>
        /// Get the data stored in the user's sandbox
        /// </summary>
        /// <param name="callback"></param>
        public void GetUserSandboxData(Action<string> callback)
        {
            StartCoroutine(GetUserSandboxDataCor(callback));
        }

        /// <summary>
        /// Get the user data stored in the user's sandbox
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator GetUserSandboxDataCor(Action<string> callback)
        {
            var requestData = RewardMobWebRequestFactory.CreateGetSandboxDataRequest();

            WWW postUserDataRequest = new WWW(RewardMobEndpoints.GetSendUserDataEndpoint(), requestData.Payload, requestData.Headers);

            yield return postUserDataRequest;

            callback(postUserDataRequest.text);
        }

        /// <summary>
        /// Called when the reward is succesfully sent
        /// </summary>
        /// <param name="amount">Amount of rewards</param>
        /// <param name="message">Message to send</param>
        protected virtual void OnRewardSuccessfullySent(int amount, string message)
        {
            if (RewardSuccessfullySent != null)
            {
                RewardSuccessfullySent(this, new RewardMobEventArgs { rewardAmount = amount, rewardMessage = message });
            }
        }

#region SERIALIZATION_POCO
        [Serializable]
        private class RewardMobResponseRoot
        {
            public RewardMobStatusObject status;

            public RewardMobResponseRoot(RewardMobStatusObject status)
            {
                this.status = status;
            }
        }

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
    }
#endregion

    public class RewardMobEventArgs : EventArgs
    {
        public int rewardAmount;
        public string rewardMessage;
    }

    public static class Extension_UnityWebRequest
    {
        public static void _SetRequestHeaders(this UnityWebRequest uwr, Dictionary<string, string> headers)
        {
            foreach (var header in headers)
            {
                uwr.SetRequestHeader(header.Key, header.Value);
            }
        }

        public static UnityWebRequest _Post(this UnityWebRequest uwr, string uri, string payload, Dictionary<string, string> headers)
        {
            var legacyRequest = UnityWebRequest.PostWwwForm(uri, payload);
            legacyRequest._SetRequestHeaders(headers);

            return legacyRequest;
        }
    }
}