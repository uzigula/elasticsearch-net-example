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

		static void Main(string[] args)
		{
			Client = NuSearchConfiguration.GetClient();
			DumpReader = new NugetDumpReader(@"C:\nuget-data");

			DeleteIndexIfExists();
			IndexDumps();

			Console.Read();
		}

		static void DeleteIndexIfExists()
		{
			if (Client.IndexExists("nusearch").Exists)
				Client.DeleteIndex("nusearch");
		}

		static void IndexDumps()
		{
			var packages = DumpReader.Dumps.Take(1).First().NugetPackages;

			var result = Client.Bulk(b => b.IndexMany(packages));

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

			Console.WriteLine("Done.");
		}
	}
}
