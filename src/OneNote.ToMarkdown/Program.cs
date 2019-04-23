using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using OneNote.Net.ADAL;
using Config.Net;
using System.Threading.Tasks;
using OneNote.Net;
using Serilog;
using System.Diagnostics;

namespace Aloneguid.OneNote.ToMarkdown
{
   class Program
   {
      private const string AadInstance = "https://login.microsoftonline.com/{0}";
      private static AuthenticationContext _context;
      private static ISettings _settings;
      private static IOneNoteClient _client;


      static async Task Main(string[] args)
      {
         _settings = new ConfigurationBuilder<ISettings>()
            .UseEnvironmentVariables()
            .UseIniFile(@"c:\tmp\onenote.ini")
            .Build();

         string authority = string.Format(AadInstance, _settings.TenantId);

         _context = new AuthenticationContext(authority, new FileCache());
         _client = ClientFactory.CreateClient(GetToken);

         Log.Logger = new LoggerConfiguration()
            .WriteTo.ColoredConsole()
            .MinimumLevel.Debug()
            .CreateLogger();

         var selector = new PageSelector(_client, _settings);
         Page page = await selector.SelectPageAsync();

         Log.Information("type publishing date as yyyy/mm/dd or press enter to use current date");
         string dateInput = Console.ReadLine();
         DateTime date;
         if(string.IsNullOrEmpty(dateInput))
         {
            date = DateTime.UtcNow;
         }
         else
         {
            string[] parts = dateInput.Split('/');
            date = new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), 0, 0, 0, DateTimeKind.Utc);
         }
         Log.Information("using {year}/{month}/{day}", date.Year, date.Month, date.Day);

         var converter = new DiskConverter(_client, page, _settings, date);
         string dir = await converter.ConvertAsync();

         Process.Start("code", "\"" + dir + "\"");
      }

      private static async Task<string> GetToken()
      {
         AuthenticationResult authResult = await _context.AcquireTokenAsync(
            "https://onenote.com/",
            _settings.ClientId,
            new Uri(_settings.RedirectUri),
            new PlatformParameters(PromptBehavior.Auto));

         return authResult.AccessToken;
      }

   }
}
