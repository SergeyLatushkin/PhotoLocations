using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using LocationsFromPhotos.Models.Base;
using LocationsFromPhotos.Models;
using LocationsFromPhotos.Clients;
using System.Collections.Concurrent;

namespace LocationsFromPhotos;

internal class Program
{
    private static readonly NominatimClient Client = new();
    private static readonly ConcurrentDictionary<int, HashSet<string>> Collection = new();
    private static int _progress;
    private static readonly TaskQueue TaskQueue = new(5);

    private static async Task Main(string[] args)
    {
        Tuple<DateTime, DateTime> dates = GetDates();

        var devices = new PDCollection(dates.Item1, dates.Item2);
        devices.Refresh();

        foreach (PD device in devices)
        {
            device.Connect();

            ProcessMessage("...getting files information", 6);
            PDFolder content = device.GetContents();

            await TraverseDirectory(content.Objects, device.DownloadFile);

            TaskQueue.MarkAsLastBatch();

            await TaskQueue.WaitUntilAllTasksCompleted();

            device.Disconnect();

            Console.WriteLine("...RESULT...");
            foreach (KeyValuePair<int, HashSet<string>> item in Collection.OrderBy(item => item.Key))
            {
                Console.WriteLine(item.Key);

                foreach (string country in item.Value)
                {
                    Console.WriteLine($"---{country}");
                }
            }
        }

        Console.ReadKey();
    }

    private static async Task TraverseDirectory(IList<PDObject> content, Func<string, PDFileStream> downloadFile)
    {
        foreach (PDObject obj in content)
        {
            switch (obj.Type)
            {
                case "FILE" when
                    obj.Name.EndsWith(".HEIC", StringComparison.InvariantCultureIgnoreCase) ||
                    obj.Name.EndsWith(".JPG", StringComparison.InvariantCultureIgnoreCase):
                {
                    PDFileStream stream = downloadFile(obj.Id);

                    IReadOnlyList<MetadataExtractor.Directory> metadata = ImageMetadataReader.ReadMetadata(stream);

                    var gpsDirectory = metadata.OfType<GpsDirectory>().FirstOrDefault();

                    if (gpsDirectory != null)
                    {
                        double? latitude = gpsDirectory.GetGeoLocation()?.Latitude;
                        double? longitude = gpsDirectory.GetGeoLocation()?.Longitude;

                        if (latitude.HasValue && longitude.HasValue)
                        {
                            await TaskQueue.EnqueueTask(() =>
                            {
                                return Task.Run(async () =>
                                {
                                    string country = await Client.GetCountry(latitude, longitude);

                                    Collection.AddOrUpdate(
                                        ((PDFile)obj).CreatedDate.Year,
                                        _ => [country],
                                        (_, existingSet) =>
                                        {
                                            lock (existingSet)
                                            {
                                                existingSet.Add(country);

                                                return existingSet;
                                            }
                                        });
                                });
                            });

                            ProcessMessage($"...processing files {100 * ++_progress / PD.Counter}%", 6);
                            ProcessMessage($"file: {obj.Name}");
                        }
                        else
                        {
                            ++_progress;
                            ProcessMessage("GPS-data is unavailable");
                        }
                    }
                    else
                    {
                        ++_progress;
                        ProcessMessage("GPS-data is unavailable");
                    }

                    break;
                }
                case "FOLDER":
                    await TraverseDirectory(((PDFolder)obj).Objects, downloadFile);
                    break;
                default:
                    ++_progress;
                    ProcessMessage("unsupported file");
                    break;
            }
        }
    }

    private static void ProcessMessage(string message, int line = 7)
    {
        Console.SetCursorPosition(0, line);
        Console.WriteLine($"{message}{new string(' ', Console.WindowWidth)}");
    }

    private static Tuple<DateTime, DateTime> GetDates()
    {
        DateTime startDate;
        do
        {
            Console.WriteLine("Enter start date (format: 01.01.2001)");
        }
        while (!DateTime.TryParse(Console.ReadLine(), out startDate));

        Console.WriteLine("Enter end date (format: 01.01.2001)." +
                          " If enter button will be pressed, current date will be set automatically");

        if (!DateTime.TryParse(Console.ReadLine(), out var endDate))
        {
            endDate = DateTime.Now;
        }

        return new Tuple<DateTime, DateTime>(startDate, endDate);
    }
}