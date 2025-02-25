INSERT INTO "PlaylistItem_MetaDataItem" ("PlaylistItem_Id", "MetaDataItem_Id")
SELECT @itemId, @metaDataItemId
WHERE NOT EXISTS(
	SELECT *
	FROM "PlaylistItem_MetaDataItem" 
	WHERE "PlaylistItem_Id" = @itemId AND "MetaDataItem_Id" = @metaDataItemId
);