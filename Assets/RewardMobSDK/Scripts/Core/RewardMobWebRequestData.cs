using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RewardMobSDK.Networking.WebRequests
{
    /// <summary>
    /// Struct to represent WebRequest data being utilized by Unity's WWW object
    /// </summary>
    public struct RewardMobWebRequestData
    {
        public Dictionary<string, string> Headers { get; set; }
        public byte[] Payload { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="payload"></param>
        public RewardMobWebRequestData(Dictionary<string, string> headers, byte[] payload)
        {
            this.Headers = headers;
            this.Payload = payload;
        }
    }
}