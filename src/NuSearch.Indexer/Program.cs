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

        static void IndexDumps()
        {
            var packages = DumpReader.Dumps.Take(1).First().NugetPackages;

            var result = Client.IndexMany(packages);

            if (!result.IsValid)
            {
                foreach (var item in result.ItemsWithErrors)
                    Console.WriteLine($"Failed to Index document {item.Id} : {item.Error}");
                Console.WriteLine(result.ConnectionStatus.OriginalException.Message);
                Console.Read();
                Environment.Exit(1);
            }

            Console.WriteLine("Done.");
        }

        static void DeleteIndexIfExists()
        {
            if (Client.IndexExists(NuSearchConfiguration.LiveIndexAlias).Exists)
                Client.DeleteIndex(NuSearchConfiguration.LiveIndexAlias);
        }

        static void CreateIndex()
        {
            Client.CreateIndex(NuSearchConfiguration.LiveIndexAlias,idx=> idx
                        .NumberOfShards(2)
                        .NumberOfReplicas(0)
                        .AddMapping<Package>(m=> m
                            .MapFromAttributes()
                            .Properties( prop => prop
                                .NestedObject<PackageVersion>(n=> n
                                    .Name(p=>p.Versions.First())
                                    .MapFromAttributes()
                                    .Properties(nprop => nprop
                                        .NestedObject<PackageDependency>(nd => nd
                                            .Name(pnd=>pnd.Dependencies.First())
                                            .MapFromAttributes()
                                        )
                                    )
                                )
                                .NestedObject<PackageAuthor>(n => n
                                    .Name(p=>p.Authors.First())
                                    .MapFromAttributes()
                                )
                            )
                        )
                );
        }
    }
}
