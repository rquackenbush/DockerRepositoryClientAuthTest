using System;

namespace JwtParser
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
  
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            TestAsync().GetAwaiter().GetResult();
        }

        private static async Task TestAsync()
        {
            const string uri = "https://registry-1.docker.io/v2/microsoft/dotnet/tags/list";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(uri);

                Console.WriteLine(response.StatusCode);

                //Check to see if it worked (it shouldn't have)
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine(response.Headers.WwwAuthenticate);

                    //Get the bearer authenticate header
                    var bearer = response.Headers.WwwAuthenticate.FirstOrDefault(h => h.Scheme == "Bearer");

                    if (bearer != null)
                    {
                        //parse it
                        var bearerBits = AuthenticateParser.ParseTyped(bearer.Parameter);

                        var oauthClient = new OAuthClient();

                        //Get the auth token
                        var token = await oauthClient.GetTokenAsync(bearerBits.Realm, bearerBits.Service,
                            bearerBits.Scope);

                        DisplayToken(token.Token);

                        var request = new HttpRequestMessage(HttpMethod.Get, uri);

                        //Specify the header token
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

                        var response2 = await httpClient.SendAsync(request);

                        if (response2.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            Console.WriteLine("This is bullshit.");
                        }
                        else
                        {
                            Console.WriteLine("Woot!");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Display for debug purposes
        /// </summary>
        /// <param name="token"></param>
        private static void DisplayToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var parsedToken = tokenHandler.ReadJwtToken(token);

            JObject jtoken = JObject.Parse(parsedToken.Payload.SerializeToJson());

            Console.WriteLine(jtoken.ToString(Formatting.Indented));

            Console.WriteLine(parsedToken.ToString());
        }
    }

 

}
