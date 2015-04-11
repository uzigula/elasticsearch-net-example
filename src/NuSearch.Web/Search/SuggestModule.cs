using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Owin;
using NuSearch.Domain;
using NuSearch.Domain.Model;
using Nest;

namespace NuSearch.Web.Search
{
	public class SuggestModule : NancyModule
	{
		public SuggestModule()
		{

			Post["/suggest"] = x =>
			{
				var form = this.Bind<SearchForm>();
				var client = NuSearchConfiguration.GetClient();
				var result = client.Suggest<Package>(s => s
					.Index<Package>()
					.Completion("package-suggestions", c => c
						.Text(form.Query)
						.OnField(p => p.Suggest)
					)
				);

				var suggestions = result.Suggestions["package-suggestions"]
					.FirstOrDefault()
					.Options
					.Select(suggest => suggest.Payload);

				return Response.AsJson(suggestions);
			};
		}
	}
}
