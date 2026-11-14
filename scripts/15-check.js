// 15 - after every upgrade step: binary version, feature compatibility version, the data and an index definition
const fcv = db.adminCommand({ getParameter: 1, featureCompatibilityVersion: 1 }).featureCompatibilityVersion.version;
const books = db.getSiblingDB("bookstore").books;
const index = books.getIndexes().find(i => i.name === "title_1");
print(`version ${db.version()} | FCV ${fcv} | books ${books.countDocuments()} | title_1:`, JSON.stringify(index ?? "none"));
