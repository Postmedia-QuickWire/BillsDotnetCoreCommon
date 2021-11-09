using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Security.Principal;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Collections.Concurrent;


/*
 * bmiller, I implemented a simple(er) token authentication myself.
 * In this way we would manually create the tokens (API Keys) and issue them to users
 * the keys would be in the appsettings.json or a DB
 * each key would be tied to an AD user account
 * 
 * I've never used this, not even sure I've actually even tested it.
 * see the JwtTokenServiceBase class for a better token implementation
 */

namespace Common.Classes
{

	public class ApiKeyAuthOptions : AuthenticationSchemeOptions
	{
		public const string AuthenticationScheme = "ApiKeyAuth";
		public const string DefaultScheme = "ApiKeyAuth";

		public const string HeaderName = "X-Auth-Token";
		public const string CookieName = "X-Auth-Token";
		public const string UrlParamName = "apikey";
	}

	public static class AuthenticationBuilderExtensions
	{
		public static AuthenticationBuilder AddApiKeySupport(this AuthenticationBuilder authenticationBuilder, Action<ApiKeyAuthOptions> options)
		{
			return authenticationBuilder.AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>(ApiKeyAuthOptions.DefaultScheme, options);
		}
	}

	public class ApiUser
	{
		// account ID link
		public int Id { get; set; }  // as client type NameIdentifier

		// for logging
		public string Name { get; set; }

		// actual controller roles (can be comma separated, no spaces)
		public string Role { get; set; }
	}

	public interface IApiKeyService
	{
		public ApiUser FetchApiUser(string apikey);

		public void ReLoadApiKeys();

		public string GenerateApiKey();

		public string HashApiKey(string apikey);
	}

	public abstract class ApiKeyServiceBase : IApiKeyService
	{
		protected Dictionary<string, ApiUser> _ApiKeys;
		private object _ApiKeysLock = new object();
		public ApiKeyServiceBase() 
		{
			_ApiKeys = new Dictionary<string, ApiUser>();
		}
		// fetch the APiUser based on an api-key token
		public virtual ApiUser FetchApiUser(string apikey)
		{
			ApiUser user;
			var hashed_apikey = HashApiKey(apikey);
			lock (_ApiKeysLock)
			{
				if (_ApiKeys.TryGetValue(hashed_apikey, out user))
					return user;
			}
			return null;
		}

		// replace ALL api keys
		public virtual void ReplaceApiKeys(Dictionary<string, ApiUser> apikeys)
		{
			lock (_ApiKeysLock)
			{
				_ApiKeys = apikeys;
			}
		}

		public abstract void ReLoadApiKeys();
		public abstract string GenerateApiKey();
		public abstract string HashApiKey(string apikey);

	}


	public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthOptions>
	{
		public IServiceProvider ServiceProvider { get; set; }

		private readonly ILogger<ApiKeyAuthHandler> _logger;
		private readonly IApiKeyService _ApiKeyService;

		public ApiKeyAuthHandler(IOptionsMonitor<ApiKeyAuthOptions> options
			, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock
			, IServiceProvider serviceProvider
			, IApiKeyService ApiKeyService
			)
				: base(options, logger, encoder, clock)
		{
			ServiceProvider = serviceProvider;
			_logger = logger.CreateLogger<ApiKeyAuthHandler>();
			_ApiKeyService = ApiKeyService;
		}

		// can't seem to return actual content 

		protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
		{
			//Response.WriteAsync(failedReason);
			//properties.RedirectUri = "/home/noapikey";
			await base.HandleChallengeAsync(properties);
		}

		protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
		{
			await base.HandleForbiddenAsync(properties);
		}


		public static string FetchToken(HttpRequest request)
		{
			var token = "";
			if (request.Headers.ContainsKey(ApiKeyAuthOptions.HeaderName))
				token = request.Headers[ApiKeyAuthOptions.HeaderName];
			//else if (Request.Cookies.ContainsKey(TokenAuthenticationOptions.CookieName)) NO!
			//	token = Request.Cookies[TokenAuthenticationOptions.CookieName];
			else if (request.Query.ContainsKey(ApiKeyAuthOptions.UrlParamName))
				token = request.Query[ApiKeyAuthOptions.UrlParamName];
			return token;
		}

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			string path = OriginalPath.ToString().ToLower();
			if (!path.Contains("/api/"))
				return Task.FromResult(AuthenticateResult.NoResult());

			var token = FetchToken(Request);
			if (string.IsNullOrEmpty(token))
			{
				//return AuthenticateResult.Fail("no token");
				return Task.FromResult(AuthenticateResult.NoResult());
			}

			// check for the user session

			ApiUser user = _ApiKeyService.FetchApiUser(token);
			if (user == null)
			{
				return Task.FromResult(AuthenticateResult.Fail("no creds found for token"));
			}

			// the only claim required is Role, maybe Name

			var claims = new List<Claim> { new Claim("token", token), new Claim(ClaimTypes.Name, user.Name), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) };
			foreach (var role in user.Role.Split(new char[] { ',' }))
				claims.Add(new Claim(ClaimTypes.Role, role));

			var identity = new ClaimsIdentity(claims, nameof(ApiKeyAuthHandler));
			var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), this.Scheme.Name);
			return Task.FromResult(AuthenticateResult.Success(ticket));
		}
	}


}
