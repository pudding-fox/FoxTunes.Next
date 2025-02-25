DELETE FROM [LibraryItem_MetaDataItem]
WHERE [LibraryItem_Id] IN
(
	SELECT [Id]
	FROM [LibraryItems]
	WHERE @status IS NULL OR [LibraryItems].[Status] = @status
);

DELETE FROM [PlaylistItem_MetaDataItem]
WHERE [PlaylistItem_Id] IN
(
	SELECT [PlaylistItems].[Id]
	FROM [PlaylistItems]
		JOIN [LibraryItems] ON [PlaylistItems].[LibraryItem_Id] = [LibraryItems].[Id]
	WHERE @status IS NULL OR [LibraryItems].[Status] = @status
);

DELETE FROM [PlaylistItems]
WHERE [Id] IN
(
	SELECT [PlaylistItems].[Id]
	FROM [PlaylistItems]
		JOIN [LibraryItems] ON [PlaylistItems].[LibraryItem_Id] = [LibraryItems].[Id]
	WHERE @status IS NULL OR [LibraryItems].[Status] = @status
);

DELETE FROM [LibraryItems]
WHERE @status IS NULL OR [LibraryItems].[Status] = @status;