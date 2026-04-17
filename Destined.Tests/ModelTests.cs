using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Destined.Models;
using Xunit;

namespace Destined.Tests
{
    public class ModelTests
    {
        private IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }

        // --- Ticket Tests ---

        [Fact]
        public void Ticket_ValidData_PassesValidation()
        {
            var ticket = new Ticket
            {
                To = "Paris",
                DepartureTime = DateTime.Now.AddDays(1),
                NumberOfPassengers = 2
            };

            var results = ValidateModel(ticket);

            Assert.Empty(results);
        }

        [Fact]
        public void Ticket_MissingTo_FailsValidation()
        {
            var ticket = new Ticket
            {
                // To is missing
                DepartureTime = DateTime.Now.AddDays(1),
                NumberOfPassengers = 2
            };

            var results = ValidateModel(ticket);

            Assert.Contains(results, v => v.MemberNames.Contains("To"));
        }

        [Fact]
        public void Ticket_PassengersExceedsMax_FailsValidation()
        {
            var ticket = new Ticket
            {
                To = "London",
                DepartureTime = DateTime.Now.AddDays(1),
                NumberOfPassengers = 101 // Valid range is 1-100
            };

            var results = ValidateModel(ticket);

            Assert.Contains(results, v => v.MemberNames.Contains("NumberOfPassengers"));
        }

        [Fact]
        public void Ticket_PassengersBelowMin_FailsValidation()
        {
            var ticket = new Ticket
            {
                To = "Rome",
                DepartureTime = DateTime.Now.AddDays(1),
                NumberOfPassengers = 0 // Valid range is 1-100
            };

            var results = ValidateModel(ticket);

            Assert.Contains(results, v => v.MemberNames.Contains("NumberOfPassengers"));
        }

        // --- TicketReport Tests ---

        [Fact]
        public void TicketReport_ValidData_PassesValidation()
        {
            var report = new TicketReport
            {
                TicketId = 1,
                Reason = "Inappropriate content"
            };

            var results = ValidateModel(report);

            Assert.Empty(results);
        }

        [Fact]
        public void TicketReport_MissingReason_FailsValidation()
        {
            var report = new TicketReport
            {
                TicketId = 1
                // Reason is missing
            };

            var results = ValidateModel(report);

            Assert.Contains(results, v => v.MemberNames.Contains("Reason"));
        }

        [Fact]
        public void TicketReport_ReasonTooLong_FailsValidation()
        {
            var report = new TicketReport
            {
                TicketId = 1,
                Reason = new string('A', 301) // Max length is 300
            };

            var results = ValidateModel(report);

            Assert.Contains(results, v => v.MemberNames.Contains("Reason"));
        }

        // --- TicketComment Tests ---

        [Fact]
        public void TicketComment_ValidData_PassesValidation()
        {
            var comment = new TicketComment
            {
                TicketId = 1,
                Content = "This is a valid comment"
            };

            var results = ValidateModel(comment);

            Assert.Empty(results);
        }

        [Fact]
        public void TicketComment_MissingContent_FailsValidation()
        {
            var comment = new TicketComment
            {
                TicketId = 1
                // Content is missing
            };

            var results = ValidateModel(comment);

            Assert.Contains(results, v => v.MemberNames.Contains("Content"));
        }

        [Fact]
        public void TicketComment_ContentExceedsMaxLength_FailsValidation()
        {
            var comment = new TicketComment
            {
                TicketId = 1,
                Content = new string('X', 1001) // Max length is 1000
            };

            var results = ValidateModel(comment);

            Assert.Contains(results, v => v.MemberNames.Contains("Content"));
        }

        // --- JournalPage Tests ---

        [Fact]
        public void JournalPage_ValidData_PassesValidation()
        {
            var page = new JournalPage
            {
                TicketId = 1,
                Content = "This is a journal entry.",
                PageNumber = 1
            };

            var results = ValidateModel(page);

            Assert.Empty(results);
        }

        [Fact]
        public void JournalPage_MissingContent_FailsValidation()
        {
            var page = new JournalPage
            {
                TicketId = 1,
                PageNumber = 1
                // Content is missing
            };

            var results = ValidateModel(page);

            Assert.Contains(results, v => v.MemberNames.Contains("Content"));
        }

        // --- ChatMessage Tests ---

        [Fact]
        public void ChatMessage_ValidData_PassesValidation()
        {
            var message = new ChatMessage
            {
                SenderId = "user1",
                ReceiverId = "user2",
                Content = "Hello!"
            };

            var results = ValidateModel(message);

            Assert.Empty(results);
        }

        [Fact]
        public void ChatMessage_MissingSenderId_FailsValidation()
        {
            var message = new ChatMessage
            {
                ReceiverId = "user2",
                Content = "Hello!"
                // SenderId is missing
            };

            var results = ValidateModel(message);

            Assert.Contains(results, v => v.MemberNames.Contains("SenderId"));
        }

        [Fact]
        public void ChatMessage_MissingContent_FailsValidation()
        {
            var message = new ChatMessage
            {
                SenderId = "user1",
                ReceiverId = "user2"
                // Content is missing
            };

            var results = ValidateModel(message);

            Assert.Contains(results, v => v.MemberNames.Contains("Content"));
        }

        // --- UserFriendCode Tests ---

        [Fact]
        public void UserFriendCode_ValidData_PassesValidation()
        {
            var code = new UserFriendCode
            {
                UserId = "user1",
                FriendCode = "ABCDEF"
            };

            var results = ValidateModel(code);

            Assert.Empty(results);
        }

        [Fact]
        public void UserFriendCode_FriendCodeTooLong_FailsValidation()
        {
            var code = new UserFriendCode
            {
                UserId = "user1",
                FriendCode = "ABCDEFG" // Max length is 6
            };

            var results = ValidateModel(code);

            Assert.Contains(results, v => v.MemberNames.Contains("FriendCode"));
        }

        // --- FriendRequest Tests ---

        [Fact]
        public void FriendRequest_ValidData_PassesValidation()
        {
            var request = new FriendRequest
            {
                SenderId = "user1",
                ReceiverId = "user2",
                Status = FriendRequestStatus.Pending
            };

            var results = ValidateModel(request);

            Assert.Empty(results);
        }

        [Fact]
        public void FriendRequest_MissingReceiverId_FailsValidation()
        {
            var request = new FriendRequest
            {
                SenderId = "user1",
                Status = FriendRequestStatus.Pending
                // ReceiverId is missing
            };

            var results = ValidateModel(request);

            Assert.Contains(results, v => v.MemberNames.Contains("ReceiverId"));
        }
    }
}
