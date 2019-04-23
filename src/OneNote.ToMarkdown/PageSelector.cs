using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneNote.Net;
using Serilog;

namespace Aloneguid.OneNote.ToMarkdown
{
   class PageSelector
   {
      private readonly IOneNoteClient _client;
      private readonly ISettings _settings;

      public PageSelector(IOneNoteClient client, ISettings settings)
      {
         _client = client;
         _settings = settings;
      }

      public async Task<Page> SelectPageAsync()
      {
         //select notebook
         Notebook notebook = await SelectNotebookAsync();
         Section section = await SelectSectionAsync(notebook);

         return await SelectPageAsync(section);
      }

      private async Task<Notebook> SelectNotebookAsync()
      {
         Log.Debug("loading notebooks...");
         NotebooksResponse allNotebooks = await _client.GetNotebooksAsync();

         if (_settings.NotebookName == null || allNotebooks.Notebooks.FirstOrDefault(n => n.Name == _settings.NotebookName) == null)
         {
            Log.Debug("select notebook");
            int i = 0;
            foreach (Notebook notebook in allNotebooks.Notebooks)
            {
               Log.Debug("{index}. {name}", ++i, notebook.Name);
            }

            int idx = AskNumber();
            Notebook selected = allNotebooks.Notebooks[idx - 1];
            _settings.NotebookName = selected.Name;
            Log.Debug("selected {name}", selected.Name);
            return selected;
         }

         Notebook cached = allNotebooks.Notebooks.First(n => n.Name == _settings.NotebookName);
         Log.Debug("selected {name}", cached.Name);
         return cached;
      }

      private async Task<Section> SelectSectionAsync(Notebook notebook)
      {
         Log.Debug("loading sections...");
         SectionsResponse allSections = await _client.GetNotebookSectionsAsync(notebook.Id);
         if(_settings.SectionName == null || allSections.Sections.FirstOrDefault(n => n.Name == _settings.SectionName) == null)
         {
            Log.Debug("select section");
            int i = 0;
            foreach(Section section in allSections.Sections)
            {
               Log.Debug("{index}. {name}", ++i, section.Name);
            }
            int idx = AskNumber();
            Section selected = allSections.Sections[idx - 1];
            _settings.SectionName = selected.Name;
            Log.Debug("selected {name}", selected.Name);
            return selected;

         }

         Section cached = allSections.Sections.First(s => s.Name == _settings.SectionName);
         Log.Debug("selected {name}", cached.Name);
         return cached;
      }
      
      private async Task<Page> SelectPageAsync(Section section)
      {
         Log.Debug("fetching pages...");
         PagesResponse pages = await _client.GetSectionPagesAsync(section.Id);
         Log.Debug("select page:");

         List<Page> pageList = pages.Pages.OrderBy(p => p.CreatedTime).ToList();
         int i = 0;
         foreach(Page page in pageList)
         {
            Log.Debug("{i}. {title}", ++i, page.Title);
         }
         int idx = AskNumber();
         Page result = pageList[idx - 1];
         Log.Debug("selected {title}", result.Title);
         return result;
      }

      private int AskNumber()
      {
         string s = Console.ReadLine();
         return int.Parse(s);
      }
   }
}
