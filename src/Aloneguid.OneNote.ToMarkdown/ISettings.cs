using Config.Net;

namespace Aloneguid.OneNote.ToMarkdown
{
   public interface ISettings
   {
      string TenantId { get; }
      string ClientId { get; }
      string RedirectUri { get; }

      string NotebookName { get; set; }
      string SectionName { get; set; }
      string RootDir { get; }

      [Option(DefaultValue = 1959)]
      int TitleWidth { get; }

      [Option(DefaultValue = 812)]
      int TitleHeight { get; }

      [Option(DefaultValue = 95)]
      int TitleQuality { get; }

      [Option(DefaultValue = 1959)]
      int ImageWidth { get; }

      [Option(DefaultValue = 0)]
      int ImageHeight { get; }

      [Option(DefaultValue = 85)]
      int ImageQuality { get; }
   }
}
