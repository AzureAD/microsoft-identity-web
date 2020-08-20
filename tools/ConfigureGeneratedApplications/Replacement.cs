using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigureGeneratedApplications
{
    public class Replacement
    {
        public Replacement(string filePath, int index, int length, string replaceFrom, string replaceBy)
        {
            FilePath = filePath;
            Index = index;
            Length = length;
            ReplaceFrom = replaceFrom;
            ReplaceBy = replaceBy;
        }
        public string FilePath { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }
        public string ReplaceFrom { get; set; }
        public string ReplaceBy { get; set; }

        public override string ToString()
        {
            return $"Replace '{ReplaceFrom}' by '{ReplaceBy}'";
        }
    }
}
