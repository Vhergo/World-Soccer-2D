using RewardMobSDK;
using RewardMobSDK.Gateway;
using RewardMobSDK.Networking.WebRequests;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace RewardMobSDK
{
    public class RewardMobCountdownTimer : MonoBehaviour
    {
        public float secondsRemaining = 0f;
        private Text textToUpdate;
        private Image countdownTimerImage;

        public static RewardMobCountdownTimer instance;

        private void Awake()
        {
            instance = this;
        }

        // Use this for initialization
        void Start()
        {
            countdownTimerImage = gameObject.GetComponent<Image>();
            textToUpdate = gameObject.GetComponentInChildren<Text>();

            countdownTimerImage.enabled = false;
            textToUpdate.enabled = false;

            StartCoroutine(UpdateSeconds(true));
        }

        public IEnumerator UpdateSeconds(bool shouldUpdateTime)
        {
            using (var req = UnityWebRequest.Get(RewardMobEndpoints.GetTournamentTimeRemainingEndpoint() + RewardMob.instance.gameID))
            {
                if (RewardMob.instance.Token != null)
                    req._SetRequestHeaders(RewardMobWebRequestFactory.CreateAuthorizedHeaderRequest().Headers);

                yield return req.Send();

                float.TryParse(req.downloadHandler.text, out secondsRemaining);

                if (shouldUpdateTime)
                {
                    countdownTimerImage.enabled = true;
                    textToUpdate.enabled = true;

                    InvokeRepeating("UpdateTime", 0f, 1.0f);
                }
            }
        }

        private void UpdateTime()
        {
            float delta = secondsRemaining--;

            if (secondsRemaining >= 0)
            {
                // calculate (and subtract) whole days
                var days = Mathf.Floor(delta / 86400);
                delta -= days * 86400;

                // calculate (and subtract) whole hours
                var hours = Mathf.Floor(delta / 3600) % 24;
                delta -= hours * 3600;

                // calculate (and subtract) whole minutes
                var minutes = Mathf.Floor(delta / 60) % 60;
                delta -= minutes * 60;

                // what's left is seconds
                var seconds = Mathf.Floor(delta % 60);

                textToUpdate.text = 
                    (days > 0 ? (days + "D  ") : "") +
                    (hours > 0 || days > 0 ? (hours + "H  ") : "") +
                    (minutes > 0 || hours > 0 || days > 0 ? (minutes + "M  ") : "") +
                    (seconds > 0 || minutes > 0 || hours > 0 || days > 0 ? (seconds + "S  ") : "");
            }
            else
            {
                this.gameObject.SetActive(false);
                Destroy(this);
            }
        }
    }
}