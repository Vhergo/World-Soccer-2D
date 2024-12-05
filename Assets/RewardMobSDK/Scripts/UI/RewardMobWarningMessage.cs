using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RewardMobSDK
{
    public class RewardMobWarningMessage : MonoBehaviour, IRewardMobDropdownMessage
    {
        public const int MAX_MESSAGE_LENGTH = 25;

        public void UpdateMessage(string message)
        {
            if (message.Length > MAX_MESSAGE_LENGTH)
            {
                string newMessage = message.Substring(0, MAX_MESSAGE_LENGTH - 3);
                newMessage += "...";

                message = newMessage;
            }

            GetComponent<Text>().text = message;
        }
    }

    public interface IRewardMobDropdownMessage
    {
        void UpdateMessage(string message);
    }
}