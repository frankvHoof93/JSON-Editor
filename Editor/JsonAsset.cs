using Newtonsoft.Json.Linq;
using UnityEngine;

namespace dev.vanHoof.JsonEditor
{
    /// <summary>
    /// A reference to a JSON-Asset(/file)
    /// </summary>
    internal class JsonAsset
    {
        /// <summary>
        /// Empty Asset
        /// </summary>
        internal static readonly JsonAsset EMPTY = new JsonAsset(null, null, null);

        /// <summary>
        /// Path to file (usually absolute path)
        /// </summary>
        internal readonly string Path;
        /// <summary>
        /// TextAsset for File (can be NULL)
        /// </summary>
        internal readonly TextAsset Asset;
        /// <summary>
        /// Root-JToken for file
        /// </summary>
        internal readonly JToken RootToken;

        internal JsonAsset(string path, TextAsset asset, JToken rootToken)
        {
            Path = path;
            Asset = asset;
            RootToken = rootToken;
        }
    }
}
