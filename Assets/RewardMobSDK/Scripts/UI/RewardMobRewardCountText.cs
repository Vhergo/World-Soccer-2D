using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

namespace RewardMobSDK
{
    public class RewardMobRewardCountText : MonoBehaviour
    {
        public void UpdateRewardCount(int? count)
        {
            string message;

            try
            {
                message = count > 1 ? "Rewards" : "Reward";
                GetComponent<Text>().text = (count.ToString() + " " + message);
            }
            catch
            {
                print("(RM) - Can't find Text component.");
            }
        }
    }
}
