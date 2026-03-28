WITH "PlaylistItemsRowNumber" AS (
	SELECT "PlaylistItems"."Id", 
		ROW_NUMBER() OVER (ORDER BY rowid) AS "RowNumber"
	FROM "PlaylistItems"
	WHERE "PlaylistItems"."Playlist_Id" = @playlistId 
		AND "PlaylistItems"."Status" = @status
)

UPDATE "PlaylistItems"
SET "Sequence" = "Sequence" + 
(
	SELECT "PlaylistItemsRowNumber"."RowNumber" - 1
	FROM "PlaylistItemsRowNumber"
	WHERE "PlaylistItemsRowNumber"."Id" = "PlaylistItems"."Id"
)
WHERE "PlaylistItems"."Playlist_Id" = @playlistId 
	AND "PlaylistItems"."Status" = @status