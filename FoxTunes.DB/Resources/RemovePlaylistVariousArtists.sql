WITH "MetaData"
AS
(
	SELECT "MetaDataItems"."Id"
	FROM "PlaylistItem_MetaDataItem"
		JOIN "MetaDataItems" 
			ON "MetaDataItems"."Id" = "PlaylistItem_MetaDataItem"."MetaDataItem_Id"
		JOIN "PlaylistItems"
			ON "PlaylistItems"."Id" =  "PlaylistItem_MetaDataItem"."PlaylistItem_Id"
	WHERE "MetaDataItems"."Name" = @name 
		AND  "MetaDataItems"."Type" = @type
		AND "PlaylistItems"."Status" = @status
)
	
DELETE FROM "PlaylistItem_MetaDataItem"
WHERE "MetaDataItem_Id" IN
(
	SELECT "Id"
	FROM "MetaData"
)