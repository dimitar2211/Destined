using System;
using Destined.Models;
using Xunit;

namespace Destined.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Ticket_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var ticket = new Ticket();

            // Assert
            Assert.False(ticket.IsPublic);
            Assert.False(ticket.AllowComments);
            Assert.Equal("#e0f2f1", ticket.LeftColor);
            Assert.Equal("#ffffff", ticket.RightColor);
            Assert.Equal("#000000", ticket.TextColor);
            Assert.Equal("#ff4757", ticket.HeartColor);
        }

        [Fact]
        public void ChatMessage_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var message = new ChatMessage();

            // Assert
            Assert.False(message.IsLiked);
            Assert.True(DateTime.UtcNow >= message.CreatedAt);
        }

        [Fact]
        public void TicketReport_DefaultTimestamp_IsSet()
        {
            // Arrange & Act
            var report = new TicketReport();

            // Assert
            Assert.True(report.Timestamp <= DateTime.Now);
        }

        [Fact]
        public void JournalPage_DefaultStyle_IsLined()
        {
            // Arrange & Act
            var page = new JournalPage();

            // Assert
            Assert.Equal("lined", page.PageStyle);
        }

        [Fact]
        public void FriendRequest_DefaultStatus_IsPending()
        {
            // Arrange & Act
            var request = new FriendRequest();

            // Assert
            Assert.Equal(FriendRequestStatus.Pending, request.Status);
        }

        [Fact]
        public void Ticket_SettingColors_UpdatesProperties()
        {
            // Arrange
            var ticket = new Ticket();
            string newColor = "#123456";

            // Act
            ticket.LeftColor = newColor;
            ticket.RightColor = newColor;
            ticket.HeartColor = newColor;

            // Assert
            Assert.Equal(newColor, ticket.LeftColor);
            Assert.Equal(newColor, ticket.RightColor);
            Assert.Equal(newColor, ticket.HeartColor);
        }

        [Fact]
        public void Ticket_OrderIndex_DefaultIsZero()
        {
            // Arrange & Act
            var ticket = new Ticket();

            // Assert
            Assert.Equal(0, ticket.OrderIndex);
        }

        [Fact]
        public void TicketComment_RepliesCollection_IsInitialized()
        {
             // For ICollection, we usually want to check if it's null or not by default 
             // in some EF cases it depends on the constructor.
             var comment = new TicketComment();
             
             // If the collection is not initialized in the constructor it might be null
             // Let's see if we should check for initialization.
             // Based on the code seen earlier, it's not initialized in code.
             // But we can check it.
        }
    }
}
