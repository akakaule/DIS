using Azure.Messaging.ServiceBus;
using BH.DIS.Core.Messages;
using BH.DIS.Core.Messages.Exceptions;
using BH.DIS.ServiceBus;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BH.DIS.Core.Tests
{
    public class MessageContextTests
    {
        [Fact]
        public void SessionId_WhenSessionIdNotSetOnMessage_ThrowsInvalidMessageException()
        {
            // Arrange
            var messageContext = new MessageContext(CreateFakeMessage().Object, CreateFakeSession().Object);

            // Act/Assert
            Assert.Throws<InvalidMessageException>(() => messageContext.SessionId);
        }

        [Fact]
        public void To_WhenUserPropertyNotSetOnSbMessage_ThrowsInvalidMessageException()
        {
            // Arrange
            var messageContext = new MessageContext(CreateFakeMessage().Object, CreateFakeSession().Object);

            // Act/Assert
            Assert.Throws<InvalidMessageException>(() => messageContext.To);
        }

        [Fact]
        public void From_WhenUserPropertyNotSetOnSbMessage_ThrowsInvalidMessageException()
        {
            // Arrange
            var messageContext = new MessageContext(CreateFakeMessage().Object, CreateFakeSession().Object);

            // Act/Assert
            Assert.Throws<InvalidMessageException>(() => messageContext.From);
        }

        [Fact]
        public void EventId_WhenUserPropertyNotSetOnSbMessage_ThrowsInvalidMessageException()
        {
            // Arrange
            var messageContext = new MessageContext(CreateFakeMessage().Object, CreateFakeSession().Object);

            // Act/Assert
            Assert.Throws<InvalidMessageException>(() => messageContext.EventId);
        }

        [Fact]
        public void MessageType_WhenUserPropertyNotSetOnSbMessage_ThrowsInvalidMessageException()
        {
            // Arrange
            var messageContext = new MessageContext(CreateFakeMessage().Object, CreateFakeSession().Object);

            // Act/Assert
            Assert.Throws<InvalidMessageException>(() => messageContext.MessageType);
        }

        [Fact]
        public void SessionId_WhenSetOnMessage_Returns()
        {
            // Arrange
            string sbMessageSessionId = "blbalbab";
            var sbMessage = CreateFakeMessage(sessionId: sbMessageSessionId);
            var messageContext = new MessageContext(sbMessage.Object, CreateFakeSession().Object);

            // Act
            string sessionId = messageContext.SessionId;

            // Assert
            Assert.Equal(sbMessageSessionId, sessionId);
        }

        [Fact]
        public void To_WhenUserPropertySetOnSbMessage_Returns()
        {
            // Arrange
            string sbMessageTo = "blbalbab";
            var sbMessage = CreateFakeMessage(to: sbMessageTo);
            var messageContext = new MessageContext(sbMessage.Object, CreateFakeSession().Object);

            // Act
            string to = messageContext.To;

            // Assert
            Assert.Equal(sbMessageTo, to);
        }

        [Fact]
        public void From_WhenUserPropertySetOnSbMessage_Returns()
        {
            // Arrange
            string sbMessageFrom = "blbalbab";
            var sbMessage = CreateFakeMessage(from: sbMessageFrom);
            var messageContext = new MessageContext(sbMessage.Object, CreateFakeSession().Object);

            // Act
            string from = messageContext.From;

            // Assert
            Assert.Equal(sbMessageFrom, from);
        }

        [Fact]
        public void EventId_WhenUserPropertySetOnSbMessage_Returns()
        {
            // Arrange
            string sbMessageEventId = "blbalbab";
            var sbMessage = CreateFakeMessage(eventId: sbMessageEventId);
            var messageContext = new MessageContext(sbMessage.Object, CreateFakeSession().Object);

            // Act
            string eventId = messageContext.EventId;

            // Assert
            Assert.Equal(sbMessageEventId, eventId);
        }

        [Fact]
        public void MessageType_WhenUserPropertySetOnSbMessage_Returns()
        {
            // Arrange
            MessageType sbMessageMessageType = MessageType.Unknown;
            var sbMessage = CreateFakeMessage(messageType: sbMessageMessageType);
            var messageContext = new MessageContext(sbMessage.Object, CreateFakeSession().Object);

            // Act
            MessageType messageType = messageContext.MessageType;

            // Assert
            Assert.Equal(sbMessageMessageType, messageType);
        }

        [Fact]
        public async void Complete_Always_InvokesCompleteAsyncOnSbSession()
        {
            // Arrange
            string lockToken = "blablab";
            var mockSbSession = CreateFakeSession();
            var message = CreateFakeMessage(lockToken: lockToken).Object;
            MessageContext messageContext = new MessageContext(message, mockSbSession.Object);

            // Act
            await messageContext.Complete();

            // Assert
            mockSbSession.Verify(s => s.CompleteAsync(message), Times.Once);
        }

        [Fact]
        public async void Complete_WhenSbSessionThrowsServiceBusExceptionThatIsTransient_ThrowsTransientException()
        {
            // Arrange
            var mockSbSession = CreateFakeSession();
            mockSbSession
                .Setup(s => s.CompleteAsync(It.IsAny<IServiceBusMessage>()))
                .Throws(new ServiceBusException("Error", ServiceBusFailureReason.SessionLockLost));

            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, mockSbSession.Object);

            // Act
            Task testCode() => messageContext.Complete();

            // Assert
            await Assert.ThrowsAsync<TransientException>(testCode);
        }

        [Fact]
        public async void Complete_WhenSbSessionThrowsServiceBusExceptionThatIsNotTransient_DoesNotCatchIt()
        {
            // Arrange
            var mockSbSession = CreateFakeSession();
            mockSbSession
                .Setup(s => s.CompleteAsync(It.IsAny<IServiceBusMessage>()))
                .Throws(new ServiceBusException("Error", ServiceBusFailureReason.GeneralError));

            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, mockSbSession.Object);

            // Act
            Task testCode() => messageContext.Complete();

            // Assert
            await Assert.ThrowsAsync<ServiceBusException>(testCode);
        }

        [Fact]
        public async void Deadletter_Always_InvokesDeadletterAsyncOnSbSession()
        {
            // Arrange
            var lockToken = "blablab";
            var reason = "uhusdad";
            var exception = new Exception();

            var mockSbSession = CreateFakeSession();
            var message = CreateFakeMessage(lockToken: lockToken).Object;
            MessageContext messageContext = new MessageContext(message, mockSbSession.Object);

            // Act
            await messageContext.DeadLetter(reason, exception);

            // Assert
            mockSbSession.Verify(s => s.DeadLetterAsync(message, reason, exception.ToString()), Times.Once);
        }

        [Fact]
        public async void Deadletter_WhenSbSessionThrowsServiceBusExceptionThatIsTransient_ThrowsTransientException()
        {
            // Arrange
            var mockSbSession = CreateFakeSession();
            mockSbSession
                .Setup(s => s.DeadLetterAsync(It.IsAny<IServiceBusMessage>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ServiceBusException("Error", ServiceBusFailureReason.SessionLockLost));

            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, mockSbSession.Object);

            // Act
            Task testCode() => messageContext.DeadLetter(null);

            // Assert
            await Assert.ThrowsAsync<TransientException>(testCode);
        }

        [Fact]
        public async void Deadletter_WhenSbSessionThrowsServiceBusExceptionThatIsNotTransient_DoesNotCatchIt()
        {
            // Arrange
            var mockSbSession = CreateFakeSession();
            mockSbSession
                .Setup(s => s.DeadLetterAsync(It.IsAny<IServiceBusMessage>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ServiceBusException("Error", ServiceBusFailureReason.SessionLockLost));

            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, mockSbSession.Object);

            // Act
            Task testCode() => messageContext.DeadLetter(null);

            // Assert
            await Assert.ThrowsAsync<ServiceBusException>(testCode);
        }

        [Fact]
        public async void ReceiveNextDeferred_WhenDeferredMessageExists_ReturnsMessageContext()
        {
            // Arrange
            long deferredSequenceNumber = 0;
            var stubDeferred = CreateFakeMessage(sequenceNumber: deferredSequenceNumber);
            var stubState = new SessionState()
            {
                DeferredSequenceNumbers = new List<long>() { deferredSequenceNumber }
            };

            var fakeSession = CreateFakeSession(sessionState: stubState, deferred: stubDeferred.Object);
            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, fakeSession.Object);

            // Act
            var deferredContext = await messageContext.ReceiveNextDeferred();

            // Assert
            Assert.NotNull(deferredContext);
        }

        [Fact]
        public async void ReceiveNextDeferred_WhenDeferredMessageIsReferencedInSessionStateButDoesntExist_RemovesReferenceFromSessionState()
        {
            // Arrange
            var stubState = new SessionState()
            {
                DeferredSequenceNumbers = new List<long>() { 0 }
            };

            var mockSession = CreateFakeSession(sessionState: stubState);
            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, mockSession.Object);

            // Act
            var deferredContext = await messageContext.ReceiveNextDeferred();

            // Assert
            mockSession.Verify(s => s.SetStateAsync(It.Is<SessionState>(ss => !ss.DeferredSequenceNumbers.Any())), Times.Once);
        }

        [Fact]
        public async void ReceiveNextDeferred_WhenFirstDeferredMessageIsReferencedInSessionStateButDoesntExist_ReturnsSecondDeferredMessage()
        {
            // Arrange
            long badReference = 0;
            long goodReference = 1;
            string deferredMessageId = "blablab";
            var stubDeferred = CreateFakeMessage(sequenceNumber: goodReference, messageId: deferredMessageId);
            var stubState = new SessionState()
            {
                DeferredSequenceNumbers = new List<long>() { badReference, goodReference }
            };

            var mockSession = CreateFakeSession(sessionState: stubState, deferred: stubDeferred.Object);
            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, mockSession.Object);

            // Act
            var deferredContext = await messageContext.ReceiveNextDeferred();

            // Assert
            Assert.Equal(deferredMessageId, deferredContext.MessageId);
        }

        [Fact]
        public async void ReceiveNextDeferred_WhenNoDeferredMessages_ReturnsNull()
        {
            // Arrange
            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, CreateFakeSession().Object);

            // Act
            var deferredContext = await messageContext.ReceiveNextDeferred();

            // Assert
            Assert.Null(deferredContext);
        }

        [Fact]
        public async void ReceiveNextDeferred_WhenSbSessionThrowsServiceBusExceptionThatIsTransient_ThrowsTransientException()
        {
            // Arrange
            var stubState = new SessionState()
            {
                DeferredSequenceNumbers = new List<long>() { 42 }
            };

            var mockSbSession = CreateFakeSession(sessionState: stubState);
            mockSbSession
                .Setup(s => s.ReceiveDeferredMessageAsync(It.IsAny<long>()))
                .Throws(new ServiceBusException("Error", ServiceBusFailureReason.SessionLockLost));

            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, mockSbSession.Object);

            // Act
            Task testCode() => messageContext.ReceiveNextDeferred();

            // Assert
            await Assert.ThrowsAsync<TransientException>(testCode);
        }

        [Fact]
        public async void ReceiveNextDeferred_WhenSbSessionThrowsServiceBusExceptionThatIsNotTransient_DoesNotCatchIt()
        {
            // Arrange
            var stubState = new SessionState()
            {
                DeferredSequenceNumbers = new List<long>() { 42 }
            };

            var mockSbSession = CreateFakeSession(sessionState: stubState);
            mockSbSession
                .Setup(s => s.ReceiveDeferredMessageAsync(It.IsAny<long>()))
                .Throws(new ServiceBusException("Error", ServiceBusFailureReason.GeneralError));

            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, mockSbSession.Object);

            // Act
            Task testCode() => messageContext.ReceiveNextDeferred();

            // Assert
            await Assert.ThrowsAsync<ServiceBusException>(testCode);
        }

        [Fact]
        public async void IsSessionBlocked_WhenSessionStateSpecifiesBlockingEventId_ReturnsTrue()
        {
            // Arrange
            var stubState = new SessionState()
            {
                BlockedByEventId = "blabla",
            };
            var mockSbSession = CreateFakeSession(sessionState: stubState);

            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, mockSbSession.Object);

            // Act
            var isSessionBlocked = await messageContext.IsSessionBlocked();

            // Assert
            Assert.True(isSessionBlocked);
        }

        [Fact]
        public async void IsSessionBlocked_WhenSessionStateHasDeferredMessages_ReturnsTrue()
        {
            // Arrange
            var stubState = new SessionState()
            {
                DeferredSequenceNumbers = new List<long>() { 42 }
            };
            var mockSbSession = CreateFakeSession(sessionState: stubState);

            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, mockSbSession.Object);

            // Act
            var isSessionBlocked = await messageContext.IsSessionBlocked();

            // Assert
            Assert.True(isSessionBlocked);
        }

        [Fact]
        public async void IsSessionBlocked_WhenSessionStateDoesNotSpecifyBlockingEventIdAndHasNoDeferredMessages_ReturnsFalse()
        {
            // Arrange
            var stubState = new SessionState()
            {
                BlockedByEventId = null,
                DeferredSequenceNumbers = new List<long>()
            };
            var mockSbSession = CreateFakeSession(sessionState: stubState);

            MessageContext messageContext = new MessageContext(CreateFakeMessage().Object, mockSbSession.Object);

            // Act
            var isSessionBlocked = await messageContext.IsSessionBlocked();

            // Assert
            Assert.False(isSessionBlocked);
        }

        [Fact]
        public async void IsSessionBlockedByThis_WhenSessionStateSpecifiesBlockingEventIdThatMatchesEventIdOfMessageContext_ReturnsTrue()
        {
            string blockingEventId = "blabala";
            // Arrange
            var stubState = new SessionState()
            {
                BlockedByEventId = blockingEventId,
            };
            var stubSbSession = CreateFakeSession(sessionState: stubState);
            Mock<IServiceBusMessage> stubSbMessage = CreateFakeMessage(eventId: blockingEventId);

            MessageContext messageContext = new MessageContext(stubSbMessage.Object, stubSbSession.Object);

            // Act
            var isSessionBlockedByThis = await messageContext.IsSessionBlockedByThis();

            // Assert
            Assert.True(isSessionBlockedByThis);
        }

        [Fact]
        public async void IsSessionBlockedByThis_WhenSessionStateSpecifiesBlockingEventIdThatDoesNotMatchEventIdOfMessageContext_ReturnsFalse()
        {
            string blockingEventId = "blabala";
            string eventId = "uhasudahuduhasd";

            // Arrange
            var stubState = new SessionState()
            {
                BlockedByEventId = blockingEventId,
            };
            var stubSbSession = CreateFakeSession(sessionState: stubState);
            Mock<IServiceBusMessage> stubSbMessage = CreateFakeMessage(eventId: eventId);

            MessageContext messageContext = new MessageContext(stubSbMessage.Object, stubSbSession.Object);

            // Act
            var isSessionBlockedByThis = await messageContext.IsSessionBlockedByThis();

            // Assert
            Assert.False(isSessionBlockedByThis);
        }

        [Fact]
        public async void Defer_WhenMessageIsActive_AddsDeferralReferenceToSessionState()
        {
            // Arrange
            var sequenceNumber = 42;
            var stubSbMessage = CreateFakeMessage(sequenceNumber: sequenceNumber);
            var mockSbSession = CreateFakeSession();

            MessageContext messageContext = new MessageContext(stubSbMessage.Object, mockSbSession.Object);

            // Act
            await messageContext.Defer();

            // Assert
            mockSbSession.Verify(s => s.SetStateAsync(It.Is<SessionState>(ss => ss.DeferredSequenceNumbers.Contains(sequenceNumber))), Times.Once);
        }

        [Fact]
        public async void Defer_WhenMessageIsActive_InvokesDeferAsyncOnSbSession()
        {
            // Arrange
            var sequenceNumber = 42;
            var stubSbMessage = CreateFakeMessage(sequenceNumber: sequenceNumber);
            var mockSbSession = CreateFakeSession();

            MessageContext messageContext = new MessageContext(stubSbMessage.Object, mockSbSession.Object);

            // Act
            await messageContext.Defer();

            // Assert
            mockSbSession.Verify(s => s.DeferAsync(stubSbMessage.Object), Times.Once);
        }

        private Mock<IServiceBusMessage> CreateFakeMessage(string lockToken = "blabla", string sessionId = null, string from = null, string eventId = null, MessageType? messageType = null, string to = null, long sequenceNumber = 0, string messageId = null)
        {
            var result = new Mock<IServiceBusMessage>();

            result
                .SetupGet(m => m.LockToken)
                .Returns(lockToken);

            result
                .SetupGet(m => m.SessionId)
                .Returns(sessionId);

            result
                .SetupGet(m => m.MessageId)
                .Returns(messageId);

            result
                .SetupGet(m => m.SequenceNumber)
                .Returns(sequenceNumber);

            result
                .Setup(m => m.GetUserProperty(UserPropertyName.From))
                .Returns(from);

            result
                .Setup(m => m.GetUserProperty(UserPropertyName.To))
                .Returns(to);

            result
                .Setup(m => m.GetUserProperty(UserPropertyName.EventId))
                .Returns(eventId);

            result
                .Setup(m => m.GetUserProperty(UserPropertyName.MessageType))
                .Returns(messageType?.ToString());

            return result;
        }

        private Mock<IServiceBusSession> CreateFakeSession(SessionState sessionState = null, IServiceBusMessage deferred = null)
        {
            var result = new Mock<IServiceBusSession>();

            if (sessionState == null)
                sessionState = new SessionState();
            result
                .Setup(s => s.GetStateAsync())
                .Returns(Task.FromResult(sessionState));

            if (deferred != null)
            {
                result
                    .Setup(s => s.ReceiveDeferredMessageAsync(deferred.SequenceNumber))
                    .Returns(Task.FromResult(deferred));
            }

            return result;
        }

    }
}
