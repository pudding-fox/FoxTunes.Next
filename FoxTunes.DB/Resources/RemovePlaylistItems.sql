DELETE FROM [PlaylistItem_MetaDataItem]
WHERE [PlaylistItem_Id] IN
(
	SELECT [Id]
	FROM [PlaylistItems]
	WHERE [Playlist_Id] = @playlistId 
		AND [Status] = @status
);

DELETE FROM [PlaylistItems]
WHERE [Playlist_Id] = @playlistId 
	AND [Status] = @status