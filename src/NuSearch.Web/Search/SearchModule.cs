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
					.Skip(form.Page * form.PageSize)
					.Take(form.PageSize)
					.Sort(sort =>
					{
						if (form.Sort == SearchSort.Downloads)
							return sort.Field(f => f
								.NestedPath(p => p.Versions)
								.Field(p => p.Versions.First().DownloadCount)
								.Mode(SortMode.Sum)
								.Descending()
							);
						if (form.Sort == SearchSort.Recent)
							return sort.Field(f => f
								.NestedPath(p => p.Versions)
								.Field(p => p.Versions.First().LastUpdated)
								.Mode(SortMode.Max)
								.Descending()
							);
						return sort.Descending(SortSpecialField.Score);
					})
					.Query(q => q
						.Bool(b => b
							.Must(must => must
								.Match(m => m
									.Field(p => p.Id.Suffix("keyword"))
									.Boost(1000)
									.Query(form.Query)
								) || must
								.FunctionScore(fs => fs
									.MaxBoost(100)
									.Functions(ff => ff
										.FieldValueFactor(fvf => fvf
											.Field(p => p.DownloadCount)
											.Factor(0.0001)
										)
									)
									.Query(query => query
										.MultiMatch(m => m
											.Fields(fields => fields
												.Field(p => p.Id.Suffix("keyword"), 1.5)
												.Field(p => p.Id, 1.2)
												.Field(p => p.Summary, 0.8)
											)
											.Operator(Operator.And)
											.Query(form.Query)
										)
									)
								)
							)
							.Filter(f => f
								.Nested(nf => nf
									.Path("authors")
									.Query(nq => nq
										.Term(t => t.Authors.First().Name.Suffix("raw"), form.Author)
									)
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
				model.TotalPages = (int)Math.Ceiling(result.Total / (double)form.PageSize);
				model.Form = form;

				return View[model];
			};

		}
	}
}
