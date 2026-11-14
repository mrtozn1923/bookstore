using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Bookstore.MongoLab;

// Keeps the author name copied into books (post 4) in sync, using a change stream on authors
public sealed class AuthorNameSync(
    IMongoCollection<Author> authors,
    IMongoCollection<Book> books,
    IMongoDatabase database,
    ILogger<AuthorNameSync> logger) : BackgroundService
{
    private const string TokenId = "author-name-sync";
    private readonly TaskCompletionSource _watching = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _processed;

    public Task Watching => _watching.Task;
    public int Processed => Volatile.Read(ref _processed);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IMongoCollection<BsonDocument> tokens = database.GetCollection<BsonDocument>("resume_tokens");
        BsonDocument? saved = await tokens.Find(new BsonDocument("_id", TokenId)).FirstOrDefaultAsync(stoppingToken);

        PipelineDefinition<ChangeStreamDocument<Author>, ChangeStreamDocument<Author>> pipeline =
            new EmptyPipelineDefinition<ChangeStreamDocument<Author>>()
                .Match(c => c.OperationType == ChangeStreamOperationType.Update);
        ChangeStreamOptions options = new()
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
            ResumeAfter = saved?["token"].AsBsonDocument      // null on the first run: start from now
        };

        using IChangeStreamCursor<ChangeStreamDocument<Author>> cursor =
            await authors.WatchAsync(pipeline, options, stoppingToken);
        logger.LogInformation("Watching authors ({Mode})", saved is null ? "from now" : "resuming after saved token");
        _watching.TrySetResult();

        while (await cursor.MoveNextAsync(stoppingToken))
        {
            foreach (ChangeStreamDocument<Author> change in cursor.Current)
            {
                Author author = change.FullDocument;
                string name = $"{author.FirstName} {author.LastName}";
                UpdateResult result = await books.UpdateManyAsync(
                    Builders<Book>.Filter.ElemMatch(b => b.Authors, a => a.AuthorId == author.Id),
                    Builders<Book>.Update.Set("authors.$[a].name", name),
                    new UpdateOptions
                    {
                        ArrayFilters = [new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("a.authorId", author.Id))]
                    },
                    stoppingToken);

                // Save the token only after the work is done: a restart repeats at most this one change
                await tokens.ReplaceOneAsync(new BsonDocument("_id", TokenId),
                    new BsonDocument { { "_id", TokenId }, { "token", change.ResumeToken } },
                    new ReplaceOptions { IsUpsert = true }, stoppingToken);
                logger.LogInformation("Author {AuthorId} is now {Name}: {Books} book(s) updated", author.Id, name, result.ModifiedCount);
                Interlocked.Increment(ref _processed);
            }
        }
    }
}
