using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aloneguid.OneNote.Sdk;
using Html2Markdown;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using Serilog;

namespace Aloneguid.OneNote.ToMarkdown
{
   class DiskConverter
   {
      private readonly IOneNoteClient _client;
      private readonly Page _page;
      private readonly ISettings _settings;
      private readonly Dictionary<string, string> _resourceLocalToRemoteId = new Dictionary<string, string>();
      private readonly string _baseDir;

      public DiskConverter(IOneNoteClient client, Page page, ISettings settings)
      {
         _client = client;
         _page = page;
         _settings = settings;
         DateTime date = DateTime.UtcNow;
         _baseDir = Path.Combine(settings.RootDir, date.Year.ToString(), $"{date.Month,2:D2}", $"{date.Day,2:D2}");
         if (!Directory.Exists(_baseDir)) Directory.CreateDirectory(_baseDir);
      }

      public async Task<string> ConvertAsync()
      {
         await ConvertBasics();

         await ConvertImages();

         return _baseDir;
      }

      private async Task ConvertBasics()
      {
         Log.Debug("downloading html");
         string html = await _client.GetPageHtmlContent(_page.Id);
         File.WriteAllText(Path.Combine(_baseDir, "post.html"), html);

         Log.Debug("converting");
         var converter = new Converter(new Html2MarkdownScheme(_page.Title));
         string markdown = converter.Convert(html);

         //extract resources
         Log.Debug("fixing resources");
         _resourceLocalToRemoteId.Clear();
         string[] resources = Page.ExtractResourceIds(markdown);
         int resourceIndex = 0;

         foreach (string rid in resources)
         {
            string localName = $"{resourceIndex++.ToString().PadLeft(3, '0')}.png";
            _resourceLocalToRemoteId[localName] = rid;

            Log.Debug("{rid} => {local}", rid, localName);

            markdown = markdown.Replace(Page.MakeFullResourceId(rid), localName);
         }
         markdown = markdown.RemoveLinesContaining("000.png", StringComparison.OrdinalIgnoreCase);

         Log.Debug("resources fixed in markdown");

         File.WriteAllText(Path.Combine(_baseDir, "post.md"), markdown);
      }

      private async Task ConvertImages()
      {
         bool isTitle = true;
         foreach (KeyValuePair<string, string> resPair in _resourceLocalToRemoteId)
         {
            string remoteId = resPair.Value;
            string localName = resPair.Key;
            Log.Debug("downloading {name}...", remoteId);
            using (Stream source = await _client.DownloadResource(remoteId))
            {
               SaveImage(source, localName, isTitle);
               isTitle = false;
            }
         }
      }

      private void SaveImage(Stream source, string localName, bool isTitle)
      {
         Log.Debug("saving {name}", localName);

         //save the original
         string destName = Path.Combine(_baseDir, localName);
         using (Stream dest = File.Create(destName))
         {
            source.CopyTo(dest);
         }


         if(isTitle)
         {
            Log.Debug("saving as title");
            Recompress(localName, "title.jpg", _settings.TitleWidth, _settings.TitleHeight, _settings.TitleQuality);
            File.Delete(Path.Combine(_baseDir, localName));
         }
         else
         {
            //re-compress optimal version

            //leave originals for now

            /*string viewName = Path.ChangeExtension(destName, "view.jpg");
            Log.Debug("compressing to {name}", viewName);
            Recompress(localName, viewName, _settings.ImageWidth, _settings.ImageHeight, _settings.ImageQuality);

            File.Move(destName, Path.ChangeExtension(destName, ".o.jpg"));
            File.Move(viewName, destName);*/

         }
      }

      private void Recompress(string sourceName, string destName, int width, int height, int quality)
      {
         ISupportedImageFormat format = new JpegFormat { Quality = quality };
         var size = new Size(width, height);
         var rl = new ResizeLayer(size, ResizeMode.Crop);

         using (FileStream src = File.OpenRead(Path.Combine(_baseDir, sourceName)))
         {
            string destPath = Path.Combine(_baseDir, destName);
            if (File.Exists(destPath)) File.Delete(destPath);
            using (FileStream dest = File.Create(destPath))
            {
               using (var factory = new ImageFactory())
               {
                  factory
                      .Load(src)
                      .Resize(rl)
                      .Format(format)
                      .Save(dest);
               }
            }
         }
      }

      private void SaveTitle(string name)
      {
         ISupportedImageFormat format = new JpegFormat { Quality = _settings.TitleQuality };
         var size = new Size(_settings.TitleWidth, _settings.TitleHeight);
         var rl = new ResizeLayer(size, ResizeMode.Crop);

         using (FileStream src = File.OpenRead(Path.Combine(_baseDir, name)))
         {
            using (FileStream dest = File.Create(Path.Combine(_baseDir, "title.jpg")))
            {
               using (var factory = new ImageFactory())
               {
                  factory
                      .Load(src)
                      .Resize(rl)
                      .Format(format)
                      .Save(dest);
               }
            }
         }
      }
   }
}
