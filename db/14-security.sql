/*
    Post 14 - roles, privileges, RLS and password hashing. Run as the postgres superuser:
        psql -U postgres -d bookstore -f /lab/14-security.sql
    Everything the script creates (audit_log, bookstore_report, legacy_md5, the RLS policy) is removed at the end.
    TLS and pg_hba.conf steps are in the post (they change files in the data directory).
*/
\set ON_ERROR_STOP off

/* 1. role attributes */
SELECT rolname, rolsuper, rolcreaterole, rolcreatedb, rolcanlogin, rolbypassrls
FROM pg_roles WHERE rolname IN ('postgres', 'bookstore_owner', 'bookstore_app') ORDER BY rolname;

/* 2. privileges on a table: role=privileges/grantor */
\dp orders

/* 3. a table created by a different role does not get the default privileges */
CREATE TABLE public.audit_log (id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY, note text);
SET ROLE bookstore_app;
SELECT count(*) FROM audit_log;
CREATE TABLE app_scratch (id int);
RESET ROLE;
DROP TABLE public.audit_log;

/* 4. read-only reporting role through a predefined role */
CREATE ROLE bookstore_report NOLOGIN;
GRANT pg_read_all_data TO bookstore_report;
SET ROLE bookstore_report;
SELECT count(*) AS orders FROM orders;
UPDATE books SET price = price WHERE book_id = 1;
RESET ROLE;

/* 5. row-level security: the app sees only the current customer's orders */
ALTER TABLE orders ENABLE ROW LEVEL SECURITY;
CREATE POLICY orders_current_customer ON orders TO bookstore_app
    USING (customer_id = current_setting('app.customer_id', true)::int);
SET ROLE bookstore_app;
SET app.customer_id = '2';
SELECT count(*) AS customer_2 FROM orders;
RESET app.customer_id;
SELECT count(*) AS no_customer FROM orders;
RESET ROLE;
SET ROLE bookstore_owner;
SELECT count(*) AS as_owner FROM orders;
RESET ROLE;
DROP POLICY orders_current_customer ON orders;
ALTER TABLE orders DISABLE ROW LEVEL SECURITY;

/* 6. how passwords are stored; md5 is deprecated in 18 */
SELECT rolname,
       CASE WHEN rolpassword LIKE 'SCRAM-SHA-256$%' THEN 'SCRAM-SHA-256' WHEN rolpassword LIKE 'md5%' THEN 'md5' END AS stored_as
FROM pg_authid WHERE rolname LIKE 'bookstore%' ORDER BY rolname;
\set pw `head -c 12 /dev/urandom | od -An -tx1 | tr -d ' \n'`
SET password_encryption = 'md5';
CREATE ROLE legacy_md5 LOGIN PASSWORD :'pw';
RESET password_encryption;
\unset pw
DROP ROLE legacy_md5;
DROP ROLE bookstore_report;
