using UnityEditor;

namespace Glitchfjord.SudokuClash
{
    public class CsprojModifier : AssetPostprocessor
    {
        private static string OnGeneratedCSProject(string path, string content)
        {
            if (path.Contains("Assembly-CSharp.csproj") || path.Contains("Gosuman"))
            {
                content = content.Replace("<LangVersion>9.0</LangVersion>", "<LangVersion>10</LangVersion>");
            }
            return content;
        }
    }
}
