INSERT INTO "MetaDataItems" ("Name", "Type", "Value")
SELECT @name, @type, @value
WHERE NOT EXISTS(SELECT * FROM "MetaDataItems" WHERE "Name" = @name AND "Type" = @type AND "Value" = @value);

WITH "MetaData"
AS
(
	SELECT 
		"PlaylistItem_MetaDataItem"."PlaylistItem_Id" AS "Id",
		"MetaDataItems"."Name",
		"MetaDataItems"."Value"
	FROM "PlaylistItem_MetaDataItem" 
		JOIN "MetaDataItems" 
			ON "MetaDataItems"."Id" = "PlaylistItem_MetaDataItem"."MetaDataItem_Id"
),

"Artist"
AS
(
	SELECT "Artist"."Id", "Artist"."DirectoryName", "Album", "Artist"
	FROM
	(
		SELECT "PlaylistItems".*,
		(
			SELECT "Value"
			FROM "MetaData"
			WHERE "MetaData"."Id" = "PlaylistItems"."Id" AND "MetaData"."Name" = 'Album'
		) AS "Album",
		(
			SELECT "Value"
			FROM "MetaData"
			WHERE "MetaData"."Id" = "PlaylistItems"."Id" AND "MetaData"."Name" = 'Artist'
		) AS "Artist"
		FROM "PlaylistItems"
		WHERE "Status" = @status
	) AS "Artist"
),

"VariousArtists" AS
(
	SELECT "DirectoryName", "Album"
	FROM "Artist"
	GROUP BY "DirectoryName", "Album"
	HAVING COUNT(DISTINCT "Artist") > 1
),

"VA_MetaDataItem" AS
(
	SELECT "Id" 
	FROM "MetaDataItems" 
	WHERE "Name" = @name AND "Type" = @type AND "Value" = @value
)
	
INSERT INTO "PlaylistItem_MetaDataItem" ("PlaylistItem_Id", "MetaDataItem_Id")
SELECT "Artist"."Id", (SELECT "Id" FROM "VA_MetaDataItem")
FROM "Artist"
JOIN "VariousArtists" ON "Artist"."DirectoryName" = "VariousArtists"."DirectoryName" 
	AND "Artist"."Album" = "VariousArtists"."Album"
WHERE NOT EXISTS(
	SELECT * 
	FROM "PlaylistItem_MetaDataItem"
	JOIN "VA_MetaDataItem" ON "PlaylistItem_MetaDataItem"."MetaDataItem_Id" = "VA_MetaDataItem"."Id"
	WHERE "PlaylistItem_MetaDataItem"."PlaylistItem_Id" = "Artist"."Id"
)