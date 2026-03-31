SELECT *
FROM "PlaylistItems"
WHERE "Playlist_Id" = @playlistId
ORDER BY "Sequence"