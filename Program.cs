using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Flurl.Http;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using System.Data.SqlClient;
using static System.Console;


namespace graphAPI
{
    class Program
    {
        //Azure App regestration info here
        private static readonly string _clientID = "YOUR_CLIENT_ID";
        private static readonly string _tenantID ="YOUR_TENANT_ID";
        private static readonly string _clientSecret ="YOUR_CLIENT_SECRET";

        public static async Task Main(string[] args)
        {
            Console.WriteLine("----- Hello MSAL! -----");
            Console.WriteLine("--------------------------------");

            //Get Access token first via CC
            // string accessToken = await GetAccessTokenViaClientCredentialsFlow();
            
            //Test device token using device flow 
            //await GetUserProfileInfo();


            //Using the Graph SDK
            await GetUserProfileInfoViaGraphSDK();

            //Test device flow using device code flow
            //await GetAccessToken_DeviceFlow();

            //Get data from database using access token
            //await AzureDatabaseAPI();

            Console.WriteLine("------------- The END! --------------");
        }

        private static async Task<string> GetAccessTokenViaClientCredentialsFlow()
        {
            /*
            - We need client secret and API permission for application level
            - tenantID also if we want this app for local access
            */

            var app = ConfidentialClientApplicationBuilder.Create(_clientID)
                        .WithAuthority(AzureCloudInstance.AzurePublic, _tenantID)
                        .WithRedirectUri("http://localhost:1234")
                        .WithClientSecret(_clientSecret)
                        .Build();
            
            //we need to provide the full URI for the scope instead of just User.Read
            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };
            
            //Get Token
            var result = await app.AcquireTokenForClient(scopes)
                            .ExecuteAsync();
            
            //Console.WriteLine(result.AccessToken);
            return result.AccessToken;
        }
        
        //
        private static async Task GetUserProfileInfo()
        {
            /*
                This block is used to get the access token which will allows us to authenticate and 
                have the needed authorization to request the needed resource from Graph API later
            */

            
            var app = PublicClientApplicationBuilder.Create(_clientID)
                .WithRedirectUri("http://localhost:1234")
                .Build();

            string[] scopes = new string[] { "User.Read" };
            
            var result = await app.AcquireTokenInteractive(scopes)
                .ExecuteAsync();
                
            var token = result.AccessToken;
            

            //This block will query graphAPI
            //using Flurl

            string graphAPICall = await "https://graph.microsoft.com/v1.0/users/"
                .WithOAuthBearerToken(token)
                .GetStringAsync();

            Console.WriteLine(graphAPICall);

            // Console.WriteLine("------------------------------------");
            // string ondriveGraphCall = await "https://graph.microsoft.com/beta/drive"
            //     .WithOAuthBearerToken(token)
            //     .GetStringAsync();
            
            // Console.WriteLine(ondriveGraphCall);

            // Console.WriteLine("------------------------------------");
            // string filesoneDriveGraphCall = await "https://graph.microsoft.com/beta/drive/root/children"
            //     .WithOAuthBearerToken(token)
            //     .GetStringAsync();
            
            // Console.WriteLine(filesoneDriveGraphCall);
        }

        //Get profile info using SDK
        private static async Task GetUserProfileInfoViaGraphSDK()
        {
            //This demo is using Graph beta SDK
            /*
                This block is used to get the access token which will allows us to authenticate and 
                have the needed authorization to request the needed resource from Graph API later
            */
    
            var app = PublicClientApplicationBuilder.Create(_clientID)
                .WithRedirectUri("http://localhost:1234")
                .Build();

            string[] scopes = new string[] { "User.Read.All" };

            //Using Graph Auth lib
            //this line will get our access token
            var provider = new InteractiveAuthenticationProvider(app, scopes);
            
            var client = new GraphServiceClient(provider);

            var result = await client.Me.Request().GetAsync();

            Console.WriteLine($"\nWelcome |'{result.DisplayName}'|");
            Console.WriteLine("___________________________________");
            Console.WriteLine("You AzreAD Info:");
            Console.WriteLine("*******************\n");
            Console.WriteLine($"Mobile Phone:\t{result.MobilePhone}");
            Console.WriteLine($"Email Address:\t{result.UserPrincipalName}");
            Console.WriteLine($"Job:\t\t{result.JobTitle}");
            Console.WriteLine($"Department:\t{result.Department ?? "No Data Found"}");
            Console.WriteLine($"Company Name:\t{result.CompanyName ?? "No Data Found"}");
            Console.WriteLine($"Country:\t{result.Country ?? "No Data Found"}");
            Console.WriteLine($"City:\t\t{result.City ?? "No Data Found"}");
            Console.WriteLine($"ID:\t\t{result.Id}");
            Console.WriteLine("___________________________________\n");
        }

        /***
        Device Code Flow example
        ----------------------------------------
        ***/
        private static async Task GetAccessToken_DeviceFlow()

        {
            var app = PublicClientApplicationBuilder.Create(_clientID)
            .WithRedirectUri("http://localhost:1234")
            .Build();
            
            string[] scopes = new string[] { "user.read" };

            var result = await app
                            .AcquireTokenWithDeviceCode(scopes, HandlePrompt)
                            .ExecuteAsync();

            Console.WriteLine(result.AccessToken);
        }
        private static async Task HandlePrompt(DeviceCodeResult prompt)
        {
            await Console.Out.WriteLineAsync(prompt.Message);
        }

        //using Azure SQL scopes to get access token
        private static async Task AzureDatabaseAPI()
        {
            WriteLine("\n * ------ |Access Azure Database| ------- *");
             /*
                This block is used to get the access token which will allows us to authenticate and 
                have the needed authorization to request the needed resource from Graph API later
            */
            var app = PublicClientApplicationBuilder.Create(_clientID)
                .WithRedirectUri("http://localhost:1234")
                .Build();
            
            string[] scopes = new string[] { "https://database.windows.net/.default" };

            var result = await app.AcquireTokenInteractive(scopes)
                                    .ExecuteAsync();
            
            string connectionString = "YOUR_CONNECTION_STRING";

            using SqlConnection connection = new (connectionString);
            connection.AccessToken = result.AccessToken;
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM dbo.Person";
            // using the as keyworkd to get the result as nullable int
            var count = await command.ExecuteScalarAsync() as int?; 

            WriteLine($"Count:\t{count ?? 0}");
        }
    }
}
