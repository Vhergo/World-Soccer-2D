
/*
 * Copyright (C) 2011 Keijiro Takahashi
 * Copyright (C) 2012 GREE, Inc.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using System.Collections;
using UnityEngine;
using RewardMobSDK;
using RewardMobSDK.Networking.WebRequests;
using System.Runtime.InteropServices;

public class SampleWebView : MonoBehaviour
{
    public static WebViewObject webViewObject;

#if UNITY_IOS
	[DllImport ("__Internal")]
	private static extern void _OpenSafari (string url);

	[DllImport ("__Internal")]
	private static extern void _LogoutUser (string mode);

	[DllImport ("__Internal")]
	private static extern void _OpenRewards (string url);
#endif
    
    /// <summary>
    /// Register 
    /// </summary>
    /// <param name="message"></param>
    private static void RegisterWebviewSuccessCallbacks(string message)
    {
        //check to see if we're loading a URL from the callback
        if (message.StartsWith("url="))
        {
			// Strip url= from string, leaving just the URL.
			string url = message.Substring (4);

            // Perform iOS Specific check.
#if UNITY_IOS
			if (url.Contains ("oauth/authorize") || url.Contains("auth/logout")) {
				// Logging in or out.
				// If iOS, call native code to load in SafariViewController.
				if(url.Contains("oauth/authorize")) {
					RewardMob.instance.isLoggingIn = true;
				}
                _OpenSafari(url);
				return;
			} else if(url.Contains("open-rewards")) {
				// User tapping "Open Now" button to open rewards.
				// Determine if app is installed and launch via custom URI scheme, otherwise
				// launch the old school way.
				_OpenRewards(url);
				return;
			}
#endif

            if(url.Contains("oauth/authorize"))
                RewardMob.instance.isLoggingIn = true;

            //Load in default browser if not IOS
            Application.OpenURL(url);
            return;
        }

        //handle different callback cases
        switch (message)
        {
            //bring user through authentication flow
            case ("authenticateUser"):
                Application.OpenURL(RewardMobEndpoints.GetAuthenticationURL());
            break;

            //log the user out
			case ("logoutUser"):
				#if UNITY_IOS	
                // If iOS, hit the Logout page in SafariViewController.
                _LogoutUser(RewardMobEndpoints.CurrentModeToString());
				#endif
				
				RewardMob.instance.ClearPersistentData ();

                SampleWebView.webViewObject.SetVisibility(false);
                RewardMob.instance.OnClosedWebView();
                RewardMob.instance.TotalRewardCount = 0;
            break;
            
			//hide the webview
            case ("hideWebView"):
                SampleWebView.webViewObject.SetVisibility(false);
                RewardMob.instance.OnClosedWebView();
            break;
            
			//let client know that the page is done loading
            case ("pageDoneLoading"):
                RewardMob.instance.ToggleLoadingScreen(false);
                webViewObject.SetVisibility(true);
            break;
            
            default:
            break;
        }
    }

    private static void RegisterWebviewErrorCallbacks(string message)
    {
        Debug.LogError(string.Format("CallOnError[{0}]", message));
    }

    private static void RegisterWebviewLoadedCallbacks(string message)
    {
        webViewObject.SetVisibility(true);
#if UNITY_IOS
            RewardMob.instance.ToggleLoadingScreen(false);
            webViewObject.SetVisibility(true);
#endif

        Debug.Log(string.Format("CallOnLoaded[{0}]", message));
#if !UNITY_ANDROID
                webViewObject.EvaluateJS(@"
                  window.Unity = {
                    call: function(message) {
                      var iframe = document.createElement('IFRAME');
                      iframe.setAttribute('src', 'unity:' + message);
                      document.documentElement.appendChild(iframe);
                      iframe.parentNode.removeChild(iframe);
                      iframe = null;
                    }
                  }
                ");
#endif
    }

    public static void Init()
    {
        webViewObject.Init(
            cb: RegisterWebviewSuccessCallbacks, 
            err: RegisterWebviewErrorCallbacks, 
            ld: RegisterWebviewLoadedCallbacks, 
            enableWKWebView: true
        );

        webViewObject.SetMargins(0, 0, 0, 0);
    }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f);

        webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();

        Init();
    }
}