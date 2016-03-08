using System;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.Azure;
using Microsoft.Extensions.OptionsModel;

namespace Apoffice.Utils
{

    public class Storage
    {
        private CloudFileClient _fileClient;
        private IOptions<AppSettings> _appSettings;

        public Storage(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings;
            _fileClient = CloudStorageAccount.Parse(_appSettings.Value.StorageConnectionString).CreateCloudFileClient();
        }

        private CloudFileShare getShare()
        {
            CloudFileShare share = _fileClient.GetShareReference(_appSettings.Value.StorageRootDirectoryName);
            if (share.Exists())
            {
                return share;
            }
            throw new Exception("Share does not exist");
        }

        public CloudFileDirectory getDirectory(string directoryPath)
        {
            CloudFileDirectory rootDir = this.getShare().GetRootDirectoryReference();
            CloudFileDirectory sampleDir = rootDir.GetDirectoryReference(directoryPath);

            if (sampleDir.Exists())
            {
                return sampleDir;
            }
            throw new Exception("Directory does not exist");
        }
    }
}
