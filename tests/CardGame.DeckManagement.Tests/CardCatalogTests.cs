namespace CardGame.DeckManagement.Tests;

public class CardCatalogTests
{
    private static string CreateTempFilePath()
    {
        return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
    }

    [Fact]
    public void Load_ParsesValidJsonFile_ReturnsMatchingTemplates()
    {
        // Arrange
        var path = CreateTempFilePath();
        File.WriteAllText(
            path,
            """
            [
                { "Name": "Spark", "Cost": 1, "Charge": 1, "Attack": 2, "Defense": 1, "ImagePath": "images/spark.png" },
                { "Name": "Blaze", "Cost": 1, "Charge": 1, "Attack": 1, "Defense": 2, "ImagePath": null }
            ]
            """
        );

        try
        {
            // Act
            var templates = CardCatalog.Load(path);

            // Assert
            Assert.Equal(2, templates.Count);
            Assert.Equal("Spark", templates[0].Name);
            Assert.Equal(1, templates[0].Cost);
            Assert.Equal("images/spark.png", templates[0].ImagePath);
            Assert.Equal("Blaze", templates[1].Name);
            Assert.Null(templates[1].ImagePath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var path = CreateTempFilePath();

        // Act + Assert
        Assert.Throws<FileNotFoundException>(() => CardCatalog.Load(path));
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsTemplatesUnchanged()
    {
        // Arrange
        var path = CreateTempFilePath();
        var templates = new List<CardTemplate>
        {
            new("Spark", 1, 1, 2, 1, "images/spark.png"),
            new("Blaze", 1, 1, 1, 2, null),
        };

        try
        {
            // Act
            CardCatalog.Save(path, templates);
            var reloaded = CardCatalog.Load(path);

            // Assert
            Assert.Equal(templates, reloaded);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
