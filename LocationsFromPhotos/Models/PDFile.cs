using LocationsFromPhotos.Models.Base;

namespace LocationsFromPhotos.Models;

public class PDFile(string id, string name, string type, string createdDate)
    : PDObject(id, name, type)
{
    public DateTime CreatedDate { get; private set; } = DateTime.ParseExact(
        createdDate,
        "yyyy/MM/dd:HH:mm:ss.fff",
        System.Globalization.CultureInfo.InvariantCulture
    );
}