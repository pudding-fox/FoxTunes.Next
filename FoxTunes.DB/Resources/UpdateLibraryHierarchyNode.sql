INSERT INTO [LibraryHierarchyItem_LibraryItem] ([LibraryHierarchyItem_Id], [LibraryItem_Id])
SELECT @libraryHierarchyItemId, @libraryItemId
WHERE NOT EXISTS(
	SELECT * 
	FROM [LibraryHierarchyItem_LibraryItem]
	WHERE [LibraryHierarchyItem_Id] = @libraryHierarchyItemId
		AND [LibraryItem_Id] = @libraryItemId
);