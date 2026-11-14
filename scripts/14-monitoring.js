// 14 - monitoring: profiler with a filter, the slow query log, WiredTiger cache and $queryStats (admin)
const bookstore = db.getSiblingDB("bookstore");

// Profile only operations that scan a whole collection
bookstore.setProfilingLevel(1, { filter: { planSummary: "COLLSCAN" } });
bookstore.books.find({ title: { $regex: /^the/i } }).toArray();
bookstore.orders.find({ "customer.customerId": 17 }).toArray();
bookstore.system.profile.find({}, { op: 1, ns: 1, planSummary: 1, docsExamined: 1, nreturned: 1, millis: 1 })
    .forEach(p => print("profiled:", p.op, p.ns, p.planSummary, "docsExamined", p.docsExamined, "nreturned", p.nreturned));
bookstore.setProfilingLevel(0);
bookstore.system.profile.drop();

// The same filter decides what goes to the log as a slow query
const slow = db.adminCommand({ getLog: "global" }).log.map(l => JSON.parse(l))
    .filter(e => e.msg === "Slow query" && e.attr.ns === "bookstore.books").at(-1);
print("slow query log:", slow.attr.planSummary, "docsExamined", slow.attr.docsExamined, "durationMillis", slow.attr.durationMillis);

// WiredTiger cache: how full is it?
const cache = db.serverStatus().wiredTiger.cache;
print("WiredTiger cache:", Math.round(cache["bytes currently in the cache"] / 1024 / 1024), "MB used of",
      Math.round(cache["maximum bytes configured"] / 1024 / 1024), "MB");

try {
    // 9.0: collected by default for a sample of reads and writes
    db.getSiblingDB("admin").aggregate([{ $queryStats: {} }]).forEach(s =>
        print("$queryStats:", s.key.queryShape.command, `${s.key.queryShape.cmdNs.db}.${s.key.queryShape.cmdNs.coll}`,
              "execCount", Number(s.metrics.execCount)));
} catch (e) {
    print("$queryStats:", e.codeName, e.message);
}
const rates = db.adminCommand({ getParameter: 1, internalQueryStatsSampleRate: 1, internalQueryStatsWriteCmdSampleRate: 1 });
print("sample rate: reads", rates.internalQueryStatsSampleRate, "writes", rates.internalQueryStatsWriteCmdSampleRate);
