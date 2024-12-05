using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RewardMobSDK
{
    public class RewardMobLoadingIcon : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            gameObject.transform.Rotate(new Vector3(0, 0, -8f));
        }
    }
}
