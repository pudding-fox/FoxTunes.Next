DELETE FROM [LibraryHierarchyItem_LibraryItem]
WHERE (@libraryHierarchyId IS NULL OR [LibraryHierarchyItem_Id] IN
	(
		SELECT [Id]
		FROM [LibraryHierarchyItems]
		WHERE [LibraryHierarchy_Id] = @libraryHierarchyId
	))
	AND (@status IS NULL OR [LibraryItem_Id] IN
	(
		SELECT [Id] 
		FROM [LibraryItems]
		WHERE @status IS NULL OR [Status] = @status
	));

DELETE FROM [LibraryHierarchyItems]
WHERE [Id] IN
(
	SELECT [LibraryHierarchyItems].[Id]
	FROM [LibraryHierarchyItems]
		 LEFT JOIN [LibraryHierarchyItem_LibraryItem]
			ON [LibraryHierarchyItems].[Id] = [LibraryHierarchyItem_LibraryItem].[LibraryHierarchyItem_Id]
	WHERE [LibraryHierarchyItem_LibraryItem].[Id] IS NULL
);