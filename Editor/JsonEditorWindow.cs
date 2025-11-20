using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using JSON = dev.vanHoof.JsonEditor.DisplayJson;

namespace dev.vanHoof.JsonEditor
{
    /// <summary>
    /// EditorWindow used to display & edit JSON-files
    /// </summary>
    internal class JsonEditorWindow : EditorWindow
    {
        #region Variables
        private State state;
        private bool showFileSettings = true, showCredits = true, showFileContents;
        
        private JsonAsset activeAsset = null;
        private Vector2 scrollPosContent = Vector2.zero;
        #endregion

        #region Methods
        private void OnFileOpened(string path)
        {
            showCredits = false;
            showFileContents = true;
            state = new State();
            scrollPosContent = Vector2.zero;
        }

        private void OnFileClosed()
        {
            showCredits = true;
            showFileContents = false;
        }
        #endregion

        #region UnityMethods
        [MenuItem("Tools/JSON Editor")]
        public static void ShowWindow()
        {
            Display.InitStyles();
            GetWindow(typeof(JsonEditorWindow));
        }

        private void OnEnable()
        {
            // Hook into File-Events
            FileUtils.OnFileOpened += OnFileOpened;
            FileUtils.OnFileClosed += OnFileClosed;
        }

        private void OnDisable()
        {
            // Clear hooks from File-Events
            FileUtils.OnFileOpened -= OnFileOpened;
            FileUtils.OnFileClosed -= OnFileClosed;
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            // File Settings
            showFileSettings = EditorGUILayout.Foldout(showFileSettings, "File Settings", Display.BoldFoldoutStyle);
            if (showFileSettings)
                activeAsset = Display.ShowFileSettings(activeAsset, FileUtils.CreateJsonFile, FileUtils.OpenJSONFile, FileUtils.SaveJsonToFile, FileUtils.CloseJSONFile, FileUtils.EnsureRelativePath);
            // Credits
            showCredits = EditorGUILayout.Foldout(showCredits, "Credits", Display.BoldFoldoutStyle);
            if (showCredits)
                Display.ShowCredits();
            // Contents
            showFileContents = EditorGUILayout.Foldout(showFileContents, "File Contents", Display.BoldFoldoutStyle);
            if (showFileContents)
            {
                if (activeAsset == null || activeAsset.RootToken == null)
                    GUILayout.Label("No JSON file opened", Display.LargeLabelStyle);
                else
                {
                    // Active File Buttons
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Clear Changes"))
                    {
                        FileUtils.OpenJSONFile(activeAsset.Path);
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        return;
                    }
                    if (GUILayout.Button("Save Changes"))
                    {
                        FileUtils.SaveJsonToFile(activeAsset.RootToken, activeAsset.Path);
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        return;
                    }
                    if (GUILayout.Button("Save As"))
                    {
                        string path = FileUtils.SaveJsonToFile(activeAsset.RootToken);
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        // After "Save As", we open the new file.
                        FileUtils.OpenJSONFile(path);
                        return;
                    }
                    GUILayout.EndHorizontal();

                    // ScrollView for File
                    GUILayout.BeginVertical();
                    scrollPosContent = GUILayout.BeginScrollView(scrollPosContent, new GUIStyle());
                    // Add to Root Object
                    JSON.DisplayAddRow(activeAsset.RootToken, state, out bool changed);
                    if (changed)
                    {
                        GUILayout.EndVertical();
                        return;
                    }
                    IEnumerable<object> children = null;
                    Func<object, JToken> GetToken = null;

                    if (activeAsset.RootToken.Type == JTokenType.Object)
                    {
                        JObject obj = (JObject)activeAsset.RootToken;
                        children = obj.Properties();
                        GetToken = (obj) => ((JProperty)obj).Value;
                    }
                    else if (activeAsset.RootToken.Type == JTokenType.Array)
                    {
                        JArray arr = (JArray)activeAsset.RootToken;
                        children = arr.Children();
                        GetToken = (obj) => (JToken)obj;
                    }
                    else throw new Exception("Invalid Root-TokenType: " + activeAsset.RootToken.Type);

                    foreach (object obj in children)
                    {
                        JToken token = GetToken(obj);
                        JSON.DisplayRowJToken(token, state, out changed);
                        // Vertical Padding
                        GUILayout.Space(2f);
                        if (changed)
                            break;
                    }
                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();
        }
        #endregion
    }
}