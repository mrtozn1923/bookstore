/*
    Post 10 - full-text search. Run as bookstore_owner after schema.sql:
        psql -U bookstore_owner -d bookstore -f /lab/10-full-text.sql
    Adds twelve Turkish classics with short descriptions (written for this lab) next to the 40,000
    English demo titles, a weighted search_vector column, unaccent and pg_trgm (both trusted extensions).
*/
\set ON_ERROR_STOP on

/* 1. columns for search: language, author name copied into the row, description */
ALTER TABLE books
    ADD COLUMN language text NOT NULL DEFAULT 'en' CHECK (language IN ('en', 'tr')),
    ADD COLUMN author_name text,
    ADD COLUMN description text;

INSERT INTO authors (name, country)
VALUES ('Sabahattin Ali', 'TR'), ('Ahmet Hamdi Tanpınar', 'TR'), ('Oğuz Atay', 'TR'), ('Yaşar Kemal', 'TR'),
       ('Reşat Nuri Güntekin', 'TR'), ('Halide Edib Adıvar', 'TR'), ('Yusuf Atılgan', 'TR'),
       ('Yakup Kadri Karaosmanoğlu', 'TR'), ('Mehmet Rauf', 'TR');

INSERT INTO books (isbn, title, author_id, category, price, published_year, language, description)
SELECT '97897500' || lpad(t.n::text, 5, '0'), t.title, a.author_id, 'Classic', t.price, t.published_year, 'tr', t.description
FROM (VALUES
    (1, 'Kürk Mantolu Madonna', 'Sabahattin Ali', 1943, 12.50,
        'Ankara''da sıradan bir memur olan Raif Efendi''nin defterinden, gençliğinde Berlin''de bir ressamla yaşadığı aşk çıkar.'),
    (2, 'Kuyucaklı Yusuf', 'Sabahattin Ali', 1937, 11.00,
        'Ailesini kaybeden Yusuf''un Ege''de bir kasabada büyüyüşü, aşkı ve haksızlığa karşı mücadelesi.'),
    (3, 'Saatleri Ayarlama Enstitüsü', 'Ahmet Hamdi Tanpınar', 1961, 15.00,
        'Hayri İrdal''ın anlattığı, saatleri ayarlamak için kurulan bir kurumun etrafında bürokrasiyi ve modernleşmeyi hicveden roman.'),
    (4, 'Huzur', 'Ahmet Hamdi Tanpınar', 1949, 14.00,
        'İstanbul''da, İkinci Dünya Savaşı öncesinde Mümtaz ile Nuran''ın aşkı; şehir, musiki ve şiir üzerine bir roman.'),
    (5, 'Tutunamayanlar', 'Oğuz Atay', 1972, 18.00,
        'Turgut''un, intihar eden arkadaşı Selim''in hayatını onun bıraktığı yazılardan yeniden kurmaya çalışması.'),
    (6, 'İnce Memed', 'Yaşar Kemal', 1955, 16.00,
        'Toroslarda ağaların zulmüne karşı dağa çıkan genç Memed''in eşkıyalık hikâyesi.'),
    (7, 'Çalıkuşu', 'Reşat Nuri Güntekin', 1922, 13.00,
        'Feride''nin İstanbul''dan Anadolu''ya öğretmen olarak gidişi ve kasabalarda geçen yılları.'),
    (8, 'Sinekli Bakkal', 'Halide Edib Adıvar', 1936, 12.00,
        'Rabia''nın İstanbul''un bir mahallesinde, Meşrutiyet öncesinin baskı ortamında büyümesi.'),
    (9, 'Aylak Adam', 'Yusuf Atılgan', 1959, 10.50,
        'C. adındaki bir adamın İstanbul sokaklarında aradığı kadını ve kendini arayışı.'),
    (10, 'Yaban', 'Yakup Kadri Karaosmanoğlu', 1932, 11.50,
        'Savaşta kolunu kaybeden bir subayın Anadolu köyünde yabancı kalışı.'),
    (11, 'Kiralık Konak', 'Yakup Kadri Karaosmanoğlu', 1922, 11.50,
        'Bir İstanbul konağında üç kuşağın değerleri arasındaki çatışma.'),
    (12, 'Eylül', 'Mehmet Rauf', 1901, 9.50,
        'Bir evlilik içinde gelişen yasak bir duygunun yalnızlık ve suçlulukla anlatıldığı psikolojik roman.')
) AS t(n, title, author, published_year, price, description)
JOIN authors a ON a.name = t.author
ORDER BY t.n;

INSERT INTO stock_items (book_id, quantity_on_hand, quantity_reserved, updated_at)
SELECT book_id, 20, 0, now() FROM books WHERE language = 'tr';

UPDATE books b SET author_name = a.name FROM authors a WHERE a.author_id = b.author_id;

/* 2. Turkish search without diacritics: unaccent before the Turkish stemmer */
CREATE EXTENSION IF NOT EXISTS unaccent;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
DROP TEXT SEARCH CONFIGURATION IF EXISTS turkish_unaccent;
CREATE TEXT SEARCH CONFIGURATION turkish_unaccent (COPY = turkish);
ALTER TEXT SEARCH CONFIGURATION turkish_unaccent
    ALTER MAPPING FOR hword, hword_part, word WITH unaccent, turkish_stem;

/* 3. weighted search vector: title A, author B, description C; STORED, because 18 defaults to VIRTUAL */
ALTER TABLE books ADD COLUMN search_vector tsvector GENERATED ALWAYS AS (
    CASE language
        WHEN 'tr' THEN setweight(to_tsvector('turkish_unaccent', title), 'A')
                    || setweight(to_tsvector('turkish_unaccent', coalesce(author_name, '')), 'B')
                    || setweight(to_tsvector('turkish_unaccent', coalesce(description, '')), 'C')
        ELSE setweight(to_tsvector('english', title), 'A')
          || setweight(to_tsvector('english', coalesce(author_name, '')), 'B')
          || setweight(to_tsvector('english', coalesce(description, '')), 'C')
    END) STORED;
CREATE INDEX ix_books_search_vector ON books USING gin (search_vector);
CREATE INDEX ix_books_title_trgm ON books USING gin (title gin_trgm_ops);
ANALYZE books;

/* 4. guess first: which inflected forms find Çalıkuşu? */
SELECT q, to_tsvector('turkish', description) @@ websearch_to_tsquery('turkish', q) AS found
FROM books, unnest(ARRAY['istanbul', 'kasabalar', 'kasaba', 'öğretmen', 'öğretmenler', 'anadoluda']) AS q
WHERE title = 'Çalıkuşu';
SELECT to_tsvector('turkish', description) FROM books WHERE title = 'Çalıkuşu';

/* 5. the Turkish stemmer word by word */
SELECT w, to_tsvector('turkish', w) AS lexemes
FROM unnest(ARRAY['kasaba', 'kasabalarda', 'öğretmen', 'öğretmenler', 'roman', 'romanlar', 'aşk', 'aşkı',
                  'Efendi''nin']) AS w;
SELECT to_tsquery('simple', 'kasaba:*') @@ to_tsvector('simple', description) AS kasaba,
       to_tsquery('simple', 'anadolu:*') @@ to_tsvector('simple', description) AS anadolu
FROM books WHERE title = 'Çalıkuşu';

/* 6. unaccent */
SELECT w, to_tsvector('turkish', w) AS turkish, to_tsvector('turkish_unaccent', w) AS turkish_unaccent
FROM unnest(ARRAY['Çalıkuşu', 'calikusu', 'öğretmenler', 'ogretmenler']) AS w;

/* 7. weighted search, ranking, headline and paging */
\set q 'yusuf'
WITH q AS (SELECT websearch_to_tsquery('turkish_unaccent', :'q') || websearch_to_tsquery('english', :'q') AS query)
SELECT book_id, title, author_name, round(ts_rank(search_vector, query)::numeric, 4) AS rank
FROM books, q WHERE search_vector @@ query ORDER BY rank DESC, book_id LIMIT 5;
\set q 'istanbul'
WITH q AS (SELECT websearch_to_tsquery('turkish_unaccent', :'q') || websearch_to_tsquery('english', :'q') AS query)
SELECT title, ts_headline('turkish_unaccent', description, query, 'StartSel=[, StopSel=], MaxWords=8, MinWords=3') AS snippet
FROM books, q WHERE search_vector @@ query ORDER BY ts_rank(search_vector, query) DESC, book_id LIMIT 5;
\set q 'hobbit'
WITH q AS (SELECT websearch_to_tsquery('turkish_unaccent', :'q') || websearch_to_tsquery('english', :'q') AS query)
SELECT book_id, title, round(ts_rank(search_vector, query)::numeric, 4) AS rank, count(*) OVER () AS total
FROM books, q WHERE search_vector @@ query ORDER BY rank DESC, book_id LIMIT 3 OFFSET 20;
SELECT websearch_to_tsquery('turkish_unaccent', '"saatleri ayarlama" or calikusu -huzur');

SET max_parallel_workers_per_gather = 0;
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT book_id, title FROM books WHERE search_vector @@ websearch_to_tsquery('turkish_unaccent', 'calikusu');

/* 8. typo tolerance with pg_trgm */
SELECT title, similarity(title, 'kurk mantolu madona') AS sim
FROM books WHERE title % 'kurk mantolu madona' ORDER BY sim DESC LIMIT 5;
SHOW pg_trgm.similarity_threshold;
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT title FROM books WHERE title % 'kurk mantolu madona';
SELECT title, similarity(title, 'saatleri ayarlma') AS sim, word_similarity('ayarlma', title) AS word_sim
FROM books WHERE title % 'saatleri ayarlma' OR 'ayarlma' <% title;

/*
    Dotless i and the collation provider (18): as postgres, compare a libc database with an ICU tr-TR one.
        CREATE DATABASE fts_tr_icu LOCALE_PROVIDER icu ICU_LOCALE 'tr-TR' LOCALE 'en_US.utf8' TEMPLATE template0;
        \c fts_tr_icu
        SELECT to_tsvector('turkish', 'IŞIK İSTANBUL ışık istanbul');
        \c postgres
        DROP DATABASE fts_tr_icu;
*/
