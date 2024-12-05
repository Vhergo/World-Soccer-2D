#if UNITY_ANDROID
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RewardMobSDK;

/// <summary>
/// RewardMob Android Postprocess to automatically add Activity
/// </summary>
public class RewardMobPostprocessAndroidBuild : MonoBehaviour
{
    /// <summary>
    /// Reference to the entire Manifest document
    /// </summary>
    private static XElement manifestDocument;

    /// <summary>
    /// Default Game ID
    /// </summary>
    private static readonly string defaultFillerId = "-YOUR_GAME_ID_HERE-";

    /// <summary>
    /// XML Namespace used to colon delimit Android elements
    /// </summary>
    private static readonly XNamespace androidNamespaceURL = "http://schemas.android.com/apk/res/android";

    /// <summary>
    /// Called on Android build
    /// </summary>
    /// <param name="buildTarget"></param>
    /// <param name="path"></param>
    [PostProcessBuild(int.MaxValue)]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {
        //create path to primary AndroidManifest.xml
        string mainProjectManifestPath = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");
        string gameID = Resources.LoadAll<RewardMobData>("")[0].GameId;

        if (File.Exists(Path.Combine(Application.dataPath, "Resources/RewardMob/config")))
        {
            gameID = File.ReadAllText(Path.Combine(Application.dataPath, "Resources/RewardMob/config"));
        }       

        if (!File.Exists(mainProjectManifestPath))
        {
            throw new FileNotFoundException("Can't find AndroidManifest.xml file at \"Plugins/Android\".");
        }

        //load in the manifest
        manifestDocument = XElement.Load(mainProjectManifestPath);

        //if the activity is present, set the URL scheme
        XElement dataNode;
        if (HasRewardMobActivity(out dataNode))
        {
            //only force a rebuild if the game ID is the default
            if (dataNode.Value != defaultFillerId) return;

            if (string.IsNullOrEmpty(gameID))
            {
                gameID = defaultFillerId;
                Debug.LogError("Please fill the \"Game Id\" field at the RewardMobData object.");
            }

            //grab a reference to the game id, and set the URL scheme to it
            var identifierString = string.IsNullOrEmpty(gameID) ? defaultFillerId : gameID;

            dataNode.FirstAttribute.SetValue("rm" + identifierString);
            manifestDocument.Save(mainProjectManifestPath);

            return;
        }

        //automatically add the activity
        if (!string.IsNullOrEmpty(gameID))
        {
            AddRewardMobActivity(mainProjectManifestPath, gameID);
            return;
        }

        Debug.LogError("Please fill the \"Game Id\" field at the RewardMobData object.");
    }

    /// <summary>
    /// Add a RewardMob activity to a given manifest
    /// </summary>
    /// <param name="manifestPath">Path to the manifest file</param>
    /// <param name="gameID"></param>
    private static void AddRewardMobActivity(string manifestPath, string gameID)
    {
        //ID conditionals
        var identifierString = string.IsNullOrEmpty(gameID) ? defaultFillerId : gameID;

        //parse through the document
        foreach (var topLevelElement in manifestDocument.Elements())
        {
            if (topLevelElement.Name == "application")
            {
                //add root node
                XElement activityNode = new XElement("activity",
                    new XAttribute(androidNamespaceURL + "configChanges", "keyboard|keyboardHidden|orientation|screenSize"),
                    new XAttribute(androidNamespaceURL + "name", "com.rewardmob.sdk.android.AuthPlugin"),
                    new XAttribute(androidNamespaceURL + "screenOrientation", "fullSensor")
                );

                //add inner nodes
                XElement intentNode = new XElement("intent-filter");
                intentNode.Add(new XElement("data", new XAttribute(androidNamespaceURL + "scheme", "rm" + identifierString)));
                intentNode.Add(new XElement("action", new XAttribute(androidNamespaceURL + "name", "android.intent.action.VIEW")));
                intentNode.Add(new XElement("category", new XAttribute(androidNamespaceURL + "name", "android.intent.category.DEFAULT")));
                intentNode.Add(new XElement("category", new XAttribute(androidNamespaceURL + "name", "android.intent.category.BROWSABLE")));

                //add to document
                activityNode.Add(intentNode);
                topLevelElement.Add(activityNode);

                break;
            }
        }

        //save the manifest
        manifestDocument.Save(manifestPath);

        Debug.LogError("Added RewardMob Activity to AndroidManifest.xml. Please Rebuild the Application.");
    }

    /// <summary>
    /// Parses through XML document searching for a RewardMob activity
    /// </summary>
    /// <param name="el">Reference to be set if a data node happens to exist</param>
    /// <returns>If a RewardMob activity exists or not.</returns>
    private static bool HasRewardMobActivity(out XElement el)
    {
        //get RewardMob data node
        var dataNode = manifestDocument
            .Descendants()
            .Where(node => node.Name == "data")
            .Where(node => node.FirstAttribute.Value.StartsWith("rm"))
            .FirstOrDefault();

        //set reference to returned value
        el = dataNode;

        return dataNode != null;
    }
}
#endif