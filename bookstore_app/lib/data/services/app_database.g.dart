// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'app_database.dart';

// ignore_for_file: type=lint
class $BooksTable extends Books with TableInfo<$BooksTable, CachedBook> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $BooksTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _isbnMeta = const VerificationMeta('isbn');
  @override
  late final GeneratedColumn<String> isbn = GeneratedColumn<String>(
    'isbn',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _titleMeta = const VerificationMeta('title');
  @override
  late final GeneratedColumn<String> title = GeneratedColumn<String>(
    'title',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _authorMeta = const VerificationMeta('author');
  @override
  late final GeneratedColumn<String> author = GeneratedColumn<String>(
    'author',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _priceKurusMeta = const VerificationMeta(
    'priceKurus',
  );
  @override
  late final GeneratedColumn<int> priceKurus = GeneratedColumn<int>(
    'price_kurus',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _publishedYearMeta = const VerificationMeta(
    'publishedYear',
  );
  @override
  late final GeneratedColumn<int> publishedYear = GeneratedColumn<int>(
    'published_year',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
  );
  static const VerificationMeta _descriptionMeta = const VerificationMeta(
    'description',
  );
  @override
  late final GeneratedColumn<String> description = GeneratedColumn<String>(
    'description',
    aliasedName,
    true,
    type: DriftSqlType.string,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _catalogPageMeta = const VerificationMeta(
    'catalogPage',
  );
  @override
  late final GeneratedColumn<int> catalogPage = GeneratedColumn<int>(
    'catalog_page',
    aliasedName,
    true,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _catalogPositionMeta = const VerificationMeta(
    'catalogPosition',
  );
  @override
  late final GeneratedColumn<int> catalogPosition = GeneratedColumn<int>(
    'catalog_position',
    aliasedName,
    true,
    type: DriftSqlType.int,
    requiredDuringInsert: false,
  );
  static const VerificationMeta _fetchedAtMeta = const VerificationMeta(
    'fetchedAt',
  );
  @override
  late final GeneratedColumn<DateTime> fetchedAt = GeneratedColumn<DateTime>(
    'fetched_at',
    aliasedName,
    false,
    type: DriftSqlType.dateTime,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [
    isbn,
    title,
    author,
    priceKurus,
    publishedYear,
    description,
    catalogPage,
    catalogPosition,
    fetchedAt,
  ];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'books';
  @override
  VerificationContext validateIntegrity(
    Insertable<CachedBook> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('isbn')) {
      context.handle(
        _isbnMeta,
        isbn.isAcceptableOrUnknown(data['isbn']!, _isbnMeta),
      );
    } else if (isInserting) {
      context.missing(_isbnMeta);
    }
    if (data.containsKey('title')) {
      context.handle(
        _titleMeta,
        title.isAcceptableOrUnknown(data['title']!, _titleMeta),
      );
    } else if (isInserting) {
      context.missing(_titleMeta);
    }
    if (data.containsKey('author')) {
      context.handle(
        _authorMeta,
        author.isAcceptableOrUnknown(data['author']!, _authorMeta),
      );
    } else if (isInserting) {
      context.missing(_authorMeta);
    }
    if (data.containsKey('price_kurus')) {
      context.handle(
        _priceKurusMeta,
        priceKurus.isAcceptableOrUnknown(data['price_kurus']!, _priceKurusMeta),
      );
    } else if (isInserting) {
      context.missing(_priceKurusMeta);
    }
    if (data.containsKey('published_year')) {
      context.handle(
        _publishedYearMeta,
        publishedYear.isAcceptableOrUnknown(
          data['published_year']!,
          _publishedYearMeta,
        ),
      );
    } else if (isInserting) {
      context.missing(_publishedYearMeta);
    }
    if (data.containsKey('description')) {
      context.handle(
        _descriptionMeta,
        description.isAcceptableOrUnknown(
          data['description']!,
          _descriptionMeta,
        ),
      );
    }
    if (data.containsKey('catalog_page')) {
      context.handle(
        _catalogPageMeta,
        catalogPage.isAcceptableOrUnknown(
          data['catalog_page']!,
          _catalogPageMeta,
        ),
      );
    }
    if (data.containsKey('catalog_position')) {
      context.handle(
        _catalogPositionMeta,
        catalogPosition.isAcceptableOrUnknown(
          data['catalog_position']!,
          _catalogPositionMeta,
        ),
      );
    }
    if (data.containsKey('fetched_at')) {
      context.handle(
        _fetchedAtMeta,
        fetchedAt.isAcceptableOrUnknown(data['fetched_at']!, _fetchedAtMeta),
      );
    } else if (isInserting) {
      context.missing(_fetchedAtMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {isbn};
  @override
  CachedBook map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return CachedBook(
      isbn: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}isbn'],
      )!,
      title: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}title'],
      )!,
      author: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}author'],
      )!,
      priceKurus: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}price_kurus'],
      )!,
      publishedYear: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}published_year'],
      )!,
      description: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}description'],
      ),
      catalogPage: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}catalog_page'],
      ),
      catalogPosition: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}catalog_position'],
      ),
      fetchedAt: attachedDatabase.typeMapping.read(
        DriftSqlType.dateTime,
        data['${effectivePrefix}fetched_at'],
      )!,
    );
  }

  @override
  $BooksTable createAlias(String alias) {
    return $BooksTable(attachedDatabase, alias);
  }
}

class CachedBook extends DataClass implements Insertable<CachedBook> {
  final String isbn;
  final String title;
  final String author;
  final int priceKurus;
  final int publishedYear;
  final String? description;
  final int? catalogPage;
  final int? catalogPosition;
  final DateTime fetchedAt;
  const CachedBook({
    required this.isbn,
    required this.title,
    required this.author,
    required this.priceKurus,
    required this.publishedYear,
    this.description,
    this.catalogPage,
    this.catalogPosition,
    required this.fetchedAt,
  });
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['isbn'] = Variable<String>(isbn);
    map['title'] = Variable<String>(title);
    map['author'] = Variable<String>(author);
    map['price_kurus'] = Variable<int>(priceKurus);
    map['published_year'] = Variable<int>(publishedYear);
    if (!nullToAbsent || description != null) {
      map['description'] = Variable<String>(description);
    }
    if (!nullToAbsent || catalogPage != null) {
      map['catalog_page'] = Variable<int>(catalogPage);
    }
    if (!nullToAbsent || catalogPosition != null) {
      map['catalog_position'] = Variable<int>(catalogPosition);
    }
    map['fetched_at'] = Variable<DateTime>(fetchedAt);
    return map;
  }

  BooksCompanion toCompanion(bool nullToAbsent) {
    return BooksCompanion(
      isbn: Value(isbn),
      title: Value(title),
      author: Value(author),
      priceKurus: Value(priceKurus),
      publishedYear: Value(publishedYear),
      description: description == null && nullToAbsent
          ? const Value.absent()
          : Value(description),
      catalogPage: catalogPage == null && nullToAbsent
          ? const Value.absent()
          : Value(catalogPage),
      catalogPosition: catalogPosition == null && nullToAbsent
          ? const Value.absent()
          : Value(catalogPosition),
      fetchedAt: Value(fetchedAt),
    );
  }

  factory CachedBook.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return CachedBook(
      isbn: serializer.fromJson<String>(json['isbn']),
      title: serializer.fromJson<String>(json['title']),
      author: serializer.fromJson<String>(json['author']),
      priceKurus: serializer.fromJson<int>(json['priceKurus']),
      publishedYear: serializer.fromJson<int>(json['publishedYear']),
      description: serializer.fromJson<String?>(json['description']),
      catalogPage: serializer.fromJson<int?>(json['catalogPage']),
      catalogPosition: serializer.fromJson<int?>(json['catalogPosition']),
      fetchedAt: serializer.fromJson<DateTime>(json['fetchedAt']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'isbn': serializer.toJson<String>(isbn),
      'title': serializer.toJson<String>(title),
      'author': serializer.toJson<String>(author),
      'priceKurus': serializer.toJson<int>(priceKurus),
      'publishedYear': serializer.toJson<int>(publishedYear),
      'description': serializer.toJson<String?>(description),
      'catalogPage': serializer.toJson<int?>(catalogPage),
      'catalogPosition': serializer.toJson<int?>(catalogPosition),
      'fetchedAt': serializer.toJson<DateTime>(fetchedAt),
    };
  }

  CachedBook copyWith({
    String? isbn,
    String? title,
    String? author,
    int? priceKurus,
    int? publishedYear,
    Value<String?> description = const Value.absent(),
    Value<int?> catalogPage = const Value.absent(),
    Value<int?> catalogPosition = const Value.absent(),
    DateTime? fetchedAt,
  }) => CachedBook(
    isbn: isbn ?? this.isbn,
    title: title ?? this.title,
    author: author ?? this.author,
    priceKurus: priceKurus ?? this.priceKurus,
    publishedYear: publishedYear ?? this.publishedYear,
    description: description.present ? description.value : this.description,
    catalogPage: catalogPage.present ? catalogPage.value : this.catalogPage,
    catalogPosition: catalogPosition.present
        ? catalogPosition.value
        : this.catalogPosition,
    fetchedAt: fetchedAt ?? this.fetchedAt,
  );
  CachedBook copyWithCompanion(BooksCompanion data) {
    return CachedBook(
      isbn: data.isbn.present ? data.isbn.value : this.isbn,
      title: data.title.present ? data.title.value : this.title,
      author: data.author.present ? data.author.value : this.author,
      priceKurus: data.priceKurus.present
          ? data.priceKurus.value
          : this.priceKurus,
      publishedYear: data.publishedYear.present
          ? data.publishedYear.value
          : this.publishedYear,
      description: data.description.present
          ? data.description.value
          : this.description,
      catalogPage: data.catalogPage.present
          ? data.catalogPage.value
          : this.catalogPage,
      catalogPosition: data.catalogPosition.present
          ? data.catalogPosition.value
          : this.catalogPosition,
      fetchedAt: data.fetchedAt.present ? data.fetchedAt.value : this.fetchedAt,
    );
  }

  @override
  String toString() {
    return (StringBuffer('CachedBook(')
          ..write('isbn: $isbn, ')
          ..write('title: $title, ')
          ..write('author: $author, ')
          ..write('priceKurus: $priceKurus, ')
          ..write('publishedYear: $publishedYear, ')
          ..write('description: $description, ')
          ..write('catalogPage: $catalogPage, ')
          ..write('catalogPosition: $catalogPosition, ')
          ..write('fetchedAt: $fetchedAt')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(
    isbn,
    title,
    author,
    priceKurus,
    publishedYear,
    description,
    catalogPage,
    catalogPosition,
    fetchedAt,
  );
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is CachedBook &&
          other.isbn == this.isbn &&
          other.title == this.title &&
          other.author == this.author &&
          other.priceKurus == this.priceKurus &&
          other.publishedYear == this.publishedYear &&
          other.description == this.description &&
          other.catalogPage == this.catalogPage &&
          other.catalogPosition == this.catalogPosition &&
          other.fetchedAt == this.fetchedAt);
}

class BooksCompanion extends UpdateCompanion<CachedBook> {
  final Value<String> isbn;
  final Value<String> title;
  final Value<String> author;
  final Value<int> priceKurus;
  final Value<int> publishedYear;
  final Value<String?> description;
  final Value<int?> catalogPage;
  final Value<int?> catalogPosition;
  final Value<DateTime> fetchedAt;
  final Value<int> rowid;
  const BooksCompanion({
    this.isbn = const Value.absent(),
    this.title = const Value.absent(),
    this.author = const Value.absent(),
    this.priceKurus = const Value.absent(),
    this.publishedYear = const Value.absent(),
    this.description = const Value.absent(),
    this.catalogPage = const Value.absent(),
    this.catalogPosition = const Value.absent(),
    this.fetchedAt = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  BooksCompanion.insert({
    required String isbn,
    required String title,
    required String author,
    required int priceKurus,
    required int publishedYear,
    this.description = const Value.absent(),
    this.catalogPage = const Value.absent(),
    this.catalogPosition = const Value.absent(),
    required DateTime fetchedAt,
    this.rowid = const Value.absent(),
  }) : isbn = Value(isbn),
       title = Value(title),
       author = Value(author),
       priceKurus = Value(priceKurus),
       publishedYear = Value(publishedYear),
       fetchedAt = Value(fetchedAt);
  static Insertable<CachedBook> custom({
    Expression<String>? isbn,
    Expression<String>? title,
    Expression<String>? author,
    Expression<int>? priceKurus,
    Expression<int>? publishedYear,
    Expression<String>? description,
    Expression<int>? catalogPage,
    Expression<int>? catalogPosition,
    Expression<DateTime>? fetchedAt,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (isbn != null) 'isbn': isbn,
      if (title != null) 'title': title,
      if (author != null) 'author': author,
      if (priceKurus != null) 'price_kurus': priceKurus,
      if (publishedYear != null) 'published_year': publishedYear,
      if (description != null) 'description': description,
      if (catalogPage != null) 'catalog_page': catalogPage,
      if (catalogPosition != null) 'catalog_position': catalogPosition,
      if (fetchedAt != null) 'fetched_at': fetchedAt,
      if (rowid != null) 'rowid': rowid,
    });
  }

  BooksCompanion copyWith({
    Value<String>? isbn,
    Value<String>? title,
    Value<String>? author,
    Value<int>? priceKurus,
    Value<int>? publishedYear,
    Value<String?>? description,
    Value<int?>? catalogPage,
    Value<int?>? catalogPosition,
    Value<DateTime>? fetchedAt,
    Value<int>? rowid,
  }) {
    return BooksCompanion(
      isbn: isbn ?? this.isbn,
      title: title ?? this.title,
      author: author ?? this.author,
      priceKurus: priceKurus ?? this.priceKurus,
      publishedYear: publishedYear ?? this.publishedYear,
      description: description ?? this.description,
      catalogPage: catalogPage ?? this.catalogPage,
      catalogPosition: catalogPosition ?? this.catalogPosition,
      fetchedAt: fetchedAt ?? this.fetchedAt,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (isbn.present) {
      map['isbn'] = Variable<String>(isbn.value);
    }
    if (title.present) {
      map['title'] = Variable<String>(title.value);
    }
    if (author.present) {
      map['author'] = Variable<String>(author.value);
    }
    if (priceKurus.present) {
      map['price_kurus'] = Variable<int>(priceKurus.value);
    }
    if (publishedYear.present) {
      map['published_year'] = Variable<int>(publishedYear.value);
    }
    if (description.present) {
      map['description'] = Variable<String>(description.value);
    }
    if (catalogPage.present) {
      map['catalog_page'] = Variable<int>(catalogPage.value);
    }
    if (catalogPosition.present) {
      map['catalog_position'] = Variable<int>(catalogPosition.value);
    }
    if (fetchedAt.present) {
      map['fetched_at'] = Variable<DateTime>(fetchedAt.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('BooksCompanion(')
          ..write('isbn: $isbn, ')
          ..write('title: $title, ')
          ..write('author: $author, ')
          ..write('priceKurus: $priceKurus, ')
          ..write('publishedYear: $publishedYear, ')
          ..write('description: $description, ')
          ..write('catalogPage: $catalogPage, ')
          ..write('catalogPosition: $catalogPosition, ')
          ..write('fetchedAt: $fetchedAt, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

class $CartLinesTable extends CartLines
    with TableInfo<$CartLinesTable, CartLineRow> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $CartLinesTable(this.attachedDatabase, [this._alias]);
  static const VerificationMeta _isbnMeta = const VerificationMeta('isbn');
  @override
  late final GeneratedColumn<String> isbn = GeneratedColumn<String>(
    'isbn',
    aliasedName,
    false,
    type: DriftSqlType.string,
    requiredDuringInsert: true,
    defaultConstraints: GeneratedColumn.constraintIsAlways(
      'REFERENCES books (isbn)',
    ),
  );
  static const VerificationMeta _quantityMeta = const VerificationMeta(
    'quantity',
  );
  @override
  late final GeneratedColumn<int> quantity = GeneratedColumn<int>(
    'quantity',
    aliasedName,
    false,
    type: DriftSqlType.int,
    requiredDuringInsert: true,
  );
  @override
  List<GeneratedColumn> get $columns => [isbn, quantity];
  @override
  String get aliasedName => _alias ?? actualTableName;
  @override
  String get actualTableName => $name;
  static const String $name = 'cart_lines';
  @override
  VerificationContext validateIntegrity(
    Insertable<CartLineRow> instance, {
    bool isInserting = false,
  }) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('isbn')) {
      context.handle(
        _isbnMeta,
        isbn.isAcceptableOrUnknown(data['isbn']!, _isbnMeta),
      );
    } else if (isInserting) {
      context.missing(_isbnMeta);
    }
    if (data.containsKey('quantity')) {
      context.handle(
        _quantityMeta,
        quantity.isAcceptableOrUnknown(data['quantity']!, _quantityMeta),
      );
    } else if (isInserting) {
      context.missing(_quantityMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {isbn};
  @override
  CartLineRow map(Map<String, dynamic> data, {String? tablePrefix}) {
    final effectivePrefix = tablePrefix != null ? '$tablePrefix.' : '';
    return CartLineRow(
      isbn: attachedDatabase.typeMapping.read(
        DriftSqlType.string,
        data['${effectivePrefix}isbn'],
      )!,
      quantity: attachedDatabase.typeMapping.read(
        DriftSqlType.int,
        data['${effectivePrefix}quantity'],
      )!,
    );
  }

  @override
  $CartLinesTable createAlias(String alias) {
    return $CartLinesTable(attachedDatabase, alias);
  }
}

class CartLineRow extends DataClass implements Insertable<CartLineRow> {
  final String isbn;
  final int quantity;
  const CartLineRow({required this.isbn, required this.quantity});
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['isbn'] = Variable<String>(isbn);
    map['quantity'] = Variable<int>(quantity);
    return map;
  }

  CartLinesCompanion toCompanion(bool nullToAbsent) {
    return CartLinesCompanion(isbn: Value(isbn), quantity: Value(quantity));
  }

  factory CartLineRow.fromJson(
    Map<String, dynamic> json, {
    ValueSerializer? serializer,
  }) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return CartLineRow(
      isbn: serializer.fromJson<String>(json['isbn']),
      quantity: serializer.fromJson<int>(json['quantity']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'isbn': serializer.toJson<String>(isbn),
      'quantity': serializer.toJson<int>(quantity),
    };
  }

  CartLineRow copyWith({String? isbn, int? quantity}) =>
      CartLineRow(isbn: isbn ?? this.isbn, quantity: quantity ?? this.quantity);
  CartLineRow copyWithCompanion(CartLinesCompanion data) {
    return CartLineRow(
      isbn: data.isbn.present ? data.isbn.value : this.isbn,
      quantity: data.quantity.present ? data.quantity.value : this.quantity,
    );
  }

  @override
  String toString() {
    return (StringBuffer('CartLineRow(')
          ..write('isbn: $isbn, ')
          ..write('quantity: $quantity')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(isbn, quantity);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is CartLineRow &&
          other.isbn == this.isbn &&
          other.quantity == this.quantity);
}

class CartLinesCompanion extends UpdateCompanion<CartLineRow> {
  final Value<String> isbn;
  final Value<int> quantity;
  final Value<int> rowid;
  const CartLinesCompanion({
    this.isbn = const Value.absent(),
    this.quantity = const Value.absent(),
    this.rowid = const Value.absent(),
  });
  CartLinesCompanion.insert({
    required String isbn,
    required int quantity,
    this.rowid = const Value.absent(),
  }) : isbn = Value(isbn),
       quantity = Value(quantity);
  static Insertable<CartLineRow> custom({
    Expression<String>? isbn,
    Expression<int>? quantity,
    Expression<int>? rowid,
  }) {
    return RawValuesInsertable({
      if (isbn != null) 'isbn': isbn,
      if (quantity != null) 'quantity': quantity,
      if (rowid != null) 'rowid': rowid,
    });
  }

  CartLinesCompanion copyWith({
    Value<String>? isbn,
    Value<int>? quantity,
    Value<int>? rowid,
  }) {
    return CartLinesCompanion(
      isbn: isbn ?? this.isbn,
      quantity: quantity ?? this.quantity,
      rowid: rowid ?? this.rowid,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (isbn.present) {
      map['isbn'] = Variable<String>(isbn.value);
    }
    if (quantity.present) {
      map['quantity'] = Variable<int>(quantity.value);
    }
    if (rowid.present) {
      map['rowid'] = Variable<int>(rowid.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('CartLinesCompanion(')
          ..write('isbn: $isbn, ')
          ..write('quantity: $quantity, ')
          ..write('rowid: $rowid')
          ..write(')'))
        .toString();
  }
}

abstract class _$AppDatabase extends GeneratedDatabase {
  _$AppDatabase(QueryExecutor e) : super(e);
  $AppDatabaseManager get managers => $AppDatabaseManager(this);
  late final $BooksTable books = $BooksTable(this);
  late final $CartLinesTable cartLines = $CartLinesTable(this);
  @override
  Iterable<TableInfo<Table, Object?>> get allTables =>
      allSchemaEntities.whereType<TableInfo<Table, Object?>>();
  @override
  List<DatabaseSchemaEntity> get allSchemaEntities => [books, cartLines];
}

typedef $$BooksTableCreateCompanionBuilder = BooksCompanion Function({
  required String isbn,
  required String title,
  required String author,
  required int priceKurus,
  required int publishedYear,
  Value<String?> description,
  Value<int?> catalogPage,
  Value<int?> catalogPosition,
  required DateTime fetchedAt,
  Value<int> rowid,
});
typedef $$BooksTableUpdateCompanionBuilder = BooksCompanion Function({
  Value<String> isbn,
  Value<String> title,
  Value<String> author,
  Value<int> priceKurus,
  Value<int> publishedYear,
  Value<String?> description,
  Value<int?> catalogPage,
  Value<int?> catalogPosition,
  Value<DateTime> fetchedAt,
  Value<int> rowid,
});

final class $$BooksTableReferences
    extends BaseReferences<_$AppDatabase, $BooksTable, CachedBook> {
  $$BooksTableReferences(super.$_db, super.$_table, super.$_typedResult);

  static MultiTypedResultKey<$CartLinesTable, List<CartLineRow>>
  _cartLinesRefsTable(_$AppDatabase db) => MultiTypedResultKey.fromTable(
    db.cartLines,
    aliasName: 'books__isbn__cart_lines__isbn',
  );

  $$CartLinesTableProcessedTableManager get cartLinesRefs {
    final manager = $$CartLinesTableTableManager(
      $_db,
      $_db.cartLines,
    ).filter((f) => f.isbn.isbn.sqlEquals($_itemColumn<String>('isbn')!));

    final cache = $_typedResult.readTableOrNull(_cartLinesRefsTable($_db));
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: cache),
    );
  }
}

class $$BooksTableFilterComposer extends Composer<_$AppDatabase, $BooksTable> {
  $$BooksTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<String> get isbn => $composableBuilder(
    column: $table.isbn,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get title => $composableBuilder(
    column: $table.title,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get author => $composableBuilder(
    column: $table.author,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get priceKurus => $composableBuilder(
    column: $table.priceKurus,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get publishedYear => $composableBuilder(
    column: $table.publishedYear,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<String> get description => $composableBuilder(
    column: $table.description,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get catalogPage => $composableBuilder(
    column: $table.catalogPage,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<int> get catalogPosition => $composableBuilder(
    column: $table.catalogPosition,
    builder: (column) => ColumnFilters(column),
  );

  ColumnFilters<DateTime> get fetchedAt => $composableBuilder(
    column: $table.fetchedAt,
    builder: (column) => ColumnFilters(column),
  );

  Expression<bool> cartLinesRefs(
    Expression<bool> Function($$CartLinesTableFilterComposer f) f,
  ) {
    final $$CartLinesTableFilterComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.isbn,
      referencedTable: $db.cartLines,
      getReferencedColumn: (t) => t.isbn,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$CartLinesTableFilterComposer(
            $db: $db,
            $table: $db.cartLines,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return f(composer);
  }
}

class $$BooksTableOrderingComposer
    extends Composer<_$AppDatabase, $BooksTable> {
  $$BooksTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<String> get isbn => $composableBuilder(
    column: $table.isbn,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get title => $composableBuilder(
    column: $table.title,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get author => $composableBuilder(
    column: $table.author,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get priceKurus => $composableBuilder(
    column: $table.priceKurus,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get publishedYear => $composableBuilder(
    column: $table.publishedYear,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<String> get description => $composableBuilder(
    column: $table.description,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get catalogPage => $composableBuilder(
    column: $table.catalogPage,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<int> get catalogPosition => $composableBuilder(
    column: $table.catalogPosition,
    builder: (column) => ColumnOrderings(column),
  );

  ColumnOrderings<DateTime> get fetchedAt => $composableBuilder(
    column: $table.fetchedAt,
    builder: (column) => ColumnOrderings(column),
  );
}

class $$BooksTableAnnotationComposer
    extends Composer<_$AppDatabase, $BooksTable> {
  $$BooksTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<String> get isbn =>
      $composableBuilder(column: $table.isbn, builder: (column) => column);

  GeneratedColumn<String> get title =>
      $composableBuilder(column: $table.title, builder: (column) => column);

  GeneratedColumn<String> get author =>
      $composableBuilder(column: $table.author, builder: (column) => column);

  GeneratedColumn<int> get priceKurus => $composableBuilder(
    column: $table.priceKurus,
    builder: (column) => column,
  );

  GeneratedColumn<int> get publishedYear => $composableBuilder(
    column: $table.publishedYear,
    builder: (column) => column,
  );

  GeneratedColumn<String> get description => $composableBuilder(
    column: $table.description,
    builder: (column) => column,
  );

  GeneratedColumn<int> get catalogPage => $composableBuilder(
    column: $table.catalogPage,
    builder: (column) => column,
  );

  GeneratedColumn<int> get catalogPosition => $composableBuilder(
    column: $table.catalogPosition,
    builder: (column) => column,
  );

  GeneratedColumn<DateTime> get fetchedAt =>
      $composableBuilder(column: $table.fetchedAt, builder: (column) => column);

  Expression<T> cartLinesRefs<T extends Object>(
    Expression<T> Function($$CartLinesTableAnnotationComposer a) f,
  ) {
    final $$CartLinesTableAnnotationComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.isbn,
      referencedTable: $db.cartLines,
      getReferencedColumn: (t) => t.isbn,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$CartLinesTableAnnotationComposer(
            $db: $db,
            $table: $db.cartLines,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return f(composer);
  }
}

class $$BooksTableTableManager
    extends
        RootTableManager<
          _$AppDatabase,
          $BooksTable,
          CachedBook,
          $$BooksTableFilterComposer,
          $$BooksTableOrderingComposer,
          $$BooksTableAnnotationComposer,
          $$BooksTableCreateCompanionBuilder,
          $$BooksTableUpdateCompanionBuilder,
          (CachedBook, $$BooksTableReferences),
          CachedBook,
          PrefetchHooks Function({bool cartLinesRefs})
        > {
  $$BooksTableTableManager(_$AppDatabase db, $BooksTable table)
    : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$BooksTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$BooksTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$BooksTableAnnotationComposer($db: db, $table: table),
          updateCompanionCallback:
              ({
                Value<String> isbn = const Value.absent(),
                Value<String> title = const Value.absent(),
                Value<String> author = const Value.absent(),
                Value<int> priceKurus = const Value.absent(),
                Value<int> publishedYear = const Value.absent(),
                Value<String?> description = const Value.absent(),
                Value<int?> catalogPage = const Value.absent(),
                Value<int?> catalogPosition = const Value.absent(),
                Value<DateTime> fetchedAt = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => BooksCompanion(
                isbn: isbn,
                title: title,
                author: author,
                priceKurus: priceKurus,
                publishedYear: publishedYear,
                description: description,
                catalogPage: catalogPage,
                catalogPosition: catalogPosition,
                fetchedAt: fetchedAt,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String isbn,
                required String title,
                required String author,
                required int priceKurus,
                required int publishedYear,
                Value<String?> description = const Value.absent(),
                Value<int?> catalogPage = const Value.absent(),
                Value<int?> catalogPosition = const Value.absent(),
                required DateTime fetchedAt,
                Value<int> rowid = const Value.absent(),
              }) => BooksCompanion.insert(
                isbn: isbn,
                title: title,
                author: author,
                priceKurus: priceKurus,
                publishedYear: publishedYear,
                description: description,
                catalogPage: catalogPage,
                catalogPosition: catalogPosition,
                fetchedAt: fetchedAt,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable<$BooksTable, CachedBook>(table),
                  $$BooksTableReferences(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: ({cartLinesRefs = false}) {
            return PrefetchHooks(
              db: db,
              explicitlyWatchedTables: [if (cartLinesRefs) db.cartLines],
              addJoins: null,
              getPrefetchedDataCallback: (items) async {
                return [
                  if (cartLinesRefs)
                    await $_getPrefetchedDataByColumns<
                      CachedBook,
                      $BooksTable,
                      CartLineRow,
                      $CartLinesTable
                    >(
                      currentTable: table,
                      referencedTable: $$BooksTableReferences
                          ._cartLinesRefsTable(db),
                      managerFromTypedResult: (p0) =>
                          $$BooksTableReferences(db, table, p0).cartLinesRefs,
                      currentColumn: 'isbn',
                      referencedColumn: 'isbn',
                      typedResults: items,
                    ),
                ];
              },
            );
          },
        ),
      );
}

typedef $$BooksTableProcessedTableManager =
    ProcessedTableManager<
      _$AppDatabase,
      $BooksTable,
      CachedBook,
      $$BooksTableFilterComposer,
      $$BooksTableOrderingComposer,
      $$BooksTableAnnotationComposer,
      $$BooksTableCreateCompanionBuilder,
      $$BooksTableUpdateCompanionBuilder,
      (CachedBook, $$BooksTableReferences),
      CachedBook,
      PrefetchHooks Function({bool cartLinesRefs})
    >;
typedef $$CartLinesTableCreateCompanionBuilder = CartLinesCompanion Function({
  required String isbn,
  required int quantity,
  Value<int> rowid,
});
typedef $$CartLinesTableUpdateCompanionBuilder = CartLinesCompanion Function({
  Value<String> isbn,
  Value<int> quantity,
  Value<int> rowid,
});

final class $$CartLinesTableReferences
    extends BaseReferences<_$AppDatabase, $CartLinesTable, CartLineRow> {
  $$CartLinesTableReferences(super.$_db, super.$_table, super.$_typedResult);

  static $BooksTable _isbnTable(_$AppDatabase db) =>
      db.books.createAlias('cart_lines__isbn__books__isbn');

  $$BooksTableProcessedTableManager get isbn {
    final $_column = $_itemColumn<String>('isbn')!;

    final manager = $$BooksTableTableManager(
      $_db,
      $_db.books,
    ).filter((f) => f.isbn.sqlEquals($_column));
    final item = $_typedResult.readTableOrNull(_isbnTable($_db));
    if (item == null) return manager;
    return ProcessedTableManager(
      manager.$state.copyWith(prefetchedData: [item]),
    );
  }
}

class $$CartLinesTableFilterComposer
    extends Composer<_$AppDatabase, $CartLinesTable> {
  $$CartLinesTableFilterComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnFilters<int> get quantity => $composableBuilder(
    column: $table.quantity,
    builder: (column) => ColumnFilters(column),
  );

  $$BooksTableFilterComposer get isbn {
    final $$BooksTableFilterComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.isbn,
      referencedTable: $db.books,
      getReferencedColumn: (t) => t.isbn,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$BooksTableFilterComposer(
            $db: $db,
            $table: $db.books,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }
}

class $$CartLinesTableOrderingComposer
    extends Composer<_$AppDatabase, $CartLinesTable> {
  $$CartLinesTableOrderingComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  ColumnOrderings<int> get quantity => $composableBuilder(
    column: $table.quantity,
    builder: (column) => ColumnOrderings(column),
  );

  $$BooksTableOrderingComposer get isbn {
    final $$BooksTableOrderingComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.isbn,
      referencedTable: $db.books,
      getReferencedColumn: (t) => t.isbn,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$BooksTableOrderingComposer(
            $db: $db,
            $table: $db.books,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }
}

class $$CartLinesTableAnnotationComposer
    extends Composer<_$AppDatabase, $CartLinesTable> {
  $$CartLinesTableAnnotationComposer({
    required super.$db,
    required super.$table,
    super.joinBuilder,
    super.$addJoinBuilderToRootComposer,
    super.$removeJoinBuilderFromRootComposer,
  });
  GeneratedColumn<int> get quantity =>
      $composableBuilder(column: $table.quantity, builder: (column) => column);

  $$BooksTableAnnotationComposer get isbn {
    final $$BooksTableAnnotationComposer composer = $composerBuilder(
      composer: this,
      getCurrentColumn: (t) => t.isbn,
      referencedTable: $db.books,
      getReferencedColumn: (t) => t.isbn,
      builder:
          (
            joinBuilder, {
            $addJoinBuilderToRootComposer,
            $removeJoinBuilderFromRootComposer,
          }) => $$BooksTableAnnotationComposer(
            $db: $db,
            $table: $db.books,
            $addJoinBuilderToRootComposer: $addJoinBuilderToRootComposer,
            joinBuilder: joinBuilder,
            $removeJoinBuilderFromRootComposer:
                $removeJoinBuilderFromRootComposer,
          ),
    );
    return composer;
  }
}

class $$CartLinesTableTableManager
    extends
        RootTableManager<
          _$AppDatabase,
          $CartLinesTable,
          CartLineRow,
          $$CartLinesTableFilterComposer,
          $$CartLinesTableOrderingComposer,
          $$CartLinesTableAnnotationComposer,
          $$CartLinesTableCreateCompanionBuilder,
          $$CartLinesTableUpdateCompanionBuilder,
          (CartLineRow, $$CartLinesTableReferences),
          CartLineRow,
          PrefetchHooks Function({bool isbn})
        > {
  $$CartLinesTableTableManager(_$AppDatabase db, $CartLinesTable table)
    : super(
        TableManagerState(
          db: db,
          table: table,
          createFilteringComposer: () =>
              $$CartLinesTableFilterComposer($db: db, $table: table),
          createOrderingComposer: () =>
              $$CartLinesTableOrderingComposer($db: db, $table: table),
          createComputedFieldComposer: () =>
              $$CartLinesTableAnnotationComposer($db: db, $table: table),
          updateCompanionCallback:
              ({
                Value<String> isbn = const Value.absent(),
                Value<int> quantity = const Value.absent(),
                Value<int> rowid = const Value.absent(),
              }) => CartLinesCompanion(
                isbn: isbn,
                quantity: quantity,
                rowid: rowid,
              ),
          createCompanionCallback:
              ({
                required String isbn,
                required int quantity,
                Value<int> rowid = const Value.absent(),
              }) => CartLinesCompanion.insert(
                isbn: isbn,
                quantity: quantity,
                rowid: rowid,
              ),
          withReferenceMapper: (p0) => p0
              .map(
                (e) => (
                  e.readTable<$CartLinesTable, CartLineRow>(table),
                  $$CartLinesTableReferences(db, table, e),
                ),
              )
              .toList(),
          prefetchHooksCallback: ({isbn = false}) {
            return PrefetchHooks(
              db: db,
              explicitlyWatchedTables: [],
              addJoins:
                  <
                    T extends TableManagerState<
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic,
                      dynamic
                    >
                  >(state) {
                    if (isbn) {
                      state = state.withJoin(
                        currentTable: table,
                        currentColumn: table.isbn,
                        referencedTable: $$CartLinesTableReferences._isbnTable(
                          db,
                        ),
                        referencedColumn: $$CartLinesTableReferences
                            ._isbnTable(db)
                            .isbn,
                      ) as T;
                    }

                    return state;
                  },
              getPrefetchedDataCallback: (items) async {
                return [];
              },
            );
          },
        ),
      );
}

typedef $$CartLinesTableProcessedTableManager =
    ProcessedTableManager<
      _$AppDatabase,
      $CartLinesTable,
      CartLineRow,
      $$CartLinesTableFilterComposer,
      $$CartLinesTableOrderingComposer,
      $$CartLinesTableAnnotationComposer,
      $$CartLinesTableCreateCompanionBuilder,
      $$CartLinesTableUpdateCompanionBuilder,
      (CartLineRow, $$CartLinesTableReferences),
      CartLineRow,
      PrefetchHooks Function({bool isbn})
    >;

class $AppDatabaseManager {
  final _$AppDatabase _db;
  $AppDatabaseManager(this._db);
  $$BooksTableTableManager get books =>
      $$BooksTableTableManager(_db, _db.books);
  $$CartLinesTableTableManager get cartLines =>
      $$CartLinesTableTableManager(_db, _db.cartLines);
}
