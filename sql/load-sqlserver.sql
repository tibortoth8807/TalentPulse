-- Bulk-load the generated CSV files into the hr schema (SQL Server 2017+ / Azure SQL with a storage DATA_SOURCE).
-- Change the folder below to your repo's data folder. The SQL Server service account needs read access to it.
DECLARE @folder nvarchar(400) = N'C:\Repos\TalentPulse\data\';
DECLARE @sql nvarchar(max);

SET @sql = N'BULK INSERT hr.dim_date FROM ''' + @folder + N'dim_date.csv'' WITH (FORMAT = ''CSV'', FIRSTROW = 2, CODEPAGE = ''65001'', FIELDTERMINATOR = '','', ROWTERMINATOR = ''0x0a'', TABLOCK);';
EXEC sp_executesql @sql;
SET @sql = N'BULK INSERT hr.dim_location FROM ''' + @folder + N'dim_location.csv'' WITH (FORMAT = ''CSV'', FIRSTROW = 2, CODEPAGE = ''65001'', FIELDTERMINATOR = '','', ROWTERMINATOR = ''0x0a'', TABLOCK);';
EXEC sp_executesql @sql;
SET @sql = N'BULK INSERT hr.dim_org_unit FROM ''' + @folder + N'dim_org_unit.csv'' WITH (FORMAT = ''CSV'', FIRSTROW = 2, CODEPAGE = ''65001'', FIELDTERMINATOR = '','', ROWTERMINATOR = ''0x0a'', TABLOCK);';
EXEC sp_executesql @sql;
SET @sql = N'BULK INSERT hr.dim_job FROM ''' + @folder + N'dim_job.csv'' WITH (FORMAT = ''CSV'', FIRSTROW = 2, CODEPAGE = ''65001'', FIELDTERMINATOR = '','', ROWTERMINATOR = ''0x0a'', TABLOCK);';
EXEC sp_executesql @sql;
SET @sql = N'BULK INSERT hr.dim_source FROM ''' + @folder + N'dim_source.csv'' WITH (FORMAT = ''CSV'', FIRSTROW = 2, CODEPAGE = ''65001'', FIELDTERMINATOR = '','', ROWTERMINATOR = ''0x0a'', TABLOCK);';
EXEC sp_executesql @sql;
SET @sql = N'BULK INSERT hr.dim_employee FROM ''' + @folder + N'dim_employee.csv'' WITH (FORMAT = ''CSV'', FIRSTROW = 2, CODEPAGE = ''65001'', FIELDTERMINATOR = '','', ROWTERMINATOR = ''0x0a'', TABLOCK);';
EXEC sp_executesql @sql;
SET @sql = N'BULK INSERT hr.security_user_country FROM ''' + @folder + N'security_user_country.csv'' WITH (FORMAT = ''CSV'', FIRSTROW = 2, CODEPAGE = ''65001'', FIELDTERMINATOR = '','', ROWTERMINATOR = ''0x0a'', TABLOCK);';
EXEC sp_executesql @sql;
SET @sql = N'BULK INSERT hr.fact_headcount_monthly FROM ''' + @folder + N'fact_headcount_monthly.csv'' WITH (FORMAT = ''CSV'', FIRSTROW = 2, CODEPAGE = ''65001'', FIELDTERMINATOR = '','', ROWTERMINATOR = ''0x0a'', TABLOCK);';
EXEC sp_executesql @sql;
SET @sql = N'BULK INSERT hr.fact_employee_event FROM ''' + @folder + N'fact_employee_event.csv'' WITH (FORMAT = ''CSV'', FIRSTROW = 2, CODEPAGE = ''65001'', FIELDTERMINATOR = '','', ROWTERMINATOR = ''0x0a'', TABLOCK);';
EXEC sp_executesql @sql;
SET @sql = N'BULK INSERT hr.fact_requisition FROM ''' + @folder + N'fact_requisition.csv'' WITH (FORMAT = ''CSV'', FIRSTROW = 2, CODEPAGE = ''65001'', FIELDTERMINATOR = '','', ROWTERMINATOR = ''0x0a'', TABLOCK);';
EXEC sp_executesql @sql;
SET @sql = N'BULK INSERT hr.fact_application FROM ''' + @folder + N'fact_application.csv'' WITH (FORMAT = ''CSV'', FIRSTROW = 2, CODEPAGE = ''65001'', FIELDTERMINATOR = '','', ROWTERMINATOR = ''0x0a'', TABLOCK);';
EXEC sp_executesql @sql;

SELECT 'employees' AS [table], COUNT(*) AS [rows] FROM hr.dim_employee UNION ALL SELECT 'applications', COUNT(*) FROM hr.fact_application;
