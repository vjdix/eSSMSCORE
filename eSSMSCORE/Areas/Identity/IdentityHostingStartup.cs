using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(eSSMSCORE.Areas.Identity.IdentityHostingStartup))]
namespace eSSMSCORE.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
            });
        }
    }
}