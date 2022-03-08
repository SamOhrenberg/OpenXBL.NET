using OpenXBL.Resources.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenXBL
{
    /// <summary>
    /// Provides HTTP communications
    /// </summary>
    public class HttpService
    {
        /// <summary>
        /// Static Members in the HttpService are used to have a singleton webclient - no need for a bunch of different webclients as far as I am aare
        /// </summary>
        #region Static Members

        private static readonly HttpClient _webClient = new HttpClient();

        #endregion

        public enum SupportedMethods
        {
            GET
        }

        private string _apiKey;
        private string _baseUrl;
        private Dictionary<string, string> _headers = new Dictionary<string, string>();

        public HttpService(string apiKey, string baseUrl)
        {
            _apiKey = apiKey;
            _baseUrl = baseUrl;
        }

        internal void AddHeader(string key, string val)
        {
            _headers.Add(key, val);
        }

        internal Task<T> GetAsync<T>(string endpoint)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// The main client class for interacting with the OpenXBL API. The goal is for this thing to abstract away the actual trasmittal portion of the Api so you can just enjoy the objects it grants access to!
    /// </summary>
    public class LiveClient
    {
        /// <summary>
        /// HTTP Service that handles web requests for the API
        /// </summary>
        protected HttpService _http;

        /// <summary>
        /// A task container holding the process that is pulling the ApiKey-holder's default gamer profile information.
        /// </summary>
        /// <remarks>
        /// Virtual for your overriding pleasure!
        /// </remarks>
        public virtual Task<GamerProfile>? CurrentAsync { get; private set; }

        /// <summary>
        /// The current GamerProfile for the holder of the api key. Will be null if <a cref="LiveClient(string, bool)">beginGamerProfilePull</a> was left false in the constructor
        /// <br/><i><b>Warning:</b> if you set <a cref="LiveClient(string, bool)">beginGamerProfilePull</a> = true in the constructor, this might throw an exception when you access it.</i>
        /// <br/><i>Otherwise, feel feel to have fun!</i>
        /// </summary>
        /// <remarks>
        /// Virtual for your overriding pleasure!
        /// </remarks>
        public virtual GamerProfile? Current => CurrentAsync?.Result ?? null;

        /// <summary>
        /// Constructs a liveclient that can be used to access the OpenXBL api and initiates a procedure to pull the current user's gamer profile. 
        /// Ideally this shouldn't take long, so by the time the user calls it it may already be pulled
        /// </summary>
        /// <param name="apiKey">
        /// A valid api key that will be used to access OpenXBL. This will either be a personal token that you have created at https://xbl.io/profile or a token you've generated on behalf of a user via an app at https://xbl.io/apps. 
        /// </param>
        /// <param name="beginGamerProfilePull">
        /// Optional configuration to start a background thread that will begin to load the gamer profile. This can be beneficial if you're wanting to get the gamerprofile information available as soon as possible. 
        /// This makes it to where, as long as an error didn't occur, you can call <a cref="Current" /> to get the GamerProfile
        /// </param>
        public LiveClient(string apiKey, bool beginGamerProfilePull = false)
        {
            _http = new HttpService(apiKey, XboxLiveApi.BaseUrl);
            _http.AddHeader(XboxLiveApi.AcceptHeader, XboxLiveApi.AcceptJson);
            _http.AddHeader(XboxLiveApi.ContractHeader, XboxLiveApi.ContractHeaderValue);
            _http.AddHeader(XboxLiveApi.AuthHeader, apiKey);

            // if the caller requested the auto pull, then begin
            if (!beginGamerProfilePull) CurrentAsync = GetDefaultGamerProfile();

        }

        /// <summary>
        /// Internal method used to standardize calling the OpenXBL api through the HttpService. Utilizes the OpenXBLEndpoint attribute to parse the value of the endpoint value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="InvalidOperationException">Thrown if typeparam T does not have the OpenXBLEndpoint attribute used to identify the endpoint used in OpenXBL</exception>
        /// <returns>The return value from the http service api</returns>
        protected virtual async Task<T> RequestAsync<T>(HttpService.SupportedMethods method) where T : OpenXBLDto
        {
            // get the endpoint attribute so we can find the correct xbl endpoint to use
            var endpointAttribute = (OpenXBLEndpointAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(OpenXBLEndpointAttribute));

            if (endpointAttribute == null) throw new InvalidOperationException(Errors.OpenXBLEndpointAttributeMissing);

            // match up the given method with the appropriate method off of the HttpService 
            Task<T> puller = method switch
            {
                HttpService.SupportedMethods.GET => _http.GetAsync<T>(endpointAttribute.Endpoint),

                // TODO: IMPLEMENT PUTPOST HttpService.SupportedMethods.PUTPOST => _http.PutPostAsync<T>(endpointAttribute.Endpoint, object? bodyObject) (to be parsed into by the method, I want that to be a responsibility of the HTTP class)
                //          I KNOW THEY ARENT THE SAME THING BUT AS FAR AS I CAN TELL IT DOESNT MATTER FOR THE API DOCS
                // TODO: IMPLEMENT DELETE (IF IT IS EVEN USED?!)

                _ => throw new MissingMethodException(),
            };

            // pull the value off of the endpoint
            return await puller;
        }


        /// <summary>
        /// Returns basic profile information about the person providing the Api Key 
        /// </summary>
        /// <returns>Basic gamer profile information</returns>
        protected virtual async Task<GamerProfile> GetDefaultGamerProfile()
        {
            // get the data from the openxbl api, we will need to transform it
            var profileUsersDtos = await RequestAsync<ProfileUsersDto>(HttpService.SupportedMethods.GET);

            return GamerProfileFactory.CopyFromProfileUsers(profileUsersDtos);
        }

    }

    public static class GamerProfileFactory
    {
        public static GamerProfile CopyFromProfileUsers(ProfileUsersDto profileUsersDtos)
        {
            throw new NotImplementedException();
        }
    }

    [OpenXBLEndpoint(Endpoint)]
    public class ProfileUsersDto : OpenXBLDto
    {
        internal const string Endpoint = "/account";
    }

    public class OpenXBLDto
    {
    }

    internal class OpenXBLEndpointAttribute : Attribute
    {
        public string Endpoint { get; private set; }

        public OpenXBLEndpointAttribute(string endpoint)
        {
            Endpoint = endpoint;
        }
    }

    public class GamerProfile
    {
        public string Gamertag { get; set; }
        public int Gamerscore { get; set; }
        public AccountTier AccountTier { get; set; }
        public AccountReputation Reputation { get; set; }
        public string GamerPictureUrl { get; private set; }
        public string RealName { get; set; }
        public string Bio { get; set; }
        public string Location { get; set; }

        public ReadOnlyCollection<PlayedGame> PlayedGames => GetPlayedGames();

        private ReadOnlyCollection<PlayedGame> GetPlayedGames()
        {
            throw new NotImplementedException();
        }
    }

    public class PlayedGame
    {
        public string Name { get; private set; }
    }

    public enum AccountReputation
    {
    }

    public enum AccountTier
    {
        Gold
    }

}
