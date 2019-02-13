namespace Aloneguid.OneNote.ToMarkdown
{
   public interface ISettings
   {
      string TenantId { get; }
      string ClientId { get; }
      string RedirectUri { get; }

      string NotebookName { get; set; }
      string SectionName { get; set; }
   }
}
