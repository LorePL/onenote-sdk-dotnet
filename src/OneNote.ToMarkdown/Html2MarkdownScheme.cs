using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Html2Markdown.Replacement;
using Html2Markdown.Scheme;
using HtmlAgilityPack;

namespace Aloneguid.OneNote.ToMarkdown
{
   class Html2MarkdownScheme : IScheme
   {
      private readonly Markdown _builtIn = new Markdown();
      private readonly List<IReplacer> _replacers;
      private const string CodeBlockMarker = "```";

      public Html2MarkdownScheme(string pageTitle)
      {
         _replacers = new List<IReplacer>();

         //pre-processing
         _replacers.Add(new OneNoteHapReplacer(true));

         _replacers.AddRange(_builtIn.Replacers());

         //OneNote block decoration
         _replacers.Add(new PatternReplacer("<div\\s+style\\s*=\\s*\"position:absolute(.+?)>", ""));
         _replacers.Add(new PatternReplacer("</div>", ""));

         //post-processing
         _replacers.Add(new OneNoteHapReplacer(false));

         //remove odd whitespaces
         _replacers.Add(new WhitespaceRemover());

         //format for blog
         _replacers.Add(new BlogFormatter(pageTitle));
      }

      public IList<IReplacer> Replacers() => _replacers;

      internal class OneNoteHapReplacer : IReplacer
      {
         private readonly bool _pre;

         public OneNoteHapReplacer(bool pre)
         {
            _pre = pre;
         }

         public string Replace(string html)
         {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            if (_pre)
            {
               ProcessImageAlts(doc);
               ProcessCodeBlocks(doc);
            }
            else
            {
               ProcessFontStyles(doc);
               ProcessTables(doc);
            }

            return doc.DocumentNode.OuterHtml;
         }

         private void ProcessCodeBlocks(HtmlDocument doc)
         {
            while (ProcessCodeBlock(doc)) { }
         }

         private bool ProcessCodeBlock(HtmlDocument doc)
         {
            HtmlNode codeBlockStart = doc.DocumentNode.SelectSingleNode("//p[@style='font-family:Consolas;margin-top:0pt;margin-bottom:0pt']");
            if (codeBlockStart == null || !codeBlockStart.InnerText.StartsWith(CodeBlockMarker)) return false;

            var codeNodes = new List<HtmlNode>();  //list of nodes that compose code block originally
            var sb = new StringBuilder();          //cleaned out code block

            //find code text and nodes
            for(HtmlNode codeLine = codeBlockStart; ; codeLine = codeLine.NextSibling)
            {
               codeNodes.Add(codeLine);

               if (codeLine.Name != "p") continue;

               sb.AppendLine(codeLine.InnerText);

               if (codeLine != codeBlockStart && codeLine.InnerText.StartsWith(CodeBlockMarker))
               {
                  break;
               }
            }

            //replace first node with formatted code
            HtmlNode code = doc.CreateElement("pre");
            code.InnerHtml = sb.ToString();
            codeBlockStart.ParentNode.ReplaceChild(code, codeBlockStart);


            //clean up the rest
            foreach(HtmlNode leftover in codeNodes.Skip(1))
            {
               leftover.Remove();
            }

            return true;
         }

         private void ProcessFontStyles(HtmlDocument doc)
         {
            HtmlNodeCollection fontStyles = doc.DocumentNode.SelectNodes("//span[@style]");
            if (fontStyles == null) return;
            foreach (HtmlNode node in fontStyles)
            {
               string style = node.GetAttributeValue("style", null);
               if (style == null) continue;

               string[] styles = style.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
               var decorations = new List<string>();
               if (styles.Contains("font-style:italic")) decorations.Add("_");
               if (styles.Contains("font-weight:bold")) decorations.Add("**");
               if (styles.Contains("font-decoration:line-through")) decorations.Add("~~");
               // there's no underline in markdown? ignore it for now

               string replacement = Decorate(node.InnerHtml, decorations);

               node.ParentNode.ReplaceChild(doc.CreateTextNode(replacement), node);
            }
         }

         private void ProcessImageAlts(HtmlDocument doc)
         {
            HtmlNodeCollection images = doc.DocumentNode.SelectNodes("//img");
            if (images == null) return;

            foreach(HtmlNode image in images)
            {
               string alt = image.GetAttributeValue("alt", null);
               if (alt == null) continue;

               //remove weird OCR characters from alts

               string newAlt = string.Empty;
               foreach(char ch in alt)
               {
                  if(char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
                  {
                     newAlt += char.IsWhiteSpace(ch) ? ' ' : ch;
                  }
               }
               image.SetAttributeValue("alt", newAlt);

            }
         }

         private void ProcessTables(HtmlDocument doc)
         {
            HtmlNodeCollection tables = doc.DocumentNode.SelectNodes("table");
            if (tables == null) return;
            foreach(HtmlNode table in tables)
            {
               var s = new StringBuilder();
               bool isHeader = true;

               //there are text nodes in children, they are just line breaks and safe to ignore
               foreach(HtmlNode row in table.ChildNodes.Where(n => n.Name == "tr"))
               {
                  int cellCount = 0;
                  s.Append("|");
                  foreach(HtmlNode cell in row.ChildNodes.Where(n => n.Name == "td"))
                  {
                     s.Append(cell.InnerText.Trim());
                     s.Append("|");
                     cellCount++;
                  }
                  s.AppendLine();

                  if(isHeader)
                  {
                     s.Append("|");
                     for(int i = 0; i < cellCount; i++)
                     {
                        s.Append("-|");
                     }
                     s.AppendLine();
                     isHeader = false;
                  }
               }

               table.ParentNode.ReplaceChild(doc.CreateTextNode(s.ToString()), table);
            }
         }

         private string Decorate(string text, IReadOnlyCollection<string> decorations)
         {
            foreach(string dec in decorations)
            {
               text = dec + text + dec;
            }

            return text;
         }
      }

      internal class PatternReplacer : IReplacer
      {
         public PatternReplacer(string pattern, string replacement)
         {
            Pattern = pattern;
            Replacement = replacement;
         }

         public string Pattern { get; }

         public string Replacement { get; }

         public string Replace(string html)
         {
            return new Regex(Pattern).Replace(html, Replacement);
         }
      }

      internal class WhitespaceRemover : IReplacer
      {
         public string Replace(string html)
         {
            var sb = new StringBuilder();

            using (var sr = new StringReader(html))
            {
               string s;
               while((s = sr.ReadLine()) != null)
               {
                  if (s.StartsWith(CodeBlockMarker))
                  {
                     ReplaceCodeBlock(sr, sb, s);
                  }
                  else
                  {
                     s = s.Trim();
                     sb.AppendLine(s);
                  }
               }
            }

            return sb.ToString();
         }

         private void ReplaceCodeBlock(StringReader sr, StringBuilder output, string current)
         {
            var blockLines = new List<string>();

            //read all block lines
            blockLines.Add(current);
            while((current = sr.ReadLine()) != null)
            {
               if(current.Trim().StartsWith(CodeBlockMarker))
               {
                  blockLines.Add(current.Trim());
                  break;
               }

               blockLines.Add(current);
            }

            //find minimum spacing
            int spaceMin = blockLines.Skip(1).Take(blockLines.Count - 2).Min(l => l.Length - l.TrimStart().Length);

            //remove minimum spacing from each line
            for(int i = 1; i < blockLines.Count - 1; i++)
            {
               blockLines[i] = blockLines[i].Substring(spaceMin);
            }

            //add to result
            foreach(string line in blockLines)
            {
               output.AppendLine(line);
            }
         }
      }

      internal class BlogFormatter : IReplacer
      {
         private readonly string _title;

         public BlogFormatter(string title)
         {
            _title = title;
         }

         public string Replace(string html)
         {
            html = html
               .Replace("####", "#####")
               .Replace("###", "####")
               .Replace("##", "###")
               .Replace("#", "##");

            var sb = new StringBuilder();
            sb.Append("# ");
            sb.AppendLine(_title);
            sb.AppendLine();

            sb.Append(html);

            return sb.ToString();
         }
      }
   }
}
