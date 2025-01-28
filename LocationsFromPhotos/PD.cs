using LocationsFromPhotos.Models;
using LocationsFromPhotos.Models.Base;
using PortableDeviceApiLib;
using System.Runtime.InteropServices;

namespace LocationsFromPhotos;

internal class PD(string deviceId, DateTime startDate, DateTime endDate)
{
    private bool _isConnected;
    private readonly PortableDeviceClass _device = new();
    private IPortableDeviceResources _pdResources;
    private IPortableDeviceContent _ppContent;
    private IPortableDeviceProperties _ppProperties;

    public static int Counter { get; private set; }
    public string FriendlyName
    {
        get
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to device.");
            }

            _ppProperties.GetValues("DEVICE", null, out IPortableDeviceValues ppValues);
            _tagpropertykey key = default;
            key.fmtid = new Guid(651466650u, 58947, 17958, 158, 43, 115, 109, 192, 201, 47, 220);
            key.pid = 12u;
            ppValues.GetStringValue(ref key, out var pValue);

            return pValue;
        }
    }

    public void Connect()
    {
        if (_isConnected) return;

        var pClientInfo = (IPortableDeviceValues)new PortableDeviceTypesLib.PortableDeviceValuesClass();
        _device.Open(deviceId, pClientInfo);
        _device.Content(out _ppContent);
        _ppContent.Transfer(out _pdResources);
        _ppContent.Properties(out _ppProperties);

        _isConnected = true;
    }

    public void Disconnect()
    {
        if (!_isConnected) return;

        _device.Close();
        _isConnected = false;
    }

    public PDFileStream DownloadFile(string pObjIDs)
    {
        IStream? ppStream = null;

        try
        {
            var key = new _tagpropertykey
            {
                fmtid = new Guid(0xE81E79BE, 0x34F0, 0x41BF, 0xB5, 0x3F, 0xF1, 0xA0, 0x6A, 0xE8, 0x78, 0x42),
                pid = 0u
            };

            uint optimalBufferSize = 0u;

            _pdResources.GetStream(
                pObjIDs,
                ref key,
                0,
                ref optimalBufferSize,
                out ppStream);

            return new PDFileStream(ppStream);
        }
        finally
        {
            if (ppStream != null)
            {
                Marshal.ReleaseComObject(ppStream);
            }
        }
    }

    public PDFolder GetContents()
    {
        var portableDeviceFolder = new PDFolder("DEVICE", FriendlyName, "DEVICE");
        EnumerateContents(portableDeviceFolder);

        return portableDeviceFolder;
    }

    private void EnumerateContents(PDFolder parent)
    {
        _ppContent.EnumObjects(0u, parent.Id, null, out IEnumPortableDeviceObjectIDs ppenum);

        uint pcFetched = 0u;
        do
        {
            ppenum.Next(1u, out string pObjIDs, ref pcFetched);
            if (pcFetched == 0) continue;

            PDObject portableDeviceObject = WrapObject(pObjIDs);
            switch (portableDeviceObject)
            {
                case PDFile file when
                    startDate <= file.CreatedDate && file.CreatedDate <= endDate:
                    parent.Objects.Add(portableDeviceObject);
                    Counter++;
                    break;
                case PDFolder folder:
                    parent.Objects.Add(folder);
                    EnumerateContents(folder);
                    break;
            }
        }
        while (pcFetched != 0);
    }

    private PDObject WrapObject(string pObjIDs)
    {
        _ppProperties.GetSupportedProperties(pObjIDs, out var ppKeys);
        _ppProperties.GetValues(pObjIDs, ppKeys, out var ppValues);
        _tagpropertykey tagPropertyKey = default;
        tagPropertyKey.fmtid = new Guid(4016785677u, 23768, 17274, 175, 252, 218, 139, 96, 238, 74, 60);

        tagPropertyKey.pid = 4u;
        if (!PropertyExists(pObjIDs, tagPropertyKey))
        {
            tagPropertyKey.pid = 3u;
        }

        IPortableDeviceValues pdValues = ppValues;
        _tagpropertykey key = tagPropertyKey;
        pdValues.GetStringValue(ref key, out string nameValue);

        tagPropertyKey = default;
        tagPropertyKey.fmtid = new Guid(4016785677u, 23768, 17274, 175, 252, 218, 139, 96, 238, 74, 60);
        tagPropertyKey.pid = 7u;
        IPortableDeviceValues pdValues2 = ppValues;
        key = tagPropertyKey;
        pdValues2.GetGuidValue(ref key, out var pValue2);

        if (pValue2 == new Guid(669180818u, 41233, 18656, 171, 12, 225, 119, 5, 160, 95, 133) ||
            pValue2 == new Guid(2582446432u, 6143, 19524, 157, 152, 29, 122, 111, 148, 25, 33))
        {
            return new PDFolder(pObjIDs, nameValue, "FOLDER");
        }

        tagPropertyKey.pid = 12u;
        IPortableDeviceValues pdValues3 = ppValues;
        key = tagPropertyKey;
        pdValues3.GetStringValue(ref key, out nameValue);

        tagPropertyKey.pid = 18u;
        IPortableDeviceValues pdValues4 = ppValues;
        key = tagPropertyKey;
        pdValues4.GetStringValue(ref key, out string dateValue);

        return new PDFile(pObjIDs, nameValue, "FILE", dateValue);
    }

    private bool PropertyExists(string pObjIDs, _tagpropertykey key)
    {
        var keys = GetSupportedPropertyKeys(pObjIDs);
        return keys.Any(k => k.fmtid == key.fmtid && k.pid == key.pid);
    }

    private List<_tagpropertykey> GetSupportedPropertyKeys(string pObjIDs)
    {
        _ppProperties.GetSupportedProperties(pObjIDs, out var ppKeys);

        uint count = 0;
        ppKeys.GetCount(ref count);

        var keys = new List<_tagpropertykey>();
        for (uint i = 0; i < count; i++)
        {
            _tagpropertykey key = default;
            ppKeys.GetAt(i, ref key);
            keys.Add(key);
        }

        return keys;
    }
}