using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Aloneguid.OneNote.Sdk.ADAL;
using Config.Net;
using System.Threading.Tasks;
using Aloneguid.OneNote.Sdk;
using Serilog;

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
