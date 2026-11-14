using Bookstore.ConcurrencyLab.Commands;

System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
string command = args.Length > 0 ? args[0] : "help";

bool handled = BasicsCommands.Run(command, args)
    || RaceCommands.Run(command, args);

if (!handled)
    Console.WriteLine("usage: dotnet run -c Release -- <command> [arguments]; see the post for the commands");
