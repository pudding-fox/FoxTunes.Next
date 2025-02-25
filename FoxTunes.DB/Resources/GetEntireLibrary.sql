WITH "MetaData"
AS (
    SELECT 
        "LibraryItems"."FileName",
        MAX(CASE WHEN "MetaDataItems"."Name" = @artist THEN "MetaDataItems"."Value" END) AS "Artist",
        MAX(CASE WHEN "MetaDataItems"."Name" = @album THEN "MetaDataItems"."Value" END) AS "Album",
        MAX(CASE WHEN "MetaDataItems"."Name" = @track THEN "MetaDataItems"."Value" END) AS "Track",
        MAX(CASE WHEN "MetaDataItems"."Name" = @title THEN "MetaDataItems"."Value" END) AS "Title",
        MAX(CASE WHEN "MetaDataItems"."Name" = @genre THEN "MetaDataItems"."Value" END) AS "Genre",
        MAX(CASE WHEN "MetaDataItems"."Name" = @year THEN "MetaDataItems"."Value" END) AS "Year",
        IFNULL(MAX(CASE WHEN "MetaDataItems"."Name" = @like THEN CASE WHEN "MetaDataItems"."Value" = 1 THEN 'True' ELSE 'False' END END), 'False') AS "Like",
        IFNULL(MAX(CASE WHEN "MetaDataItems"."Name" = @rating THEN "MetaDataItems"."Value" END), 0) AS "Rating"
    FROM "LibraryItems"
        JOIN "LibraryItem_MetaDataItem"
            ON "LibraryItems"."Id" = "LibraryItem_MetaDataItem"."LibraryItem_Id"
        JOIN "MetaDataItems"
            ON "LibraryItem_MetaDataItem"."MetaDataItem_Id" = "MetaDataItems"."Id"
    WHERE "MetaDataItems"."Name" IN (@artist, @album, @track, @title, @genre, @year, @like, @rating)
    GROUP BY "LibraryItems"."FileName"
)

SELECT *
FROM "MetaData"
ORDER BY "Artist", "Album", CAST("Track" AS INT)