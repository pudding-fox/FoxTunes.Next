DELETE FROM "LibraryItem_MetaDataItem"
WHERE "Id" IN
(
	SELECT "LibraryItem_MetaDataItem"."Id"
	FROM "LibraryItem_MetaDataItem"
		JOIN "MetaDataItems"
			ON "LibraryItem_MetaDataItem"."MetaDataItem_Id" = "MetaDataItems"."Id"
				AND (@type & "MetaDataItems"."Type") =  "MetaDataItems"."Type"
	WHERE "LibraryItem_MetaDataItem"."LibraryItem_Id" = @itemId
)