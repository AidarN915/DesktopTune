using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DesktopTune.Services
{
    public class HostService
    {
        public IHost HostS;


        public async Task StartAsync(Player player)
        {
            HostS = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(player);
                    services.AddSignalR();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel()
                              .UseUrls("http://localhost:5000") 
                              .Configure(async app =>
                              {
                                  app.UseStaticFiles();
                                  app.UseRouting();
                                  app.UseEndpoints(endpoints =>
                                  {

                                      endpoints.MapGet("/", async ctx =>
                                      {
                                          ctx.Response.Redirect("/player.html");
                                      });
                                      endpoints.MapHub<PlayerHub>("/hub/player");
                                  });
                              });
                })
                .Build();

            await HostS.StartAsync();
        }
        public async Task StopAsync()
        {
            if (HostS != null)
                await HostS.StopAsync();
        }

    }
}
