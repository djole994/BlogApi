using Microsoft.AspNetCore.Mvc;
using BlogAPI.Models;
using BlogApi.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/comments/post/5 - svi komentari za dati post
        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetCommentsForPost(int postId)
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.BlogPostId == postId)
                .ToListAsync();
            return Ok(comments);
        }

        // POST: api/comments
        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto dto)
        {
            // U stvarnoj aplikaciji, korisnikov Id bi se izvukao iz tokena
            var user = await _context.Users.FindAsync(dto.UserId);
            var post = await _context.BlogPosts.FindAsync(dto.BlogPostId);

            if (user == null || post == null)
                return BadRequest("Neispravan korisnik ili post.");

            var comment = new Comment
            {
                Content = dto.Content,
                BlogPostId = post.Id,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok(comment);
        }

        // DELETE: api/comments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
                return NotFound("Komentar nije pronađen.");

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Komentar je obrisan." });
        }
    }

    // DTO klasa za kreiranje komentara
    public class CreateCommentDto
    {
        public string Content { get; set; } = string.Empty;
        public int BlogPostId { get; set; }
        public int UserId { get; set; }
    }
}
