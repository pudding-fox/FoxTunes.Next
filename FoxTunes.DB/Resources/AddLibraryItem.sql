INSERT INTO "LibraryItems" ("DirectoryName", "FileName", "ImportDate", "Status", "Flags")
SELECT @directoryName, @fileName, @importDate, @status, @flags
WHERE NOT EXISTS(
	SELECT *
	FROM "LibraryItems" 
	WHERE "FileName" = @fileName
);

SELECT "Id"
FROM "LibraryItems" 
WHERE "FileName" = @fileName;