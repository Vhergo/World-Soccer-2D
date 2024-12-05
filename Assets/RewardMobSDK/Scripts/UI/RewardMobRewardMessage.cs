using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RewardMobSDK
{
    public class RewardMobRewardMessage : MonoBehaviour, IRewardMobDropdownMessage
    {
        public const int MAX_MESSAGE_LENGTH = 30;

        public void UpdateMessage(string message)
        {
            try
            {
                if (message.Length >= MAX_MESSAGE_LENGTH)
                {
                    string newMessage = message.Substring(0, MAX_MESSAGE_LENGTH - 4);
                    newMessage += "...";

                    message = newMessage;
                }

                GetComponent<Text>().text = message;
            }
            catch
            {
                print("(RM) - Can't find Text component.");
            }
        }
    }
}
