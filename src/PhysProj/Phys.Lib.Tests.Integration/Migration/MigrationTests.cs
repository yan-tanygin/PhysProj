﻿using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Phys.Lib.Autofac;
using Phys.Lib.Core.Authors;
using Phys.Lib.Core.Files;
using Phys.Lib.Core.Migration;
using Phys.Lib.Core.Users;
using Phys.Lib.Core.Works.Migration;
using Phys.Lib.Db.Authors;
using Phys.Lib.Db.Files;
using Phys.Lib.Db.Users;
using Phys.Lib.Db.Works;
using Phys.Lib.Tests.Db;
using Phys.Mongo.HistoryDb;
using Phys.Shared.Broker;
using Phys.Shared.EventBus.Broker;
using Phys.Shared.Queue.Broker;
using Shouldly;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Phys.Lib.Tests.Integration.Migration
{
    public class MigrationTests : BaseTests
    {
        private readonly IConfiguration configuration = ConfigurationFactory.CreateTestConfiguration();

        private readonly PostgreSqlContainer postgres = TestContainerFactory.CreatePostgres();
        private readonly MongoDbContainer mongo = TestContainerFactory.CreateMongo();
        private readonly RabbitMqContainer rabbit = TestContainerFactory.CreateRabbit();

        public MigrationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData("mongo", "postgres")]
        [InlineData("postgres", "mongo")]
        public void Tests(string source, string destination)
        {
            using var lifetimeScope = Container.BeginLifetimeScope();

            lifetimeScope.Resolve<BrokerRegistrarService>().StartAsync(CancellationToken.None).Wait();

            UsersTests(source, destination, lifetimeScope);
            AuthorsTests(source, destination, lifetimeScope);
            FilesTests(source, destination, lifetimeScope);
            WorksTests(source, destination, lifetimeScope);
        }

        private void WorksTests(string source, string destination, ILifetimeScope lifetimeScope)
        {
            var srcDb = lifetimeScope.ResolveNamed<IWorksDb>(source);
            var migrations = lifetimeScope.Resolve<IMigrationService>();

            var works = new[]
            {
                new WorkDbo { Code = "work-1", Language = "ru", Publish = "1234" },
                new WorkDbo { Code = "work-2", Infos = new List<WorkDbo.InfoDbo> { new WorkDbo.InfoDbo { Language = "en", Description = "desc" } } },
                new WorkDbo { Code = "work-3", AuthorsCodes = new List<string> { "author-1", "author-2" } },
                new WorkDbo { Code = "work-4", FilesCodes = new List<string> { "file-1", "file-2" } },
                new WorkDbo { Code = "work-5", SubWorksCodes = new List<string> { "work-6", "work-3" } },
                new WorkDbo { Code = "work-6", },
                new WorkDbo { Code = "work-7" },
            }.OrderBy(u => u.Code).ToList();
            new WorksBaseWriter(srcDb, loggerFactory.CreateLogger<WorksMigrator>()).Write(works, new MigrationDto.StatsDto());
            new WorksLinksWriter(srcDb).Write(works, new MigrationDto.StatsDto());

            var migration = migrations.Create(new MigrationParams { Migrator = MigratorName.Works, Source = source, Destination = destination });
            while (!migration.CompletedAt.HasValue)
            {
                migration = migrations.Get(migration.Id);
                if (migration.CreatedAt < DateTime.UtcNow.AddSeconds(-10))
                    Assert.Fail("timeout");
            }

            migration.Error.ShouldBeNull(migration.Error);

            var dstUsers = lifetimeScope.ResolveNamed<IWorksDb>(destination);
            var migrated = dstUsers.Find(new WorksDbQuery()).OrderBy(u => u.Code).ToList();
            Assert.Equivalent(works, migrated);
        }

        private static void FilesTests(string source, string destination, ILifetimeScope lifetimeScope)
        {
            var srcDb = lifetimeScope.ResolveNamed<IFilesDb>(source);
            var migrations = lifetimeScope.Resolve<IMigrationService>();

            var files = new[]
            {
                new FileDbo { Code = "file-1", Format = "pdf", Links = new List<FileDbo.LinkDbo> { new FileDbo.LinkDbo { StorageCode = "type-1", FileId = "path-1" } } },
                new FileDbo { Code = "file-2", Format = "pdf", Size = 1024, Links = new List<FileDbo.LinkDbo>
                {
                    new FileDbo.LinkDbo { StorageCode = "type-2", FileId = "path-2" },
                    new FileDbo.LinkDbo { StorageCode = "type-2", FileId = "path-3" }
                } },
            }.OrderBy(u => u.Code).ToList();
            new FilesWriter(srcDb).Write(files, new MigrationDto.StatsDto());

            var migration = migrations.Create(new MigrationParams { Migrator = MigratorName.Files, Source = source, Destination = destination });
            while (!migration.CompletedAt.HasValue)
            {
                migration = migrations.Get(migration.Id);
                if (migration.CreatedAt < DateTime.UtcNow.AddSeconds(-10))
                    Assert.Fail("timeout");
            }

            migration.Error.ShouldBeNull(migration.Error);

            var dstUsers = lifetimeScope.ResolveNamed<IFilesDb>(destination);
            var migrated = dstUsers.Find(new FilesDbQuery()).OrderBy(u => u.Code).ToList();
            Assert.Equivalent(files, migrated);
        }

        private static void AuthorsTests(string source, string destination, ILifetimeScope lifetimeScope)
        {
            var srcDb = lifetimeScope.ResolveNamed<IAuthorsDb>(source);
            var migrations = lifetimeScope.Resolve<IMigrationService>();

            var authors = new[]
            {
                new AuthorDbo { Code = "author-1", Born = "1234", Infos = new List<AuthorDbo.InfoDbo>
                {
                    new AuthorDbo.InfoDbo { Language = "en", FullName = "FN" },
                    new AuthorDbo.InfoDbo { Language = "ru", FullName = "Имя" }
                } },
                new AuthorDbo { Code = "author-2", Died = "2345" },
            }.OrderBy(u => u.Code).ToList();
            new AuthorsWriter(srcDb).Write(authors, new MigrationDto.StatsDto());

            var migration = migrations.Create(new MigrationParams { Migrator = MigratorName.Authors, Source = source, Destination = destination });
            while (!migration.CompletedAt.HasValue)
            {
                migration = migrations.Get(migration.Id);
                if (migration.CreatedAt < DateTime.UtcNow.AddSeconds(-10))
                    Assert.Fail("timeout");
            }

            migration.Error.ShouldBeNull(migration.Error);

            var dstAuthors = lifetimeScope.ResolveNamed<IAuthorsDb>(destination);
            var migrated = dstAuthors.Find(new AuthorsDbQuery()).OrderBy(u => u.Code).ToList();
            Assert.Equivalent(authors, migrated);
        }

        private static void UsersTests(string source, string destination, ILifetimeScope lifetimeScope)
        {
            var srcDb = lifetimeScope.ResolveNamed<IUsersDb>(source);
            var migrations = lifetimeScope.Resolve<IMigrationService>();

            var users = new[]
            {
                new UserDbo { Name = "user-1", NameLowerCase = "user-1", PasswordHash = "1", Roles = new List<string> { "role1", "role2" } },
                new UserDbo { Name = "user-2", NameLowerCase = "user-2", PasswordHash = "1", Roles = new List<string> { "role3" } }
            }.OrderBy(u => u.Name).ToList();
            new UsersWriter(srcDb).Write(users, new MigrationDto.StatsDto());

            var migration = migrations.Create(new MigrationParams { Migrator = MigratorName.Users, Source = source, Destination = destination });
            while (!migration.CompletedAt.HasValue)
            {
                migration = migrations.Get(migration.Id);
                if (migration.CreatedAt < DateTime.UtcNow.AddSeconds(-10))
                    Assert.Fail("timeout");
                Task.Delay(500).Wait();
            }

            migration.Error.ShouldBeNull(migration.Error);

            var dstUsers = lifetimeScope.ResolveNamed<IUsersDb>(destination);
            var migrated = dstUsers.Find(new UsersDbQuery()).OrderBy(u => u.Name).ToList();
            Assert.Equivalent(users, migrated);
        }

        protected override void BuildContainer(ContainerBuilder builder)
        {
            base.BuildContainer(builder);

            var mongoUrl = new MongoUrl(mongo.GetConnectionString());
            builder.Register(_ => configuration).As<IConfiguration>().SingleInstance();
            builder.RegisterModule(new PostgresDbModule(postgres.GetConnectionString(), loggerFactory));
            builder.RegisterModule(new MongoDbModule(mongoUrl, "mongo", loggerFactory));
            builder.RegisterModule(new RabbitMqModule(loggerFactory, rabbit.GetConnectionString()));
            builder.RegisterModule(new CoreModule());

            builder.RegisterQueueConsumer<MigrationsExecutor, MigrationExecuteMessage>();

            builder.Register(c => new MongoHistoryDbFactory(mongoUrl, "physlib", "history-", loggerFactory))
                .SingleInstance()
                .AsImplementedInterfaces();
        }

        protected override async Task Init()
        {
            await mongo.StartAsync();
            await postgres.StartAsync();
            await rabbit.StartAsync();

            await base.Init();
        }

        protected override async Task Release()
        {
            await mongo.StopAsync();
            await postgres.StopAsync();
            await rabbit.StopAsync();

            await base.Release();
        }
    }
}
