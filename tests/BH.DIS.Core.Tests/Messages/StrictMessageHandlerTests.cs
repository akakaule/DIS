using BH.DIS.Core.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BH.DIS.Core.Messages
{
    public class StrictMessageHandlerTests
    {
        protected Mock<IEventContextHandler> mockEventContextHandler;
        protected Mock<IResponseService> mockResponseService;
        protected Mock<ILoggerProvider> mockLoggerProvider;
        protected Mock<ILogger> mockLogger;

        protected StrictMessageHandler strictReceiver;

        public StrictMessageHandlerTests()
        {
            mockEventContextHandler = new Mock<IEventContextHandler>();
            mockEventContextHandler
                .Setup(h => h.Handle(It.IsAny<IMessageContext>(), It.IsAny<ILogger>()))
                .Returns(Task.CompletedTask);

            mockResponseService = new Mock<IResponseService>();
            
            mockLogger = new Mock<ILogger>();

            mockLoggerProvider = new Mock<ILoggerProvider>();
            mockLoggerProvider
                .Setup(p => p.GetContextualLogger(It.IsAny<IMessageContext>()))
                .Returns(mockLogger.Object);

            strictReceiver = new StrictMessageHandler(mockEventContextHandler.Object, mockResponseService.Object, mockLoggerProvider.Object);
        }

        [Fact]
        public async void HandleEventRequest_WhenSessionIsNotBlocked_InvokesAbstract()
        {
            // Arrange
            IMessageContext stubEventRequest = CreateFakeMessageContext().Object;

            // Act
            await strictReceiver.HandleEventRequest(stubEventRequest, mockLogger.Object);

            // Assert
            mockEventContextHandler.Verify(h => h.Handle(It.IsAny<IMessageContext>(), It.IsAny<ILogger>()), Times.Once);
        }

        [Fact]
        public async void HandleEventRequest_WhenSessionIsBlocked_DefersMessage()
        {
            // Arrange
            Mock<IMessageContext> mockMessageContext = CreateFakeMessageContext(isSessionBlocked: true);

            // Act
            await strictReceiver.HandleEventRequest(mockMessageContext.Object, mockLogger.Object);

            // Assert
            mockMessageContext.Verify(c => c.Defer(), Times.Once);
        }

        [Fact]
        public async void HandleEventRequest_WhenSessionIsBlocked_SendsDeferralResponse()
        {
            // Arrange
            IMessageContext stubMessageContext = CreateFakeMessageContext(isSessionBlocked: true).Object;

            // Act
            await strictReceiver.HandleEventRequest(stubMessageContext, mockLogger.Object);

            // Assert
            mockResponseService.Verify(r => r.SendDeferralResponse(stubMessageContext, It.IsAny<SessionBlockedException>()), Times.Once);
        }

        [Fact]
        public async void HandleEventRequest_WhenEventHandlerThrows_CompletesMessage()
        {
            // Arrange. 
            Mock<IMessageContext> mockMessageContext = CreateFakeMessageContext();

            // Setup event handler to throw exception
            mockEventContextHandler
                .Setup(h => h.Handle(It.IsAny<IMessageContext>(), It.IsAny<ILogger>()))
                .Throws(new Exception());

            // Act.
            await strictReceiver.HandleEventRequest(mockMessageContext.Object, mockLogger.Object);

            // Assert.
            mockMessageContext.Verify(c => c.Complete(), Times.Once);
        }

        [Fact]
        public async void HandleEventRequest_WhenEventHandlerThrows_BlocksSession()
        {
            // Arrange
            Mock<IMessageContext> mockMessageContext = CreateFakeMessageContext();

            // Setup event handler to throw exception
            mockEventContextHandler
                .Setup(h => h.Handle(It.IsAny<IMessageContext>(), It.IsAny<ILogger>()))
                .Throws(new Exception());

            // Act.
            await strictReceiver.HandleEventRequest(mockMessageContext.Object, mockLogger.Object);

            // Assert.
            mockMessageContext.Verify(c => c.BlockSession(), Times.Once);
        }

        [Fact]
        public async void HandleEventRequest_WhenEventHandlerThrows_SendsErrorResponse()
        {
            // Arrange
            IMessageContext stubMessageContext = CreateFakeMessageContext(nextDeferred: CreateFakeMessageContext().Object).Object;

            // Setup event handler to throw exception
            mockEventContextHandler
                .Setup(h => h.Handle(It.IsAny<IMessageContext>(), It.IsAny<ILogger>()))
                .Throws(new Exception());

            // Act
            await strictReceiver.HandleEventRequest(stubMessageContext, mockLogger.Object);

            // Assert
            mockResponseService.Verify(r => r.SendErrorResponse(stubMessageContext, It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public async void HandleEventRequest_WhenEventHandlerSucceeds_SendsResolutionResponse()
        {
            // Arrange
            IMessageContext stubMessageContext = CreateFakeMessageContext().Object;

            // Act.
            await strictReceiver.HandleEventRequest(stubMessageContext, mockLogger.Object);

            // Assert.
            mockResponseService.Verify(r => r.SendResolutionResponse(stubMessageContext), Times.Once);
        }

        [Fact]
        public async void HandleEventRequest_WhenEventHandlerSucceeds_CompletesMessage()
        {
            // Arrange
            Mock<IMessageContext> mockMessageContext = CreateFakeMessageContext();

            // Act.
            await strictReceiver.HandleEventRequest(mockMessageContext.Object, mockLogger.Object);

            // Assert.
            mockMessageContext.Verify(c => c.Complete(), Times.Once);
        }

        [Fact]
        public async void HandleSkipRequest_WhenSessionIsBlockedByThis_UnblocksSession()
        {
            // Arrange
            Mock<IMessageContext> mockMessageContext = CreateFakeMessageContext(isSessionBlocked: true, isSessionBlockedByThis: true, from: Constants.ManagerId);

            // Act
            await strictReceiver.HandleSkipRequest(mockMessageContext.Object, mockLogger.Object);

            // Assert
            mockMessageContext.Verify(c => c.UnblockSession(), Times.Once);
        }

        [Fact]
        public async void HandleSkipRequest_WhenSessionIsNotBlockedByThis_SendsResolutionResponse()
        {
            // Arrange
            IMessageContext stubMessageContext = CreateFakeMessageContext(isSessionBlocked: true, isSessionBlockedByThis: false, from: Constants.ManagerId).Object;

            // Act
            await strictReceiver.HandleSkipRequest(stubMessageContext, mockLogger.Object);

            // Assert
            mockResponseService.Verify(s => s.SendResolutionResponse(stubMessageContext), Times.Once);
        }

        [Fact]
        public async void HandleSkipRequest_WhenSessionIsNotBlockedByThis_Completes()
        {
            // Arrange
            Mock<IMessageContext> mockMessageContext = CreateFakeMessageContext(isSessionBlocked: true, isSessionBlockedByThis: false, from: Constants.ManagerId);

            // Act
            await strictReceiver.HandleSkipRequest(mockMessageContext.Object, mockLogger.Object);

            // Assert
            mockMessageContext.Verify(c => c.Complete(), Times.Once);
        }

        [Fact]
        public async void HandleResubmissionRequest_WhenSessionIsBlockedByThis_InvokesEventHandler()
        {
            // Arrange
            IMessageContext stubMessageContext = CreateFakeMessageContext(MessageType.ResubmissionRequest, isSessionBlocked: true, isSessionBlockedByThis: true, from: Constants.ManagerId).Object;

            // Act
            await strictReceiver.HandleResubmissionRequest(stubMessageContext, mockLogger.Object);

            // Assert
            mockEventContextHandler.Verify(h => h.Handle(It.IsAny<IMessageContext>(), It.IsAny<ILogger>()), Times.Once);
        }

        [Fact]
        public async void HandleResubmissionRequest_WhenSessionIsNotBlockedByThis_Completes()
        {
            // Arrange
            Mock<IMessageContext> mockMessageContext = CreateFakeMessageContext(isSessionBlocked: true, isSessionBlockedByThis: false, from: Constants.ManagerId);

            // Act
            await strictReceiver.HandleResubmissionRequest(mockMessageContext.Object, mockLogger.Object);

            // Assert
            mockMessageContext.Verify(c => c.Complete(), Times.Once);
        }

        [Fact]
        public async void HandleResubmissionRequest_WhenSessionIsNotBlockedByThis_SendsResolutionResponse()
        {
            // Arrange
            IMessageContext stubMessageContext = CreateFakeMessageContext(isSessionBlocked: true, isSessionBlockedByThis: false, from: Constants.ManagerId).Object;

            // Act
            await strictReceiver.HandleResubmissionRequest(stubMessageContext, mockLogger.Object);

            // Assert
            mockResponseService.Verify(r => r.SendResolutionResponse(stubMessageContext), Times.Once);
        }

        [Fact]
        public async void HandleResubmissionRequest_WhenEventHandlerSucceeds_Completes()
        {
            // Arrange
            Mock<IMessageContext> mockMessageContext = CreateFakeMessageContext(isSessionBlocked: true, isSessionBlockedByThis: true, from: Constants.ManagerId);

            // Act
            await strictReceiver.HandleResubmissionRequest(mockMessageContext.Object, mockLogger.Object);

            // Assert
            mockMessageContext.Verify(c => c.Complete(), Times.Once);
        }

        [Fact]
        public async void HandleResubmissionRequest_WhenEventHandlerSucceeds_SendsResolutionResponse()
        {
            // Arrange
            IMessageContext stubMessageContext = CreateFakeMessageContext(isSessionBlocked: true, isSessionBlockedByThis: true, from: Constants.ManagerId).Object;

            // Act
            await strictReceiver.HandleResubmissionRequest(stubMessageContext, mockLogger.Object);

            // Assert
            mockResponseService.Verify(r => r.SendResolutionResponse(stubMessageContext), Times.Once);
        }

        [Fact]
        public async void HandleResubmissionRequest_WhenEventHandlerSucceedsAndThereAreDeferredMessages_SendsContinuationRequest()
        {
            // Arrange
            IMessageContext stubDeferredMessageContext = CreateFakeMessageContext().Object;
            IMessageContext stubMessageContext = CreateFakeMessageContext(nextDeferred: stubDeferredMessageContext, isSessionBlocked: true, isSessionBlockedByThis: true, from: Constants.ManagerId).Object;

            // Act
            await strictReceiver.HandleResubmissionRequest(stubMessageContext, mockLogger.Object);

            // Assert.
            mockResponseService.Verify(r => r.SendContinuationRequestToSelf(stubDeferredMessageContext), Times.Once);
        }

        [Fact]
        public async void HandleResubmissionRequest_WhenEventHandlerSucceedsAndThereAreNoDeferredMessages_DoesntSendContinuationRequest()
        {
            // Arrange
            IMessageContext stubMessageContext = CreateFakeMessageContext(isSessionBlocked: true, isSessionBlockedByThis: true, from: Constants.ManagerId).Object;

            // Act
            await strictReceiver.HandleResubmissionRequest(stubMessageContext, mockLogger.Object);

            // Assert.
            mockResponseService.Verify(r => r.SendContinuationRequestToSelf(It.IsAny<IMessageContext>()), Times.Never);
        }

        [Fact]
        public async void HandleResubmissionRequest_WhenEventHandlerThrows_SendsErrorResponse()
        {
            // Arrange
            IMessageContext stubMessageContext = CreateFakeMessageContext(isSessionBlocked: true, isSessionBlockedByThis: true, from: Constants.ManagerId).Object;

            // Setup event handler to throw exception
            mockEventContextHandler
                .Setup(h => h.Handle(It.IsAny<IMessageContext>(), It.IsAny<ILogger>()))
                .Throws(new Exception());

            // Act
            await strictReceiver.HandleResubmissionRequest(stubMessageContext, mockLogger.Object);

            // Assert.
            mockResponseService.Verify(r => r.SendErrorResponse(stubMessageContext, It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public async void HandleResubmissionRequest_WhenEventHandlerThrows_Completes()
        {
            // Arrange
            Mock<IMessageContext> mockMessageContext = CreateFakeMessageContext(isSessionBlocked: true, isSessionBlockedByThis: true, from: Constants.ManagerId);

            // Setup event handler to throw exception
            mockEventContextHandler
                .Setup(h => h.Handle(It.IsAny<IMessageContext>(), It.IsAny<ILogger>()))
                .Throws(new Exception());

            // Act
            await strictReceiver.HandleResubmissionRequest(mockMessageContext.Object, mockLogger.Object);

            // Assert
            mockMessageContext.Verify(c => c.Complete(), Times.Once);
        }

        [Fact]
        public async void HandleContinuationRequest_WhenEventIsNextDeferred_InvokesEventHandler()
        {
            // Arrange
            string eventId = Guid.NewGuid().ToString();

            IMessageContext stubMessageContext = CreateFakeMessageContext(eventId: eventId, nextDeferred: CreateFakeMessageContext(eventId: eventId).Object, from: Constants.ContinuationId).Object;

            // Act
            await strictReceiver.HandleContinuationRequest(stubMessageContext, mockLogger.Object);

            // Assert
            mockEventContextHandler.Verify(h => h.Handle(It.IsAny<IMessageContext>(), It.IsAny<ILogger>()), Times.Once);
        }

        [Fact]
        public async void HandleContinuationRequest_WhenDeferredHandled_CompletesDeferred()
        {
            // Arrange
            string eventId = Guid.NewGuid().ToString();

            Mock<IMessageContext> mockDeferredMessageContext = CreateFakeMessageContext(eventId: eventId);
            IMessageContext stubMessageContext = CreateFakeMessageContext(eventId: eventId, nextDeferred: mockDeferredMessageContext.Object, from: Constants.ContinuationId).Object;

            // Act
            await strictReceiver.HandleContinuationRequest(stubMessageContext, mockLogger.Object);

            // Assert
            mockDeferredMessageContext.Verify(c => c.Complete(), Times.Once);
        }

        [Fact]
        public async void HandleContinuationRequest_WhenDeferredHandled_Completes()
        {
            // Arrange
            string eventId = Guid.NewGuid().ToString();

            Mock<IMessageContext> mockMessageContext = CreateFakeMessageContext(eventId: eventId, nextDeferred: CreateFakeMessageContext(eventId: eventId).Object, from: Constants.ContinuationId);

            // Act
            await strictReceiver.HandleContinuationRequest(mockMessageContext.Object, mockLogger.Object);

            // Assert
            mockMessageContext.Verify(c => c.Complete(), Times.Once);
        }

        [Fact]
        public async void HandleContinuationRequest_WhenDeferredHandled_SendsResolutionResponse()
        {
            // Arrange
            string eventId = Guid.NewGuid().ToString();

            IMessageContext stubDeferredMessageContext = CreateFakeMessageContext(eventId: eventId).Object;
            IMessageContext stubMessageContext = CreateFakeMessageContext(eventId: eventId, nextDeferred: stubDeferredMessageContext, from: Constants.ContinuationId).Object;

            // Act
            await strictReceiver.HandleContinuationRequest(stubMessageContext, mockLogger.Object);

            // Assert
            mockResponseService.Verify(r => r.SendResolutionResponse(stubDeferredMessageContext), Times.Once);
        }

        [Fact]
        public async void HandleContinuationRequest_WhenEventIsNotNext_Completes()
        {
            // Arrange
            Mock<IMessageContext> mockMessageContext = CreateFakeMessageContext(nextDeferred: CreateFakeMessageContext().Object, from: Constants.ContinuationId);

            // Act
            await strictReceiver.HandleContinuationRequest(mockMessageContext.Object, mockLogger.Object);

            // Assert
            mockMessageContext.Verify(c => c.Complete(), Times.Once);
        }

        private Mock<IMessageContext> CreateFakeMessageContext(MessageType messageType = MessageType.Unknown, MessageContent messageContent = null, string eventId = null, bool isSessionBlocked = false, bool isSessionBlockedByThis = false, IMessageContext nextDeferred = null, string from = null)
        {
            var result = new Mock<IMessageContext>();

            result
               .SetupGet(c => c.MessageType)
               .Returns(messageType);

            if (messageContent == null)
                messageContent = new MessageContent();
            result
                .SetupGet(c => c.MessageContent)
                .Returns(messageContent);

            if (eventId == null)
                eventId = Guid.NewGuid().ToString();
            result
                .SetupGet(c => c.EventId)
                .Returns(eventId);

            result
                .Setup(c => c.IsSessionBlocked())
                .Returns(Task.FromResult(isSessionBlocked));

            result
                .Setup(c => c.IsSessionBlockedByThis())
                .Returns(Task.FromResult(isSessionBlockedByThis));

            result
                .Setup(r => r.ReceiveNextDeferred())
                .Returns(Task.FromResult(nextDeferred));

            result
                .SetupGet(r => r.From)
                .Returns(from);

            return result;
        }
    }
}