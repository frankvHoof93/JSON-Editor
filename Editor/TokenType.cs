namespace dev.vanHoof.UnityJsonEditor
{
    /// <summary>
    /// Type of JToken
    /// </summary>
    internal enum TokenType : short
    {
        JObject = 0,
        JArray = 1,
        Boolean = 2,
        Integer = 3,
        Double = 4,
        String = 5
    }
}
