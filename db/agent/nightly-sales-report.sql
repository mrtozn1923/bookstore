/* Run after 22-daily-sales-report.sql. Re-runnable: drops and recreates the job. */
USE msdb;
GO
IF EXISTS (SELECT 1 FROM dbo.sysjobs WHERE name = N'sqls_22_nightly_sales_report')
    EXEC dbo.sp_delete_job @job_name = N'sqls_22_nightly_sales_report', @delete_unused_schedule = 1;
EXEC dbo.sp_add_job
    @job_name = N'sqls_22_nightly_sales_report',
    @description = N'Fills dbo.DailySalesReport for yesterday. Source: db/agent/nightly-sales-report.sql',
    @owner_login_name = N'sa';
EXEC dbo.sp_add_jobstep
    @job_name = N'sqls_22_nightly_sales_report',
    @step_name = N'Fill daily sales report',
    @subsystem = N'TSQL',
    @database_name = N'sqls_22',
    @command = N'EXEC dbo.FillDailySalesReport;',
    @retry_attempts = 2,
    @retry_interval = 5,
    @on_success_action = 1,
    @on_fail_action = 2;
EXEC dbo.sp_add_schedule
    @schedule_name = N'sqls_22_daily_0230',
    @freq_type = 4,
    @freq_interval = 1,
    @active_start_time = 023000;
EXEC dbo.sp_attach_schedule @job_name = N'sqls_22_nightly_sales_report', @schedule_name = N'sqls_22_daily_0230';
EXEC dbo.sp_add_jobserver @job_name = N'sqls_22_nightly_sales_report', @server_name = N'(local)';
IF NOT EXISTS (SELECT 1 FROM dbo.sysoperators WHERE name = N'sqls_22_bookstore_dba')
    EXEC dbo.sp_add_operator @name = N'sqls_22_bookstore_dba', @email_address = N'dba-team@example.com';
EXEC dbo.sp_update_job
    @job_name = N'sqls_22_nightly_sales_report',
    @notify_level_email = 2,
    @notify_email_operator_name = N'sqls_22_bookstore_dba';
GO
EXEC msdb.dbo.sp_add_alert
    @name = N'sqls_22_error_824',
    @message_id = 824,
    @delay_between_responses = 600,
    @include_event_description_in = 1;
EXEC msdb.dbo.sp_add_notification
    @alert_name = N'sqls_22_error_824',
    @operator_name = N'sqls_22_bookstore_dba',
    @notification_method = 1;
SELECT name, message_id, severity, enabled, occurrence_count FROM msdb.dbo.sysalerts WHERE name LIKE N'sqls[_]22%';
GO
/* Database Mail: needs sp_configure 'Database Mail XPs', 1 and an SMTP server; set Agent's profile with mssql-conf or MSSQL_AGENT_EMAIL_PROFILE. */
EXEC msdb.dbo.sysmail_add_account_sp
    @account_name = N'sqls_22_bookstore_alerts',
    @email_address = N'sql-alerts@example.com',
    @display_name = N'Bookstore SQL Server',
    @mailserver_name = N'smtp.example.com',
    @port = 587,
    @enable_ssl = 1;
EXEC msdb.dbo.sysmail_add_profile_sp @profile_name = N'sqls_22_bookstore';
EXEC msdb.dbo.sysmail_add_profileaccount_sp
    @profile_name = N'sqls_22_bookstore',
    @account_name = N'sqls_22_bookstore_alerts',
    @sequence_number = 1;
EXEC msdb.dbo.sp_send_dbmail
    @profile_name = N'sqls_22_bookstore',
    @recipients = N'dba-team@example.com',
    @subject = N'Database Mail test',
    @body = N'Test message from the Bookstore SQL Server.';
GO
/* History (on a server where Agent runs) */
SELECT TOP (20) j.name, h.step_id, h.step_name, h.run_status,
       msdb.dbo.agent_datetime(h.run_date, h.run_time) AS run_at, h.run_duration, h.retries_attempted, h.message
FROM msdb.dbo.sysjobhistory AS h
JOIN msdb.dbo.sysjobs AS j ON j.job_id = h.job_id
WHERE j.name = N'sqls_22_nightly_sales_report'
ORDER BY h.instance_id DESC;
GO
