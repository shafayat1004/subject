-- Run manually or via dev-stack after SQL is up (SQL Server Docker does not auto-run init scripts like Postgres).
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'Todo_Dev')
BEGIN
    CREATE DATABASE Todo_Dev;
END
GO
