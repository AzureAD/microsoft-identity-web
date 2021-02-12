using System.Linq;

namespace DotnetTool.Project
{
    public class MatchesForProjectType
    {
        public string? FileRelativePath { get; set; }

        public string[]? MatchAny { get; set; }
        public string? Sets { get; set; }

        public string? FolderRelativePath { get; set; }

        /// <summary>
        /// Either FileRelativePath is defined, along with MatchAny
        /// Or FolderRelativePath is defined
        /// </summary>
        /// <returns></returns>
        internal bool IsValid()
        {
            bool isValid =
                !string.IsNullOrEmpty(FileRelativePath) || !string.IsNullOrEmpty(FolderRelativePath)
                && (string.IsNullOrEmpty(FileRelativePath) || (MatchAny != null && MatchAny.Any()));
            return isValid;
        }
    }
}
