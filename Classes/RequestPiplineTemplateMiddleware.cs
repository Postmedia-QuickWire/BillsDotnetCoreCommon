using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Classes
{
	/// <summary>
	/// a template for a request pipeline object
	/// call app.UseRequestPiplineTemplate() from Configure in startup
	/// </summary>
	public static class RequestPiplineTemplateExtensions
	{
		public static IApplicationBuilder UseRequestPiplineTemplate(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<RequestPiplineTemplateMiddleware>();
		}
	}
	public class RequestPiplineTemplateMiddleware
	{
		private readonly RequestDelegate _next;

		public RequestPiplineTemplateMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task Invoke(HttpContext context)
		{
			// do something here, like deny Bob
			if (context.Request.Path.StartsWithSegments("/bob"))
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				return;
			}

			await _next.Invoke(context);
		}
	}
}
