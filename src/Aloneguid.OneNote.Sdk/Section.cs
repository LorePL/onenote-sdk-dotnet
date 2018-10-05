using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aloneguid.OneNote.Sdk
{
   public class SectionsResponse
   {
      [JsonProperty("@odata.context")]
      public string Context { get; set; }

      [JsonProperty("value")]
      public Section[] Sections { get; set; }
   }

   public class Section : OneNoteEntity
   {
   }
}
