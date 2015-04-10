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
	public class SearchModule : NancyModule
	{
		public SearchModule()
		{
			Get["/"] = x =>
			{
				var form = this.Bind<SearchForm>();
				var model = new SearchViewModel();

				var client = NuSearchConfiguration.GetClient();
				var result = client.Search<Package>(s => s
					.Size(25)
					.Query(q => q
						.MultiMatch(m => m
							.OnFields(p => p.Id, p => p.Summary)
							.Operator(Operator.And)
							.Query(form.Query)
						)
					)
				);

				model.Packages = result.Documents;
				model.Total = result.Total;
				model.Form = form;

				return View[model];
			};

		}
	}
}
