﻿using System;
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
         _baseDir = Path.Combine(settings.RootDir, Guid.NewGuid().ToString());
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

            markdown = markdown.Replace(Page.MakeFullResourceId(rid), localName);
         }
         File.WriteAllText(Path.Combine(_baseDir, "post.md"), markdown);
      }

      private async Task ConvertImages()
      {
         bool hasTitle = false;
         foreach (KeyValuePair<string, string> resPair in _resourceLocalToRemoteId)
         {
            Log.Debug("downloading {name}...", resPair.Value);
            using (Stream source = await _client.DownloadResource(resPair.Value))
            {
               using (Stream dest = File.Create(Path.Combine(_baseDir, resPair.Key)))
               {
                  Log.Debug("saving to {name}...", resPair.Key);
                  source.CopyTo(dest);

               }
            }

            if (!hasTitle)
            {
               SaveTitle(resPair.Key);
               hasTitle = true;
            }

         }
      }

      private void SaveTitle(string name)
      {
         ISupportedImageFormat format = new JpegFormat { Quality = 70 };
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