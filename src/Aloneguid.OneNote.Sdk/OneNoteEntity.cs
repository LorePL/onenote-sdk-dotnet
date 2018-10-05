using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aloneguid.OneNote.Sdk
{
   public abstract class OneNoteEntity
   {
      [JsonProperty("id")]
      public string Id { get; set; }

      [JsonProperty("name")]
      public string Name { get; set; }

      [JsonProperty("createdBy")]
      public string CreatedBy { get; set; }

      [JsonProperty("lastModifiedBy")]
      public string LastModifiedBy { get; set; }

      [JsonProperty("lastModifiedTime")]
      public DateTime LastModifiedTime { get; set; }

      public override string ToString()
      {
         return Name;
      }
   }
}
