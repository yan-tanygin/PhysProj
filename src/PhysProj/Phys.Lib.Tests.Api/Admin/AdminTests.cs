﻿using Autofac;
using Phys.Lib.Core.Users;
using Phys.Lib.Admin.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Phys.Files;
using Phys.Files.Local;
using Phys.Lib.Autofac;

namespace Phys.Lib.Tests.Api.Admin
{
    public partial class AdminTests : ApiTests
    {
        private const string nonExistentCode = "non-existent";
        private const string url = "http://localhost:17188/";

        private readonly IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { {"ConnectionStrings:db", "mongo"} }).Build();

        private readonly AdminApiClient api;

        private LocalFileStorage? fileStorage;

        private FileInfo AdminApiPath => new(Path.Combine(solutionDir.FullName, "Phys.Lib.Admin.Api", "Phys.Lib.Admin.Api.csproj"));
        private FileInfo AppPath => new(Path.Combine(solutionDir.FullName, "Phys.Lib.App", "Phys.Lib.App.csproj"));

        public AdminTests(ITestOutputHelper output) : base(output)
        {
            api = new AdminApiClient(url, http);
        }

        protected override async Task Init()
        {
            await base.Init();

            StartApp(url, AppPath);
            var appDir = StartApp(url, AdminApiPath);

            fileStorage = new LocalFileStorage("local", new DirectoryInfo(Path.Combine(appDir.FullName, "data/files")), loggerFactory.CreateLogger<LocalFileStorage>());

            var container = BuildContainer();
            using var scope = container.BeginLifetimeScope();
            var users = scope.Resolve<IUsersService>();
            CreateUsers(users);
        }

        private static void CreateUsers(IUsersService users)
        {
            var user = users.Create("user", "123456");
            users.AddRole(user, UserRole.User);
            var admin = users.Create("admin", "123qwe");
            users.AddRole(admin, UserRole.Admin);
        }

        private IContainer BuildContainer()
        {
            var builder = new ContainerBuilder();
            builder.Register(_ => configuration).As<IConfiguration>().SingleInstance();
            builder.RegisterModule(new LoggerModule(loggerFactory));
            builder.RegisterModule(new MongoDbModule(GetMongoUrl(), "mongo", loggerFactory));
            builder.RegisterModule(new CoreModule());
            return builder.Build();
        }

        [Fact]
        public void Tests()
        {
            Log("testing");

            TestHealth();
            TestUsers();
            TestAuthors();
            TestWorks();
            TestFiles();
            TestMigrations();

            Log("tested");
        }

        private void TestMigrations()
        {
            var tests = new MigrationTests(api);

            tests.List(0);

            var usersMigration = tests.Start(new MigrationTaskModel { Migrator = "users", Source = "mongo-test", Destination = "postgres" });
            tests.List(1);
            tests.WaitCompleted(usersMigration.Id, TimeSpan.FromSeconds(10), "success", 2);
        }

        private void TestUsers()
        {
            var tests = new UsersTests(api);

            tests.LoginFailed(new LoginModel { Username = "non-existent", Password = "123456" });
            tests.LoginFailed(new LoginModel { Username = "user", Password = "123456" });
            tests.LoginFailed(new LoginModel { Username = "admin", Password = "123456" });

            tests.GetUserInfoUnauthorized();
            var token = tests.LoginSuccess(new LoginModel { Username = "admin", Password = "123qwe" });
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            tests.GetUserInfoAuthorized("admin");
        }

        private void TestHealth()
        {
            var check = api.HealthCheckAsync().Result;
            check.Should().NotBeNull();
        }

        private void TestAuthors()
        {
            var authors = new AuthorsTests(api);
            var works = new WorksTests(api);

            // TODO: extract complex scenarios
            // empty list at start
            authors.List();
            // not found author
            authors.NotFound("decartes", ErrorCode.NotFound);
            // create invalid author
            authors.CreateFailed("@$#");
            // create valid author
            authors.Create("decartes");
            authors.Found("decartes");
            // create second valid author
            authors.Create("galilei");
            authors.Found("galilei");
            authors.List("decartes", "galilei");
            // create author duplicate failed
            authors.CreateFailed("galilei");
            authors.List("decartes", "galilei");
            // search by code
            authors.Create("galileo");
            authors.Search("dec", new[] { "decartes" });
            authors.Search("Dec", new[] { "decartes" });
            authors.Search("gal", new[] { "galileo", "galilei" });
            authors.Delete("galileo");
            // search by name
            authors.Create("galileo");
            authors.InfoUpdate("galileo", "en", new AuthorInfoUpdateModel { FullName = "name" });
            authors.InfoUpdate("galilei", "en", new AuthorInfoUpdateModel { Description = "desc" });
            authors.Search("name", new[] { "galileo" });
            authors.Search("desc", new[] { "galilei" });
            authors.Delete("galileo");
            // delete is idempotent
            authors.Delete(nonExistentCode);
            authors.List("decartes", "galilei");
            // delete author
            authors.Delete("galilei");
            authors.NotFound("galilei", ErrorCode.NotFound);
            authors.List("decartes");
            // update with invalid lifetime
            authors.UpdateLifetimeFailed("decartes", new AuthorLifetimeUpdateModel { Born = nonExistentCode });
            authors.UpdateLifetimeFailed("decartes", new AuthorLifetimeUpdateModel { Born = "165o" });
            authors.UpdateLifetimeFailed("decartes", new AuthorLifetimeUpdateModel { Born = "1696", Died = "1650" });
            // update with valid lifetime
            authors.UpdateLifetime("decartes", new AuthorLifetimeUpdateModel { Died = "1650" });
            // separate update with invalid lifetime
            authors.UpdateLifetimeFailed("decartes", new AuthorLifetimeUpdateModel { Born = "1696" });
            // update with valid lifetimes
            authors.UpdateLifetime("decartes", new AuthorLifetimeUpdateModel { Born = string.Empty, Died = "1650" });
            authors.UpdateLifetime("decartes", new AuthorLifetimeUpdateModel { Born = "1596", Died = "1650" });
            // update not found author
            authors.InfoUpdateFailed(nonExistentCode, "en", new AuthorInfoUpdateModel(), ErrorCode.NotFound);
            // update info with invalid language
            authors.InfoUpdateFailed("decartes", nonExistentCode, new AuthorInfoUpdateModel(), ErrorCode.InvalidArgument);
            // update info with valid languages
            authors.InfoUpdate("decartes", "en", new AuthorInfoUpdateModel { FullName = "René Descartes", Description = "French philosopher, scientist, and mathematician" });
            authors.InfoUpdate("decartes", "ru", new AuthorInfoUpdateModel { FullName = "Рене Декарт", Description = "французский философ, математик и естествоиспытатель" });
            // search by full name ignore case
            authors.Search("декарт", new[] { "decartes" });
            // delete info
            authors.InfoDelete("decartes", "en");
            // delete info is idempotent
            authors.InfoDelete("decartes", nonExistentCode);
            // can not delete author linked to work
            authors.Create("author");
            works.Create("work-of-author");
            works.LinkAuthor("work-of-author", "author");
            authors.DeleteFailed("author");
            works.Delete("work-of-author");
            authors.Delete("author");
        }

        private void TestWorks()
        {
            var authors = new AuthorsTests(api);
            var works = new WorksTests(api);
            var files = new FilesTests(api);

            // TODO: extract complex scenarios
            // empty list at start
            works.List();
            // not found work
            works.NotFound("discourse-on-method", ErrorCode.NotFound);
            // create invalid work
            works.CreateFailed("dm");
            works.CreateFailed("@@#");
            // create valid work
            works.Create("discourse-on-method");
            works.Found("discourse-on-method");
            works.List("discourse-on-method");
            // can not create duplicate
            works.CreateFailed("discourse-on-method");
            // update non existing failed
            works.UpdateFailed(nonExistentCode, new WorkUpdateModel(), ErrorCode.NotFound);
            // update with invalid language failed
            works.UpdateFailed("discourse-on-method", new WorkUpdateModel { Date = "1637", Language = nonExistentCode });
            // update with invalid date failed
            works.UpdateFailed("discourse-on-method", new WorkUpdateModel { Date = nonExistentCode });
            // update with valid date and language
            works.Update("discourse-on-method", new WorkUpdateModel { Date = "1737" });
            works.Update("discourse-on-method", new WorkUpdateModel { Date = "1637", Language = "FR" });
            works.Update("discourse-on-method", new WorkUpdateModel { Date = string.Empty, Language = string.Empty });
            works.Update("discourse-on-method", new WorkUpdateModel { Date = "1637", Language = "fr", IsPublic = true });
            works.Update("discourse-on-method", new WorkUpdateModel { IsPublic = false });
            // update non existing failed
            works.UpdateInfoFailed(nonExistentCode, "ru", new WorkInfoUpdateModel(), ErrorCode.NotFound);
            // update with invalid language failed
            works.UpdateInfoFailed("discourse-on-method", nonExistentCode, new WorkInfoUpdateModel());
            // update with valid info
            works.UpdateInfo("discourse-on-method", "en", new WorkInfoUpdateModel { Name = "Discourse on the Method", Description = "one of the most influential works in the history of modern philosophy" });
            works.UpdateInfo("discourse-on-method", "ru", new WorkInfoUpdateModel { Name = "Рассуждение о методе", Description = "Считается переломной работой, ознаменовавшей переход от философии Ренессанса и начавшей эпоху философии Нового времени" });
            // search by name ignore case
            works.Search("рассужд", new[] { "discourse-on-method" });
            // delete info
            works.DeleteInfo("discourse-on-method", "ru");
            // delete info is idempotent
            works.DeleteInfo("discourse-on-method", "es");
            
            // link invalid author failed
            works.LinkAuthorFailed("discourse-on-method", nonExistentCode);
            // link valid author
            works.LinkAuthor("discourse-on-method", "decartes");
            // update with publish before author's born failed
            works.UpdateFailed("discourse-on-method", new WorkUpdateModel { Date = "1537" });
            // update born of author which has work published before failed
            authors.UpdateLifetimeFailed("decartes", new AuthorLifetimeUpdateModel { Born = "1650" });
            // unlink author
            works.UnlinkAuthor("discourse-on-method", "decartes");
            // unlink author is idempotent
            works.UnlinkAuthor("discourse-on-method", "decartes");
            works.UnlinkAuthor("discourse-on-method", nonExistentCode);

            // link invalid sub-work author failed
            works.LinkSubWorkAuthorFailed("discourse-on-method", nonExistentCode);
            // link valid sub-work author
            works.LinkSubWorkAuthor("discourse-on-method", "decartes");
            // unlink sub-work author
            works.UnlinkSubWorkAuthor("discourse-on-method", "decartes");
            // unlink sub-work author is idempotent
            works.UnlinkSubWorkAuthor("discourse-on-method", "decartes");
            works.UnlinkSubWorkAuthor("discourse-on-method", nonExistentCode);

            // link with valid original
            works.Create("discourse-on-method-original");
            // link original with circular dependency failed
            works.Create("discourse-on-method-proxy");
            // link invalid work to collected work failed
            works.LinkSubWorkFailed("discourse-on-method", nonExistentCode);
            // link works to collected work
            works.Create("discourse-on-method-chapter-one");
            works.Create("discourse-on-method-chapter-two");
            works.Create("discourse-on-method-chapter-three");
            works.LinkSubWork("discourse-on-method", "discourse-on-method-chapter-one");
            works.LinkSubWork("discourse-on-method", "discourse-on-method-chapter-two");
            works.LinkSubWork("discourse-on-method", "discourse-on-method-chapter-three");
            // link collected work to self failed
            works.LinkSubWorkFailed("discourse-on-method", "discourse-on-method");
            // unlink works from collected work
            works.UnlinkSubWork("discourse-on-method", "discourse-on-method-chapter-one");
            works.UnlinkSubWork("discourse-on-method", "discourse-on-method-chapter-three");
            // can not delete work linked as sub-work
            works.Create("work");
            works.Create("sub-work");
            works.LinkSubWork("work", "sub-work");
            works.DeleteFailed("sub-work");
            works.Delete("work");
            works.Delete("sub-work");
            // search
            works.Create("work-abc-1");
            works.UpdateInfo("work-abc-1", "en", new WorkInfoUpdateModel { Name = "abcname" });
            works.Create("work-abc-2");
            works.UpdateInfo("work-abc-2", "en", new WorkInfoUpdateModel { Name = "abcdesc" });
            works.Create("work-abd-2");
            // search by code
            works.Search("abc", new[] { "work-abc-1", "work-abc-2" });
            // search by name
            works.Search("abcname", new[] { "work-abc-1" });
            works.Search("abcdesc", new[] { "work-abc-2" });
            // link not existing file
            works.LinkFileFailed("discourse-on-method", nonExistentCode);
            // link existing file
            using (var stream = files.GetMockStream())
                fileStorage!.Upload(stream, "discourse-on-method.pdf");
            files.LinkStorageFile("local", "discourse-on-method.pdf");
            works.LinkFile("discourse-on-method", "discourse-on-method-pdf");
            fileStorage.Delete("discourse-on-method.pdf");
            // unlink file
            files.DeleteFailed("discourse-on-method-pdf");
            works.UnlinkFile("discourse-on-method", nonExistentCode);
            works.UnlinkFile("discourse-on-method", "discourse-on-method-pdf");
            files.Delete("discourse-on-method-pdf");
        }

        private void TestFiles()
        {
            var files = new FilesTests(api);

            files.ListStorages("local", "pcloud");
            // list local storage files files
            files.ListStorageFiles("local");
            using (var stream = files.GetMockStream())
                fileStorage!.Upload(stream, "works/work-1.txt");
            files.RefreshStorage("local");

            files.ListStorageFiles("local", "works/work-1.txt");
            // list empty files links
            files.ListFiles();
            // link storage file
            files.LinkStorageFile("local", "works/work-1.txt");
            files.ListFiles("work-1-txt");
        }
    }
}