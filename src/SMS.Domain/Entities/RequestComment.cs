using SMS.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMS.Domain.Entities
{
    /// <summary>
    /// A comment/discussion entry on an SMS request.
    /// </summary>
    [Table("sms_request_comments")]
    public class RequestComment : BaseEntity, ITenantAwareEntity
    {
        [Column("request_id")]
        public Guid RequestId { get; set; }

        [Required]
        [MaxLength(100)]
        public string AuthorUserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAtUtc { get; set; }

        [MaxLength(2000)]
        public string? EditedMessage { get; set; }

        public bool IsEdited => !string.IsNullOrEmpty(EditedMessage);

        [MaxLength(100)]
        public string? EditedByUserId { get; set; }

        public virtual Request Request { get; set; } = null!;

        private RequestComment() { }

        public static RequestComment Create(Guid requestId, string authorUserId, string message)
        {
            if (string.IsNullOrWhiteSpace(authorUserId))
                throw new ArgumentException("Author user is required.", nameof(authorUserId));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message is required.", nameof(message));

            return new RequestComment
            {
                RequestId = requestId,
                AuthorUserId = authorUserId,
                Message = message.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        public void Edit(string editorUserId, string newMessage)
        {
            if (string.IsNullOrWhiteSpace(editorUserId))
                throw new ArgumentException("Editor user is required.", nameof(editorUserId));
            if (string.IsNullOrWhiteSpace(newMessage))
                throw new ArgumentException("New message is required.", nameof(newMessage));

            EditedMessage = newMessage.Trim();
            EditedAtUtc = DateTime.UtcNow;
            EditedByUserId = editorUserId;
        }
    }
}
