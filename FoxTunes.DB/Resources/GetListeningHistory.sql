WITH "MetaData" AS (
    SELECT 
        "LibraryItems"."FileName",
        MAX(CASE WHEN "MetaDataItems"."Name" = @artist THEN "MetaDataItems"."Value" END) AS "Artist",
        MAX(CASE WHEN "MetaDataItems"."Name" = @album THEN "MetaDataItems"."Value" END) AS "Album",
        MAX(CASE WHEN "MetaDataItems"."Name" = @title THEN "MetaDataItems"."Value" END) AS "Title",
        MAX(CASE WHEN "MetaDataItems"."Name" = @lastPlayed THEN "MetaDataItems"."Value" END) AS "LastPlayed"
    FROM "LibraryItems"
        JOIN "LibraryItem_MetaDataItem"
            ON "LibraryItems"."Id" = "LibraryItem_MetaDataItem"."LibraryItem_Id"
        JOIN "MetaDataItems"
            ON "LibraryItem_MetaDataItem"."MetaDataItem_Id" = "MetaDataItems"."Id"
    GROUP BY "LibraryItems"."Id"
),

"Ranked" AS (
    SELECT 
        *,
        ROW_NUMBER() OVER (PARTITION BY "Album" ORDER BY "LastPlayed" DESC) AS "RowNumber"
    FROM "MetaData"
    WHERE "LastPlayed" IS NOT NULL
)

SELECT 
	"FileName",
    "Artist",
    "Album",
	"Title",
   "LastPlayed"
FROM "Ranked"
WHERE "RowNumber" = 1
ORDER BY "LastPlayed" DESC
LIMIT @limit