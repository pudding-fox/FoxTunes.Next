INSERT INTO "LibraryItem_MetaDataItem" ("LibraryItem_Id", "MetaDataItem_Id")
SELECT @itemId, @metaDataItemId
WHERE NOT EXISTS(
	SELECT *
	FROM "LibraryItem_MetaDataItem" 
	WHERE "LibraryItem_Id" = @itemId AND "MetaDataItem_Id" = @metaDataItemId
);