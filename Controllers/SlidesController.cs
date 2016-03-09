using System.IO;
using System.Collections.Generic;

using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;
using Microsoft.AspNet.Http;
using Microsoft.WindowsAzure.Storage.File;

using OpenXmlPowerTools;

using Apoffice.Utils;


namespace Apoffice.Controllers
{

    [Route("api/[controller]")]
    public class SlidesController : Controller
    {
        private readonly ILogger<SlidesController>  _logger;
        private Storage _storageClient;

        public SlidesController(ILogger<SlidesController> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _storageClient = new Storage(appSettings);
        }

        // POST api/slides/merge/outputFilePath
        [HttpPost("merge/{*path}")]
        public void Post(string path, IFormCollection formData)
        {
            List<SlideSource> sources = new List<SlideSource>();
            CloudFileDirectory cloudFileDirectory = _storageClient.getDirectory("slides");
            MemoryStream ms = null;
            CloudFile cloudFile = null;
            PmlDocument pml = null;

            foreach (string inputFilePath in formData["inputFilesPath"])
            {
                ms = new MemoryStream();
                cloudFile = cloudFileDirectory.GetFileReference(inputFilePath);

                cloudFile.DownloadToStream(ms);
                ms.Position = 0;
                _logger.LogInformation("File downloaded: " + inputFilePath);

                pml = new PmlDocument(inputFilePath, ms);
                sources.Add(new SlideSource(new PmlDocument(pml), true));
            }

            pml = PresentationBuilder.BuildPresentation(sources);
            ms = new MemoryStream();
            pml.WriteByteArray(ms);
            ms.Position = 0;

            cloudFile = cloudFileDirectory.GetFileReference(path);
            cloudFile.UploadFromStream(ms);
        }

        // PUT api/slides/replace/filePath
        [HttpPut("replace/{*path}")]
        public void Put(string path, IFormCollection formData)
        {
            CloudFileDirectory cloudFileDirectory = _storageClient.getDirectory("slides");
            MemoryStream ms = new MemoryStream();
            CloudFile cloudFile = cloudFileDirectory.GetFileReference(path);
            cloudFile.DownloadToStream(ms);
            ms.Position = 0;

            PmlDocument pml = new PmlDocument(path, ms);
            PmlDocument pmlReplaced = pml.SearchAndReplace(formData["oldValue"], formData["newValue"], true);

            ms = new MemoryStream();
            pmlReplaced.WriteByteArray(ms);
            ms.Position = 0;
            cloudFile = cloudFileDirectory.GetFileReference(path);
            cloudFile.UploadFromStream(ms);
        }
    }
}
