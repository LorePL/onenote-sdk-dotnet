using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aloneguid.OneNote.ToMarkdown
{
   public static class StringExtensions
   {
      public static string RemoveLinesContaining(this string input, string substring, StringComparison stringComparison = StringComparison.CurrentCulture)
      {
         if (string.IsNullOrEmpty(input)) return input;

         var result = new StringBuilder();

         using (var sr = new StringReader(input))
         {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
               if (line.IndexOf(substring, stringComparison) != -1) continue;

               result.AppendLine(line);
            }
         }

         return result.ToString();
      }

   }
}
