/* Post 04: fragmentation, page count and page fullness of the benchmark tables */
SET NOCOUNT ON;
SELECT OBJECT_NAME(s.object_id) AS TableName, i.name AS IndexName,
       CAST(s.avg_fragmentation_in_percent AS decimal(5,1)) AS FragPct,
       s.page_count AS Pages,
       CAST(s.avg_page_space_used_in_percent AS decimal(5,1)) AS PageFullPct
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'SAMPLED') s
JOIN sys.indexes i ON i.object_id = s.object_id AND i.index_id = s.index_id
WHERE OBJECT_NAME(s.object_id) LIKE 'Ord%' AND s.index_level = 0
ORDER BY TableName, i.index_id;
