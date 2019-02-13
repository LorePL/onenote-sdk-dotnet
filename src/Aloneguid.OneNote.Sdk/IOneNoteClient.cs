using Refit;
using System;
using System.IO;
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

      [Get("/v1.0/me/notes/pages/{pageId}/preview")]
      Task<PagePreviewResponse> GetPagePreview(string pageId);

      //https://www.onenote.com/api/v1.0/me/notes/resources/1-9f2f281b6f3143339e2407cd68b3e41f!1-a3887b9d-f776-406c-84d8-09917f1b7dca/$value
      [Get("/v1.0/me/notes/resources/{id}/$value")]
      Task<Stream> DownloadResource(string id);
   }
}
