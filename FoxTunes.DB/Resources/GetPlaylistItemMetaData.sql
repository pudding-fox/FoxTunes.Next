SELECT "MetaDataItems".*
FROM "MetaDataItems"
INNER JOIN "PlaylistItem_MetaDataItem"
    ON "MetaDataItems"."Id" = "PlaylistItem_MetaDataItem"."MetaDataItem_Id"
WHERE "PlaylistItem_MetaDataItem"."PlaylistItem_Id" = @playlistItemId
UNION  
SELECT "MetaDataItems".*
FROM "MetaDataItems"
INNER JOIN "LibraryItem_MetaDataItem"
    ON "MetaDataItems"."Id" = "LibraryItem_MetaDataItem"."MetaDataItem_Id"
WHERE "LibraryItem_MetaDataItem"."LibraryItem_Id" = @libraryItemId