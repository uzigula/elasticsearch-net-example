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
					.Query(q => 
						q.Match(m => m
							.OnField(p => p.Id.Suffix("keyword"))
							.Boost(1000)
							.Query(form.Query)
						) || 
						q.FunctionScore(fs => fs
							.MaxBoost(100)
							.Functions(ff => ff
								.FieldValueFactor(fvf => fvf
									.Field(p => p.DownloadCount)
									.Factor(0.0001)
								)
							)
							.Query(query => query
								.MultiMatch(m => m
									.OnFieldsWithBoost(fields => fields
										.Add(p => p.Id.Suffix("keyword"), 1.5)
										.Add(p => p.Id, 1.2)
										.Add(p => p.Summary, 0.8)
									)
									.Operator(Operator.And)
									.Query(form.Query)
								)
							)
						)
					)
					.Aggregations(a => a
						.Nested("authors", n => n
							.Path("authors")
							.Aggregations(aa => aa
								.Terms("author-names", ts => ts
									.Field(p => p.Authors.First().Name.Suffix("raw"))
								)
							)
						)
					)
				);

				var authors = result.Aggs.Nested("authors")
					.Terms("author-names")
					.Items
					.ToDictionary(k => k.Key, v => v.DocCount);

				model.Authors = authors;
				model.Packages = result.Documents;
				model.Total = result.Total;
				model.Form = form;

				return View[model];
			};

		}
	}
}
