SELECT "LibraryItems"."FileName", "Album"."Value" AS "Album", "Title"."Value" AS "Title", MAX("LastPlayed"."Value") AS "LastPlayed"
FROM "LibraryItems"

INNER JOIN "LibraryItem_MetaDataItem" AS "LibraryItem_MetaDataItem_LastPlayed_Album"
    ON "LibraryItems"."Id" = "LibraryItem_MetaDataItem_LastPlayed_Album"."LibraryItem_Id"
INNER JOIN "MetaDataItems" AS "Album"
    ON "LibraryItem_MetaDataItem_LastPlayed_Album"."MetaDataItem_Id" = "Album"."Id"
	
INNER JOIN "LibraryItem_MetaDataItem" AS "LibraryItem_MetaDataItem_LastPlayed_Title"
    ON "LibraryItems"."Id" = "LibraryItem_MetaDataItem_LastPlayed_Title"."LibraryItem_Id"
INNER JOIN "MetaDataItems" AS "Title"
    ON "LibraryItem_MetaDataItem_LastPlayed_Title"."MetaDataItem_Id" = "Title"."Id"

INNER JOIN "LibraryItem_MetaDataItem" AS "LibraryItem_MetaDataItem_LastPlayed"
    ON "LibraryItems"."Id" = "LibraryItem_MetaDataItem_LastPlayed"."LibraryItem_Id"
INNER JOIN "MetaDataItems" AS "LastPlayed"
    ON "LibraryItem_MetaDataItem_LastPlayed"."MetaDataItem_Id" = "LastPlayed"."Id"
	
WHERE "Album"."Name" = @album
	AND "Title"."Name" = @title
	AND "LastPlayed"."Name" = @lastPlayed 
	

GROUP BY "Album"."Value"
ORDER BY "LastPlayed" DESC
LIMIT @limit