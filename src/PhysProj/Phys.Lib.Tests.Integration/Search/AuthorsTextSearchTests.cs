﻿using Autofac;
using Phys.Lib.Autofac;
using Phys.Lib.Core;
using Phys.Lib.Search;
using Shouldly;

namespace Phys.Lib.Tests.Integration.Search
{
    public class AuthorsTextSearchTests : BaseTests
    {
        public AuthorsTextSearchTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override void BuildContainer(ContainerBuilder builder)
        {
            base.BuildContainer(builder);
            builder.RegisterModule(new MeilisearchModule("http://192.168.2.67:7700/", "phys-lib-tests", loggerFactory));
        }

        [Fact]
        public async Task Tests()
        {
            var search = container.Resolve<ITextSearch<AuthorTso>>();

            await search.Reset(Language.AllAsStrings);
            await search.Index(new[]
            {
                new AuthorTso { Code = "1", Names = new Dictionary<string, string?> { ["en"] = "On the Nature of Things", ["ru"] = "О природе вещей" } },
                new AuthorTso { Code = "2", Names = new Dictionary<string, string?> { ["en"] = "On The Heavens" } },
                new AuthorTso { Code = "3", Names = new Dictionary<string, string?> { ["ru"] = "Клеомед - Учение о круговращении небесных тел" } },
                new AuthorTso { Code = "4", Names = new Dictionary<string, string?> { ["en"] = "introduction-to-the-phenomena", ["ru"] = "Гемин - Введение в явления" } },
            });

            search.Search("учение").Result.Count.ShouldBe(1);
            search.Search("the").Result.Count.ShouldBe(3);
            search.Search("4he").Result.Count.ShouldBe(0);
        }
    }
}
