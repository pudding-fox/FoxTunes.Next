DELETE FROM [LibraryItems]
WHERE NOT EXISTS
(
		SELECT *
		FROM [LibraryHierarchyItem_LibraryItem]
		WHERE [LibraryHierarchyItem_LibraryItem].[LibraryItem_Id] = [LibraryItems].[Id]
)