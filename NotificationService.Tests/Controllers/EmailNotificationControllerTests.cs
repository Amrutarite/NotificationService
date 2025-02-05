using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Controllers;
using NotificationService.DTOs;
using NotificationService.Services;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using NotificationService.ErrorHandling;
using NotificationService.Models;
using NotificationService.Repositories;
using NotificationService.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using Dapper;

namespace NotificationService.Tests
{
    public class EmailNotificationControllerTests
    {
        private readonly Mock<IEmailSenderService> _emailSenderServiceMock;
        private readonly Mock<JwtTokenService> _jwtTokenServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly EmailNotificationController _controller;

        public EmailNotificationControllerTests()
        {
            _emailSenderServiceMock = new Mock<IEmailSenderService>();

            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(config => config["JwtSettings:Issuer"]).Returns("TestIssuer");
            _configurationMock.Setup(config => config["JwtSettings:Audience"]).Returns("TestAudience");
            _configurationMock.Setup(config => config["JwtSettings:SecretKey"]).Returns("TestSecretKey");

            _jwtTokenServiceMock = new Mock<JwtTokenService>(_configurationMock.Object);

            _controller = new EmailNotificationController(_emailSenderServiceMock.Object, _jwtTokenServiceMock.Object);
        }

        // Email Notification Test Cases

        [Fact]
        public async Task SendEmail_ShouldReturnOk_WhenRequestIsValid()
        {
            var request = new EmailNotificationRequest
            {
                Recipient = "validemail@test.com",
                Subject = "Test Subject",
                Body = "Test Body",
                ScheduledTime = DateTime.UtcNow.AddMinutes(5)
            };

            _emailSenderServiceMock.Setup(service => service.ScheduleEmail(It.IsAny<EmailNotificationRequest>()))
                .ReturnsAsync(new EmailNotificationResponse(true, "Email scheduled successfully.", "sent"));

            var result = await _controller.SendEmail(request);

            result.Should().BeOfType<OkObjectResult>();
        }


        

        


        [Fact]
        public async Task SendEmail_ShouldReturnBadRequest_WhenRecipientEmailIsInvalid()
        {
            var request = new EmailNotificationRequest
            {
                Recipient = "invalid-email",
                Subject = "Test Subject",
                Body = "Test Body"
            };

            var result = await _controller.SendEmail(request);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)result;
            var errorResponse = (ErrorResponse)badRequestResult.Value;
            errorResponse.Message.Should().Be("Invalid recipient email format.");
        }

        [Fact]
        public async Task SendEmail_ShouldReturnBadRequest_WhenCCEmailIsInvalid()
        {
            // Arrange
            var request = new EmailNotificationRequest
            {
                Recipient = "validemail@test.com",
                CC = "invalid-cc-email",
                Subject = "Test Subject",
                Body = "Test Body"
            };

            // Act
            var result = await _controller.SendEmail(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)result;
            var errorResponse = (ErrorResponse)badRequestResult.Value;

            errorResponse.Should().NotBeNull();
            errorResponse.Code.Should().Be(ErrorCodes.ValidationError);
            errorResponse.Message.Should().Be("Invalid CC email format.");
            errorResponse.Details.Should().Be("CC email: invalid-cc-email");
            errorResponse.TraceId.Should().NotBeNullOrWhiteSpace(); // Ensure TraceId is set
        }


        [Fact]
        public async Task SendEmail_ShouldReturnBadRequest_WhenScheduledTimeIsInThePast()
        {
            var request = new EmailNotificationRequest
            {
                Recipient = "validemail@test.com",
                Subject = "Test Subject",
                Body = "Test Body",
                ScheduledTime = DateTime.UtcNow.AddMinutes(-5)
            };

            _emailSenderServiceMock.Setup(service => service.ScheduleEmail(It.IsAny<EmailNotificationRequest>()))
                .Throws(new NullReferenceException("Simulated NullReferenceException"));

            var result = await _controller.SendEmail(request);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)result;
            var errorResponse = (ErrorResponse)badRequestResult.Value;
            errorResponse.Message.Should().Be("Invalid ScheduledTime.");
            errorResponse.Details.Should().Be("The ScheduledTime cannot be in the past.");
        }

        [Fact]
        public async Task SendEmail_ShouldReturnOk_WhenScheduledTimeIsNull()
        {
            var request = new EmailNotificationRequest
            {
                Recipient = "validemail@test.com",
                Subject = "Test Subject",
                Body = "Test Body",
                ScheduledTime = null
            };

            _emailSenderServiceMock.Setup(service => service.ScheduleEmail(It.IsAny<EmailNotificationRequest>()))
                .ReturnsAsync(new EmailNotificationResponse(true, "Email sent successfully.", "sent"));

            var result = await _controller.SendEmail(request);

            result.Should().BeOfType<OkObjectResult>();
        }

        // JwtTokenService Test Cases

        [Fact]
        public void GenerateJwtToken_ShouldReturnToken_WhenUsernameIsValid()
        {
            var username = "testuser";

            var token = _jwtTokenServiceMock.Object.GenerateJwtToken(username);

            Assert.NotNull(token);
            Assert.Contains("eyJ", token);
        }

        [Fact]
        public void GenerateJwtToken_ShouldThrowUnauthorizedAccessException_WhenUsernameIsInvalid()
        {
            var username = "invaliduser";

            var exception = Assert.Throws<UnauthorizedAccessException>(() => _jwtTokenServiceMock.Object.GenerateJwtToken(username));
            Assert.Equal("Invalid username.", exception.Message);
        }

        // Repository Test Cases

        public class EmailNotificationRepositoryTests
        {
            private readonly Mock<DBExecutor> _dbExecutorMock;
            private readonly Mock<ILogger<EmailNotificationRepository>> _loggerMock;
            private readonly EmailNotificationRepository _repository;

            public EmailNotificationRepositoryTests()
            {
                _dbExecutorMock = new Mock<DBExecutor>();
                _loggerMock = new Mock<ILogger<EmailNotificationRepository>>();

                _repository = new EmailNotificationRepository(_dbExecutorMock.Object, _loggerMock.Object);
            }

            [Fact]
            public async Task SaveEmailNotificationAsync_ShouldReturnInt_WhenSuccess()
            {
                var emailNotification = new EmailNotification
                {
                    Sender = "test@sender.com",
                    Recipient = "test@recipient.com",
                    CC = "test@cc.com",
                    Subject = "Test Email",
                    Body = "This is a test email.",
                    Status = "Pending",
                    ScheduledTime = DateTime.Now
                };

                _dbExecutorMock.Setup(db => db.ExecuteScalarAsync<int>("SaveEmailNotification", It.IsAny<object>()))
                    .ReturnsAsync(1);

                var result = await _repository.SaveEmailNotificationAsync(emailNotification);

                Assert.Equal(1, result);
            }

            [Fact]
            public async Task GetEmailNotificationStatusAsync_ShouldReturnEmailNotification_WhenFound()
            {
                var emailNotification = new EmailNotification { Id = 1, Status = "Sent" };
                _dbExecutorMock.Setup(db => db.QueryAsync<EmailNotification>("SELECT * FROM EmailNotifications WHERE Id = @Id", It.IsAny<object>()))
                    .ReturnsAsync(new List<EmailNotification> { emailNotification });

                var result = await _repository.GetEmailNotificationStatusAsync(1);

                Assert.NotNull(result);
                Assert.Equal("Sent", result.Status);
            }

            [Fact]
            public async Task GetEmailNotificationStatusAsync_ShouldReturnNull_WhenNotFound()
            {
                _dbExecutorMock.Setup(db => db.QueryAsync<EmailNotification>("SELECT * FROM EmailNotifications WHERE Id = @Id", It.IsAny<object>()))
                    .ReturnsAsync(new List<EmailNotification>());

                var result = await _repository.GetEmailNotificationStatusAsync(999);

                Assert.Null(result);
            }

            [Fact]
            public async Task GetNotificationsByStatusAsync_ShouldReturnNotifications_WhenFound()
            {
                var emailNotifications = new List<EmailNotification>
                {
                    new EmailNotification { Id = 1, Status = "Sent" },
                    new EmailNotification { Id = 2, Status = "Sent" }
                };
                _dbExecutorMock.Setup(db => db.QueryAsync<EmailNotification>("SELECT * FROM EmailNotifications WHERE Status = @Status", It.IsAny<object>()))
                    .ReturnsAsync(emailNotifications);

                var result = await _repository.GetNotificationsByStatusAsync("Sent");

                Assert.NotNull(result);
                Assert.Equal(2, result.Count());
            }

            [Fact]
            public async Task UpdateNotificationStatusAsync_ShouldReturnVoid_WhenSuccess()
            {
                _dbExecutorMock.Setup(db => db.ExecuteAsync("UPDATE EmailNotifications SET Status = @Status, SentAt = @SentAt WHERE Id = @Id", It.IsAny<object>()))
                    .ReturnsAsync(1);

                await _repository.UpdateNotificationStatusAsync(1, "Sent", DateTime.Now);

                _dbExecutorMock.Verify(db => db.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
            }
        }
    }

    // EmailNotificationRequest Tests
    public class EmailNotificationRequestTests
    {
        // Test 1: Test default constructor
        [Fact]
        public void EmailNotificationRequest_DefaultConstructor_ShouldInitializeEmptyProperties()
        {
            // Arrange & Act
            var request = new EmailNotificationRequest();

            // Assert
            Assert.Null(request.Recipient);  // Recipient should be null
            Assert.Null(request.CC);         // CC should be null
            Assert.Null(request.Subject);    // Subject should be null
            Assert.Null(request.Body);       // Body should be null
            Assert.Null(request.ScheduledTime); // ScheduledTime should be null
        }

        // Test 2: Test parameterized constructor with all properties set
        [Fact]
        public void EmailNotificationRequest_ConstructorWithParameters_ShouldInitializeWithValues()
        {
            // Arrange
            var recipient = "test@recipient.com";
            var cc = "test@cc.com";
            var subject = "Test Email Subject";
            var body = "This is a test email body.";
            var scheduledTime = DateTime.Now.AddHours(1); // Set a future scheduled time

            // Act
            var request = new EmailNotificationRequest(recipient, cc, subject, body, scheduledTime);

            // Assert
            Assert.Equal(recipient, request.Recipient); // Assert Recipient matches
            Assert.Equal(cc, request.CC);               // Assert CC matches
            Assert.Equal(subject, request.Subject);     // Assert Subject matches
            Assert.Equal(body, request.Body);           // Assert Body matches
            Assert.Equal(scheduledTime, request.ScheduledTime); // Assert ScheduledTime matches
        }

        // Test 3: Test parameterized constructor without scheduled time
        [Fact]
        public void EmailNotificationRequest_ConstructorWithoutScheduledTime_ShouldInitializeWithNullScheduledTime()
        {
            // Arrange
            var recipient = "test@recipient.com";
            var cc = "test@cc.com";
            var subject = "Test Email Subject";
            var body = "This is a test email body.";

            // Act
            var request = new EmailNotificationRequest(recipient, cc, subject, body);

            // Assert
            Assert.Null(request.ScheduledTime); // Assert ScheduledTime is null as it wasn't provided
        }

        // Test 4: Test setting properties individually after initialization
        [Fact]
        public void EmailNotificationRequest_SetProperties_ShouldUpdateValues()
        {
            // Arrange
            var request = new EmailNotificationRequest();

            // Act
            request.Recipient = "new@recipient.com";
            request.CC = "new@cc.com";
            request.Subject = "Updated Subject";
            request.Body = "Updated body of the email.";
            request.ScheduledTime = DateTime.Now.AddMinutes(30); // Set new scheduled time

            // Assert
            Assert.Equal("new@recipient.com", request.Recipient);
            Assert.Equal("new@cc.com", request.CC);
            Assert.Equal("Updated Subject", request.Subject);
            Assert.Equal("Updated body of the email.", request.Body);
            Assert.NotNull(request.ScheduledTime);  // Assert that ScheduledTime is updated
        }
    }
}
