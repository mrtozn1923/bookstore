import 'package:drift/drift.dart';

part 'app_database.g.dart';

/// Books the app has seen: the catalog cache and the books in the cart.
@DataClassName('CachedBook')
class Books extends Table {
  TextColumn get isbn => text()();
  TextColumn get title => text()();
  TextColumn get author => text()();
  IntColumn get priceKurus => integer()();
  IntColumn get publishedYear => integer()();
  // Added in schema version 2
  TextColumn get description => text().nullable()();
  // Where the book was in the catalog; null if it came from elsewhere
  IntColumn get catalogPage => integer().nullable()();
  IntColumn get catalogPosition => integer().nullable()();
  DateTimeColumn get fetchedAt => dateTime()();

  @override
  Set<Column> get primaryKey => {isbn};
}

@DataClassName('CartLineRow')
class CartLines extends Table {
  TextColumn get isbn => text().references(Books, #isbn)();
  IntColumn get quantity => integer()();

  @override
  Set<Column> get primaryKey => {isbn};
}

@DriftDatabase(tables: [Books, CartLines])
class AppDatabase extends _$AppDatabase {
  AppDatabase(super.executor);

  @override
  int get schemaVersion => 2;

  @override
  MigrationStrategy get migration => MigrationStrategy(
    onCreate: (m) => m.createAll(),
    onUpgrade: (m, from, to) async {
      // Version 1 had no description column
      if (from < 2) await m.addColumn(books, books.description);
    },
    beforeOpen: (details) => customStatement('PRAGMA foreign_keys = ON'),
  );

  /// Emits the number of copies in the cart now and after every change.
  Stream<int> watchCartCount() {
    final total = cartLines.quantity.sum();
    return (selectOnly(
      cartLines,
    )..addColumns([total])).map((row) => row.read(total) ?? 0).watchSingle();
  }
}
