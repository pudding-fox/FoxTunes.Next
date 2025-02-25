WITH "MetaData"
AS
(
	SELECT "MetaDataItems"."Id"
	FROM "LibraryItem_MetaDataItem"
		JOIN "MetaDataItems" 
			ON "MetaDataItems"."Id" = "LibraryItem_MetaDataItem"."MetaDataItem_Id"
		JOIN "LibraryItems"
			ON "LibraryItems"."Id" =  "LibraryItem_MetaDataItem"."LibraryItem_Id"
	WHERE "MetaDataItems"."Name" = @name 
		AND  "MetaDataItems"."Type" = @type
		AND "LibraryItems"."Status" = @status
)
	
DELETE FROM "LibraryItem_MetaDataItem"
WHERE "MetaDataItem_Id" IN
(
	SELECT "Id"
	FROM "MetaData"
)