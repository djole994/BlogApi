using Microsoft.AspNetCore.Mvc;
using BlogAPI.Models;
using BlogApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BlogApi.Models;

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

        [HttpGet("post/{postId}")]
        public async Task<IActionResult> GetCommentsForPost(int postId)
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.BlogPostId == postId)
                .Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    BlogPostId = c.BlogPostId,
                    UserId = c.UserId,
                    ParentCommentId = c.ParentCommentId,
                    User = c.User == null ? null : new UserDto
                    {
                        Id = c.User.Id,
                        Username = c.User.Username,
                        ProfileImageUrl = c.User.ProfileImageUrl
                    }
                })
                .ToListAsync();

            return Ok(comments);
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            var post = await _context.BlogPosts.FindAsync(dto.BlogPostId);

            if (user == null || post == null)
                return BadRequest("Neispravan korisnik ili post.");

            var comment = new Comment
            {
                Content = dto.Content,
                BlogPostId = post.Id,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ParentCommentId = dto.ParentCommentId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // NOVO: Kreiranje notifikacije autoru posta
            var postAuthor = await _context.Users.FindAsync(post.UserId);
            if (postAuthor == null)
                return BadRequest("Autor posta nije pronađen.");

            var message = $"Korisnik '{user.Username}' je komentarisao Vaš post '{post.Title}'.";
            var notification = new Notification
            {
                UserId = postAuthor.Id,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(comment);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentDto dto)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
                return NotFound("Komentar nije pronađen.");

      
            comment.Content = dto.Content;
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

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || userId != comment.UserId.ToString())
                return Forbid("Nije dozvoljeno brisanje tuđih komentara.");

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
        public int? ParentCommentId { get; set; } 

    }
    public class UpdateCommentDto
    {
        public string Content { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
    }

    public class CommentResponseDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int BlogPostId { get; set; }
        public int UserId { get; set; }
        public UserDto? User { get; set; }
        public int? ParentCommentId { get; set; }

    }
}
