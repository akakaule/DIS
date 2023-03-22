namespace BH.DIS.CommandLine
{
    using McMaster.Extensions.CommandLineUtils;
    using System;
    using System.Threading.Tasks;

    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "dis-cli"
            };

            var sbConnectionString = new CommandOption("-sbc|--sb-connection-string", CommandOptionType.SingleValue)
            {
                Description = $"Overrides environment variable '{CommandRunner.SbConnectionStringEnvName}'"
            };

            var dbConnectionString = new CommandOption("-dbc|--db-connection-string", CommandOptionType.SingleValue)
            {
                Description = $"Overrides environment variable '{CommandRunner.DbConnectionStringEnvName}'"
            };

            app.HelpOption(inherited: true);

            app.Command("endpoint", endpointCommand =>
            {
                endpointCommand.OnExecute(() =>
                {
                    Console.WriteLine("Specify a subcommand");
                    endpointCommand.ShowHelp();
                    return 1;
                });

                endpointCommand.Command("session", sessionCommand =>
                {
                    sessionCommand.OnExecute(() =>
                    {
                        Console.WriteLine("Specify a subcommand");
                        sessionCommand.ShowHelp();
                        return 1;
                    });

                    sessionCommand.Command("delete", deleteSessionCommand =>
                    {
                        deleteSessionCommand.Description = "Deletes messages on a session and its session state";

                        var endpointName = deleteSessionCommand.Argument("endpoint-name", "Name of endpoint (requried)").IsRequired();
                        var sessionId = deleteSessionCommand.Argument("session", "Session id (required)").IsRequired();

                        deleteSessionCommand.AddOption(sbConnectionString);
                        deleteSessionCommand.AddOption(dbConnectionString);

                        deleteSessionCommand.OnExecuteAsync(async ct =>
                        {
                            await CommandRunner.Run(sbConnectionString, dbConnectionString, (sbClient, dbClient, sbAdmin) => Endpoint.DeleteSession(sbClient, dbClient, endpointName, sessionId));

                            Console.WriteLine($"Endpoint '{endpointName.Value}' is ready.");
                        });
                    });
                });
                
                endpointCommand.Command("topics", sessionCommand =>
                {
                    sessionCommand.OnExecute(() =>
                    {
                        Console.WriteLine("Specify a subcommand");
                        sessionCommand.ShowHelp();
                        return 1;
                    });
                    
                    sessionCommand.Command("removeDeprecated", removeDeprecatedCommand =>
                    {
                        removeDeprecatedCommand.Description = "Deletes deprecated topics and the underlying subscriptions and rules from the service bus";
                        
                        var endpointName = removeDeprecatedCommand.Argument("endpoint-name", "Name of endpoint (requried)").IsRequired();
                        
                        removeDeprecatedCommand.AddOption(sbConnectionString);

                        removeDeprecatedCommand.OnExecuteAsync(async ct =>
                        {
                            await CommandRunner.Run(sbConnectionString, dbConnectionString, (sbClient, dbClient, sbAdmin) => Endpoint.RemoveDeprecated(sbAdmin, endpointName));
                        });
                    });
                });
            });

            app.OnExecute(() =>
            {
                Console.WriteLine("Specify a subcommand");
                app.ShowHelp();
                return 1;
            });

            try
            {
                return app.Execute(args);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Command failed with exception ({exception.GetType().Name}): {exception.Message}");
                return 1;
            }
        }
    }
}