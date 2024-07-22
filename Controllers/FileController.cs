using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp.Formats;
using Models;

namespace Travel_App_Web.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FileController : Controller
    {
        private readonly string uploadDirectory = "wwwroot/images";

        [HttpPost("upload-tour-image")]
        public async Task<ActionResult> UploadFile(UploadImage uploadModel)
        {
            if (uploadModel == null || uploadModel.Size == 0)
                return BadRequest("Incorrect file");

            var file = uploadModel.ToFormFile();

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

        [HttpGet("delete-file/{filePath}")]
        public IActionResult DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return BadRequest("Incorrect file path");
            }

            try
            {
                filePath = filePath.Replace('_', '/');
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath.TrimStart('/'));

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    return Ok("File successfully deleted");
                }
                else
                {
                    return NotFound("File not found");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting the file: {ex.Message}");
            }
        }

        private bool IsImage(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                IImageFormat imageFormat = Image.DetectFormat(stream);
                return imageFormat != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
