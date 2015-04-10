using Nest;
using NuSearch.Domain.Data;
using NuSearch.Domain.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using NuSearch.Domain;
using NuSearch.Domain.Extensions;
using ShellProgressBar;

namespace NuSearch.Indexer
{
	class Program
	{
		private static ElasticClient Client { get; set; }
		private static NugetDumpReader DumpReader { get; set; }
		private static string IndexName { get; set; }

		static void Main(string[] args)
		{
			Client = NuSearchConfiguration.GetClient();
			DumpReader = new NugetDumpReader(@"C:\nuget-data");
			IndexName = NuSearchConfiguration.CreateIndexName();

			CreateIndex();
			IndexDumps();
			SwapAlias();

			Console.Read();
		}

		private static void SwapAlias()
		{
			Client.Alias(alias => alias
				.Remove(r => r
					.Index("nusearch-*")
					.Alias(NuSearchConfiguration.LiveIndexAlias)
				)
				.Add(a => a
					.Index("nusearch-*")
					.Alias(NuSearchConfiguration.OldIndexAlias)
				)
				.Add(a => a
					.Index(IndexName)
					.Alias(NuSearchConfiguration.LiveIndexAlias)
				)
			);

			Client.Alias(alias => alias
				.Remove(r => r
					.Index(IndexName)
					.Alias(NuSearchConfiguration.OldIndexAlias)
				)
			);
		}

		static void CreateIndex()
		{
			Client.CreateIndex(IndexName, i => i
				.NumberOfShards(2)
				.NumberOfReplicas(0)
				.AddMapping<Package>(m => m
					.MapFromAttributes()
					.Properties(ps => ps
						.NestedObject<PackageVersion>(n => n
							.Name(p => p.Versions.First())
							.MapFromAttributes()
							.Properties(pps => pps
								.NestedObject<PackageDependency>(nn => nn
									.Name(pv => pv.Dependencies.First())
									.MapFromAttributes()
								)
							)
						)
						.NestedObject<PackageAuthor>(n => n
							.Name(p => p.Authors.First())
							.MapFromAttributes()
						)
					)
				)
			);
		}

		static void IndexDumps()
		{
			var packages = DumpReader.GetPackages();
			var partitions = packages.Partition(1000).ToList();
			foreach (var partition in partitions)
			{
				var result = Client.IndexMany(partition, IndexName);

				if (!result.IsValid)
				{
					Console.WriteLine(result.ConnectionStatus.OriginalException.Message);
					Console.Read();
					Environment.Exit(1);
				}

				if (result.Errors)
				{
					foreach (var item in result.ItemsWithErrors)
						Console.WriteLine("Failed to index document {0}: {1}", item.Id, item.Error);
				}
			}
			Console.WriteLine("Done.");
		}
	}
}
