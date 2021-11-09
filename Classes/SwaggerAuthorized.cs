using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Common.Classes
{

	// need to lock down the swagger controller
	//https://github.com/domaindrivendev/Swashbuckle/issues/384
	public static class SwaggerAuthorizeExtensions
	{
		public static IApplicationBuilder UseSwaggerAuthorized(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<SwaggerAuthorizedMiddleware>();
		}
	}
	public class SwaggerAuthorizedMiddleware
	{
		private readonly RequestDelegate _next;

		public SwaggerAuthorizedMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task Invoke(HttpContext context)
		{
			if (context.Request.Path.StartsWithSegments("/swagger"))
			{
				context.Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
				context.Response.Headers[HeaderNames.Expires] = "0";
				context.Response.Headers[HeaderNames.Pragma] = "no-cache";
				if (!context.User.Identity.IsAuthenticated)
				{
					context.Response.StatusCode = StatusCodes.Status401Unauthorized;
					return;
				}
			}

			await _next.Invoke(context);
		}
	}
}
