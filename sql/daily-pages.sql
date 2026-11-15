-- Input of the impact command, from the archive filled by the fetch job (post 03).
SELECT DataDate AS [date], Page AS page, SUM(Clicks) AS clicks, SUM(Impressions) AS impressions
FROM dbo.SearchAnalyticsDaily
WHERE SearchType = 'web'
GROUP BY DataDate, Page
ORDER BY DataDate, Page;
