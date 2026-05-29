DELETE FROM [LibraryHierarchyItem_LibraryItem] 
WHERE [LibraryItem_Id] = @libraryItemId;
DELETE FROM [LibraryHierarchyItems]
WHERE NOT [Id] IN
(
	SELECT [LibraryHierarchyItem_Id]
	FROM [LibraryHierarchyItem_LibraryItem]
	WHERE [LibraryHierarchyItem_Id] = [LibraryHierarchyItems].[Id]
);