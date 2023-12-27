INSERT INTO "MetaDataItems" ("Name", "Type", "Value") 
SELECT @name, @type, @value
WHERE NOT EXISTS(
	SELECT *
	FROM "MetaDataItems" 
	WHERE "Name" = @name AND "Type" = @type AND "Value" = @value
);

SELECT "Id"
FROM "MetaDataItems" 
WHERE "Name" = @name AND "Type" = @type AND "Value" = @value;