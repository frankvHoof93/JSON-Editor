using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace dev.vanHoof.JsonEditor
{
    /// <summary>
    /// Util-methods used to display JSON in ScrollView
    /// </summary>
    internal static class DisplayJson
    {
        /// <summary>
        /// Displays a row for a JToken (Object, Array or Value)
        /// </summary>
        /// <param name="token">Token to draw for</param>
        /// <param name="state">State of Editor-Window</param>
        /// <param name="changed">OUT: if <paramref name="token"/> was changed</param>
        internal static void DisplayRowJToken(JToken token, State state, out bool changed)
        {
            changed = false;
            GUILayout.BeginHorizontal();
            GUILayout.Space(Display.Indentation);
            // Draw a Row, so the width & height are computed
            GUILayout.Box(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing));
            Rect rowRect = GUILayoutUtility.GetLastRect();
            Display.CalcRowRects(rowRect, token, out Rect remBtnRect, out Rect keyFieldRect, out Rect valueFieldRect, out Rect typeLblRect);
            DisplayRemoveButton(remBtnRect, token, out changed);
            bool nested = false;
            if (!changed)
            {
                DisplayKey(keyFieldRect, token, out changed);
                if (token.Type == JTokenType.Array || token.Type == JTokenType.Object)
                {
                    nested = GetObjectFoldLabel(rowRect, token, state);
                }
                else
                {
                    nested = false;
                    DisplayValue(valueFieldRect, typeLblRect, token, out changed);
                }
            }
            GUILayout.EndHorizontal();
            if (!changed && nested)
            {
                // Draw inner
                EditorGUI.indentLevel++;
                DisplayAddRow(token, state, out changed);
                if (changed)
                {
                    EditorGUI.indentLevel--;
                    return;
                }
                GUILayout.BeginVertical();
                IEnumerable<object> children = null;
                Func<object, JToken> GetToken = null;

                if (token.Type == JTokenType.Object)
                {
                    JObject obj = (JObject)token;
                    children = obj.Properties();
                    GetToken = (obj) => ((JProperty)obj).Value;
                }
                else if (token.Type == JTokenType.Array)
                {
                    JArray arr = (JArray)token;
                    children = arr.Children();
                    GetToken = (obj) => (JToken)obj;
                }
                else throw new Exception("Invalid TokenType: " + token.Type);

                foreach (object obj in children)
                {
                    JToken child = GetToken(obj);
                    DisplayRowJToken(child, state, out changed);
                    // Vertical Padding
                    GUILayout.Space(2f);
                    if (changed)
                    {
                        // Stop drawing, await next cycle
                        break;
                    }
                }
                EditorGUI.indentLevel--;
                GUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Displays a row used to add a new JToken to a parent
        /// </summary>
        /// <param name="parent">Parent for new JToken</param>
        /// <param name="state">EditorWindow-State</param>
        internal static void DisplayAddRow(JToken parent, State state, out bool added)
        {
            added = false;
            GUILayout.BeginHorizontal();
            GUILayout.Space(Display.Indentation);
            string name = string.Empty;
            if (parent.Type == JTokenType.Object)
            {
                if (state.AddObjName.ContainsKey(parent))
                    name = state.AddObjName[parent];
                else state.AddObjName.Add(parent, name);
                name = GUILayout.TextField(name, Display.KeyFieldStyle, GUILayout.ExpandWidth(true));
                state.AddObjName[parent] = name;
            }
            TokenType selected = TokenType.JObject;
            if (state.AddObjType.ContainsKey(parent))
                selected = state.AddObjType[parent];
            else state.AddObjType.Add(parent, selected);
            // Set indentation to 0 to render Popup itself
            int i = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            selected = (TokenType)EditorGUILayout.Popup("", Enum.GetNames(typeof(TokenType)).AsSpan().IndexOf(selected.ToString()), Enum.GetNames(typeof(TokenType)), Display.DropDownStyle, GUILayout.MaxWidth(100f));
            EditorGUI.indentLevel = i;
            state.AddObjType[parent] = selected;
            if (GUILayout.Button("Add", Display.AddBtnStyle, GUILayout.Width(38f)))
            {
                JToken val;
                switch (selected)
                {
                    case TokenType.JObject:
                        val = new JObject();
                        break;
                    case TokenType.JArray:
                        val = new JArray();
                        break;
                    case TokenType.Boolean:
                        val = new bool();
                        break;
                    case TokenType.Integer:
                        val = new int();
                        break;
                    case TokenType.Double:
                        val = new double();
                        break;
                    case TokenType.String:
                        val = string.Empty;
                        break;
                    default:
                        throw new ArgumentException("Invalid Object Type");
                }
                if (parent.Type == JTokenType.Array)
                {
                    JArray array = (JArray)parent;
                    array.Add(val);
                    added = true;
                }
                else if (parent.Type == JTokenType.Object)
                {
                    JObject obj = (JObject)parent;
                    if (!string.IsNullOrEmpty(name) && obj[name] == null)
                    {
                        obj.Add(name, val);
                        state.AddObjName[parent] = string.Empty;
                        added = true;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private static bool GetObjectFoldLabel(Rect rowRect, JToken objectToken, State state)
        {
            Rect valRect = new Rect(rowRect);
            if (objectToken.Parent.Type == JTokenType.Property)
            {
                valRect.x += rowRect.width * .3f + 1f;
                valRect.width *= .3f;
            }
            else
            {
                float offset = Display.RemBtnWidth + 1f;
                valRect.x += offset;
                valRect.width = valRect.width - offset - 1f;
            }

            bool foldOut = false;
            if (state.FoldState.ContainsKey(objectToken))
                foldOut = state.FoldState[objectToken];
            else
                state.FoldState.Add(objectToken, false);
            int i = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            string label = "JObject";
            if (objectToken.Type == JTokenType.Array)
                label = "JArray";
            if (objectToken.Parent.Type == JTokenType.Property)
                label += " - " + ((JProperty)objectToken.Parent).Name;
            foldOut = EditorGUI.Foldout(valRect, foldOut, label);
            EditorGUI.indentLevel = i;
            state.FoldState[objectToken] = foldOut;
            return foldOut;
        }

        private static void DisplayRemoveButton(Rect btnRect, JToken token, out bool removed)
        {
            removed = false;
            if (GUI.Button(btnRect, "-", Display.RemBtnStyle))
            {
                // Value is child of property (key-value pair). Remove entire property.
                if (token.Parent.Type == JTokenType.Property)
                {
                    ((JProperty)token.Parent).Remove();
                    removed = true;
                }
                // Remove value from Array
                else if (token.Parent.Type == JTokenType.Array)
                    removed = ((JArray)token.Parent).Remove(token);
            }
        }

        private static void DisplayKey(Rect keyRect, JToken token, out bool changed)
        {
            changed = false;
            if (token.Parent.Type != JTokenType.Property)
                return; // Only Properties have keys
            JProperty property = (JProperty)(token.Parent);
            string key = GUI.TextField(keyRect, property.Name, Display.KeyFieldStyle);
            if (!key.Equals(property.Name))
            {
                // Create new Property to change Key
                JProperty newToken = new JProperty(key, property.Value);
                try
                {
                    property.Replace(newToken);
                    changed = true;
                }
                catch (ArgumentException) { }
            }
        }

        private static void DisplayValue(Rect valRect, Rect typelblRect, JToken token, out bool changed)
        {
            changed = false;
            int i = EditorGUI.indentLevel;
            // Reset indent-level to 0 for this inner draw
            EditorGUI.indentLevel = 0;
            var val = token;
            // TODO: Find ways to implement more complex TokenTypes
            if (token.Type.Equals(JTokenType.Boolean))
                val = GUI.Toggle(valRect, (bool)token, ((bool)token ? "true" : "false"));
            else if (token.Type.Equals(JTokenType.String))
                val = GUI.TextField(valRect, (string)token, Display.TextFieldStyle);
            else if (token.Type.Equals(JTokenType.Integer))
                val = EditorGUI.IntField(valRect, (int)token, Display.TextFieldStyle);
            else if (token.Type.Equals(JTokenType.Float))
                val = EditorGUI.DoubleField(valRect, (double)token, Display.TextFieldStyle);
            else // Unparsed value. Display only
                EditorGUI.LabelField(valRect, token.ToString());
            if (!val.Equals(token))
            {
                changed = true;
                if (token.Parent.Type == JTokenType.Property)
                {
                    // Set new value on existing Property
                    ((JProperty)(token.Parent)).Value = val;
                }
                else if (token.Parent.Type == JTokenType.Array)
                {
                    // Replace value in Array
                    token.Replace(val);
                }
            }
            // Draw Token-Type
            GUI.Label(typelblRect, token.Type.ToString(), Display.TypeTextStyle);
            // Restore indent-level
            EditorGUI.indentLevel = i;
        }
    }
}
