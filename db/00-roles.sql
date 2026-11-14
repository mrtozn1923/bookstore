/*
    PostgreSQL series - lab roles and database (run once as the postgres superuser).
    Usage:
        psql -U postgres -f 00-roles.sql      (\password asks for both passwords, nothing is echoed)
    bookstore_owner : owns the database and every table; runs schema.sql and migrations.
    bookstore_app   : what the application connects with; DML only, no DDL, not a superuser.
*/
\set ON_ERROR_STOP on

CREATE ROLE bookstore_owner LOGIN;
CREATE ROLE bookstore_app LOGIN;
\password bookstore_owner
\password bookstore_app

CREATE DATABASE bookstore OWNER bookstore_owner;
REVOKE ALL ON DATABASE bookstore FROM PUBLIC;
GRANT CONNECT, TEMPORARY ON DATABASE bookstore TO bookstore_app;

\connect bookstore
/* every table and sequence bookstore_owner creates later is usable by the app role */
ALTER DEFAULT PRIVILEGES FOR ROLE bookstore_owner IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO bookstore_app;
ALTER DEFAULT PRIVILEGES FOR ROLE bookstore_owner IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO bookstore_app;
