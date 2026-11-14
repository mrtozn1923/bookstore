// 13 - inside the replica set: the last election, the oplog window and a hidden, delayed member (admin)
const user = process.env.MONGO_ROOT_USER;
const pwd = process.env.MONGO_ROOT_PASSWORD;
const direct = host => new Mongo(`mongodb://${user}:${pwd}@${host}/?authSource=admin&directConnection=true`);

const metrics = rs.status().electionCandidateMetrics;
print("last election:", metrics.lastElectionReason, "| electionTimeoutMillis", Number(metrics.electionTimeoutMillis),
      "| votes needed", metrics.numVotesNeeded);

// The oplog window: how far back a secondary or a change stream can catch up
const oplog = db.getSiblingDB("local").getReplicationInfo();
print("oplog size", Math.round(oplog.logSizeMB), "MB | used", oplog.usedMB, "MB | first to last entry", oplog.timeDiffHours, "hours");

// A secondary becomes hidden, priority 0 and delayed by 60 seconds
const cfg = rs.conf();
const primary = db.hello().primary;
const member = cfg.members.filter(m => m.host !== primary).at(-1);
Object.assign(member, { priority: 0, hidden: true, secondaryDelaySecs: 60 });
rs.reconfig(cfg);
sleep(2000);
print(member.host, "is hidden; members the drivers see:", db.hello().hosts.join(", "));

// An accidental delete on the primary: the delayed member still has the document
const orders = db.getSiblingDB("bookstore").orders;
orders.deleteOne({ _id: 378 }, { writeConcern: { w: "majority" } });
sleep(2000);
const delayed = direct(member.host).getDB("bookstore").orders.findOne({ _id: 378 });
print("order 378 on the primary:", orders.findOne({ _id: 378 }) ? "found" : "deleted",
      "| on the delayed member:", delayed ? `found (${delayed.status})` : "deleted");
orders.insertOne(delayed);
print("restored from the delayed member:", orders.findOne({ _id: 378 }).status);

// Undo: the member becomes a normal secondary again
const undo = rs.conf();
Object.assign(undo.members.find(m => m.host === member.host), { priority: 1, hidden: false, secondaryDelaySecs: 0 });
rs.reconfig(undo);
