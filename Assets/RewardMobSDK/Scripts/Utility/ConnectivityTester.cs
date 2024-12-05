using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RewardMobSDK.Networking.Connectivity
{
    /// <summary>
    /// Checks to see if peer is connected to the internet.
    /// 
    /// Use: ConnectivityTester.HasInternetConnection(); 
    /// </summary>
    public static class ConnectivityTester
    {
        /// <summary>
        /// Wrapper around Unity's implementation of checking internet connection.
        /// </summary>
        /// <returns> Boolean value: if user has an internet connection, or not. </returns>
        public static bool HasInternetConnection()
        {
            return (Application.internetReachability != NetworkReachability.NotReachable);
        }
    }
}