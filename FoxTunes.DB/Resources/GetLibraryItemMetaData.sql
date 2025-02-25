SELECT "MetaDataItems".*
FROM "MetaDataItems"
	JOIN "LibraryItem_MetaDataItem" ON "LibraryItem_MetaDataItem"."MetaDataItem_Id" = "MetaDataItems"."Id"
WHERE "LibraryItem_MetaDataItem"."LibraryItem_Id" = @libraryItemId 
	AND (@type & "MetaDataItems"."Type") =  "MetaDataItems"."Type"