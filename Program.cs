using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.Swagger.Model;
using Swashbuckle.SwaggerGen.Generator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using React.AspNet;
using Microsoft.AspNetCore.Session;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using RimDev.Stuntman.Core;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

public class Program
{
    public static void Main(string[] args)
    {   
        var host = new WebHostBuilder()
            .UseKestrel()
            .UseWebRoot("assets")
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseStartup<Handler>()
            .Build();

        host.Run();
    }
}

public partial class Handler {

    public enum SessionOptions { None, Cookie, Identity };
    public enum DatabaseOptions { None, InMemory, Sqlite, Postgres };
    public enum JSOptions { None, ReactDotNet };
    public enum RestfulOptions { None, CORS };
    public enum SwaggerOptions { None, JSON, UI };
    public enum WebsocketOptions { None, SignalR };
    [Flags]
    public enum AuthOptions { Google, Facebook };

    // configure your app
    // --------------------------------------------------------
    protected SessionOptions _session = SessionOptions.Identity;
    protected bool _convertConnectionString = true;
    protected DatabaseOptions _db = DatabaseOptions.InMemory;
    protected JSOptions _js = JSOptions.None;
    protected RestfulOptions _restful = RestfulOptions.CORS;
    protected SwaggerOptions _swagger = SwaggerOptions.UI;
    protected WebsocketOptions _websockets = WebsocketOptions.None;
    protected AuthOptions _auth = AuthOptions.Google | AuthOptions.Facebook;
    // --------------------------------------------------------

    public IConfigurationRoot config { get; }
    private IHostingEnvironment Env;

    public Handler(IHostingEnvironment env)
    {
        Env = env;

        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables(prefix: "ASPNETCORE_");

        if (env.IsDevelopment())
        {
            // For more details on using the user secret store see https://go.microsoft.com/fwlink/?LinkID=532709
            // builder.AddUserSecrets();

            // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
            builder.AddApplicationInsightsSettings(developerMode: true);
        }

        config = builder.Build();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddApplicationInsightsTelemetry(config);

        switch(_db){
            case DatabaseOptions.InMemory: 
                services.AddDbContext<DB>(options => options.UseInMemoryDatabase());
                break;
            case DatabaseOptions.Sqlite:
                services.AddDbContext<DB>(options => options.UseSqlite(config.GetConnectionString("Sqlite")));
                break;
            case DatabaseOptions.Postgres:
                string endpoint = Env.IsDevelopment() ? "Dev" : "Prod";
                services.AddDbContext<DB>(options =>
                    options.UseNpgsql(
                        _convertConnectionString ? 
                            ParseConnectionString(config.GetConnectionString($"Postgres:{endpoint}"), "Postgres") :
                            config.GetConnectionString($"Postgres:{endpoint}")
                    ));
                break;
        }
        
        switch(_session){
            case SessionOptions.Identity:
                services.AddIdentity<IdentityUser, IdentityRole>()
                        .AddEntityFrameworkStores<DB>()
                        .AddDefaultTokenProviders();
                
                services.Configure<IdentityOptions>(options =>
                {
                    // Password settings
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    
                    // Lockout settings
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                    options.Lockout.MaxFailedAccessAttempts = 10;
                    
                    // Cookie settings
                    options.Cookies.ApplicationCookie.ExpireTimeSpan = TimeSpan.FromDays(150);
                    options.Cookies.ApplicationCookie.LoginPath = "/login";
                    options.Cookies.ApplicationCookie.LogoutPath = "/logoff";
                    
                    // User settings
                    options.User.RequireUniqueEmail = true;
                });
                services.AddScoped<IAuthService, AuthService>();
                break;
            case SessionOptions.Cookie:
                services.AddDistributedMemoryCache();
                services.AddSession(o => {
                    o.IdleTimeout = TimeSpan.FromSeconds(120);
                });
                break;
        }

        services.AddTransient<IEmail, MessageService>();
        services.AddTransient<ISms, MessageService>();        

        switch(_restful){
            case RestfulOptions.CORS:
                services.AddCors(options =>
                    options.AddPolicy("CorsPolicy",
                        builder => builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()));
                    break;
        }

        switch(_js) {
            case JSOptions.ReactDotNet:
                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                services.AddReact();
                break;
        }

        services
            .AddMvc(options => {
                // if(!Env.IsDevelopment()) {
                //     options.Filters.Add(new RequireHttpsAttribute());
                // }
            })
            .AddJsonOptions(options => {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize;
                options.SerializerSettings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects;
            });
        
        switch(_websockets){
            case WebsocketOptions.SignalR:
                // services.AddSignalR();
                break;
        }

        RegisterRepos(services);

        switch(_swagger){
            case SwaggerOptions.JSON:
                services.AddSwaggerGen();
                break;
            case SwaggerOptions.UI:
                services.AddSwaggerGen();
                services.ConfigureSwaggerGen(options =>
                {
                    options.SingleApiVersion(new Info
                    {
                        Version = config["Swagger:Version"],
                        Title = config["Swagger:Title"],
                        Description = config["Swagger:Description"]
                    });
                    options.IgnoreObsoleteActions();
                    options.IgnoreObsoleteProperties();
                    options.DescribeAllEnumsAsStrings();
                });
                break;
        }
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory logger, DB db) {
        // logger.AddConsole(Configuration.GetSection("Logging"));
        logger.AddDebug();

        switch(_session){
            case SessionOptions.Identity:
                app.UseIdentity();
                // if(env.IsDevelopment()){
                //     CreateStuntUsers();
                //     app.UseStuntman(StuntmanOptions);
                // }
                break;
            case SessionOptions.Cookie: app.UseSession(); break;
        }

        switch(_restful){
            case RestfulOptions.CORS:
                app.UseCors("CorsPolicy");
                break;
        }

        // Example custom middleware
        // app.Use(async (context, next) =>
        // {
        //     await context.Response.WriteAsync("Pre Processing");
        //     await next();
        //     await context.Response.WriteAsync("Post Processing");
        // });

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseDatabaseErrorPage();
            app.UseStatusCodePages();
        }

        Seed.Initialize(
            db, 
            _db == DatabaseOptions.InMemory,
            _db == DatabaseOptions.Postgres || _db == DatabaseOptions.Sqlite);

        app.UseApplicationInsightsRequestTelemetry();
        app.UseApplicationInsightsExceptionTelemetry();
        
        switch(_js){
            case JSOptions.ReactDotNet:
                // Initialise ReactJS.NET. Must be before static files.
                app.UseReact(config =>
                {
                    // If you want to use server-side rendering of React components,
                    // add all the necessary JavaScript files here. This includes
                    // your components as well as all of their dependencies.
                    // See http://reactjs.net/ for more information. Example:
                    // config
                    //     .AddScript("~/Scripts/First.jsx")
                    //     .AddScript("~/Scripts/Second.jsx");

                    // If you use an external build tool (for example, Babel, Webpack,
                    // Browserify or Gulp), you can improve performance by disabling
                    // ReactJS.NET's version of Babel and loading the pre-transpiled
                    // scripts. Example:
                    config
                        .SetLoadBabel(false)
                        .AddScriptWithoutTransform("~/assets/build/app.min.js");
                });
                break;
        }

        app.UseStaticFiles();

        switch(_websockets){
            case WebsocketOptions.SignalR:
                // app.UseSignalR(routes =>
                // {
                //     routes.MapHub<Chat>("/chat");
                // });
                break;
        }

        Action authfb = () => {
            var facebookId = config["Auth:Facebook:AppId"];
            var facebookSecret = config["Auth:Facebook:AppSecret"];
            if (string.IsNullOrWhiteSpace(facebookId) || string.IsNullOrWhiteSpace(facebookSecret)) return;
            app.UseFacebookAuthentication(new FacebookOptions { AppId = facebookId, AppSecret = facebookSecret });
        };

        Action authg = () => {
            var googleId = config["Auth:Google:ClientId"];
            var googleSecret = config["Auth:Google:ClientSecret"];
            if (string.IsNullOrWhiteSpace(googleId) || string.IsNullOrWhiteSpace(googleSecret)) return;
            app.UseGoogleAuthentication(new GoogleOptions { ClientId = googleId, ClientSecret = googleSecret });
        };

        if((_auth & AuthOptions.Google) != 0) authg();
        if((_auth & AuthOptions.Facebook) != 0) authfb();

        app.UseMvc();

        switch(_swagger){
            case SwaggerOptions.JSON: app.UseSwagger(); break;
            case SwaggerOptions.UI: app.UseSwagger(); app.UseSwaggerUi(); break;
        }
    }

    public static string ParseConnectionString(string c, string type){
        switch(type){
            case "Postgres":
                return new Regex(@"^postgres://(.*):(.*)@(.*):(.*)/(.*)$")
                    .Replace(c, "Server=$3; DB=$5; User Id=$1; Password=$2; SslMode=Require; Trust Server Certificate=true; Maximum Pool Size=20;");
            default: return "";
        }
    }

    public static readonly StuntmanOptions StuntmanOptions = new StuntmanOptions();

    public void CreateStuntUsers(){
        StuntmanOptions
        .AddUser(
            new StuntmanUser("user-1", "User 1")
                .AddClaim("name", "John Doe")
                .AddClaim("IsAdmin", "True"));
    }
}

