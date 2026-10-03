using OnlineJobAssignment.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineJobAssignment.ViewModels
{
    public class MessageConversationViewModel
    {
        public Job Job { get; set; } = null!;
        
        public List<Message> Messages { get; set; } = new List<Message>();

        [Required(ErrorMessage = "Message content is required.")]
        [MaxLength(1000, ErrorMessage = "Message cannot exceed 1000 characters.")]
        public string NewMessageContent { get; set; } = string.Empty;

        public string CurrentUserId { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
    }
}
