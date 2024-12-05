using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using UnityEngine.Networking;

namespace RewardMobSDK.Networking.WebRequests
{
    /// <summary>
    /// Static class intended to be used to prepare a variety of outbound requests to
    /// internal APIs managed by us.
    /// </summary>
    public static class RewardMobWebRequestFactory
    {
        /// <summary>
        /// Creates valid headers for API requests to the RewardMob API
        /// </summary>
        /// <returns>A dictionary of proper headers.</returns>
        private static Dictionary<string, string> CreateHeaders()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();

#if !UNITY_2017
            //headers["User-Agent"] = "Unity";

           // if (RewardMobEndpoints.CurrentMode == RewardMobEndpoints.SDKMode.PROD)
               // headers["Host"] = "rewardmob.com";
           // else
              //  headers["Host"] = "dev.rewardmob.com";
#endif
            headers["Content-Type"] = "application/json";

            return headers;
        }

        /// <summary>
        /// Creates valid authorization request headers for API requests to the RewardMob API
        /// </summary>
        /// <returns>A dictionary of proper headers.</returns>
        private static Dictionary<string, string> CreateAuthorizedHeaders()
        {
            //create required headers
            Dictionary<string, string> headers = CreateHeaders();

            //add the authorization header
            headers["Authorization"] = RewardMob.instance.Token;

            return headers;
        }

        /// <summary>
        /// Create a regular (non-cached) reward request to POST to our API 
        /// </summary>
        /// <param name="rewardAmount">The # of rewards to send</param>
        /// <param name="rewardComment">How the rewards were earned</param>
        /// <returns>A valid collection to be used by the WWW object</returns>
        public static RewardMobWebRequestData CreateRewardRequest(int rewardAmount, string rewardComment)
        {
            //Create a temporary reward object to be stored as JSON
            Reward rewardToSend = new Reward(RewardMob.instance.platformID, rewardAmount, rewardComment);

            //Convert the reward object, and convert it into bytes to be sent by WWW
            byte[] pData = Encoding.ASCII.GetBytes(JsonUtility.ToJson(rewardToSend).ToCharArray());

            return new RewardMobWebRequestData(CreateAuthorizedHeaders(), pData);
        }

        /// <summary>
        /// Create a request to store data in the user's sandbox
        /// </summary>
        /// <param name="data">Data to save in the sandbox</param>
        /// <returns>A valid collection to be used by the WWW object</returns>
        public static RewardMobWebRequestData CreateSendSandboxDataRequest(string data)
        {
            return new RewardMobWebRequestData(CreateAuthorizedHeaders(), Encoding.ASCII.GetBytes(data));
        }

        /// <summary>
        /// Create a request to get data stored in the user's sandbox
        /// </summary>
        /// <returns>A valid collection to be used by the WWW object</returns>
        public static RewardMobWebRequestData CreateGetSandboxDataRequest()
        {
            return new RewardMobWebRequestData(CreateAuthorizedHeaders(), null);
        }

        /// <summary>
        /// Create a cached reward request to POST to our API
        /// </summary>
        /// <param name="rewards">An array of cached rewards</param>
        /// <returns>A valid collection to be used by the WWW object</returns>
        public static RewardMobWebRequestData CreateCachedRewardsRequest(Reward[] rewards)
        {
            //Convert reward object and convert into bytes to be sent by WWW object
            byte[] pData = Encoding.ASCII.GetBytes(BuildJSON(rewards).ToCharArray());

            return new RewardMobWebRequestData(CreateAuthorizedHeaders(), pData);
        }

        /// <summary>
        /// Prepares a request with Authorized headers ONLY
        /// </summary>
        /// <returns>A valid collection to be used by the WWW object</returns>
        public static RewardMobWebRequestData CreateAuthorizedHeaderRequest()
        {
            return new RewardMobWebRequestData(CreateAuthorizedHeaders(), null);
        }

        /// <summary>
        /// NOTE:
        /// 
        /// Since Unity's JsonUtility does not support serialization of top-level primitive containers,
        /// and our API cannot accept a top-level array with a name, this method manually builds a JSON
        /// string to send to the API.
        /// 
        /// This won't be a problem in other SDKs, as their JSON serialization schemas support such a case, and
        /// we don't want to add redundant assemblies to this to support a single piece of functionality that can
        /// be easily replicated.
        /// 
        /// </summary>
        /// <param name="rewards">List of cached rewards</param>
        /// <returns>JSON string</returns>
        private static string BuildJSON(Reward[] rewards)
        {
            string arrayString = "[";
            for(int i = 0; i < rewards.Length; i++)
            {
                arrayString += JsonUtility.ToJson(rewards[i]);

                if(i != rewards.Length - 1)
                    arrayString += ",";
            }
            arrayString += "]";

            return arrayString;
        }
    }

    /// <summary>
    /// Struct to hold cached rewards temporarily. 
    /// DONT USE - Internal use only.
    /// </summary>
    [Serializable]
    public class Reward
    {
        public int platformId;
        public int totalRewards;
        public string comment;
        public string created;

        //for cached rewards
        public Reward(int platformId, int totalRewards, string rewardComment, string created)
        {
            this.platformId = platformId;
            this.totalRewards = totalRewards;
            this.comment = rewardComment;
            this.created = created;
        }

        //for regular rewards - db timestamps it manually
        public Reward(int platformId, int totalRewards, string rewardComment)
        {
            this.platformId = platformId;
            this.totalRewards = totalRewards;
            this.comment = rewardComment;
        }
    }
}