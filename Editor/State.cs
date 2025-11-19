using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace dev.vanHoof.UnityJsonEditor
{
    /// <summary>
    /// EditorWindow-State
    /// </summary>
    internal class State
    {
        internal readonly Dictionary<JToken, bool> FoldState = new Dictionary<JToken, bool>();
        internal readonly Dictionary<JToken, string> AddObjName = new Dictionary<JToken, string>();
        internal readonly Dictionary<JToken, TokenType> AddObjType = new Dictionary<JToken, TokenType>();
    }
}
