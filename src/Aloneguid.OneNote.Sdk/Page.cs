using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Aloneguid.OneNote.Sdk
{
   public class PagesResponse
   {
      [JsonProperty("value")]
      public Page[] Pages { get; set; }
   }

   public class Page
   {
      private static readonly Regex ResourceRegex = new Regex(
         "https://www.onenote.com/api/v1.0/me/notes/resources/(?<id>.*?)/\\$value",
         RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

      [JsonProperty("id")]
      public string Id { get; set; }

      [JsonProperty("createdTime")]
      public DateTime CreatedTime { get; set; }

      [JsonProperty("title")]
      public string Title { get; set; }

      public override string ToString() => Title;

      public static string[] ExtractResourceIds(string content)
      {
         MatchCollection matches = ResourceRegex.Matches(content);

         return matches.Cast<Match>().Select(m => m.Groups["id"].ToString()).ToArray();
      }

      public static string MakeFullResourceId(string id)
      {
         return $"https://www.onenote.com/api/v1.0/me/notes/resources/{id}/$value";
      }
   }
}