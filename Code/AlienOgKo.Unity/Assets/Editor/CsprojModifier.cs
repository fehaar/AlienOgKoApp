using UnityEditor;

namespace AlienOgKo.Editor
{
    public class CsprojModifier : AssetPostprocessor
    {
        private static string OnGeneratedCSProject(string path, string content)
        {
            if (path.Contains("Assembly-CSharp.csproj") || path.Contains("AlienOgKo"))
            {
                content = content.Replace("<LangVersion>9.0</LangVersion>", "<LangVersion>10</LangVersion>");
            }
            return content;
        }
    }
}
