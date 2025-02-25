DELETE FROM "PlaylistItem_MetaDataItem"
WHERE "Id" IN
(
	SELECT "PlaylistItem_MetaDataItem"."Id"
	FROM "PlaylistItem_MetaDataItem"
		JOIN "MetaDataItems"
			ON "PlaylistItem_MetaDataItem"."MetaDataItem_Id" = "MetaDataItems"."Id"
				AND (@type & "MetaDataItems"."Type") =  "MetaDataItems"."Type"
	WHERE "PlaylistItem_MetaDataItem"."PlaylistItem_Id" = @itemId
)