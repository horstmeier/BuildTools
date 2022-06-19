using System.Collections.Immutable;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace BuildVersionManager;

public static class VersionWriter
{
    /// <summary>
    /// Replaces the version field in the given project file (.csproj) with the version
    /// specified in the parameters (or adds a version field if not already present) 
    /// </summary>
    /// <param name="project">Path to the project file (.csproj, .fsproj)</param>
    /// <param name="versionString">The version string to use</param>
    /// <param name="ct">A cancellation token</param>
    /// <returns>The constructed version string</returns>
    /// <exception cref="Exception"></exception>
    public static async Task<string> Update(
        string project,
        string versionString,
        CancellationToken? ct = null)
    {
        var doc = await ReadDocument(project, ct);

        var root = doc.Root;
        if (root == null) throw new Exception("Project has no root element");

        var propertyGroups = root.Elements(XName.Get("PropertyGroup"))
            .ToImmutableList();

        var propertyGroup =
            propertyGroups.FirstOrDefault(x => x.Elements(XName.Get("Version")).Any())
            ?? propertyGroups.FirstOrDefault()
            ?? AddPropertyGroup(root);

        var versionElement = propertyGroup.Element(XName.Get("Version"))
                             ?? AddVersion(propertyGroup);
        versionElement.Value = versionString;

        await using var xmlWriter = XmlWriter.Create(project, new XmlWriterSettings
        {
            Async = true,
            Indent = true
        });
        await root.WriteToAsync(xmlWriter, ct ?? CancellationToken.None);

        return versionString;
    }



    private static async Task<XDocument> ReadDocument(string project, CancellationToken? ct)
    {
        await using var fileStream = File.OpenRead(project);
        using var textStream = new StreamReader(fileStream, Encoding.UTF8);
        var doc = await XDocument.LoadAsync(textStream, LoadOptions.None, ct ?? CancellationToken.None);
        return doc;
    }

    private static XElement AddPropertyGroup(XElement root)
    {
        var propertyGroup = new XElement(XName.Get("PropertyGroup"));
        root.Add(propertyGroup);
        return propertyGroup;
    }

    private static XElement AddVersion(XElement propertyGroup)
    {
        var element = new XElement(XName.Get("Version"));
        propertyGroup.Add(element);
        return element;
    }
}