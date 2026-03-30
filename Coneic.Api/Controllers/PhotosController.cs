using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Coneic.Api.Data;
using Coneic.Api.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Coneic.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PhotosController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/photos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Photo>>> GetPhotos()
        {
            return await _context.Photos
                .Where(p => p.IsApproved)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        // POST: api/photos
        [HttpPost]
        public async Task<ActionResult<Photo>> UploadPhoto([FromForm] IFormFile file, [FromForm] string description, [FromForm] string uploadedBy)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var photo = new Photo
            {
                Url = $"/uploads/{uniqueFileName}", // Relative path to be served statically
                Description = description ?? "",
                UploadedBy = uploadedBy ?? "Anonymous",
                CreatedAt = DateTime.UtcNow,
                IsApproved = true
            };

            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPhotos), new { id = photo.Id }, photo);
        }
    }
}
