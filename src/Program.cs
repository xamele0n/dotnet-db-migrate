namespace db_migrate
{
    using DbUp.Engine;
    
    using McMaster.Extensions.CommandLineUtils;
    
    using System;
    using System.ComponentModel.DataAnnotations;

    [Command(Description = "A tool to deploy changes to SQL databases.")]
    [HelpOption("-h|--help")]
    class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Required]
        [ConnectionString]
        [Argument(0, Description = "Required. The connection details for a database.")]
        private string ConnectionString { get; }

        [Option(Template = "-p|--provider",
            Description = "Optional. The connection provider. Default: mssql")]
        [AllowedValues("mssql", "postgres", IgnoreCase = true)]
        private string Provider { get; } = "mssql";

        [Option(Template = "-s|--scripts",
            Description = "Optional. The path to the migration scripts. Default: scripts/")]
        private string Scripts { get; } = "scripts";

        [Option(Template = "--ensure-db-exists", Description = "Optional. Create the database if it doesn't exist. Default: false")]
        private bool EnsureDatabaseExists { get; } = false;

        private int OnExecute()
        {
            DbMigrator migrator = GetMigrator();

            if (EnsureDatabaseExists)
            {
                migrator.EnsureDatabaseExists();
            }

            DatabaseUpgradeResult result;
                
            try
            {
                result = migrator.Migrate(Scripts);
            }
            catch (ConnectionFailedException e)
            {
                WriteError($"{e.Message}{Environment.NewLine}Please check the connection string for errors or use the --ensure-db-exists flag to create the db.");
                return 1;
            }

            if (!result.Successful)
            {
                WriteError(result.Error.Message);
                return 1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success!");
            Console.ResetColor();
            return 0;
        }

        private DbMigrator GetMigrator()
        {
            switch (Provider.ToLower())
            {
                case "postgres":
                    return new PostgresMigrator(ConnectionString);
                default:
                    return new MSSqlMigrator(ConnectionString);
            }
        }

        private static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ResetColor();
        }
    }
}
