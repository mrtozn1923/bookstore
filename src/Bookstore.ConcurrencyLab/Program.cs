using Bookstore.ConcurrencyLab.Commands;

System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
string command = args.Length > 0 ? args[0] : "help";

bool handled = BasicsCommands.Run(command, args)
    || RaceCommands.Run(command, args)
    || ExclusionCommands.Run(command, args)
    || DeadlockCommands.Run(command, args)
    || HardwareCommands.Run(command, args)
    || MemoryModelCommands.Run(command, args)
    || LockFreeCommands.Run(command, args)
    || await ModelCommands.RunAsync(command, args);

if (!handled)
    Console.WriteLine("usage: dotnet run -c Release -- <command> [arguments]; see the post for the commands");
