// 02 - single-node replica set: member name localhost:27017 also resolves from the host
const result = rs.initiate({
    _id: "rs0",
    members: [{ _id: 0, host: "localhost:27017" }]
});
printjson(result);
