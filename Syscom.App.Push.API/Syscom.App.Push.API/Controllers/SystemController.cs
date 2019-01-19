using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Syscom.App.Push.API.Models;

using Syscom.App.Push.API.Libs;

namespace Syscom.App.Push.API.Controllers
{
    [Route("api/v1")]
    public class SystemController : Controller
    {
        private readonly string _folder;
        private readonly ILogger _logger;
        public SystemController(IHostingEnvironment env,
                             AppPushDBContext context,
                             ILogger<AppController> logger)
        {
            // 把上傳目錄設為：wwwroot\UploadFolder
            _folder = $@"{env.WebRootPath}{Path.DirectorySeparatorChar}Uploads";
            _logger = logger;
        }

        // POST api/values/5
        [HttpPost("image/upload")]
        public async Task<JObject> ImageUpload(IFormFile photo)
        {
            System.IO.Directory.CreateDirectory(_folder);

            dynamic res = new JObject();

            _logger.LogWarning(_folder);
            _logger.LogWarning(photo + "");
            if (photo == null )
            {
                res.code = 404;
                res.reason = "file not found";
                return res;
            }

            string extension = Path.GetExtension(photo.FileName);

            Stream st = photo.OpenReadStream();
            MemoryStream mst = new MemoryStream();
            st.CopyTo(mst);

            string filename = $@"{Helpers.ToMD5Hash(mst.ToArray())}{extension}";

            var path = $@"{_folder}{Path.DirectorySeparatorChar}{filename}";
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            res.code = 200;
            res.reason = "success";
            res.data = filename;
            return res;
        }
    }
}
