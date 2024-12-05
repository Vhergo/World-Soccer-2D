#if UNITY_IOS
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using PlistCS;
using RewardMobSDK;
using UnityEditor.iOS.Xcode;

public class RewardMobPostprocessBuild : MonoBehaviour
{
    /// <summary>
    /// Called when iOS build completed
    /// </summary>
    /// <param name="target"></param>
    /// <param name="pathToBuiltProject"></param>
    [PostProcessBuild(int.MaxValue)]
    static void OnBuildDone(BuildTarget target, string pathToBuiltProject)
    {
        if (target == BuildTarget.iOS)
        {
            //grab reference to ScriptableObject configs
            var gameID = Resources.LoadAll<RewardMobData>("")[0].GameId;

            /*
            if (File.Exists(Path.Combine(Application.dataPath, "Resources/RewardMob/config")))
            {
                gameID = File.ReadAllText(Path.Combine(Application.dataPath, "Resources/RewardMob/config"));
            }

            //replace app controller with updated one
            string replacePath = pathToBuiltProject + "/Classes/UnityAppController.mm";
            string copyPath = Application.dataPath + "/RewardMobSDK/Plugins/iOS/UnityAppController.txt";

            File.Delete(replacePath);
            File.Copy(copyPath, replacePath);
            */
            //set plist path
            string plistPath = pathToBuiltProject + "/info.plist";

            //read plist
            var dict = Plist.readPlist(plistPath) as Dictionary<string,object>;

            //update plist
            dict["CFBundleURLTypes"] = new List<object> {
                new Dictionary<string,object> {
                    { "CFBundleURLName", PlayerSettings.iPhoneBundleIdentifier },
                    { "CFBundleURLSchemes", new List<object> { "rm" + gameID } }
                }
            };

            //make RewardMob URI scheme launchable from game
            dict["LSApplicationQueriesSchemes"] = new List<object> { "rewardmob" };

            //link Safari Services
            LinkLibraries(pathToBuiltProject);

            //write plist
            Plist.writeXml(dict, plistPath);
        }
    }

    /// <summary>
    /// Dynamically link SafariServices framework to project
    /// </summary>
    /// <param name="pathToBuiltProject"></param>
    static void LinkLibraries(string pathToBuiltProject)
    {
        var projectPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

        PBXProject pbx = new PBXProject();
        pbx.ReadFromFile(projectPath);

        pbx.AddFrameworkToProject(pbx.TargetGuidByName("Unity-iPhone"), "SafariServices.framework", true);

        File.WriteAllText (projectPath, pbx.WriteToString ());
    }
}
#endif