﻿using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using TASVideos.Controllers;

namespace TASVideos.Extensions
{
	public static class ApplicationBuilderExtensions
	{
		public static IApplicationBuilder UseExceptionHandlers(this IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsAnyTestEnvironment())
			{
				app.UseDeveloperExceptionPage();
				app.UseBrowserLink();
				app.UseDatabaseErrorPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
			}

			return app;
		}

		public static IApplicationBuilder UseGzipCompression(this IApplicationBuilder app, AppSettings settings)
		{
			if (settings.EnableGzipCompression)
			{
				app.UseResponseCompression();
			}

			return app;
		}

		public static IApplicationBuilder UseMvcWithOptions(this IApplicationBuilder app)
		{
			// Note: out of the box, this middleware will set cache-control
			// public only when user is logged out, else no-cache
			// Which is precisely the behavior we want
			app.UseResponseCaching();
			app.Use(async (context, next) =>
			{
				context.Response.GetTypedHeaders().CacheControl =
					new Microsoft.Net.Http.Headers.CacheControlHeaderValue
					{
						Public = true,
						MaxAge = TimeSpan.FromSeconds(30)
					};
				context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
					new[] { "Accept-Encoding" };

				await next();
			});

			app.UseMvc(routes =>
			{
				routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
				routes.MapRoute("wiki", "{*url}", defaults: new { controller = "Wiki", action = nameof(WikiController.RenderWikiPage) });
			});

			return app;
		}
	}
}
