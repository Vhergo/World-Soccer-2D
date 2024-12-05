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

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
using System.IO;
using System.Text.RegularExpressions;
#endif

using Callback = System.Action<string>;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
public class UnitySendMessageDispatcher
{
    public static void Dispatch(string name, string method, string message)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null)
            obj.SendMessage(method, message);
    }
}
#endif

public class WebViewObject : MonoBehaviour
{
    public void Awake()
    {
        if (_instance)
            DestroyImmediate(gameObject);
        else
        {
            DontDestroyOnLoad(gameObject);
            _instance = this;
        }
    }
    private static WebViewObject _instance;
    Callback onJS;
    Callback onError;
    Callback onLoaded;
    bool visibility;
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    IntPtr webView;
    Rect rect;
    Texture2D texture;
    string inputString;
    bool hasFocus;
#elif UNITY_IPHONE
    IntPtr webView;
#elif UNITY_ANDROID
    AndroidJavaObject webView;

    bool mIsKeyboardVisible = false;

    /// Called from Java native plugin to set when the keyboard is opened
    public void SetKeyboardVisible(string pIsVisible)
    {
        mIsKeyboardVisible = (pIsVisible == "true");
    }
#else
    IntPtr webView;
#endif

    public bool IsKeyboardVisible
    {
        get
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            return mIsKeyboardVisible;
#elif !UNITY_EDITOR && UNITY_IPHONE
            return TouchScreenKeyboard.visible;
#else
            return false;
#endif
        }
    }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
#if WEBVIEW_SEPARATED
    [DllImport("WebViewSeparated")]
    private static extern string _CWebViewPlugin_GetAppPath();
    [DllImport("WebViewSeparated")]
    private static extern IntPtr _CWebViewPlugin_Init(
        string gameObject, bool transparent, int width, int height, string ua, bool ineditor);
    [DllImport("WebViewSeparated")]
    private static extern int _CWebViewPlugin_Destroy(IntPtr instance);
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_SetRect(
        IntPtr instance, int width, int height);
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_SetVisibility(
        IntPtr instance, bool visibility);
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_LoadURL(
        IntPtr instance, string url);
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_LoadHTML(
        IntPtr instance, string html, string baseUrl);
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_EvaluateJS(
        IntPtr instance, string url);
    [DllImport("WebViewSeparated")]
    private static extern bool _CWebViewPlugin_CanGoBack(
        IntPtr instance);
    [DllImport("WebViewSeparated")]
    private static extern bool _CWebViewPlugin_CanGoForward(
        IntPtr instance);
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_GoBack(
        IntPtr instance);
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_GoForward(
        IntPtr instance);
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_Update(IntPtr instance,
        int x, int y, float deltaY, bool down, bool press, bool release,
        bool keyPress, short keyCode, string keyChars);
    [DllImport("WebViewSeparated")]
    private static extern int _CWebViewPlugin_BitmapWidth(IntPtr instance);
    [DllImport("WebViewSeparated")]
    private static extern int _CWebViewPlugin_BitmapHeight(IntPtr instance);
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_SetTextureId(IntPtr instance, int textureId);
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_SetCurrentInstance(IntPtr instance);
    [DllImport("WebViewSeparated")]
    private static extern IntPtr GetRenderEventFunc();
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_AddCustomHeader(IntPtr instance, string headerKey, string headerValue);
    [DllImport("WebViewSeparated")]
    private static extern string _CWebViewPlugin_GetCustomHeaderValue(IntPtr instance, string headerKey);
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_RemoveCustomHeader(IntPtr instance, string headerKey);
    [DllImport("WebViewSeparated")]
    private static extern void _CWebViewPlugin_ClearCustomHeader(IntPtr instance);
#else
    [DllImport("WebView")]
    private static extern string _CWebViewPlugin_GetAppPath();
    [DllImport("WebView")]
    private static extern IntPtr _CWebViewPlugin_Init(
        string gameObject, bool transparent, int width, int height, string ua, bool ineditor);
    [DllImport("WebView")]
    private static extern int _CWebViewPlugin_Destroy(IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_SetRect(
        IntPtr instance, int width, int height);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_SetVisibility(
        IntPtr instance, bool visibility);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_LoadURL(
        IntPtr instance, string url);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_LoadHTML(
        IntPtr instance, string html, string baseUrl);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_EvaluateJS(
        IntPtr instance, string url);
    [DllImport("WebView")]
    private static extern bool _CWebViewPlugin_CanGoBack(
        IntPtr instance);
    [DllImport("WebView")]
    private static extern bool _CWebViewPlugin_CanGoForward(
        IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_GoBack(
        IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_GoForward(
        IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_Update(IntPtr instance,
        int x, int y, float deltaY, bool down, bool press, bool release,
        bool keyPress, short keyCode, string keyChars);
    [DllImport("WebView")]
    private static extern int _CWebViewPlugin_BitmapWidth(IntPtr instance);
    [DllImport("WebView")]
    private static extern int _CWebViewPlugin_BitmapHeight(IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_SetTextureId(IntPtr instance, int textureId);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_SetCurrentInstance(IntPtr instance);
    [DllImport("WebView")]
    private static extern IntPtr GetRenderEventFunc();
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_AddCustomHeader(IntPtr instance, string headerKey, string headerValue);
    [DllImport("WebView")]
    private static extern string _CWebViewPlugin_GetCustomHeaderValue(IntPtr instance, string headerKey);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_RemoveCustomHeader(IntPtr instance, string headerKey);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_ClearCustomHeader(IntPtr instance);
#endif
#elif UNITY_IPHONE
    [DllImport("__Internal")]
    private static extern IntPtr _CWebViewPlugin_Init(string gameObject, bool transparent, string ua, bool enableWKWebView);
    [DllImport("__Internal")]
    private static extern int _CWebViewPlugin_Destroy(IntPtr instance);
    [DllImport("__Internal")]
    private static extern void _CWebViewPlugin_SetMargins(
        IntPtr instance, int left, int top, int right, int bottom);
    [DllImport("__Internal")]
    private static extern void _CWebViewPlugin_SetVisibility(
        IntPtr instance, bool visibility);
    [DllImport("__Internal")]
    private static extern void _CWebViewPlugin_LoadURL(
        IntPtr instance, string url);
    [DllImport("__Internal")]
    private static extern void _CWebViewPlugin_LoadHTML(
        IntPtr instance, string html, string baseUrl);
    [DllImport("__Internal")]
    private static extern void _CWebViewPlugin_EvaluateJS(
        IntPtr instance, string url);
    [DllImport("__Internal")]
    private static extern bool _CWebViewPlugin_CanGoBack(
        IntPtr instance);
    [DllImport("__Internal")]
    private static extern bool _CWebViewPlugin_CanGoForward(
        IntPtr instance);
    [DllImport("__Internal")]
    private static extern void _CWebViewPlugin_GoBack(
        IntPtr instance);
    [DllImport("__Internal")]
    private static extern void _CWebViewPlugin_GoForward(
        IntPtr instance);
    [DllImport("__Internal")]
    private static extern void _CWebViewPlugin_SetFrame(
        IntPtr instance, int x , int y , int width , int height);
    [DllImport("__Internal")]
    private static extern void   _CWebViewPlugin_AddCustomHeader(IntPtr instance, string headerKey, string headerValue);
    [DllImport("__Internal")]
    private static extern string _CWebViewPlugin_GetCustomHeaderValue(IntPtr instance, string headerKey);
    [DllImport("__Internal")]
    private static extern void   _CWebViewPlugin_RemoveCustomHeader(IntPtr instance, string headerKey);
    [DllImport("__Internal")]
    private static extern void   _CWebViewPlugin_ClearCustomHeader(IntPtr instance);
#endif

    public void Init(Callback cb = null, bool transparent = false, string ua = "", Callback err = null, Callback ld = null, bool enableWKWebView = false)
    {
#if !UNITY_EDITOR_OSX && !UNITY_EDITOR
        onJS = cb;
        onError = err;
        onLoaded = ld;
#if UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.init", name);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        {
            var uri = new Uri(_CWebViewPlugin_GetAppPath());
            var info = File.ReadAllText(uri.LocalPath + "Contents/Info.plist");
            if (Regex.IsMatch(info, @"<key>CFBundleGetInfoString</key>\s*<string>Unity version [5-9]\.[3-9]")
                && !Regex.IsMatch(info, @"<key>NSAppTransportSecurity</key>\s*<dict>\s*<key>NSAllowsArbitraryLoads</key>\s*<true/>\s*</dict>")) {
                Debug.LogWarning("<color=yellow>WebViewObject: NSAppTransportSecurity isn't configured to allow HTTP. If you need to allow any HTTP access, please shutdown Unity and invoke:</color>\n/usr/libexec/PlistBuddy -c \"Add NSAppTransportSecurity:NSAllowsArbitraryLoads bool true\" /Applications/Unity/Unity.app/Contents/Info.plist");
            }
        }
#if UNITY_EDITOR_OSX
        if (string.IsNullOrEmpty(ua)) {
            ua = @"Mozilla/5.0 (iPhone; CPU iPhone OS 7_1_2 like Mac OS X) AppleWebKit/537.51.2 (KHTML, like Gecko) Version/7.0 Mobile/11D257 Safari/9537.53";
        }
#endif
        webView = _CWebViewPlugin_Init(
            name,
            transparent,
            Screen.width,
            Screen.height,
            ua,
            Application.platform == RuntimePlatform.OSXEditor);
        // define pseudo requestAnimationFrame.
        EvaluateJS(@"(function() {
            var vsync = 1000 / 60;
            var t0 = window.performance.now();
            window.requestAnimationFrame = function(callback, element) {
                var t1 = window.performance.now();
                var duration = t1 - t0;
                var d = vsync - ((duration > vsync) ? duration % vsync : duration);
                var id = window.setTimeout(function() {t0 = window.performance.now(); callback(t1 + d);}, d);
                return id;
            };
        })()");
        rect = new Rect(0, 0, Screen.width, Screen.height);
        OnApplicationFocus(true);
#elif UNITY_IPHONE
        webView = _CWebViewPlugin_Init(name, transparent, ua, enableWKWebView);
#elif UNITY_ANDROID
        webView = new AndroidJavaObject("net.gree.unitywebview.CWebViewPlugin");
        webView.Call("Init", name, transparent, ua);
#else
        Debug.LogError("Webview is not supported on this platform.");
#endif
#endif
    }

    protected virtual void OnDestroy()
    {
#if UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.destroy", name);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_Destroy(webView);
        webView = IntPtr.Zero;
#elif UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_Destroy(webView);
        webView = IntPtr.Zero;
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("Destroy");
        webView = null;
#endif
    }

    /** Use this function instead of SetMargins to easily set up a centered window */
    public void SetCenterPositionWithScale(Vector2 center, Vector2 scale)
    {
#if UNITY_WEBPLAYER
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        rect.x = center.x + (Screen.width - scale.x)/2;
        rect.y = center.y + (Screen.height - scale.y)/2;
        rect.width = scale.x;
        rect.height = scale.y;
#elif UNITY_IPHONE
        if (webView == IntPtr.Zero) return;
        _CWebViewPlugin_SetFrame(webView,(int)center.x,(int)center.y,(int)scale.x,(int)scale.y);
#endif
    }

    public void SetMargins(int left, int top, int right, int bottom)
    {
#if UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.setMargins", name, left, top, right, bottom);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        int width = Screen.width - (left + right);
        int height = Screen.height - (bottom + top);
        _CWebViewPlugin_SetRect(webView, width, height);
        rect = new Rect(left, bottom, width, height);
#elif UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_SetMargins(webView, left, top, right, bottom);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("SetMargins", left, top, right, bottom);
#endif
    }

    public void SetVisibility(bool v)
    {
#if UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.setVisibility", name, v);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_SetVisibility(webView, v);
#elif UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_SetVisibility(webView, v);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("SetVisibility", v);
#endif
        visibility = v;
    }

    public bool GetVisibility()
    {
        return visibility;
    }

    public void LoadURL(string url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        if(Application.internetReachability == NetworkReachability.NotReachable)
        {
            LoadHTML("<!DOCTYPE html><html> <head> <title>RewardMob - Offline</title> <meta name='viewport' content='initial-scale=1, maximum-scale=1'> <meta charset='utf-8'> <style>html{height: 100%; width: 100%; overflow: auto; -webkit-overflow-scrolling: touch; -webkit-text-size-adjust: 100%;}body{background: #ffffff; display: flex; flex-direction: column; margin: 0; overflow: auto; padding: 0; width: 100%; height: 100%; font-family: -apple-system, system-ui, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Helvetica Neue', Arial, sans-serif;}.toolbar{align-items: center; background: #1d2124; display: flex; height: 56px; min-height: 56px; width: 100%;}.container{border-radius: 8px; padding: 15px; flex-grow: 1; width: calc(100vw - 30px);}@media (min-width: 768px){body{justify-content: center; align-items: center;}.container{display: flex; flex-direction: column; justify-content: center; width: 70%;}p{margin: 0px;}}.logo{background-image: url('data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAk4AAAB0CAYAAABzJ70XAAAACXBIWXMAAAsTAAALEwEAmpwYAAA6xWlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4KPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgNS42LWMxMzggNzkuMTU5ODI0LCAyMDE2LzA5LzE0LTAxOjA5OjAxICAgICAgICAiPgogICA8cmRmOlJERiB4bWxuczpyZGY9Imh0dHA6Ly93d3cudzMub3JnLzE5OTkvMDIvMjItcmRmLXN5bnRheC1ucyMiPgogICAgICA8cmRmOkRlc2NyaXB0aW9uIHJkZjphYm91dD0iIgogICAgICAgICAgICB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iCiAgICAgICAgICAgIHhtbG5zOnhtcE1NPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvbW0vIgogICAgICAgICAgICB4bWxuczpzdEV2dD0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL3NUeXBlL1Jlc291cmNlRXZlbnQjIgogICAgICAgICAgICB4bWxuczpwaG90b3Nob3A9Imh0dHA6Ly9ucy5hZG9iZS5jb20vcGhvdG9zaG9wLzEuMC8iCiAgICAgICAgICAgIHhtbG5zOmRjPSJodHRwOi8vcHVybC5vcmcvZGMvZWxlbWVudHMvMS4xLyIKICAgICAgICAgICAgeG1sbnM6dGlmZj0iaHR0cDovL25zLmFkb2JlLmNvbS90aWZmLzEuMC8iCiAgICAgICAgICAgIHhtbG5zOmV4aWY9Imh0dHA6Ly9ucy5hZG9iZS5jb20vZXhpZi8xLjAvIj4KICAgICAgICAgPHhtcDpDcmVhdG9yVG9vbD5BZG9iZSBQaG90b3Nob3AgQ0MgMjAxNyAoV2luZG93cyk8L3htcDpDcmVhdG9yVG9vbD4KICAgICAgICAgPHhtcDpDcmVhdGVEYXRlPjIwMTctMDctMDdUMDg6Mzg6NDItMDc6MDA8L3htcDpDcmVhdGVEYXRlPgogICAgICAgICA8eG1wOk1ldGFkYXRhRGF0ZT4yMDE3LTA3LTA3VDA4OjM4OjQyLTA3OjAwPC94bXA6TWV0YWRhdGFEYXRlPgogICAgICAgICA8eG1wOk1vZGlmeURhdGU+MjAxNy0wNy0wN1QwODozODo0Mi0wNzowMDwveG1wOk1vZGlmeURhdGU+CiAgICAgICAgIDx4bXBNTTpJbnN0YW5jZUlEPnhtcC5paWQ6NGMwNmYyMjYtMGUzYy0xYjQzLWExZGUtZmM3M2Q1NzIzMGNmPC94bXBNTTpJbnN0YW5jZUlEPgogICAgICAgICA8eG1wTU06RG9jdW1lbnRJRD5hZG9iZTpkb2NpZDpwaG90b3Nob3A6NTc5MDRmYjQtNjMyYS0xMWU3LTliZjktZGNkMGZhMmQ3ZmQyPC94bXBNTTpEb2N1bWVudElEPgogICAgICAgICA8eG1wTU06T3JpZ2luYWxEb2N1bWVudElEPnhtcC5kaWQ6NGMwYjA5NzQtNTg3Yi1kZTRhLWI5NGQtYTNmNDZiNDkwMjBiPC94bXBNTTpPcmlnaW5hbERvY3VtZW50SUQ+CiAgICAgICAgIDx4bXBNTTpIaXN0b3J5PgogICAgICAgICAgICA8cmRmOlNlcT4KICAgICAgICAgICAgICAgPHJkZjpsaSByZGY6cGFyc2VUeXBlPSJSZXNvdXJjZSI+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDphY3Rpb24+Y3JlYXRlZDwvc3RFdnQ6YWN0aW9uPgogICAgICAgICAgICAgICAgICA8c3RFdnQ6aW5zdGFuY2VJRD54bXAuaWlkOjRjMGIwOTc0LTU4N2ItZGU0YS1iOTRkLWEzZjQ2YjQ5MDIwYjwvc3RFdnQ6aW5zdGFuY2VJRD4KICAgICAgICAgICAgICAgICAgPHN0RXZ0OndoZW4+MjAxNy0wNy0wN1QwODozODo0Mi0wNzowMDwvc3RFdnQ6d2hlbj4KICAgICAgICAgICAgICAgICAgPHN0RXZ0OnNvZnR3YXJlQWdlbnQ+QWRvYmUgUGhvdG9zaG9wIENDIDIwMTcgKFdpbmRvd3MpPC9zdEV2dDpzb2Z0d2FyZUFnZW50PgogICAgICAgICAgICAgICA8L3JkZjpsaT4KICAgICAgICAgICAgICAgPHJkZjpsaSByZGY6cGFyc2VUeXBlPSJSZXNvdXJjZSI+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDphY3Rpb24+c2F2ZWQ8L3N0RXZ0OmFjdGlvbj4KICAgICAgICAgICAgICAgICAgPHN0RXZ0Omluc3RhbmNlSUQ+eG1wLmlpZDo0YzA2ZjIyNi0wZTNjLTFiNDMtYTFkZS1mYzczZDU3MjMwY2Y8L3N0RXZ0Omluc3RhbmNlSUQ+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDp3aGVuPjIwMTctMDctMDdUMDg6Mzg6NDItMDc6MDA8L3N0RXZ0OndoZW4+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDpzb2Z0d2FyZUFnZW50PkFkb2JlIFBob3Rvc2hvcCBDQyAyMDE3IChXaW5kb3dzKTwvc3RFdnQ6c29mdHdhcmVBZ2VudD4KICAgICAgICAgICAgICAgICAgPHN0RXZ0OmNoYW5nZWQ+Lzwvc3RFdnQ6Y2hhbmdlZD4KICAgICAgICAgICAgICAgPC9yZGY6bGk+CiAgICAgICAgICAgIDwvcmRmOlNlcT4KICAgICAgICAgPC94bXBNTTpIaXN0b3J5PgogICAgICAgICA8cGhvdG9zaG9wOkRvY3VtZW50QW5jZXN0b3JzPgogICAgICAgICAgICA8cmRmOkJhZz4KICAgICAgICAgICAgICAgPHJkZjpsaT5hZG9iZTpkb2NpZDpwaG90b3Nob3A6ZWVkZmRlMTUtNjMyNC0xMWU3LTliZjktZGNkMGZhMmQ3ZmQyPC9yZGY6bGk+CiAgICAgICAgICAgIDwvcmRmOkJhZz4KICAgICAgICAgPC9waG90b3Nob3A6RG9jdW1lbnRBbmNlc3RvcnM+CiAgICAgICAgIDxwaG90b3Nob3A6Q29sb3JNb2RlPjM8L3Bob3Rvc2hvcDpDb2xvck1vZGU+CiAgICAgICAgIDxkYzpmb3JtYXQ+aW1hZ2UvcG5nPC9kYzpmb3JtYXQ+CiAgICAgICAgIDx0aWZmOk9yaWVudGF0aW9uPjE8L3RpZmY6T3JpZW50YXRpb24+CiAgICAgICAgIDx0aWZmOlhSZXNvbHV0aW9uPjcyMDAwMC8xMDAwMDwvdGlmZjpYUmVzb2x1dGlvbj4KICAgICAgICAgPHRpZmY6WVJlc29sdXRpb24+NzIwMDAwLzEwMDAwPC90aWZmOllSZXNvbHV0aW9uPgogICAgICAgICA8dGlmZjpSZXNvbHV0aW9uVW5pdD4yPC90aWZmOlJlc29sdXRpb25Vbml0PgogICAgICAgICA8ZXhpZjpDb2xvclNwYWNlPjY1NTM1PC9leGlmOkNvbG9yU3BhY2U+CiAgICAgICAgIDxleGlmOlBpeGVsWERpbWVuc2lvbj41OTA8L2V4aWY6UGl4ZWxYRGltZW5zaW9uPgogICAgICAgICA8ZXhpZjpQaXhlbFlEaW1lbnNpb24+MTE2PC9leGlmOlBpeGVsWURpbWVuc2lvbj4KICAgICAgPC9yZGY6RGVzY3JpcHRpb24+CiAgIDwvcmRmOlJERj4KPC94OnhtcG1ldGE+CiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgCjw/eHBhY2tldCBlbmQ9InciPz7yktZaAAAAIGNIUk0AAHolAACAgwAA+f8AAIDpAAB1MAAA6mAAADqYAAAXb5JfxUYAADAwSURBVHja7J13mFTV+cc/ZyvLskubsUSjM4JyFQv2BrqgFDtWbElMsUQTk2hsvxg1pkeTaIwVaxKjsWAvKEFEiCh2jVwNMqMglhmFpSyw7fz+uHdlWKbPPXdmdt7P88wDO3PnnHPPbd953/e8LwiCIAiC0GeIh6wjZRbMUSNTIAiCIAhlL5YOBRYCA4C/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'); background-repeat: no-repeat; background-size: contain; flex-grow: 1; height: 42px; margin-top: -15px; margin-left: 15px; width: 220px; min-width: 220px;}.okay-btn{background: #eb2128; border: 0px; border-radius: 5px; color: #ffffff; font-family: -apple-system, system-ui, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Helvetica Neue', Arial, sans-serif; font-size: 11pt; line-height: 45px; height: 45px; min-height: 45px; width: 100%;}.close-btn{background-image: url('data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAD8AAABACAYAAACtK6/LAAAACXBIWXMAAAsTAAALEwEAmpwYAAA57GlUWHRYTUw6Y29tLmFkb2JlLnhtcAAAAAAAPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4KPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgNS42LWMxMzggNzkuMTU5ODI0LCAyMDE2LzA5LzE0LTAxOjA5OjAxICAgICAgICAiPgogICA8cmRmOlJERiB4bWxuczpyZGY9Imh0dHA6Ly93d3cudzMub3JnLzE5OTkvMDIvMjItcmRmLXN5bnRheC1ucyMiPgogICAgICA8cmRmOkRlc2NyaXB0aW9uIHJkZjphYm91dD0iIgogICAgICAgICAgICB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iCiAgICAgICAgICAgIHhtbG5zOnhtcE1NPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvbW0vIgogICAgICAgICAgICB4bWxuczpzdEV2dD0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL3NUeXBlL1Jlc291cmNlRXZlbnQjIgogICAgICAgICAgICB4bWxuczpkYz0iaHR0cDovL3B1cmwub3JnL2RjL2VsZW1lbnRzLzEuMS8iCiAgICAgICAgICAgIHhtbG5zOnBob3Rvc2hvcD0iaHR0cDovL25zLmFkb2JlLmNvbS9waG90b3Nob3AvMS4wLyIKICAgICAgICAgICAgeG1sbnM6dGlmZj0iaHR0cDovL25zLmFkb2JlLmNvbS90aWZmLzEuMC8iCiAgICAgICAgICAgIHhtbG5zOmV4aWY9Imh0dHA6Ly9ucy5hZG9iZS5jb20vZXhpZi8xLjAvIj4KICAgICAgICAgPHhtcDpDcmVhdG9yVG9vbD5BZG9iZSBQaG90b3Nob3AgQ0MgMjAxNyAoV2luZG93cyk8L3htcDpDcmVhdG9yVG9vbD4KICAgICAgICAgPHhtcDpDcmVhdGVEYXRlPjIwMTctMDctMDdUMTA6NTI6MjktMDc6MDA8L3htcDpDcmVhdGVEYXRlPgogICAgICAgICA8eG1wOk1ldGFkYXRhRGF0ZT4yMDE3LTA3LTA3VDEwOjUyOjI5LTA3OjAwPC94bXA6TWV0YWRhdGFEYXRlPgogICAgICAgICA8eG1wOk1vZGlmeURhdGU+MjAxNy0wNy0wN1QxMDo1MjoyOS0wNzowMDwveG1wOk1vZGlmeURhdGU+CiAgICAgICAgIDx4bXBNTTpJbnN0YW5jZUlEPnhtcC5paWQ6ZWQ3ODc2MTQtY2Q3OC1kMjRjLWJkODQtOWEzMGIxZjhiZTRkPC94bXBNTTpJbnN0YW5jZUlEPgogICAgICAgICA8eG1wTU06RG9jdW1lbnRJRD5hZG9iZTpkb2NpZDpwaG90b3Nob3A6MGIzZTBiYTAtNjMzZC0xMWU3LTliZjktZGNkMGZhMmQ3ZmQyPC94bXBNTTpEb2N1bWVudElEPgogICAgICAgICA8eG1wTU06T3JpZ2luYWxEb2N1bWVudElEPnhtcC5kaWQ6ZDk2MTUwMzItMDBhMy1jMjRkLTk1ZjEtYjk3ZjgzODA2ZjI4PC94bXBNTTpPcmlnaW5hbERvY3VtZW50SUQ+CiAgICAgICAgIDx4bXBNTTpIaXN0b3J5PgogICAgICAgICAgICA8cmRmOlNlcT4KICAgICAgICAgICAgICAgPHJkZjpsaSByZGY6cGFyc2VUeXBlPSJSZXNvdXJjZSI+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDphY3Rpb24+Y3JlYXRlZDwvc3RFdnQ6YWN0aW9uPgogICAgICAgICAgICAgICAgICA8c3RFdnQ6aW5zdGFuY2VJRD54bXAuaWlkOmQ5NjE1MDMyLTAwYTMtYzI0ZC05NWYxLWI5N2Y4MzgwNmYyODwvc3RFdnQ6aW5zdGFuY2VJRD4KICAgICAgICAgICAgICAgICAgPHN0RXZ0OndoZW4+MjAxNy0wNy0wN1QxMDo1MjoyOS0wNzowMDwvc3RFdnQ6d2hlbj4KICAgICAgICAgICAgICAgICAgPHN0RXZ0OnNvZnR3YXJlQWdlbnQ+QWRvYmUgUGhvdG9zaG9wIENDIDIwMTcgKFdpbmRvd3MpPC9zdEV2dDpzb2Z0d2FyZUFnZW50PgogICAgICAgICAgICAgICA8L3JkZjpsaT4KICAgICAgICAgICAgICAgPHJkZjpsaSByZGY6cGFyc2VUeXBlPSJSZXNvdXJjZSI+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDphY3Rpb24+c2F2ZWQ8L3N0RXZ0OmFjdGlvbj4KICAgICAgICAgICAgICAgICAgPHN0RXZ0Omluc3RhbmNlSUQ+eG1wLmlpZDplZDc4NzYxNC1jZDc4LWQyNGMtYmQ4NC05YTMwYjFmOGJlNGQ8L3N0RXZ0Omluc3RhbmNlSUQ+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDp3aGVuPjIwMTctMDctMDdUMTA6NTI6MjktMDc6MDA8L3N0RXZ0OndoZW4+CiAgICAgICAgICAgICAgICAgIDxzdEV2dDpzb2Z0d2FyZUFnZW50PkFkb2JlIFBob3Rvc2hvcCBDQyAyMDE3IChXaW5kb3dzKTwvc3RFdnQ6c29mdHdhcmVBZ2VudD4KICAgICAgICAgICAgICAgICAgPHN0RXZ0OmNoYW5nZWQ+Lzwvc3RFdnQ6Y2hhbmdlZD4KICAgICAgICAgICAgICAgPC9yZGY6bGk+CiAgICAgICAgICAgIDwvcmRmOlNlcT4KICAgICAgICAgPC94bXBNTTpIaXN0b3J5PgogICAgICAgICA8ZGM6Zm9ybWF0PmltYWdlL3BuZzwvZGM6Zm9ybWF0PgogICAgICAgICA8cGhvdG9zaG9wOkNvbG9yTW9kZT4zPC9waG90b3Nob3A6Q29sb3JNb2RlPgogICAgICAgICA8dGlmZjpPcmllbnRhdGlvbj4xPC90aWZmOk9yaWVudGF0aW9uPgogICAgICAgICA8dGlmZjpYUmVzb2x1dGlvbj43MjAwMDAvMTAwMDA8L3RpZmY6WFJlc29sdXRpb24+CiAgICAgICAgIDx0aWZmOllSZXNvbHV0aW9uPjcyMDAwMC8xMDAwMDwvdGlmZjpZUmVzb2x1dGlvbj4KICAgICAgICAgPHRpZmY6UmVzb2x1dGlvblVuaXQ+MjwvdGlmZjpSZXNvbHV0aW9uVW5pdD4KICAgICAgICAgPGV4aWY6Q29sb3JTcGFjZT42NTUzNTwvZXhpZjpDb2xvclNwYWNlPgogICAgICAgICA8ZXhpZjpQaXhlbFhEaW1lbnNpb24+NjM8L2V4aWY6UGl4ZWxYRGltZW5zaW9uPgogICAgICAgICA8ZXhpZjpQaXhlbFlEaW1lbnNpb24+NjQ8L2V4aWY6UGl4ZWxZRGltZW5zaW9uPgogICAgICA8L3JkZjpEZXNjcmlwdGlvbj4KICAgPC9yZGY6UkRGPgo8L3g6eG1wbWV0YT4KICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIAogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgCiAgICAgICAgICAgICAgICAgICAgICAgICAgICAKPD94cGFja2V0IGVuZD0idyI/PmDej+oAAAAgY0hSTQAAeiUAAICDAAD5/wAAgOkAAHUwAADqYAAAOpgAABdvkl/FRgAAAxVJREFUeNrU281qE1EYxvFnzum+JJtYMB8FSW5AJdGVCxGVplmKol5DpdZt1mna3IC6CASynERU0hQEtTYgeAeVumouIpNxUVImzcxkJnPe8/EuS0v5/TswnfdkrOzmLRc+U6tuwe5/gukT5mB+X+x22jg82Eer2TAa3mo2cHiwj26nHQ3f7bRRKZexxjlq21VjA7SaDdS2q1jjHJVy2TcA84NblgUA4IYGmME55wAAy7J8A7Ag+GxMC3AdPhu/ACwMblqAIHhQAOvHzxP3/r1KINw7juPA7vWxs7tnHNw7ruvidDQCG4/HcKbTSL9A1ysgDhwAptMpLi7G4KPff+r5XBbFUhGMsaU/yBhDqVhEIZ/D4GhoHNx79fL1VLo+GB7DxABJ4AAu8QBgWoCk8Dm8SQFEwBfwJgQQBffFzwIU8jmUinoFEAkPxAPA4GioVQDR8FC8TgEo4EvxOgSggkfCqwxACY+MVxGAGh4LLzOADHhsvIwAsuAr4SkDyISvjKcIIBueCC8ygAp4YryIAKrgAGAFHVrIuDWdn/9DoZBXAhfyl09yBaTTqUjfS7U/ZBA4O7t7sHt9OI4j9KGGanEqFE8RgHJjLBwvMgD1qpwELyKAjDMCMnySALIOR0jxug8pPu69/+r+K+lkiOkGlxmA6QiXFYDpCpcRgOkMpw7AVMEdx8HZ2d/It0GKAEwV3O718eDho1j/B4gOkPipLunzuMpzgUR4UYsIVQFWxovewKgIsBKeavUkO0BsPPXOTWaAWHhZy0ZZASLjZW9ZZQSIhFe1XqYOsBSvcq9OHSAUrxpOHSAQrwucMoAvXjc4VYAFvK5wigBzeN3hogNc4U2BiwzA11PpumlwUQH4xw/v6ybCvQFW/awwy2QySo+JRcybt+9g2z1MIm6EGGPY2LgBfvzte/3undvIZm+Gvmej8/s1QPRPi8/er3n24tXlDu/5y9c4+XUK13WNhEe9ArzwuQVmUABT4MsCXIfP4f0CmAYPCuAHX8B7A0wMhS8EmEx84UDIp7GePnmMz1++wvQJe4X8/wBRS47gLGVKsgAAAABJRU5ErkJggg=='); width: 16px; height: 16px; min-height: 16px; background-size: cover; min-width: 16px; border: none; border-radius: 3px; color: #ffffff; margin-right: 15px; padding: 5px;}p{line-height: 1.7em; font-size: 11pt; margin-bottom: 30px;}p:last-child{margin-bottom: 0px;}.bottom{width: calc(100% - 30px); height: 45px; padding: 15px;}@media (max-width: 320px){p{line-height: 1.3em;}}hr{margin: 25px 0px; border: none; border-bottom: 1px solid #c8c7cc;}h3{font-size: 14pt;}</style> </head> <body> <div class='toolbar'> <div class='logo'></div><button type='button' id='closeBtn' class='close-btn'></button> </div><div class='container'> <h3>Offline? You're missing out!</h3> <p>Did you know that when playing offline, you don't get the awesome benefits of earning rewards through <strong>RewardMob</strong>?</p><p> If you haven't heard, <strong>RewardMob</strong> is a fun and exciting platform that rewards you just for playing the game! Rewards can be opened for instant-win prizes, points to compete in tournaments against other players, and more! </p><p> Next time you're online, you'll start earning rewards automatically for completing certain milestones - so keep an eye out for <strong>RewardMob</strong> in the game! </p></div><div class='bottom'> <button type='button' id='okayBtn' class='okay-btn'> Got it, thanks! </button> </div><script type='text/javascript'>/* Handle calling hideWebView to Unity SDK.*/ function hideWebView(){if(!window.Unity){alert('Unity is unavailable.');}else{window.Unity.call('hideWebView');}}/* Register Click EventListeners for the close buttons. */ document.getElementById('okayBtn').addEventListener('click', hideWebView); document.getElementById('closeBtn').addEventListener('click', hideWebView); /* Call pageDoneLoading event to Unity SDK. */ if(window.Unity){window.Unity.call('pageDoneLoading');}else{console.log('Unity interface is unavailable.');}</script> </body></html>", null);
            return;
        }
#if UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.loadURL", name, url);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_LoadURL(webView, url);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("LoadURL", url);
#endif
    }

    public void LoadHTML(string html, string baseUrl)
    {
        if (string.IsNullOrEmpty(html))
            return;
        if (string.IsNullOrEmpty(baseUrl))
            baseUrl = "";
#if UNITY_WEBPLAYER
        //TODO: UNSUPPORTED
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_LoadHTML(webView, html, baseUrl);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("LoadHTML", html, baseUrl);
#endif
    }

    public void EvaluateJS(string js)
    {
#if UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.evaluateJS", name, js);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_EvaluateJS(webView, js);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("LoadURL", "javascript:" + js);
#endif
    }

    public bool CanGoBack()
    {
#if UNITY_WEBPLAYER
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return false;
        return _CWebViewPlugin_CanGoBack(webView);
#elif UNITY_ANDROID
        if (webView == null)
            return false;
        return webView.Get<bool>("canGoBack");
#else
        return false;
#endif
    }

    public bool CanGoForward()
    {
#if UNITY_WEBPLAYER
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return false;
        return _CWebViewPlugin_CanGoForward(webView);
#elif UNITY_ANDROID
        if (webView == null)
            return false;
        return webView.Get<bool>("canGoForward");
#else
        return false;
#endif
    }

    public void GoBack()
    {
#if UNITY_WEBPLAYER
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_GoBack(webView);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("GoBack");
#endif
    }

    public void GoForward()
    {
#if UNITY_WEBPLAYER
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_GoForward(webView);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("GoForward");
#endif
    }

    public void CallOnError(string error)
    {
        if (onError != null)
        {
            onError(error);
        }
    }

    public void CallOnLoaded(string url)
    {
        if (onLoaded != null)
        {
            onLoaded(url);
        }
    }

    public void CallFromJS(string message)
    {
        if (onJS != null)
        {
#if !UNITY_ANDROID
            message = WWW.UnEscapeURL(message);
#endif
            onJS(message);
        }
    }


    public void AddCustomHeader(string headerKey, string headerValue)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;

        _CWebViewPlugin_AddCustomHeader(webView, headerKey, headerValue);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("AddCustomHeader", headerKey, headerValue);
#endif
    }

    public string GetCustomHeaderValue(string headerKey)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
          return null;
        
        return _CWebViewPlugin_GetCustomHeaderValue(webView, headerKey);  
#elif UNITY_ANDROID
        if (webView == null)
            return null;
        return webView.Call<string>("GetCustomHeaderValue", headerKey);
#else
        return null;
#endif
    }

    public void RemoveCustomHeader(string headerKey)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;

        _CWebViewPlugin_RemoveCustomHeader(webView, headerKey);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("RemoveCustomHeader", headerKey);
#endif
    }

    public void ClearCustomHeader()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;

        _CWebViewPlugin_ClearCustomHeader(webView);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("ClearCustomHeader");
#endif
    }


#if UNITY_WEBPLAYER
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    void OnApplicationFocus(bool focus)
    {
        hasFocus = focus;
    }

    void Update()
    {
        if (hasFocus) {
            inputString += Input.inputString;
        }
    }

    void OnGUI()
    {
        if (webView == IntPtr.Zero || !visibility)
            return;

        Vector3 pos = Input.mousePosition;
        bool down = Input.GetButton("Fire1");
        bool press = Input.GetButtonDown("Fire1");
        bool release = Input.GetButtonUp("Fire1");
        float deltaY = Input.GetAxis("Mouse ScrollWheel");
        bool keyPress = false;
        string keyChars = "";
        short keyCode = 0;
        if (inputString != null && inputString.Length > 0) {
            keyPress = true;
            keyChars = inputString.Substring(0, 1);
            keyCode = (short)inputString[0];
            inputString = inputString.Substring(1);
        }
        _CWebViewPlugin_Update(webView,
            (int)(pos.x - rect.x), (int)(pos.y - rect.y), deltaY,
            down, press, release, keyPress, keyCode, keyChars);
        {
            var w = _CWebViewPlugin_BitmapWidth(webView);
            var h = _CWebViewPlugin_BitmapHeight(webView);
            if (texture == null || texture.width != w || texture.height != h) {
                texture = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
            }
        }
        _CWebViewPlugin_SetTextureId(webView, (int)texture.GetNativeTexturePtr());
        _CWebViewPlugin_SetCurrentInstance(webView);
#if UNITY_4_6 || UNITY_5_0 || UNITY_5_1
        GL.IssuePluginEvent(-1);
#else
        GL.IssuePluginEvent(GetRenderEventFunc(), -1);
#endif
        Matrix4x4 m = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(0, Screen.height, 0),
            Quaternion.identity, new Vector3(1, -1, 1));
        GUI.DrawTexture(rect, texture);
        GUI.matrix = m;
    }
#endif
}