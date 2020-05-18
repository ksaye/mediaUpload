using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace mediaupload
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().UseKestrel(options => {
                        // dangerous?  removing the max body limit when posting
                        options.Limits.MaxRequestBodySize = null;
                    });
                });
    }
}
