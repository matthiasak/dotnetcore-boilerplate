# .NET Core Boilerplate

# How to use

1. clone to your machine
- `npm install -g now yarn`
- `yarn && dotnet restore` -or- `npm run setup`
- if using Entity Framework / a database:

    - modify Models/*.cs to create your csharp Models for Entity Framework Core; add any seeded data to the `Seeder` class
    - `dotnet ef migrations add init` - create the initial migrations for the database seeding
    - `dotnet ef database update` - write the migrations to the database
    - if at any point you change a model, rerun the preceding steps

- `npm start` - runs and watch the files for changes. Underneath, this runs `dotnet watch run`, `npm run css:watch`, `npm run js:watch` for CSS and JS build tools.
- if at any point you install a package through NuGet or npm, or change the project.json or package.json files - hit Ctrl+C and run `npm run setup` again.
- open `http://localhost:5000` to view local server

# Migrations

1. **When using EntityFramework.InMemory**

    - You won't need to consider the creation of migration files, so we'll just develop and live happily ever after.

2. **When using Sqlite or PostgreSQL**

    - You'll need to develop your model classes, have them compile, and then generate a migration for them. Your migration files will be added to a new Migrations folder. Don't forget to `git add .` since we need to commit these Migrations to source control.
    - For either Sqlite or Postgres, you will need to create the empty database first (i.e. either on your machine or on Heroku), then create the Migration files from the `dotnet` CLI.
    - Create the database:

        - For Sqlite, create the `<project_folder>/bin/Debug/netcoreapp1.0/app.db` file with http://sqlitebrowser.org/
        - For Postgres, use the `heroku` CLI (https://devcenter.heroku.com/articles/heroku-command-line) to create an database:

            ```sh
            # from project folder
            heroku create
            heroku addons:create heroku-postgresql:hobby-dev
            heroku config
            #--> parse out the pieces from the connection string: "user:password@host:port/database"
            #--> you can use http://dbglass.web-pal.com/ to login and view the database tables
            ```
    
    - When you run `npm start` / `dotnet watch run` your app will apply the migrations to its connected database.
    - Want to create 2 databases on Heroku?

        ```sh
        # get me dev db
        heroku create
        heroku addons:create heroku-postgresql:hobby-dev
        heroku config
        # store the first connection string
        git remote remove heroku
        # get me prod db
        heroku create
        heroku addons:create heroku-postgresql:hobby-dev
        heroku config
        # store the 2nd connection string
        ```

# To deploy

To https://now.sh:

1. **The slow way**
    - from project folder: `now --docker`
    - open the url provided (`dotnetcore-boilerplate-XXXXXXXXXXXX.now.sh`); when the installation is done the browser will be redirected to your new server
    - to setup a custom URL: `now alias dotnetcore-boilerplate-XXXXXXXXXXXX.now.sh YOURAPPNAME.now.sh`
2. **The fast way**
    - from project folder: `npm run deploy`

To https://heroku.com

- install the heroku CLI (https://devcenter.heroku.com/articles/heroku-command-line)
- (update and commit all your local git files)
- `heroku create --buildpack http://github.com/noliar/dotnet-buildpack.git`
- `git push heroku master`
- `heroku open`

# Support

1. Please submit issues on GitHub with proper taggings / labels.
2. Reach out to [@matthiasak](https://twitter.com/matthiasak).