-- alter_sighting_userid_to_nvarchar.sql
ALTER TABLE dbo.Sighting
ALTER COLUMN UserId NVARCHAR(450) NOT NULL;