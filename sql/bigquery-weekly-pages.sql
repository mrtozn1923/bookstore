-- Weekly clicks per URL from the Search Console bulk data export (BigQuery).
-- Filtering on data_date limits the partitions scanned, which is what you pay for.
SELECT
  url,
  SUM(clicks) AS clicks,
  SUM(impressions) AS impressions,
  SUM(sum_position) / SUM(impressions) + 1 AS avg_position
FROM searchconsole.searchdata_url_impression
WHERE data_date BETWEEN DATE '2026-11-05' AND DATE '2026-11-11'
  AND search_type = 'WEB'
GROUP BY url
ORDER BY clicks DESC
LIMIT 20;
