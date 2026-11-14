// 08 - $graphLookup: the category tree (SQL Temelleri 12: recursive CTE)
// Path of every category: walk up through parentId
db.categories.aggregate([
    { $graphLookup: { from: "categories", startWith: "$parentId", connectFromField: "parentId",
                      connectToField: "_id", as: "ancestors", depthField: "level" } },
    { $set: { depth: { $size: "$ancestors" },
              path: { $reduce: {
                  input: { $sortArray: { input: "$ancestors", sortBy: { level: -1 } } },
                  initialValue: "",
                  in: { $concat: ["$$value", "$$this.name", " > "] } } } } },
    { $set: { path: { $concat: ["$path", "$name"] } } },
    { $sort: { path: 1 } },
    { $limit: 8 }
]).forEach(c => print(String(c._id).padStart(2), c.depth, c.path));

// Subtree: every category under Non-Fiction (8), then the books in them
const subtree = db.categories.aggregate([
    { $match: { _id: 8 } },
    { $graphLookup: { from: "categories", startWith: "$_id", connectFromField: "_id",
                      connectToField: "parentId", as: "descendants" } },
    { $project: { ids: { $concatArrays: [["$_id"], "$descendants._id"] } } }
]).next().ids;
print("categories under Non-Fiction:", subtree.sort((a, b) => a - b).join(", "),
      " books:", db.books.countDocuments({ "category.categoryId": { $in: subtree } }));
