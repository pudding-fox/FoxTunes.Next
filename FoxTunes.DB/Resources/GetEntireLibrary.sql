SELECT "LibraryItems"."FileName", 
	CASE 
		WHEN "MetaDataItems"."Name" = @like THEN 'Like' 
		ELSE "MetaDataItems"."Name" 
	END AS "Name", 
	"MetaDataItems"."Value"
FROM "LibraryItems"
	JOIN "LibraryItem_MetaDataItem" ON "LibraryItems"."Id" = "LibraryItem_MetaDataItem"."LibraryItem_Id"
	JOIN "MetaDataItems" ON "LibraryItem_MetaDataItem"."MetaDataItem_Id" = "MetaDataItems"."Id"
WHERE "MetaDataItems"."Name" IN (@artist, @album, @title, @genre, @year, @like, @rating)
ORDER BY "LibraryItems"."FileName", "MetaDataItems"."Name"