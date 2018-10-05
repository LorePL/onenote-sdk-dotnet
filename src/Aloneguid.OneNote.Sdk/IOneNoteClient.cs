using Refit;
using System;
using System.Threading.Tasks;

namespace Aloneguid.OneNote.Sdk
{
   [Headers("Authorization: Bearer")]
   public interface IOneNoteClient
   {
      [Get("/v1.0/me/notes/notebooks")]
      Task<NotebooksResponse> GetNotebooksAsync();

      [Get("/v1.0/me/notes/notebooks/{notebookId}/sections")]
      Task<SectionsResponse> GetNotebookSectionsAsync(string notebookId);

      [Get("/v1.0/me/notes/sections/{sectionId}/pages")]
      Task<PagesResponse> GetSectionPagesAsync(string sectionId);

      [Get("/v1.0/me/notes/pages/{pageId}/content")]
      Task<string> GetPageHtmlContent(string pageId);

      [Get("/api/v1.0/me/notes/pages/{pageId}/preview")]
      Task<PagePreviewResponse> GetPagePreview(string pageId);
   }
}
