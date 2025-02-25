WITH
"LibraryHierarchyItems_Lookup" AS
(
	SELECT * 
	FROM [LibraryHierarchyItems]
	WHERE ((@parentId IS NULL AND [Parent_Id] IS NULL) OR [Parent_Id] = @parentId)
		AND [LibraryHierarchy_Id] = @libraryHierarchyId
		AND [Value] = @value
		AND [IsLeaf] = @isLeaf
)

INSERT INTO [LibraryHierarchyItems] ([Parent_Id], [LibraryHierarchy_Id], [Value], [IsLeaf])
SELECT @parentId, @libraryHierarchyId, @value, @isLeaf
WHERE NOT EXISTS(SELECT * FROM "LibraryHierarchyItems_Lookup");

WITH
"LibraryHierarchyItems_Lookup" AS
(
	SELECT * 
	FROM [LibraryHierarchyItems]
	WHERE ((@parentId IS NULL AND [Parent_Id] IS NULL) OR [Parent_Id] = @parentId)
		AND [LibraryHierarchy_Id] = @libraryHierarchyId
		AND [Value] = @value
		AND [IsLeaf] = @isLeaf
)

INSERT INTO [LibraryHierarchyItem_LibraryItem] ([LibraryHierarchyItem_Id], [LibraryItem_Id])
SELECT [Id], @libraryItemId
FROM "LibraryHierarchyItems_Lookup"
WHERE NOT EXISTS(
	SELECT * 
	FROM [LibraryHierarchyItem_LibraryItem]
	WHERE [LibraryHierarchyItem_Id] = "LibraryHierarchyItems_Lookup".[Id]
		AND [LibraryItem_Id] = @libraryItemId
);

WITH
"LibraryHierarchyItems_Lookup" AS
(
	SELECT * 
	FROM [LibraryHierarchyItems]
	WHERE ((@parentId IS NULL AND [Parent_Id] IS NULL) OR [Parent_Id] = @parentId)
		AND [LibraryHierarchy_Id] = @libraryHierarchyId
		AND [Value] = @value
		AND [IsLeaf] = @isLeaf
)

SELECT [Id]
FROM "LibraryHierarchyItems_Lookup"
WHERE NOT @isLeaf = 1