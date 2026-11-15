#!/usr/bin/env dotnet
#:project ../src/Bookstore.Contracts/Bookstore.Contracts.csproj
#:include IsbnReport.cs

using Bookstore.Contracts;

// Reads one ISBN per line and reports the invalid ones; exit code 1 if any.
IsbnReport report = new();
foreach (string line in File.ReadLines(args[0]))
{
    report.Add(line, Isbn.TryParse(line, out _));
}

report.Print();
return report.InvalidCount == 0 ? 0 : 1;
