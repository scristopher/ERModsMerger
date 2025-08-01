using EasyCompressor;
using ERModsMerger.Core.Utility;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ERModsMerger.Core.BHD5Handler;

public class BHD5Reader
{
    #region ORIGINAL CODE
    /*
    private const string Data0 = "Data0";
    private const string Data1 = "Data1";
    private const string Data2 = "Data2";
    private const string Data3 = "Data3";
    private const string DLC = "DLC";
    private static readonly string Data0CachePath = $"{ModsMergerConfig.LoadedConfig.AppDataFolderPath}/BHD5Cache/{Data0}";
    private static readonly string Data1CachePath = $"{ModsMergerConfig.LoadedConfig.AppDataFolderPath}/BHD5Cache/{Data1}";
    private static readonly string Data2CachePath = $"{ModsMergerConfig.LoadedConfig.AppDataFolderPath}/BHD5Cache/{Data2}";
    private static readonly string Data3CachePath = $"{ModsMergerConfig.LoadedConfig.AppDataFolderPath}/BHD5Cache/{Data3}";
    private static readonly string DLCCachePath = $"{ModsMergerConfig.LoadedConfig.AppDataFolderPath}/BHD5Cache/{DLC}";

    private readonly BHDInfo _data0;
    private readonly BHDInfo _data1;
    private readonly BHDInfo _data2;
    private readonly BHDInfo _data3;
    private readonly BHDInfo _dlc;

    

    public BHD5Reader(string path, bool cache, CancellationToken cancellationToken)
    {

        if (!Directory.Exists(ModsMergerConfig.LoadedConfig.AppDataFolderPath + "\\BHD5Cache"))
            Directory.CreateDirectory(ModsMergerConfig.LoadedConfig.AppDataFolderPath + "\\BHD5Cache");

        bool cacheExists = File.Exists(Data0CachePath);
        byte[][] msbBytes = new byte[4][];
        List<Task> tasks = new();
        switch (cacheExists)
        {
            case false:
                tasks.Add(Task.Run(() => { msbBytes[0] = CryptoUtil.DecryptRsa($"{path}/{Data0}.bhd", ArchiveKeys.DATA0, cancellationToken).ToArray(); }));
                break;
            default:
                msbBytes[0] = File.ReadAllBytes(Data0CachePath);
                break;
        }


        try
        {
            Task.WaitAll(tasks.ToArray(), cancellationToken);
        }
        catch (AggregateException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        BHD5 data0 = readBHD5(msbBytes[0]);
        _data0 = new BHDInfo(data0, $"{path}/{Data0}");
        cancellationToken.ThrowIfCancellationRequested();

        if (cache && !cacheExists)
        {
            File.WriteAllBytes($"{Data0CachePath}.bhd", msbBytes[0]);
        }
    }


    private static BHD5 readBHD5(byte[] bytes)
    {
        using MemoryStream fs = new(bytes);
        return BHD5.Read(fs.ReadAllBytes(), BHD5.Game.EldenRing);
    }

    // Right now just works for data0, as that is where all of the files we need, are, and none of the other header files are being loaded, as it takes a while to decrypt them.    
    public byte[]? GetFile(string filePath)
    {
        ulong hash = Utils.ComputeHash(filePath, BHD5.Game.EldenRing);
        byte[]? file = _data0.GetFile(hash);
        if (file != null)
        {
            Debug.WriteLine($"{filePath} Data0: {_data0.GetSalt()}");
            return file;
        }
        // file = _data1.GetFile(hash);
        // if (file != null) {
        //     Debug.WriteLine($"{filePath} Data1: {_data1.GetSalt()}");
        //     return file;
        // }
        // file = _data2.GetFile(hash);
        // if (file != null) {
        //     Debug.WriteLine($"{filePath} Data2: {_data2.GetSalt()}");
        //     return file;
        // }
        // file = _data2.GetFile(hash);
        // if (file != null) {
        //     Debug.WriteLine($"{filePath} Data3: {_data3.GetSalt()}");
        //     return file;
        // }
        return file;
    }
    */

    #endregion


    public static List<BHD5Reader> BHD5Readers { get; set; }

    public string DataName;
    public string DataCachePath;
    public string RSAKey;
    public BHDInfo BHDInfo;

    public BHD5Reader(string dataName, string rsaKey, string pathComplement = "")
    {
        DataName = dataName;
        RSAKey = rsaKey;
        DataCachePath = ModsMergerConfig.LoadedConfig.AppDataFolderPath + "\\BHD5Cache\\" + dataName + ".bhd";

        string dataGamePathNoExt = ModsMergerConfig.LoadedConfig.GamePath + "\\" + pathComplement + DataName;


        if (!Directory.Exists(ModsMergerConfig.LoadedConfig.AppDataFolderPath + "\\BHD5Cache"))
            Directory.CreateDirectory(ModsMergerConfig.LoadedConfig.AppDataFolderPath + "\\BHD5Cache");

        

        CancellationToken cancellationToken = new CancellationToken();

        bool cacheExists = File.Exists(DataCachePath);
        byte[][] msbBytes = new byte[4][];
        List<Task> tasks = new();
        switch (cacheExists)
        {
            case false:
                tasks.Add(Task.Run(() => { msbBytes[0] = CryptoUtil.DecryptRsa(dataGamePathNoExt + ".bhd", ArchiveKeys.DATA0, cancellationToken).ToArray(); }));
                break;
            default:
                msbBytes[0] = File.ReadAllBytes(DataCachePath);
                break;
        }


        try
        {
            Task.WaitAll(tasks.ToArray(), cancellationToken);
        }
        catch (AggregateException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        BHD5 data = readBHD5(msbBytes[0]);
        BHDInfo = new BHDInfo(data, dataGamePathNoExt);
        cancellationToken.ThrowIfCancellationRequested();

        if (!cacheExists)
        {
            File.WriteAllBytes($"{DataCachePath}.bhd", msbBytes[0]);
        }

        if (BHD5Readers == null)
            BHD5Readers = new List<BHD5Reader>();

        BHD5Readers.Add(this);
    }


    public Memory<byte> GetFile(string filePath)
    {
        ulong hash = Utils.ComputeHash(filePath, BHD5.Game.EldenRing);
        byte[]? file = BHDInfo.GetFile(hash);
        if (file != null)
        {
            Debug.WriteLine($"{filePath} Data0: {BHDInfo.GetSalt()}");
            return file;
        }
        return null;
    }


    private BHD5 readBHD5(byte[] bytes)
    {
        using MemoryStream fs = new(bytes);
        return BHD5.Read(fs.ReadAllBytes(), BHD5.Game.EldenRing);
    }

    public static Memory<byte> Read(string filePath)
    {
        //format relative path to match dictionary format and search its index
        var formatedRelativePath = filePath.Replace("\\", "/");
        var dictionary = File.ReadAllText(ModsMergerConfig.LoadedConfig.AppDataFolderPath + "\\Dictionaries\\EldenRingDictionary.txt").Split('\n').ToList();
        var indexFoundInDictionary = dictionary.FindIndex(x=>x.Contains(formatedRelativePath));

        if(indexFoundInDictionary != -1)
        {
            string dataName = "";
            for(int i = indexFoundInDictionary; i > -1; i--)
            {
                if (dictionary[i].StartsWith("#"))
                {
                    dataName = dictionary[i].Replace("#", "").Replace("\r", "");
                    break;
                }
            }

            if (BHD5Readers == null)
                BHD5Readers= new List<BHD5Reader>();

            var bhdReaderFound = BHD5Readers.FindIndex(x=>x.DataName == dataName);
            if (bhdReaderFound != -1)
                return BHD5Readers[bhdReaderFound].GetFile(formatedRelativePath);
            else if(dataName == "Data0")
                return new BHD5Reader(dataName, ArchiveKeys.DATA0).GetFile(formatedRelativePath);
            else if (dataName == "Data1")
                return new BHD5Reader(dataName, ArchiveKeys.DATA1).GetFile(formatedRelativePath);
            else if (dataName == "Data2")
                return new BHD5Reader(dataName, ArchiveKeys.DATA2).GetFile(formatedRelativePath);
            else if (dataName == "Data3")
                return new BHD5Reader(dataName, ArchiveKeys.DATA3).GetFile(formatedRelativePath);
            else if (dataName == "DLC")
                return new BHD5Reader(dataName, ArchiveKeys.SD).GetFile(formatedRelativePath);
            else if (dataName == "sd")
                return new BHD5Reader(dataName, ArchiveKeys.SD).GetFile(formatedRelativePath);
            else if (dataName == "sd_dlc02")
                return new BHD5Reader(dataName, ArchiveKeys.SD).GetFile(formatedRelativePath);

        }

        return null;
    }

   

}

public static class ArchiveKeys
{
    public const string DATA0 = @"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEA9Rju2whruXDVQZpfylVEPeNxm7XgMHcDyaaRUIpXQE0qEo+6Y36L
P0xpFvL0H0kKxHwpuISsdgrnMHJ/yj4S61MWzhO8y4BQbw/zJehhDSRCecFJmFBz
3I2JC5FCjoK+82xd9xM5XXdfsdBzRiSghuIHL4qk2WZ/0f/nK5VygeWXn/oLeYBL
jX1S8wSSASza64JXjt0bP/i6mpV2SLZqKRxo7x2bIQrR1yHNekSF2jBhZIgcbtMB
xjCywn+7p954wjcfjxB5VWaZ4hGbKhi1bhYPccht4XnGhcUTWO3NmJWslwccjQ4k
sutLq3uRjLMM0IeTkQO6Pv8/R7UNFtdCWwIERzH8IQ==
-----END RSA PUBLIC KEY-----";

    public const string DATA1 = @"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAxaBCHQJrtLJiJNdG9nq3deA9sY4YCZ4dbTOHO+v+YgWRMcE6iK6o
ZIJq+nBMUNBbGPmbRrEjkkH9M7LAypAFOPKC6wMHzqIMBsUMuYffulBuOqtEBD11
CAwfx37rjwJ+/1tnEqtJjYkrK9yyrIN6Y+jy4ftymQtjk83+L89pvMMmkNeZaPON
4O9q5M9PnFoKvK8eY45ZV/Jyk+Pe+xc6+e4h4cx8ML5U2kMM3VDAJush4z/05hS3
/bC4B6K9+7dPwgqZgKx1J7DBtLdHSAgwRPpijPeOjKcAa2BDaNp9Cfon70oC+ZCB
+HkQ7FjJcF7KaHsH5oHvuI7EZAl2XTsLEQIENa/2JQ==
-----END RSA PUBLIC KEY-----";

    public const string DATA2 = @"-----BEGIN RSA PUBLIC KEY-----
MIIBDAKCAQEA0iDVVQ230RgrkIHJNDgxE7I/2AaH6Li1Eu9mtpfrrfhfoK2e7y4O
WU+lj7AGI4GIgkWpPw8JHaV970Cr6+sTG4Tr5eMQPxrCIH7BJAPCloypxcs2BNfT
GXzm6veUfrGzLIDp7wy24lIA8r9ZwUvpKlN28kxBDGeCbGCkYeSVNuF+R9rN4OAM
RYh0r1Q950xc2qSNloNsjpDoSKoYN0T7u5rnMn/4mtclnWPVRWU940zr1rymv4Jc
3umNf6cT1XqrS1gSaK1JWZfsSeD6Dwk3uvquvfY6YlGRygIlVEMAvKrDRMHylsLt
qqhYkZNXMdy0NXopf1rEHKy9poaHEmJldwIFAP////8=
-----END RSA PUBLIC KEY-----";

    public const string DATA3 = @"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAvRRNBnVq3WknCNHrJRelcEA2v/OzKlQkxZw1yKll0Y2Kn6G9ts94
SfgZYbdFCnIXy5NEuyHRKrxXz5vurjhrcuoYAI2ZUhXPXZJdgHywac/i3S/IY0V/
eDbqepyJWHpP6I565ySqlol1p/BScVjbEsVyvZGtWIXLPDbx4EYFKA5B52uK6Gdz
4qcyVFtVEhNoMvg+EoWnyLD7EUzuB2Khl46CuNictyWrLlIHgpKJr1QD8a0ld0PD
PHDZn03q6QDvZd23UW2d9J+/HeBt52j08+qoBXPwhndZsmPMWngQDaik6FM7EVRQ
etKPi6h5uprVmMAS5wR/jQIVTMpTj/zJdwIEXszeQw==
-----END RSA PUBLIC KEY-----";

    //for DLC too
    public const string SD = @"-----BEGIN RSA PUBLIC KEY-----
MIIBCwKCAQEAmYJ/5GJU4boJSvZ81BFOHYTGdBWPHnWYly3yWo01BYjGRnz8NTkz
DHUxsbjIgtG5XqsQfZstZILQ97hgSI5AaAoCGrT8sn0PeXg2i0mKwL21gRjRUdvP
Dp1Y+7hgrGwuTkjycqqsQ/qILm4NvJHvGRd7xLOJ9rs2zwYhceRVrq9XU2AXbdY4
pdCQ3+HuoaFiJ0dW0ly5qdEXjbSv2QEYe36nWCtsd6hEY9LjbBX8D1fK3D2c6C0g
NdHJGH2iEONUN6DMK9t0v2JBnwCOZQ7W+Gt7SpNNrkx8xKEM8gH9na10g9ne11Mi
O1FnLm8i4zOxVdPHQBKICkKcGS1o3C2dfwIEXw/f3w==
-----END RSA PUBLIC KEY-----";
}

