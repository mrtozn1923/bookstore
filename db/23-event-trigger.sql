/*
    Post 23 - event trigger that records DDL. Run as the postgres superuser (CREATE EVENT TRIGGER needs it):
        docker exec -it bookstore-pg psql -U postgres -d bookstore -f /lab/23-event-trigger.sql
    Then: 23-event-ddl.sql as bookstore_owner, 23-event-log.sql as postgres (shows the log and removes everything).
*/
\pset footer off
CREATE TABLE public.ddl_log
(
    ddl_log_id      bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    command_tag     text        NOT NULL,
    object_type     text        NOT NULL,
    object_identity text,
    executed_by     text        NOT NULL,
    executed_at     timestamptz NOT NULL DEFAULT now()
);
CREATE FUNCTION public.log_ddl() RETURNS event_trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, pg_temp
AS $$
BEGIN
    INSERT INTO ddl_log (command_tag, object_type, object_identity, executed_by)
    SELECT c.command_tag, c.object_type, c.object_identity, session_user
    FROM pg_event_trigger_ddl_commands() AS c;
END
$$;
CREATE EVENT TRIGGER trg_log_ddl ON ddl_command_end EXECUTE FUNCTION public.log_ddl();
