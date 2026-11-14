using Bookstore.Institutional.Activities;

Node start = new("start", NodeKind.Initial);
Node review = new("ApproverReview", NodeKind.Action);
Node needsPurchasing = new("NeedsPurchasingManager?", NodeKind.Decision);
Node purchasingReview = new("PurchasingManagerReview", NodeKind.Action);
Node merge = new("Merge", NodeKind.Merge);
Node markApproved = new("MarkApproved", NodeKind.Action);
Node fork = new("Fork", NodeKind.Fork);
Node deductBudget = new("DeductBudget", NodeKind.Action);
Node sendToDistributor = new("SendToDistributor", NodeKind.Action);
Node join = new("Join", NodeKind.Join);
Node end = new("end", NodeKind.ActivityFinal);

// MarkApproved'dan sonrası iki modelde de aynı: fork, iki paralel dal, join
Edge[] tail =
[
    new(markApproved, fork), new(fork, deductBudget), new(fork, sendToDistributor),
    new(deductBudget, join), new(sendToDistributor, join), new(join, end)
];

Console.WriteLine("== without merge ==");
decimal total = 6_840m; // #43: 6.840 TL, satın alma müdürü onayı gerekmez

new TokenGame([
    new(start, review), new(review, needsPurchasing),
    new(needsPurchasing, purchasingReview, () => total > 50_000m),
    new(needsPurchasing, markApproved, () => total <= 50_000m),
    new(purchasingReview, markApproved), .. tail
]).Run(start);

Console.WriteLine();
Console.WriteLine("== with merge ==");
new TokenGame([
    new(start, review), new(review, needsPurchasing),
    new(needsPurchasing, purchasingReview, () => total > 50_000m),
    new(needsPurchasing, merge, () => total <= 50_000m),
    new(purchasingReview, merge), new(merge, markApproved), .. tail
]).Run(start);
