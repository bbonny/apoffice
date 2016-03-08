using System.IO;
using System.Collections.Generic;

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.File;

using Apoffice.Utils;
using Microsoft.Extensions.OptionsModel;


namespace Apoffice.Controllers
{

    [Route("api/[controller]")]
    public class FilesController : Controller
    {
        private readonly ILogger<FilesController>  _logger;
        private Storage _storageClient;

        public FilesController(ILogger<FilesController> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _storageClient = new Storage(appSettings);
        }

        // GET: api/files/list/path
        [HttpGet("list/{*path}")]
        public IEnumerable<string> GetList(string path)
        {
            CloudFileDirectory directory = _storageClient.getDirectory("slides/" + path);
            FileResultSegment result = directory.ListFilesAndDirectoriesSegmented(new FileContinuationToken());
            List<string> fileNames = new List<string>();

            foreach (IListFileItem item in result.Results) {
                if (item is CloudFile)
                {
                    fileNames.Add(((CloudFile)item).Name);
                }
                else if (item is CloudFileDirectory)
                {
                    fileNames.Add(((CloudFileDirectory)item).Name);
                }
            }
            return fileNames;
        }

        // GET api/files/path
        [HttpGet("{*path}")]
        public FileResult Get(string path)
        {
            CloudFileDirectory cloudFileDirectory = _storageClient.getDirectory("slides");
            MemoryStream ms = new MemoryStream();
            CloudFile cloudFile = null;

            cloudFile = cloudFileDirectory.GetFileReference(path);
            cloudFile.DownloadToStream(ms);

            return new FileContentResult(ms.ToArray(), "application/octet-stream");
        }

        // POST api/files/directory/path
        [HttpPost("directory/{*path}")]
        public void Post(string path)
        {
            CloudFileDirectory cloudDirectory = _storageClient.getDirectory("slides").GetDirectoryReference(path);

            if (cloudDirectory.Exists()) {
                _logger.LogInformation("Directory already exists");
            } else {
                cloudDirectory.CreateAsync();
            }

        }

        // PUT api/files/path
        [HttpPut("{*path}")]
        public void Put(string path, IFormFile inputFile)
        {
            _logger.LogInformation("Going to put file on server");
            _logger.LogInformation(path);

            CloudFile cloudFile = _storageClient.getDirectory("slides").GetFileReference(path);

            using (var fileStream = inputFile.OpenReadStream())
            {
                using (var memoryStream = new MemoryStream())
                {
                    fileStream.CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    cloudFile.UploadFromStream(memoryStream);
                    _logger.LogInformation("File uploaded");
                }
            }
        }

        // DELETE api/files/path
        [HttpDelete("{*path}")]
        public void Delete(string path)
        {
            _logger.LogInformation("Going to delete file on server");
            _logger.LogInformation(path);

            CloudFile cloudFile = _storageClient.getDirectory("slides").GetFileReference(path);
            cloudFile.DeleteAsync();
        }
    }
}
