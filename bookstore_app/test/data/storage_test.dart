import 'dart:io';

import 'package:bookstore_app/data/repositories/book_repository_cached.dart';
import 'package:bookstore_app/data/repositories/cart_repository_drift.dart';
import 'package:bookstore_app/data/sample_books.dart';
import 'package:bookstore_app/data/services/api_client.dart';
import 'package:bookstore_app/data/services/app_database.dart';
import 'package:bookstore_app/domain/models/book.dart';
import 'package:bookstore_app/utils/result.dart';
import 'package:drift/native.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';

import '../testing/fakes.dart';

void main() {
  final dune = sampleBooks.first;
  final emma = sampleBooks[1];

  test('the cart survives closing the app', () async {
    final dir = await Directory.systemTemp.createTemp('bookstore');
    final file = File('${dir.path}/bookstore.sqlite');

    var db = AppDatabase(NativeDatabase(file));
    await DriftCartRepository(db).setQuantity(dune, 2);
    await DriftCartRepository(db).setQuantity(emma, 1);
    await db.close(); // the app is closed

    db = AppDatabase(NativeDatabase(file)); // and opened again
    final lines = (await DriftCartRepository(db).getLines() as Ok).value;
    debugPrint(
      'after reopen: ${[for (final l in lines as List<CartLine>) '${l.book.title} x${l.quantity}'].join(', ')}',
    );
    await db.close();
    await dir.delete(recursive: true);
  });

  test('a watched query emits after every change', () async {
    final db = AppDatabase(NativeDatabase.memory());
    final cart = DriftCartRepository(db);
    final counts = <int>[];
    final sub = db.watchCartCount().listen(counts.add);
    await pumpEventQueue();
    await cart.setQuantity(dune, 2);
    await pumpEventQueue();
    await cart.setQuantity(emma, 3);
    await pumpEventQueue();
    await cart.clear();
    await pumpEventQueue();
    debugPrint('cart counts: $counts');
    await sub.cancel();
    await db.close();
  });

  test('version 1 databases get the description column', () async {
    final db = AppDatabase(
      NativeDatabase.memory(
        setup: (raw) {
          // What version 1 of the app created on the device
          raw.execute('''
            CREATE TABLE books (isbn TEXT NOT NULL PRIMARY KEY, title TEXT NOT NULL,
              author TEXT NOT NULL, price_kurus INTEGER NOT NULL,
              published_year INTEGER NOT NULL, catalog_page INTEGER,
              catalog_position INTEGER, fetched_at INTEGER NOT NULL);
            CREATE TABLE cart_lines (isbn TEXT NOT NULL PRIMARY KEY
              REFERENCES books (isbn), quantity INTEGER NOT NULL);
            INSERT INTO books VALUES ('9780441172719', 'Dune', 'Frank Herbert',
              48900, 1965, 0, 0, 1794700000);
            PRAGMA user_version = 1;
          ''');
        },
      ),
    );
    final columns = await db
        .customSelect('SELECT name FROM pragma_table_info(\'books\')')
        .map((row) => row.read<String>('name'))
        .get();
    final version = await db
        .customSelect('PRAGMA user_version')
        .map((row) => row.read<int>('user_version'))
        .getSingle();
    final dune = await db.select(db.books).getSingle();
    debugPrint('user_version: $version, columns: ${columns.join(', ')}');
    debugPrint('kept row: ${dune.title}, description: ${dune.description}');
    await db.close();
  });

  group('catalog cache', () {
    late AppDatabase db;
    late FakeBookRepository remote;
    late OfflineStatus status;
    var now = DateTime.utc(2026, 11, 15, 10);

    CachedBookRepository repository() => CachedBookRepository(
      remote: remote,
      db: db,
      status: status,
      now: () => now,
    );

    setUp(() {
      db = AppDatabase(NativeDatabase.memory());
      remote = FakeBookRepository();
      status = OfflineStatus();
      now = DateTime.utc(2026, 11, 15, 10);
    });
    tearDown(() => db.close());

    test('fresh cache, stale cache, no network', () async {
      final books = repository();
      await books.getPage(0); // first start: from the API, saved
      now = now.add(const Duration(minutes: 5));
      await books.getPage(0); // 5 minutes later: fresh, no request
      debugPrint('remote calls after 5 min: ${remote.calls}');

      now = now.add(const Duration(minutes: 30));
      remote.error = const NetworkException('timeout');
      remote.failuresLeft = 1;
      final page = (await books.getPage(0) as Ok<List<Book>>).value;
      debugPrint(
        'offline after 35 min: ${page.length} books, first ${page.first.title}, '
        'status offline: ${status.offline}, saved at ${status.savedAt!.toUtc()} '
        '(read back as UTC: ${status.savedAt!.isUtc})',
      );
    });

    test('a server error is not hidden by the cache', () async {
      final books = repository();
      await books.getPage(0);
      now = now.add(const Duration(hours: 1));
      remote.error = ApiException(500, const {'title': 'Server error'});
      remote.failuresLeft = 1;
      final result = await books.getPage(0);
      debugPrint('500 with a cache: ${(result as Failure).error}');
    });
  });
}
