using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace Common.Classes { 

	public interface IViewRender
	{
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <param name="name"></param>
		/// <param name="model"></param>
		/// <returns></returns>
		Task<string> RenderPartialViewToStringAsync<TModel>(string name, TModel model);
	}

	public class ViewRender : IViewRender
	{
		private IRazorViewEngine _viewEngine;
		private ITempDataProvider _tempDataProvider;
		private IServiceProvider _serviceProvider;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="viewEngine"></param>
		/// <param name="tempDataProvider"></param>
		/// <param name="serviceProvider"></param>
		public ViewRender(
			IRazorViewEngine viewEngine,
			ITempDataProvider tempDataProvider,
			IServiceProvider serviceProvider)
		{
			_viewEngine = viewEngine;
			_tempDataProvider = tempDataProvider;
			_serviceProvider = serviceProvider;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <param name="name"></param>
		/// <param name="model"></param>
		/// <returns></returns>
		public async Task<string> RenderPartialViewToStringAsync<TModel>(string name, TModel model)
		{
			var actionContext = GetActionContext();

			var viewEngineResult = _viewEngine.FindView(actionContext, name, false);

			if (!viewEngineResult.Success)
			{
				throw new InvalidOperationException(string.Format("Couldn't find view '{0}'", name));
			}

			var view = viewEngineResult.View;

			using (var output = new StringWriter())
			{
				var viewContext = new ViewContext(
					actionContext,
					view,
					new ViewDataDictionary<TModel>(
						metadataProvider: new EmptyModelMetadataProvider(),
						modelState: new ModelStateDictionary())
					{
						Model = model
					},
					new TempDataDictionary(
						actionContext.HttpContext,
						_tempDataProvider),
					output,
					new HtmlHelperOptions());

				await view.RenderAsync(viewContext); //.GetAwaiter().GetResult();

				return output.ToString();
			}
		}

		private ActionContext GetActionContext()
		{
			var httpContext = new DefaultHttpContext
			{
				RequestServices = _serviceProvider
			};
			return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
		}
	}
}
