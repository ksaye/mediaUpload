using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage;
using System.IO;

namespace mediaupload
{
    public class Startup
    {
        public string mediaContainer = "mystorage";
        public string connectionString = "DefaultEndpointsProtocol=https;AccountName=******************";
        public string loginPassword = "****************";
        public string options = "throw a 8x11 sheet of paper into the trash";

        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", defaultGet);
                endpoints.MapGet("/list", defaultList);
                endpoints.MapGet("/submit", defaultSubmit);
                endpoints.MapPost("/receive", defaultPost);
            });
        }

        private async Task defaultPost(HttpContext context)
        {
            if (context.Request.Form.ContainsKey("scoutName") &&
                context.Request.Form.ContainsKey("description")) { 
                var uploadedFile = context.Request.Form.Files[0].OpenReadStream();
                //var stream = System.IO.File.Create(context.Request.Form.Files[0].FileName);
                var stream = new MemoryStream();
                uploadedFile.CopyTo(stream);
                //stream.Close();
                stream.Position = 0;

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(mediaContainer);
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(context.Request.Form.Files[0].FileName);
                //await cloudBlockBlob.UploadFromFileAsync(context.Request.Form.Files[0].FileName);
                await cloudBlockBlob.UploadFromStreamAsync(stream);

                try
                {
                    cloudBlockBlob.Metadata.Clear();
                    cloudBlockBlob.Metadata.Add("scoutName", context.Request.Form["scoutName"]);
                    cloudBlockBlob.Metadata.Add("description", context.Request.Form["description"]);
                    await cloudBlockBlob.SetMetadataAsync();
                }
                catch (Exception)
                {
                }

                context.Response.Redirect("/list");

            } else
            {
                context.Response.Redirect("/submit");
            }
        }

        private async Task defaultSubmit(HttpContext context)
        {
            if (context.Request.Cookies["authenticated"] == "true")
            {
                StringBuilder sb = new StringBuilder("<HTML><HEAD></HEAD><BODY><TABLE valign=\"center\" align=\"center\">");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<form action=\"/receive\" method=\"post\" enctype=\"multipart/form-data\">");
                sb.Append("<TR><TD>Scout Name: <input type=\"text\" name=\"scoutName\"></TD></TR>");
                sb.Append("<TR><TD>Description:<select name=\"description\">");

                foreach (string option in options.Split("|"))
                {
                    sb.Append("<option value=\"" + option + "\">" + option + "</option>");
                }

                sb.Append("</select>");
                sb.Append("</TD></TR>");
                sb.Append("<TR><TD><input type=\"file\" id=\"file\" name=\"file\"></TD></TR>");
                sb.Append("<TR><TD><input type=\"submit\" value=\"Submit\"></form></TD></TR>");
                sb.Append("</TABLE></BODY></HTML>");
                await context.Response.WriteAsync(sb.ToString());
            } else { 
                context.Response.Redirect("/");
            }
        }

        private async Task defaultList(HttpContext context)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(mediaContainer);
            SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),
                SharedAccessStartTime = DateTime.UtcNow,
                Permissions = SharedAccessBlobPermissions.Read
            };

            StringBuilder sb = new StringBuilder("<HTML><HEAD></HEAD><BODY><TABLE align=\"center\">");
            sb.Append("<TR><TD>&nbsp;</TD></TR>");
            sb.Append("<TR><TD>&nbsp;</TD></TR>");
            sb.Append("<TR><TD>&nbsp;</TD></TR>");
            sb.Append("<TR><TD>&nbsp;</TD></TR>");
            sb.Append("<TR><TD>&nbsp;</TD></TR>");
            sb.Append("<TR><TD>&nbsp;</TD></TR>");
            sb.Append("<TR><TD>&nbsp;</TD></TR>");
            sb.Append("<TR><TD>&nbsp;</TD></TR>");
            sb.Append("<TR><TD>&nbsp;</TD></TR>");
            sb.Append("<TR><TD><B>Scout Name</B></TD><TD><B>Description</B></TD><TD><B>File Name</B></TD></TR>");

            BlobContinuationToken blobContinuationToken = null;
            do
            {
                var results = await cloudBlobContainer.ListBlobsSegmentedAsync(string.Empty,
                    true, BlobListingDetails.Metadata, 100, blobContinuationToken, null, null);
                // Get the value of the continuation token returned by the listing call.
                blobContinuationToken = results.ContinuationToken;
                foreach (CloudBlob item in results.Results)
                {
                    try
                    {
                        sb.Append("<TR><TD>" + item.Metadata["scoutName"] + "</TD>");
                    }
                    catch (Exception)
                    {
                        sb.Append("<TR><TD></TD>");
                    }

                    try
                    {
                        sb.Append("<TD>" + item.Metadata["description"] + "</TD>");
                    }
                    catch (Exception)
                    {
                        sb.Append("<TD></TD>");
                    }

                    sb.Append("<TD><a href=\"" + item.Uri + item.GetSharedAccessSignature(policy) +"\">" + item.Name + "</a></TD></TR>");
                }
            } while (blobContinuationToken != null); // Loop while the continuation token is not null.            

            sb.Append("</TABLE></BODY></HTML>");
            await context.Response.WriteAsync(sb.ToString());
        }

        private async Task defaultGet(HttpContext context)
        {
            if (context.Request.Query.ContainsKey("password") && (context.Request.Query["password"].ToString().ToLower() == loginPassword.ToLower())) {
                context.Response.Cookies.Append("authenticated", "true");
                context.Response.Redirect("/submit");
            } else { 
                StringBuilder sb = new StringBuilder("<HTML><HEAD></HEAD><BODY><TABLE align=\"center\">");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>&nbsp;</TD></TR>");
                sb.Append("<TR><TD>");
                sb.Append("<form action=\"/\" method=\"get\">");
                sb.Append("Password: <input type=\"password\" name=\"password\">");
                sb.Append("<input type=\"submit\" value=\"Login\"></form>");
                sb.Append("</TD></TR>");
                sb.Append("</TABLE></BODY></HTML>");
                await context.Response.WriteAsync(sb.ToString());
            }
        }
    }
}
