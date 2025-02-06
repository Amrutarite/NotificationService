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
       
        public void GenerateJwtToken_ShouldThrowArgumentOutOfRangeException_WhenKeyIsTooShort()
        {
            // Arrange: Override the configuration with a short secret key (104 bits)
            _configurationMock.Setup(config => config["JwtSettings:SecretKey"]).Returns("ShortKey"); // 8 characters = 64 bits

            // Act & Assert: Expect an exception when generating the JWT token
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _jwtTokenServiceMock.Object.GenerateJwtToken("testuser"));

            Assert.Equal("IDX10653: The encryption algorithm 'HS256' requires a key size of at least '128' bits.", exception.Message);
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