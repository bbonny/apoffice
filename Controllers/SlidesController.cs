using System.IO;
using System.Collections.Generic;

using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;
using Microsoft.AspNet.Http;
using Microsoft.WindowsAzure.Storage.File;

using OpenXmlPowerTools;

using Apoffice.Utils;

public class Slides
{
    public string path { set; get; }
    public int start { set; get; }
    public int count { set; get; }
}

public class MergeParameters
{
    public string outputPath { set; get; }
    public List<Slides> inputSlides { set; get; }
}

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

        // POST api/slides/merge
        [HttpPost("merge")]
        public void Post([FromBody] MergeParameters mergeParameters)
        {
            List<SlideSource> sources = new List<SlideSource>();
            CloudFileDirectory cloudFileDirectory = _storageClient.getDirectory("slides");
            MemoryStream ms = null;
            CloudFile cloudFile = null;
            PmlDocument pml = null;

            foreach (Slides slides in mergeParameters.inputSlides)
            {
                ms = new MemoryStream();
                cloudFile = cloudFileDirectory.GetFileReference(slides.path);

                cloudFile.DownloadToStream(ms);
                ms.Position = 0;
                _logger.LogInformation("File downloaded: " + slides.path);

                pml = new PmlDocument(slides.path, ms);
                sources.Add(new SlideSource(new PmlDocument(pml), slides.start, slides.count, false));
            }

            pml = PresentationBuilder.BuildPresentation(sources);
            ms = new MemoryStream();
            pml.WriteByteArray(ms);
            ms.Position = 0;

            cloudFile = cloudFileDirectory.GetFileReference(mergeParameters.outputPath);
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
