using Microsoft.AspNetCore.Mvc;
using BlogAPI.Models;
using BlogApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BlogAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        private readonly ApplicationDbContext _context;

        public PostsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }


        // GET: api/posts
        [HttpGet]
        public async Task<IActionResult> GetPosts()
        {
            var posts = await _context.BlogPosts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .ToListAsync();
            return Ok(posts);
        }

        // GET: api/posts/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            var post = await _context.BlogPosts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
                return NotFound("Post nije pronađen.");
            return Ok(post);
        }

        // POST: api/posts
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return BadRequest("Neispravan korisnik.");

            string? imageUrl = null;
            if (dto.PostImage != null && dto.PostImage.Length > 0)
            {
                var webRoot = _env.WebRootPath ?? Directory.GetCurrentDirectory();
                var uploadsDir = Path.Combine(webRoot, "uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.PostImage.FileName);
                var filePath = Path.Combine(uploadsDir, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.PostImage.CopyToAsync(stream);
                }
                imageUrl = $"/uploads/{uniqueFileName}";
            }

            var post = new BlogPost
            {
                Title = dto.Title,
                Content = dto.Content,
                ImageUrl = imageUrl,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.BlogPosts.Add(post);
            await _context.SaveChangesAsync();
            return Ok(post);
        }



        // PUT: api/posts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] UpdatePostDto dto)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
                return NotFound("Post nije pronađen.");
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (post.UserId != currentUserId)
                return Forbid("Samo kreator posta može da ga uređuje.");

            post.Title = dto.Title;
            post.Content = dto.Content;
            if (dto.PostImage != null && dto.PostImage.Length > 0)
            {

                var webRoot = _env.WebRootPath ?? Directory.GetCurrentDirectory();
                var uploadsDir = Path.Combine(webRoot, "uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var uniqueFileName = Guid.NewGuid().ToString()
                                     + Path.GetExtension(dto.PostImage.FileName);

                var filePath = Path.Combine(uploadsDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.PostImage.CopyToAsync(stream);
                }

                post.ImageUrl = $"/uploads/{uniqueFileName}";
            }

            await _context.SaveChangesAsync();
            return Ok(post);
        }

        // DELETE: api/posts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
                return NotFound("Post nije pronađen.");
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (post.UserId != currentUserId)
                return Forbid("Samo kreator posta može da ga obriše.");




            _context.BlogPosts.Remove(post);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Post je obrisan." });
        }
    }

    // DTO klase za kreiranje i ažuriranje postova
    public class CreatePostDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public IFormFile? PostImage { get; set; }
    }

    public class UpdatePostDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public IFormFile? PostImage { get; set; }
    }
}
