using UnityEngine;
using RewardMobSDK.Animations;
using UnityEngine.UI;
using RewardMobSDK.Networking.WebRequests;
using RewardMobSDK.Networking.Connectivity;

namespace RewardMobSDK
{
    /// <summary>
    /// The RewardMob button inside of the scene
    /// </summary>
    public class RewardMobButton : MonoBehaviour
    {
        public Text rewardCount;

        /// <summary>
        /// This is a WIP
        /// </summary>
        void Awake()
        {
            //If we can grab a reference to the RewardMobManager, pass it a reference of this button
            try
            {
                //grab ref of manager
                RewardMob managerRef = FindObjectOfType<RewardMob>().GetComponent<RewardMob>();

                //tell the manager that this is the button
                managerRef.rewardMobButton = this.gameObject;
                managerRef.popupBubble = GetComponentInChildren<PopupBubble>().gameObject.transform;

                //give manager a reference to the canvas as well
                FindObjectOfType<RewardMob>().GetComponent<RewardMobAnimationManager>().canvas = this.gameObject.GetComponentInParent<Canvas>();

                managerRef.countdownTimer = GetComponentInChildren<RewardMobCountdownTimer>().gameObject;

                //hide all UI stuff
                managerRef.popupBubble.gameObject.SetActive(false);
                gameObject.SetActive(false);
            }
            catch
            {
                Debug.LogWarning("Warning! No RewardMobManager is present in the scene. RewardMob SDK calls will not work.");
            }
        }

        /// <summary>
        /// Attached to the button's clicked event.
        /// </summary>
        public void OnClick()
        {
#if !UNITY_EDITOR
            //if it's not visible, either load the logged in flow, or login screen based on the token's presence
            if (!SampleWebView.webViewObject.GetVisibility())
            {
                RewardMob.instance.ToggleLoadingScreen(true);
                SampleWebView.Init();

                SampleWebView.webViewObject.LoadURL(RewardMobEndpoints.GetWebViewURL());
            }
#endif
        }

        /// <summary>
        /// Updates the reward count on the button
        /// </summary>
        /// <param name="rewardAmount">The new number of rewards</param>
        public void UpdateCount(int rewardAmount)
        {
            var popup = GetComponentInChildren<PopupBubble>(true);
            popup.gameObject.SetActive(rewardAmount > 0);

            rewardCount.text = rewardAmount.ToString();
        }
    }
}