using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebUpLoadingTest
{
    public record Startup(IConfiguration Configuration)
    {
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddRazorPages(opt => opt.Conventions
            //   .AddPageApplicationModelConvention(
            //        "/FileUpload",
            //        model =>
            //        {
            //            model.Filters.Add(new GenerateAntiforgeryTokenCookieAttribute());
            //            model.Filters.Add(new DisableFormValueModelBindingAttribute());
            //        })
            //);

            services.Configure<FormOptions>(opt =>
            {
                // Set the limit to 256 MB
                opt.MultipartBodyLengthLimit = 1 * 1024 * 1024 * 1024;
            });

            services.AddControllersWithViews()
               .AddRazorRuntimeCompilation();

            services.AddMediatR(typeof(Startup));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
