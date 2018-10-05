using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aloneguid.OneNote.Sdk
{
   public class PagesResponse
   {
      [JsonProperty("value")]
      public Page[] Pages { get; set; }
   }

   public class Page
   {
      [JsonProperty("id")]
      public string Id { get; set; }

      [JsonProperty("createdTime")]
      public DateTime CreatedTime { get; set; }

      [JsonProperty("title")]
      public string Title { get; set; }
   }
}