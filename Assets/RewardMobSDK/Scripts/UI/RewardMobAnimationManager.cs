using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RewardMobSDK.Animations
{
    /// <summary>
    /// Animation manager to handle popup animation
    /// 
    /// This will be unused in iOS/Android SDKs, it's Unity-specific code
    /// </summary>
    public class RewardMobAnimationManager : MonoBehaviour
    {
        /// <summary>
        /// Actual implementation of the singleton
        /// </summary>
        private static RewardMobAnimationManager _instance;

        /// <summary>
        /// Prefab of the object to popdown for earning a Reward
        /// </summary>
        public GameObject rewardDropdownPrefab;

        /// <summary>
        /// If an animation is currently playing or not
        /// </summary>
        private bool animationCurrentlyPlaying;

        /// <summary>
        /// Prefab of the object to popdown to indicate a warning
        /// </summary>
        public GameObject warningDropdownPrefab;

        /// <summary>
        /// Prefab of object to popdown to display a notification
        /// </summary>
        public GameObject notificationDropdownPrefab;

        /// <summary>
        /// Minimum amount of characters that the dropdown will be stretched to handle
        /// </summary>
        private const int MIN_CHARACTERS = 10;

        /// <summary>
        /// Maximum amount of characters that the dropdown will be stretched to handle
        /// </summary>
        private const int REWARD_MAX_CHARACTERS = 30;

        /// <summary>
        /// Maximum amount of characters the warning dropdown will be stretched to handle before truncation
        /// </summary>
        private const int WARNING_MAX_CHARACTERS = 25;

        /// <summary>
        /// Max width of the dropdown (relative)
        /// </summary>
        private const float DROPDOWN_MAX_WIDTH = 675f;

        /// <summary>
        /// Min width of the dropdown (relative)
        /// </summary>
        private const float DROPDOWN_MIN_WIDTH = 400f;

        /// <summary>
        /// Multiplier to determine width of dropdown given X amount of characters
        /// </summary>
        private const float REWARD_DROPDOWN_SIZE_MULTIPLIER = 5f;

        /// <summary>
        /// Multiplier to determine width of dropdown given X amount of characters
        /// </summary>
        private const float WARNING_DROPDOWN_SIZE_MULTIPLIER = 10f;

        /// <summary>
        /// Time in seconds for the dropdown to wait before being destroyed
        /// </summary>
        private const float DROPDOWN_DESTRUCTION_WAIT_TIME_SECONDS = 2f;

        /// <summary>
        /// A reference to the canvas that the dropdown should appear within
        /// </summary>
        [HideInInspector]
        public Canvas canvas;

        /// <summary>
        /// Property to restrict singleton access to "get"
        /// </summary>
        public static RewardMobAnimationManager instance
        {
            get
            {
                return _instance;
            }
        }

        public enum RewardMobDropdownType
        {
            REWARD,
            NOTIFICATION,
            WARNING
        }

        /// <summary>
        /// Give our singleton a valid instance
        /// </summary>
        private void Awake()
        {
            _instance = this;
        }
        
        /// <summary>
        /// Play a dropdown animation to show the player feedback
        /// </summary>
        /// <param name="dropdownType">The type of dropdown message</param>
        /// <param name="displayMessage">The message to display</param>
        public void PlayDropdownAnimation(RewardMobDropdownType dropdownType, string displayMessage, int? rewardCount = null)
        {
            StartCoroutine(PlayDropdownAnimationCoroutine(dropdownType, displayMessage, rewardCount));
        }

        private IEnumerator PlayNotificationDropdownCoroutine(string displayMessage)
        {
            yield return new WaitUntil(() => !animationCurrentlyPlaying);

            GameObject dropdownClone = null;

            dropdownClone = Instantiate(notificationDropdownPrefab, canvas.transform);

        }

        /// <summary>
        /// Play a dropdown animation to show the player feedback
        /// </summary>
        /// <param name="dropdownType">The type of dropdown message</param>
        /// <param name="displayMessage">The message to display</param>
        private IEnumerator PlayDropdownAnimationCoroutine(RewardMobDropdownType dropdownType, string displayMessage, int? rewardCount)
        {
            //if an animation is playing, just wait
            yield return new WaitUntil(() => animationCurrentlyPlaying == false);

            //declare gameobj to hold clone
            GameObject dropdownClone = null;
            int maxCharactersAllowed;

            //declare dropdown sizes
            float dropdownSizeMultiplier;
            float dropdownWidth;

            //based on the dropdown type, set certain characteristics
            switch(dropdownType)
            {
                case (RewardMobDropdownType.REWARD):
                    dropdownClone = Instantiate(rewardDropdownPrefab, canvas.transform);
                    maxCharactersAllowed = REWARD_MAX_CHARACTERS;
                    dropdownSizeMultiplier = REWARD_DROPDOWN_SIZE_MULTIPLIER;
                    break;
                case (RewardMobDropdownType.WARNING):
                    dropdownClone = Instantiate(warningDropdownPrefab, canvas.transform);
                    maxCharactersAllowed = WARNING_MAX_CHARACTERS;
                    dropdownSizeMultiplier = WARNING_DROPDOWN_SIZE_MULTIPLIER;
                    break;
                default:
                    yield break;
            }

            //change scale if landscape
            if((Screen.orientation == ScreenOrientation.LandscapeRight) || (Screen.orientation == ScreenOrientation.LandscapeLeft))
            {
                dropdownClone.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }

            animationCurrentlyPlaying = true;

            //update the text property's field
            if (dropdownType == RewardMobDropdownType.WARNING)
            {
                dropdownClone.GetComponentInChildren<RewardMobWarningMessage>().UpdateMessage(displayMessage);
            }
            else
            {
                dropdownClone.GetComponentInChildren<RewardMobRewardMessage>().UpdateMessage(displayMessage);
                dropdownClone.GetComponentInChildren<RewardMobRewardCountText>().UpdateRewardCount(rewardCount);
            }

            //determine the dropdown's width
            if (displayMessage.Length >= maxCharactersAllowed)
                dropdownWidth = DROPDOWN_MAX_WIDTH;
            else if (displayMessage.Length <= MIN_CHARACTERS)
                dropdownWidth = DROPDOWN_MIN_WIDTH;
            else
                dropdownWidth = (DROPDOWN_MIN_WIDTH + (displayMessage.Length * dropdownSizeMultiplier));
            
            //set it to use proper width
            SetWidth(dropdownClone.GetComponent<RectTransform>(), dropdownWidth);

            //set position to the proper dropdown location
            dropdownClone.transform.position = FindObjectOfType<RewardMobDropdownLocation>().GetComponent<RectTransform>().position;

            //halt thread to wait for animation to be finished
            yield return new WaitForSeconds(DROPDOWN_DESTRUCTION_WAIT_TIME_SECONDS);

            animationCurrentlyPlaying = false;

            //destroy when done
            Destroy(dropdownClone);
        }

        /// <summary>
        /// Resizes our dropdown's width
        /// </summary>
        /// <param name="trans">The dropdown's rect transform component</param>
        /// <param name="newSize">The new size</param>
        private void SetSize(RectTransform trans, Vector2 newSize)
        {
            Vector2 oldSize = trans.rect.size;
            Vector2 deltaSize = newSize - oldSize;
            trans.offsetMin = trans.offsetMin - new Vector2(deltaSize.x * trans.pivot.x, deltaSize.y * trans.pivot.y);
            trans.offsetMax = trans.offsetMax + new Vector2(deltaSize.x * (1f - trans.pivot.x), deltaSize.y * (1f - trans.pivot.y));
        }

        /// <summary>
        /// Resizes our dropdown's width
        /// </summary>
        /// <param name="trans">The dropdown's rect transform component</param>
        /// <param name="newSize">The new size</param>
        private void SetWidth(RectTransform trans, float newSize)
        {
            SetSize(trans, new Vector2(newSize, trans.rect.size.y));
        }
    }
}
