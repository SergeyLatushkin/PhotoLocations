using PortableDeviceApiLib;
using System.Collections.ObjectModel;

namespace LocationsFromPhotos;

internal class PDCollection(DateTime startDate, DateTime endDate) : Collection<PD>
{
    private readonly PortableDeviceManager _deviceManager = new PortableDeviceManagerClass();

    public void Refresh()
    {
        _deviceManager.RefreshDeviceList();

        string[] deviceIds = new string[1];
        uint pcPnPDeviceIDs = 1u;
        _deviceManager.GetDevices(ref deviceIds[0], ref pcPnPDeviceIDs);

        if (pcPnPDeviceIDs == 0) return;

        deviceIds = new string[pcPnPDeviceIDs];
        _deviceManager.GetDevices(ref deviceIds[0], ref pcPnPDeviceIDs);

        foreach (string deviceId in deviceIds)
        {
            this.Add(new PD(deviceId, startDate, endDate));
        }
    }
}