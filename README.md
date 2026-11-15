# Bookstore filter language

Code for the "Compilers and Programming Languages" series on mertozen.com.

```bash
dotnet build
dotnet test --project tests/Bookstore.Filter.Tests
dotnet run --project src/Bookstore.Filter.Cli -- tokens 'price <= 300 and author = "Jane Austen"'
dotnet run --project src/Bookstore.Filter.Cli -- ast 'price < 300 and author = "Jane Austen"'
dotnet run --project src/Bookstore.Filter.Cli -- run 'price < 300 and author = "Jane Austen"'
dotnet run --project src/Bookstore.Filter.Cli -- nest 100000 pratt

# samples
dotnet run -c Release --project samples/PipelineTour
dotnet run --project samples/RoslynTrivia
dotnet run --project samples/RegexLexer
cd samples/ts-erasure && npm install && npx tsc && node dist/book.js
node samples/v8-tiers/hot.js
```
