using MassTransit;
using MassTransit.MultiBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace WebAppTest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddHealthChecks();

            // first bus
            services.AddMassTransit(bus =>
            {
                bus.UsingInMemory((context, cfg) =>
                {
                    cfg.TransportConcurrencyLimit = 100;
                    cfg.ConfigureEndpoints(context);
                });
            });

            // second bus which should be used for azure event hub rider
            services.AddMassTransit<IAzureEventHubBus, AzureEventHubBus>(bus => 
            {
                bus.UsingAzureServiceBus((context, cfg) => 
                {
                    cfg.Host("Endpoint=..."); // <- azure event hub connection string
                    cfg.ConfigureEndpoints(context);
                });

                bus.AddRider(rider =>
                {
                    rider.UsingEventHub((context, k) => 
                    {
                        k.Host("Endpoint=..."); // <- azure event hub connection string
                        k.Storage("Endpoint=..."); // <- azure event hub connection string
                    });
                });
            });

            services.AddMassTransitHostedService();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebAppTest", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebAppTest v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public interface IAzureEventHubBus : IBus {
    }

    public class AzureEventHubBus : BusInstance<IAzureEventHubBus>, IAzureEventHubBus {
        public AzureEventHubBus(IBusControl busControl) : base(busControl) {
        }
    }
}
