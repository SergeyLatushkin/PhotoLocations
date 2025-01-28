using LocationsFromPhotos.Models.Base;

namespace LocationsFromPhotos.Models;

public class PDFolder(string id, string name, string type) : PDObject(id, name, type)
{
    public IList<PDObject> Objects { get; } = new List<PDObject>();
}