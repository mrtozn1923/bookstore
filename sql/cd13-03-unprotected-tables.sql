-- Tables that have a tenant column but no filter predicate
SELECT t.name AS UnprotectedTable
FROM sys.tables AS t
JOIN sys.columns AS c ON c.object_id = t.object_id AND c.name = N'InstitutionId'
WHERE NOT EXISTS (
    SELECT 1 FROM sys.security_predicates AS p
    WHERE p.target_object_id = t.object_id AND p.predicate_type_desc = N'FILTER');
