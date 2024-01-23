using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace Travel_App_Web.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FileController : Controller
    {
        private readonly string uploadDirectory = "wwwroot/images";

        [HttpPost("upload-tour-image")]
        public async Task<ActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file");

            if (!IsImage(file))
            {
                return BadRequest("Invalid file type. Only images are allowed.");
            }

            string directory = uploadDirectory + "/tour";
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(directory, fileName);
                        
            Directory.CreateDirectory(directory);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            filePath = filePath.Replace("wwwroot", "");
            filePath = filePath.Replace("\\", "/");
            return Ok(filePath);
        }

        [HttpPost("upload-{pathUnit}-images")]
        public async Task<ActionResult<List<string>>> UploadFiles(string pathUnit)
        {
            var files = Request.Form.Files;
            string directory;

            if (files == null || files.Count == 0)
            {
                return BadRequest("Invalid files");
            }

            switch (pathUnit)
            {
                case "days":
                    directory = uploadDirectory + "/tour/day";
                    break;
                case "hotel":
                    directory = uploadDirectory + "/hotel";
                    break;
                case "bus":
                    directory = uploadDirectory + "/bus";
                    break;
                default:
                    return BadRequest("Wrong API path");
            }

            List<string> filePaths = new List<string>();

            foreach (var file in files)
            {
                if (file == null || file.Length == 0 || !IsImage(file))
                {
                    return BadRequest("Invalid file type.Only images are allowed.");
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(directory, fileName);

                Directory.CreateDirectory(directory);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                filePath = filePath.Replace("wwwroot", "");
                filePath = filePath.Replace("\\", "/");
                filePaths.Add(filePath);
            }

            return Ok(filePaths);
        }

        //[HttpPost("upload-hotel-images")]
        //public async Task<ActionResult<List<string>>> UploadHotelFiles()
        //{
        //    var files = Request.Form.Files;

        //    if (files == null || files.Count == 0)
        //    {
        //        return BadRequest("Invalid files");
        //    }

        //    List<string> filePaths = new List<string>();

        //    foreach (var file in files)
        //    {
        //        if (file == null || file.Length == 0 || !IsImage(file))
        //        {
        //            return BadRequest("Invalid file type.Only images are allowed.");
        //        }

        //        string directory = uploadDirectory + "/hotel";
        //        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        //        var filePath = Path.Combine(directory, fileName);

        //        Directory.CreateDirectory(directory);

        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await file.CopyToAsync(stream);
        //        }

        //        filePath = filePath.Replace("wwwroot", "");
        //        filePath = filePath.Replace("\\", "/");
        //        filePaths.Add(filePath);
        //    }

        //    return Ok(filePaths);
        //}

        //[HttpPost("upload-bus-images")]
        //public async Task<ActionResult<List<string>>> UploadBusFiles()
        //{
        //    var files = Request.Form.Files;

        //    if (files == null || files.Count == 0)
        //    {
        //        return BadRequest("Invalid files");
        //    }

        //    List<string> filePaths = new List<string>();

        //    foreach (var file in files)
        //    {
        //        if (file == null || file.Length == 0 || !IsImage(file))
        //        {
        //            return BadRequest("Invalid file type.Only images are allowed.");
        //        }

        //        string directory = uploadDirectory + "/bus";
        //        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        //        var filePath = Path.Combine(directory, fileName);

        //        Directory.CreateDirectory(directory);

        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await file.CopyToAsync(stream);
        //        }

        //        filePath = filePath.Replace("wwwroot", "");
        //        filePath = filePath.Replace("\\", "/");
        //        filePaths.Add(filePath);
        //    }

        //    return Ok(filePaths);
        //}


        private bool IsImage(IFormFile file)
        {
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    IImageFormat imageFormat = Image.DetectFormat(stream);
                    return imageFormat != null;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
