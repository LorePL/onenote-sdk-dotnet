using Aloneguid.OneNote.Sdk.ADAL;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Refit;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aloneguid.OneNote.Sdk
{
   public static class ClientFactory
   {
      //https://www.onenote.com/api/
      
      public static IOneNoteClient CreateClient(Func<Task<string>> authValueGetter)
      {
         var http = new HttpClient(new AuthenticatedHttpClientHandler(authValueGetter))
         {
            BaseAddress = new Uri("https://www.onenote.com/api")
         };

         return RestService.For<IOneNoteClient>(http);
      }

      private class AuthenticatedHttpClientHandler : HttpClientHandler
      {
         private readonly Func<Task<string>> _getToken;

         public AuthenticatedHttpClientHandler(Func<Task<string>> getToken)
         {
            _getToken = getToken ?? throw new ArgumentNullException("getToken");
         }

         protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
         {
            // See if the request has an authorize header
            AuthenticationHeaderValue auth = request.Headers.Authorization;
            if (auth != null)
            {
               string token = await _getToken().ConfigureAwait(false);
               request.Headers.Authorization = new AuthenticationHeaderValue(auth.Scheme, token);
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
         }

      }
   }
}
