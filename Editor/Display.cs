using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace dev.vanHoof.JsonEditor
{
    /// <summary>
    /// Util-methods for displaying data in EditorWindow
    /// </summary>
    internal static class Display
    {
        /// <summary>
        /// Indentation in Pixels before a row
        /// </summary>
        internal static float Indentation => 4f + EditorGUI.indentLevel * 12f;
        /// <summary>
        /// Width of Remove-Button in Pixels
        /// </summary>
        internal static float RemBtnWidth => EditorGUIUtility.singleLineHeight * .85f;
        #region Styles
        #region TextStyles
        internal static GUIStyle LargeLabelStyle { get; private set; }
        internal static GUIStyle BoldLabelStyle { get; private set; }
        internal static GUIStyle TypeTextStyle { get; private set; }
        #endregion

        #region FieldStyles
        internal static GUIStyle TextFieldStyle { get; private set; }
        internal static GUIStyle KeyFieldStyle { get; private set; }
        #endregion

        #region ButtonStyles
        internal static GUIStyle BoldFoldoutStyle { get; private set; }
        internal static GUIStyle AddBtnStyle { get; private set; }
        internal static GUIStyle RemBtnStyle { get; private set; }
        internal static GUIStyle DropDownStyle { get; private set; }
        internal static GUIStyle DropDownCreateStyle { get; private set; }
        #endregion
        #endregion

        /// <summary>
        /// TokenType used when creating a new File
        /// </summary>
        private static TokenType newAssetTokenType = TokenType.JObject;

        /// <summary>
        /// Initializes GUIStyles used in Window
        /// </summary>
        internal static void InitStyles()
        {
            if (LargeLabelStyle == null)
                LargeLabelStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 14
                };
            if (BoldLabelStyle == null)
                BoldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            if (TypeTextStyle == null)
                TypeTextStyle = new GUIStyle(EditorStyles.label)
                { alignment = TextAnchor.MiddleRight };
            if (TextFieldStyle == null)
                TextFieldStyle = new GUIStyle(EditorStyles.textField)
                { stretchWidth = false };
            if (KeyFieldStyle == null)
                KeyFieldStyle = new GUIStyle(EditorStyles.textField)
                {
                    stretchHeight = false,
                    stretchWidth = true
                };
            if (BoldFoldoutStyle == null)
                BoldFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                { fontStyle = FontStyle.Bold };
            if (AddBtnStyle == null)
                AddBtnStyle = new GUIStyle(EditorStyles.miniButton)
                { alignment = TextAnchor.LowerLeft };
            if (RemBtnStyle == null)
                RemBtnStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    stretchWidth = false,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            if (DropDownStyle == null)
                DropDownStyle = new GUIStyle(EditorStyles.popup)
                { stretchWidth = false };
            if (DropDownCreateStyle == null)
                DropDownCreateStyle = new GUIStyle(EditorStyles.popup)
                {
                    alignment = TextAnchor.LowerLeft,
                    stretchWidth = false
                };
        }

        /// <summary>
        /// Calculates inner Rects for a Object-Row
        /// </summary>
        /// <param name="rowRect">Outer rect for Row</param>
        /// <param name="token">Token of Object</param>
        /// <param name="remBtnRect">OUT: Rect for remove-button</param>
        /// <param name="keyFieldRect">OUT: Rect for Property-Key</param>
        /// <param name="valFielRect">OUT: Rect for Value-Field</param>
        /// <param name="typeLblRect">OUT: Rect for Type-Label</param>
        internal static void CalcRowRects(Rect rowRect, JToken token, out Rect remBtnRect, out Rect keyFieldRect, out Rect valFielRect, out Rect typeLblRect)
        {
            // Offset Y by 1 to align with TextFields
            remBtnRect = new Rect(rowRect.x, rowRect.y + 1f, RemBtnWidth, rowRect.height);
            float keyOffset = remBtnRect.width + 2f;
            keyFieldRect = token.Parent.Type == JTokenType.Array
                // 0-width rect that just stores offset (No property, so no key)
                ? new Rect(rowRect.x + keyOffset, rowRect.y, 0f, rowRect.height)
                // Rect for Text-field for Key runs from Remove-button until 30% of width
                : new Rect(rowRect.x + keyOffset, 
                           rowRect.y,
                           rowRect.width * .3f - keyOffset - 1f,
                           rowRect.height);
            // Type-Label is fixed-width at end of row
            float lblWidth = 45f;
            typeLblRect = new Rect(rowRect.x + rowRect.width - lblWidth, rowRect.y, lblWidth, rowRect.height);
            // Val-Field fills remaining space
            float valFieldX = keyFieldRect.x + keyFieldRect.width + (keyFieldRect.width > 0 ? 1f : 0f);
            valFielRect = new Rect(valFieldX,
                                    rowRect.y,
                                    typeLblRect.x - valFieldX - 1f,
                                    rowRect.height);
        }

        /// <summary>
        /// Show generic settings about the active File
        /// Includes buttons to open & close files, save, etc.
        /// </summary>
        internal static JsonAsset ShowFileSettings(JsonAsset asset, Func<string> createJson, Func<string, KeyValuePair<string, JToken>> openJson, Func<JToken, string, string> saveJson, Action closeFile, Func<string, string> relativePath)
        {
            if (asset == null) 
                asset = JsonAsset.EMPTY;
            string path = asset.Path;
            TextAsset txtAsset = asset.Asset;
            JToken rootToken = asset.RootToken;
            if (string.IsNullOrEmpty(path)) // No Active File
            {
                if (asset.Asset != null) // Active file through TextAsset-Field.
                    ShowFileData(asset.Asset.name, AssetDatabase.GetAssetPath(asset.Asset));
                txtAsset = (TextAsset)EditorGUILayout.ObjectField(txtAsset, typeof(TextAsset), false);
                if (txtAsset == null)
                    rootToken = null;
                else // Open File for TextAsset by Path
                {
                    path = AssetDatabase.GetAssetPath(txtAsset);
                    KeyValuePair<string, JToken> kvp = openJson(path);
                    path = kvp.Key;
                    rootToken = kvp.Value;
                }
                if (GUILayout.Button("Open JSON File"))
                {
                    KeyValuePair<string, JToken> newKvp = openJson(null);
                    path = newKvp.Key;
                    rootToken = newKvp.Value;
                    if (!string.IsNullOrEmpty(path))
                    {
                        try
                        {
                            TextAsset txt = (TextAsset)AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset));
                            if (txt != null)
                                txtAsset = txt;
                        }
                        catch (Exception) { }
                    }
                }
                GUILayout.BeginHorizontal();
                newAssetTokenType = (TokenType)EditorGUILayout.Popup((int)newAssetTokenType, new string[] { TokenType.JObject.ToString(), TokenType.JArray.ToString() }, DropDownCreateStyle);
                if (GUILayout.Button("Create new JSON File"))
                {
                    JToken newRoot = newAssetTokenType == TokenType.JObject ? new JObject() : new JArray();
                    path = createJson();
                    if (saveJson(newRoot, path) == path)
                    {
                        KeyValuePair<string, JToken> newKvp = openJson(path);
                        path = newKvp.Key;
                        rootToken = newKvp.Value;
                        if (!string.IsNullOrEmpty(path))
                        {
                            try
                            {
                                TextAsset txt = (TextAsset)AssetDatabase.LoadAssetAtPath(relativePath(path), typeof(TextAsset));
                                if (txt != null)
                                    txtAsset = txt;
                            }
                            catch (Exception) { }
                        }
                    }
                    else
                    {
                        GUILayout.EndHorizontal();
                        Debug.LogError("Unable to save JSON-file to path " + path);
                        closeFile();
                        return JsonAsset.EMPTY;
                    }
                }
                GUILayout.EndHorizontal();
                return new JsonAsset(path, txtAsset, rootToken);
            }
            else // Has Active File
            {
                ShowFileData(Path.GetFileNameWithoutExtension(path), path);
                if (GUILayout.Button("Close JSON File"))
                {
                    closeFile();
                    return JsonAsset.EMPTY;
                }
                return new JsonAsset(path, txtAsset, rootToken);
            }
        }

        /// <summary>
        /// Display FileName & -Path in Labels
        /// </summary>
        internal static void ShowFileData(string name, string path)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("FileName: ");
            GUILayout.Label(name, EditorStyles.miniLabel);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("FilePath: ");
            GUILayout.Label(path, EditorStyles.miniLabel);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Display credits for UnityJsonEditor
        /// </summary>
        internal static void ShowCredits()
        {
            GUILayout.Label("JSON-Editor v1.1", LargeLabelStyle);
            GUILayout.Label("Made By: ", BoldLabelStyle);
            GUILayout.Label("Frank van Hoof");
            GUILayout.Label("Contact:", BoldLabelStyle);
            GUILayout.Label("frank@vanhoof.dev");
        }
    }
}
