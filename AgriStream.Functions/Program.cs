using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AgriStreamIOT.Data;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration["SqlConnection"];

        services.AddDbContext<AgriStreamDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddDbContext<CommerceDbContext>(options =>
            options.UseSqlServer(connectionString));
    })
    .Build();

host.Run();
