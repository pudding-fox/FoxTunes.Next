SELECT "MetaDataItems"."Value"
FROM "MetaDataItems"
	JOIN "LibraryItem_MetaDataItem" ON "LibraryItem_MetaDataItem"."MetaDataItem_Id" = "MetaDataItems"."Id"
WHERE "MetaDataItems"."Name" = @name
	AND (@type & "MetaDataItems"."Type") =  "MetaDataItems"."Type"
GROUP BY "MetaDataItems"."Value"
ORDER BY "MetaDataItems"."Value"