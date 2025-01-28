namespace LocationsFromPhotos.Models.Base;

public abstract class PDObject(string id, string name, string type)
{
    public string Id { get; private set; } = id;
    public string Name { get; private set; } = name;
    public string Type { get; private set; } = type;
}