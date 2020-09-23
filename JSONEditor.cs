using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class UnityJsonEditor : EditorWindow
{
    #region Variables
    private JToken rootObject = null;
    private Dictionary<JToken, bool> foldState = new Dictionary<JToken, bool>();
    private Dictionary<JToken, string> addObjName = new Dictionary<JToken, string>();
    private Dictionary<JToken, int> addObjType = new Dictionary<JToken, int>();
    private bool showFileSettings = true, showCredits = true, showFileContents, itemRemoved;
    private int createType = 0;
    private TextAsset asset = null;
    private string dataPath = null;
    private GUIStyle remButtonStyle, keyFieldStyle, boldFoldoutStyle, typeTextStyle, textFieldStyle;
    private GUIStyle dropDownStyle, btnAddStyle, dropDownCreateStyle;
    private GUIStyle largeLabelStyle, boldLabelStyle;
    private Vector2 scrollPosContent = Vector2.zero;
    private string[] valueTypes = new string[]
    {
        "Boolean", "Integer", "Double", "String", "JObject", "JArray"
    };
    #endregion

    #region JSONFile
    private void OpenJSON(string path)
    {
        if (path == null || path.Equals(string.Empty))
            path = EditorUtility.OpenFilePanel("Open JSON File", "", "json");
        if (path.Length != 0)
        {
            foldState.Clear();
            addObjName.Clear();
            addObjType.Clear();
            scrollPosContent = Vector2.zero;
            string text = File.ReadAllText(path);
            dataPath = path;
            asset = null;
            try
            {
                rootObject = (JToken)JsonConvert.DeserializeObject(text);
                showCredits = false;
                showFileContents = true;
            }
            catch (JsonReaderException e)
            {
                dataPath = null;
                throw new Exception("Couln't parse JSON", e);
            }
        }
    }

    private void CreateJSON()
    {
        string title = "Create New JSON File";
        string directory = Application.dataPath;
        string defaultName = string.Empty;
        string extension = "json";
        string path = EditorUtility.SaveFilePanel(title, directory, defaultName, extension);
        if (path == null || path.Equals(string.Empty))
            return;
        if (!File.Exists(path))
            File.Create(path);
        dataPath = path;
        if (createType == 0)
            rootObject = new JObject();
        else
            rootObject = new JArray();
    }

    private void SaveJSON(bool overwrite)
    {
        if (overwrite && dataPath != null && dataPath != string.Empty && rootObject != null)
            File.WriteAllText(dataPath, rootObject.ToString());
        else if (!overwrite)
            CreateJSON();
    }
    #endregion

    #region UnityMethods
    [MenuItem("Tools/JSON Editor")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(UnityJsonEditor));
    }

    private void Awake()
    {
        boldFoldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold
        };
        remButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            stretchWidth = false,
            fontStyle = FontStyle.Bold
        };
        keyFieldStyle = new GUIStyle(EditorStyles.textField)
        {
            stretchWidth = false
        };
        typeTextStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.LowerRight
        };
        textFieldStyle = new GUIStyle(EditorStyles.textField)
        {
            stretchWidth = false
        };
        dropDownStyle = new GUIStyle(EditorStyles.popup)
        {
            stretchWidth = false
        };
        btnAddStyle = new GUIStyle(EditorStyles.miniButton)
        {
            alignment = TextAnchor.LowerLeft
        };
        dropDownCreateStyle = new GUIStyle(EditorStyles.popup)
        {
            alignment = TextAnchor.LowerLeft,
            stretchWidth = false
        };
        largeLabelStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 14
        };
        boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
    }

    private void OnGUI()
    {
        SetVariableSettings();
        GUILayout.BeginVertical();
        showFileSettings = EditorGUILayout.Foldout(showFileSettings, "File Settings", boldFoldoutStyle);
        if (showFileSettings)
            DisplayFileSettings();
        showCredits = EditorGUILayout.Foldout(showCredits, "Credits", boldFoldoutStyle);
        if (showCredits)
            DisplayCredits();
        showFileContents = EditorGUILayout.Foldout(showFileContents, "File Contents", boldFoldoutStyle);
        if (showFileContents)
            if (rootObject == null)
                GUILayout.Label("No JSON file opened", EditorStyles.largeLabel);
            else if (rootObject != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear Changes"))
                {
                    OpenJSON(dataPath);
                    return;
                }
                if (GUILayout.Button("Save Changes"))
                {
                    SaveJSON(true);
                    return;
                }
                if (GUILayout.Button("Save As"))
                {
                    SaveJSON(false);
                    return;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginVertical();
                DisplayRootObject();
                GUILayout.EndVertical();
            }
        GUILayout.EndVertical();
    }

    #region Other
    private void SetVariableSettings()
    {
        keyFieldStyle.fixedWidth = Screen.width / 5f;
        textFieldStyle.fixedWidth = Screen.width * .6f;
        dropDownStyle.fixedWidth = Screen.width * .2f;
        itemRemoved = false;
    }
    #endregion
    #endregion

    #region DisplayObject
    private void DisplayRootObject()
    {
        GetAddRow(rootObject);
        scrollPosContent = GUILayout.BeginScrollView(scrollPosContent, new GUIStyle());
        if (rootObject.Type == JTokenType.Object)
        {
            JObject obj = (JObject)rootObject;
            foreach (JProperty token in obj.Properties())
            {
                switch (token.Value.Type)
                {
                    case JTokenType.Object:
                        DisplayRowJObject(token.Value);
                        break;
                    case JTokenType.Array:
                        DisplayRowJArray(token.Value);
                        break;
                    default:
                        DisplayRowJValue(token.Value);
                        break;
                }
                if (itemRemoved)
                    return;
            }
        }
        else if (rootObject.Type == JTokenType.Array)
        {
            JArray arr = (JArray)rootObject;
            foreach (JToken token in arr.Children())
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        DisplayRowJObject(token);
                        break;
                    case JTokenType.Array:
                        DisplayRowJArray(token);
                        break;
                    default:
                        DisplayRowJValue(token);
                        break;
                }
                if (itemRemoved)
                    return;
            }
        }
        GUILayout.EndScrollView();
    }

    private void DisplayRowJObject(JToken objectToken)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(5f + EditorGUI.indentLevel * 15f);
        GetRemoveButton(objectToken);
        if (itemRemoved)
            return;
        GetKey(objectToken);
        bool foldOut = GetObjectFoldLabel(objectToken);
        GUILayout.EndHorizontal();
        if (foldOut)
        {
            EditorGUI.indentLevel++;
            GetAddRow(objectToken);
            GUILayout.BeginVertical();
            foreach (JProperty token in ((JObject)(objectToken)).Properties())
            {
                switch (token.Value.Type)
                {
                    case JTokenType.Object:
                        DisplayRowJObject(token.Value);
                        break;
                    case JTokenType.Array:
                        DisplayRowJArray(token.Value);
                        break;
                    default:
                        DisplayRowJValue(token.Value);
                        break;
                }
                if (itemRemoved)
                    return;
            }
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }
    }

    private void DisplayRowJValue(JToken objectToken)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(5f + EditorGUI.indentLevel * 15f);
        GetRemoveButton(objectToken);
        if (itemRemoved)
            return;
        GetKey(objectToken);
        GetField(objectToken);
        GUILayout.EndHorizontal();
    }

    private void DisplayRowJArray(JToken objectToken)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(5f + EditorGUI.indentLevel * 15f);
        GetRemoveButton(objectToken);
        if (itemRemoved)
            return;
        GetKey(objectToken);
        bool foldOut = GetObjectFoldLabel(objectToken);
        GUILayout.EndHorizontal();
        if (foldOut)
        {
            EditorGUI.indentLevel++;
            GetAddRow(objectToken);
            GUILayout.BeginVertical();
            foreach (JToken kid in ((JArray)(objectToken)).Children())
            {
                switch (kid.Type)
                {
                    case JTokenType.Object:
                        DisplayRowJObject(kid);
                        break;
                    case JTokenType.Array:
                        DisplayRowJArray(kid);
                        break;
                    default:
                        DisplayRowJValue(kid);
                        break;
                }
                if (itemRemoved)
                    return;
            }
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }
    }

    #region RowParts
    private void GetAddRow(JToken parent)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(5 + EditorGUI.indentLevel * 15f);
        string name = string.Empty;
        if (parent.Type == JTokenType.Object)
        {
            if (addObjName.ContainsKey(parent))
                name = addObjName[parent];
            else addObjName.Add(parent, name);
            name = GUILayout.TextField(name, keyFieldStyle);
            addObjName[parent] = name;
        }
        int selected = 0;
        if (addObjType.ContainsKey(parent))
            selected = addObjType[parent];
        else addObjType.Add(parent, selected);
        selected = EditorGUILayout.Popup("", selected, valueTypes, dropDownStyle);
        addObjType[parent] = selected;
        if (GUILayout.Button("Add Object", btnAddStyle))
        {
            JToken val = new JArray();
            switch (selected)
            {
                case 0:
                    val = new bool();
                    break;
                case 1:
                    val = new int();
                    break;
                case 2:
                    val = new double();
                    break;
                case 3:
                    val = string.Empty;
                    break;
                case 4:
                    val = new JObject();
                    break;
                default:
                    throw new ArgumentException("Invalid Object Type");
            }
            if (parent.Type == JTokenType.Array)
            {
                JArray array = (JArray)parent;
                array.Add(val);
            }
            else if (parent.Type == JTokenType.Object)
            {
                JObject obj = (JObject)parent;
                if (name.Equals(string.Empty) || name == null || obj[name] != null)
                    return;
                obj.Add(name, val);
                addObjName[parent] = string.Empty;
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void GetRemoveButton(JToken toRemove)
    {
        if (GUILayout.Button("-", remButtonStyle))
        {
            if (toRemove.Parent.Type == JTokenType.Property)
                ((JProperty)toRemove.Parent).Remove();
            else if (toRemove.Parent.Type == JTokenType.Array)
                ((JArray)toRemove.Parent).Remove(toRemove);
            itemRemoved = true;
        }
    }

    private void GetKey(JToken token)
    {
        if (token.Parent.Type != JTokenType.Property)
            return;
        JProperty property = (JProperty)(token.Parent);
        string key = GUILayout.TextField(property.Name, keyFieldStyle);
        if (!key.Equals(property.Name))
        {
            JProperty newToken = new JProperty(key, property.Value);
            try
            {
                property.Replace(newToken);
            }
            catch (ArgumentException) { }
        }
    }

    private bool GetObjectFoldLabel(JToken objectToken)
    {
        bool foldOut = false;
        if (foldState.ContainsKey(objectToken))
            foldOut = foldState[objectToken];
        else
            foldState.Add(objectToken, false);
        int i = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        string label = "JObject";
        if (objectToken.Type == JTokenType.Array)
            label = "JArray";
        if (objectToken.Parent.Type == JTokenType.Property)
            label += " - " + ((JProperty)objectToken.Parent).Name;
        foldOut = EditorGUILayout.Foldout(foldOut, label);
        EditorGUI.indentLevel = i;
        foldState[objectToken] = foldOut;
        return foldOut;
    }

    private void GetField(JToken obj)
    {
        int i = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        var val = obj;
        if (obj.Type.Equals(JTokenType.Boolean))
            val = GUILayout.Toggle((bool)obj, ((bool)obj ? "true" : "false"));
        else if (obj.Type.Equals(JTokenType.String))
            val = GUILayout.TextField((string)obj, textFieldStyle);
        else if (obj.Type.Equals(JTokenType.Integer))
            val = EditorGUILayout.IntField((int)obj, textFieldStyle);
        else if (obj.Type.Equals(JTokenType.Float))
            val = EditorGUILayout.DoubleField((double)obj, textFieldStyle);
        if (!val.Equals(obj))
            if (obj.Parent.Type == JTokenType.Property)
            {
                ((JProperty)(obj.Parent)).Value = val;
            }
            else if (obj.Parent.Type == JTokenType.Array)
            {
                obj.Replace(val);
            }
        if (obj.Type != JTokenType.Boolean)
            GUILayout.Label(obj.Type.ToString(), typeTextStyle);
        EditorGUI.indentLevel = i;
    }
    #endregion
    #endregion

    #region DisplayFile
    private void ShowFileData(string name, string path)
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

    private void DisplayFileSettings()
    {
        if (dataPath == null || dataPath.Length == 0)
        {
            if (asset != null)
                ShowFileData(asset.name, AssetDatabase.GetAssetPath(asset));
            asset = (TextAsset)EditorGUILayout.ObjectField(asset, typeof(TextAsset), false);
            if (asset == null)
                rootObject = null;
            else OpenJSON(AssetDatabase.GetAssetPath(asset));
            if (GUILayout.Button("Open JSON File"))
                OpenJSON(null);
            GUILayout.BeginHorizontal();
            createType = EditorGUILayout.Popup(createType, new string[] { valueTypes[4], valueTypes[5] }, dropDownCreateStyle);
            if (GUILayout.Button("Create new JSON File"))
            {
                CreateJSON();
                SaveJSON(true);
                OpenJSON(dataPath);
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            ShowFileData(Path.GetFileNameWithoutExtension(dataPath), dataPath);
            if (GUILayout.Button("Close JSON File"))
                dataPath = null;
        }
    }

    private void DisplayCredits()
    {
        GUILayout.Label("JSON-Editor v1.0", largeLabelStyle);
        GUILayout.Label("Made By: ", boldLabelStyle);
        GUILayout.Label("Frank van Hoof");
        GUILayout.Label("Contact:", boldLabelStyle);
        GUILayout.Label("fvanhoof@gmail.com");
    }
    #endregion
}
