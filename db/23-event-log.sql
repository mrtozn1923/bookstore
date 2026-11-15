/* Post 23 - what trg_log_ddl recorded; then remove it. Run as postgres. */
\pset footer off
SELECT ddl_log_id, command_tag, object_type, object_identity, executed_by FROM ddl_log ORDER BY ddl_log_id;
SELECT evtname, evtevent, evtenabled FROM pg_event_trigger;
DROP EVENT TRIGGER trg_log_ddl;
DROP FUNCTION public.log_ddl();
DROP TABLE public.ddl_log;
