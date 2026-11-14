/*
    Post 13 - extensions. Run as bookstore_owner (owner of the database, not a superuser):
        psql -U bookstore_owner -d bookstore -f /lab/13-extensions.sql
    Expects the extensions of posts 03-10 (pgstattuple, btree_gist, pg_trgm, unaccent, pg_stat_statements)
    and ix_books_title_trgm from 10. citext, pgcrypto and the demo table are removed at the end.
    pg_cron: see 13-pg-cron/ (own image, own container).
*/
\set ON_ERROR_STOP off

/* 1. what is installed in this database, what the server offers */
\dx
SELECT count(*) AS available, count(*) FILTER (WHERE trusted) AS trusted
FROM (SELECT DISTINCT ON (name) name, trusted FROM pg_available_extension_versions ORDER BY name, version DESC) e;
SELECT string_agg(name, ', ' ORDER BY name) AS trusted_extensions
FROM (SELECT DISTINCT name FROM pg_available_extension_versions WHERE trusted) t;

/* 2. trusted vs untrusted, as the database owner */
CREATE EXTENSION citext;
CREATE EXTENSION pg_buffercache;

/* 3. versions and update paths */
SELECT name, default_version, installed_version FROM pg_available_extensions
WHERE name IN ('pg_trgm', 'citext', 'pgcrypto', 'pg_buffercache', 'uuid-ossp') ORDER BY name;
SELECT source, target, path FROM pg_extension_update_paths('pg_trgm')
WHERE target = '1.6' AND path IS NOT NULL ORDER BY source;
SELECT name, default_version, installed_version FROM pg_available_extensions
WHERE installed_version IS NOT NULL AND installed_version <> default_version;

/* 4. objects belong to the extension */
DROP EXTENSION pg_trgm;

/* 5. citext and the Turkish dotless i */
CREATE TABLE newsletter_subscribers (email citext PRIMARY KEY, city citext);
INSERT INTO newsletter_subscribers VALUES ('Ada@Example.com', 'İSTANBUL');
INSERT INTO newsletter_subscribers VALUES ('ada@example.com', 'istanbul');
SELECT city = 'istanbul' AS istanbul, 'IŞIK'::citext = 'ışık' AS isik FROM newsletter_subscribers;
DROP TABLE newsletter_subscribers;

/* 6. pgcrypto; gen_random_uuid() is in core since 13 */
\df gen_random_uuid
CREATE EXTENSION pgcrypto;
SELECT encode(digest('9780441013593', 'sha256'), 'hex') AS isbn_sha256;

DROP EXTENSION citext;
DROP EXTENSION pgcrypto;
