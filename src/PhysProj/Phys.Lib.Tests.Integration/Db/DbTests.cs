using Autofac;
using Microsoft.Extensions.Logging;
using Phys.Lib.Db.Authors;
using Phys.Lib.Db.Files;
using Phys.Lib.Db.Users;
using Phys.Lib.Db.Works;
using Phys.Lib.Autofac;
using Phys.Shared;
using Phys.Serilog;

namespace Phys.Lib.Tests.Integration.Db
{
    public abstract class DbTests : IDisposable
    {
        protected readonly LoggerFactory loggerFactory = new LoggerFactory();

        protected readonly ITestOutputHelper output;

        public DbTests(ITestOutputHelper output)
        {
            this.output = output;

            try
            {
                SerilogConfig.Configure(loggerFactory);
                PhysAppContext.Init(loggerFactory);
                Log("initializing");
                Init().Wait();
                Log("initialized");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        [Fact]
        public void Tests()
        {
            using var container = BuildContainer();
            using var lifetimeScope = container.BeginLifetimeScope();
            var users = lifetimeScope.Resolve<IUsersDb>();
            var authors = lifetimeScope.Resolve<IAuthorsDb>();
            var works = lifetimeScope.Resolve<IWorksDb>();
            var files = lifetimeScope.Resolve<IFilesDb>();
            new UsersTests(users).Run();
            new AuthorsTests(authors).Run();
            new FilesTests(files).Run();
            new WorksTests(works, authors, files).Run();
        }

        protected IContainer BuildContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new LoggerModule(loggerFactory));
            builder.RegisterModule(new CoreModule());
            Register(builder);
            return builder.Build();
        }

        public void Dispose()
        {
            Log("releasing");
            Release().Wait();
            Log("released");
        }

        protected virtual void Register(ContainerBuilder builder)
        {
        }

        protected virtual Task Init()
        {
            return Task.CompletedTask;
        }

        protected virtual Task Release()
        {
            return Task.CompletedTask;
        }

        protected void Log(string message)
        {
            output.WriteLine($"{DateTime.UtcNow}: {message}");
        }
    }
}