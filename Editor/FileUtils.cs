using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace dev.vanHoof.UnityJsonEditor
{
    /// <summary>
    /// Util-methods for working with Files
    /// </summary>
    internal static class FileUtils
    {
        /// <summary>
        /// Event fired when a (new) file is opened
        /// </summary>
        internal static event Action<string> OnFileOpened;
        /// <summary>
        /// Event fired when active file is closed
        /// </summary>
        internal static event Action OnFileClosed;

        /// <summary>
        /// Creates a new JSON-file
        /// Uses SaveFilePanel to ask for destination & filename
        /// </summary>
        /// <returns>Path to new file</returns>
        internal static string CreateJsonFile()
        {
            string title = "Create New JSON File";
            string directory = Application.dataPath;
            string defaultName = string.Empty;
            string extension = "json";
            string path = EditorUtility.SaveFilePanel(title, directory, defaultName, extension);
            if (string.IsNullOrWhiteSpace(path))
                return default;
            if (!File.Exists(path))
            {
                File.Create(path).Close();
                AssetDatabase.Refresh();
            }
            return path;
        }

        /// <summary>
        /// Closes active file
        /// </summary>
        internal static void CloseJSONFile()
        {
            OnFileClosed?.Invoke();
        }

        /// <summary>
        /// Opens a JSON-File
        /// If no path is provided, the OpenFilePanel is displayed to pick a file
        /// </summary>
        /// <param name="path">Path to file. Leave NULL to use FilePanel</param>
        /// <returns>Path, RootToken</returns>
        /// <exception cref="Exception">Exceptions thrown during deserializing of JSON</exception>
        internal static KeyValuePair<string, JToken> OpenJSONFile(string path = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = EditorUtility.OpenFilePanel("Open JSON File", "", "json");
            if (path.Length != 0)
            {
                try
                {
                    string text = File.ReadAllText(path);
                    JToken rootObject = (JToken)JsonConvert.DeserializeObject(text);
                    OnFileOpened?.Invoke(path);
                    return new KeyValuePair<string, JToken>(path, rootObject);
                }
                catch (JsonReaderException e)
                {
                    throw new Exception("Couln't parse JSON", e);
                }
            }
            return default;
        }

        /// <summary>
        /// Saves JSON to file
        /// Uses SaveFilePanel if no path is provided
        /// </summary>
        /// <param name="rootObject">RootToken for JSON to save</param>
        /// <param name="path">Path to save to. Leave NULL to use FilePanel</param>
        /// <returns>Path to file</returns>
        internal static string SaveJsonToFile(JToken rootObject, string path = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = CreateJsonFile();
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;
            File.WriteAllText(path, rootObject.ToString());
            AssetDatabase.Refresh();
            return path;
        }

        /// <summary>
        /// Converts unknown path to be absolute
        /// </summary>
        internal static string EnsureAbsolutePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            // Normalize Slashes
            path = path.Replace('\\', '/');
            if (Path.IsPathRooted(path))
                return Path.GetFullPath(path);

            // Return any non-Asset-Paths unchanged.
            if (!path.StartsWith("Assets/") && path != "Assets")
            {
                Debug.LogWarning($"Unable to resolve [{path}] to an absolute path, as it's not part of the Assets-Folder.");
                return path;
            }

            string absolute = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
            return Path.GetFullPath(absolute).Replace('\\', '/');
        }

        /// <summary>
        /// Converts unknown path to be relative to assets-folder
        /// </summary>
        internal static string EnsureRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Normalize Slashes
            path = Path.GetFullPath(path).Replace('\\', '/');
            string assetsFullPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');

            // Already relative?
            if (path.StartsWith("Assets/"))
                return path;

            // Full Path inside Assets-Folder?
            if (path.StartsWith(assetsFullPath))
            {
                return "Assets" + path.Substring(assetsFullPath.Length);
            }

            // Invalid Path
            Debug.LogWarning($"Unable to resolve [{path}] to a relative path, as it's not part of the Assets-Folder.");
            return path;
        }
    }
}
