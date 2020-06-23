﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System.Threading.Tasks;

namespace Muplonen.Clients.Messages
{
    /// <summary>
    /// Handles chat messages.
    /// </summary>
    [MessageHandler(3)]
    public class ChatMessageHandler : IMessageHandler
    {
        private readonly PlayerSessionManager _clientManager;
        private readonly ObjectPool<GodotMessage> _messageObjectPool;
        private readonly ILogger<ChatMessageHandler> _logger;

        /// <summary>
        /// Creates a new <see cref="ChatMessageHandler"/> instance.
        /// </summary>
        /// <param name="clientManager">The client manager.</param>
        /// <param name="messageObjectPool">Pool for providing <see cref="GodotMessage"/> instances.</param>
        /// <param name="logger">Logging.</param>
        public ChatMessageHandler(
            PlayerSessionManager clientManager,
            ObjectPool<GodotMessage> messageObjectPool,
            ILogger<ChatMessageHandler> logger)
        {
            _clientManager = clientManager;
            _messageObjectPool = messageObjectPool;
            _logger = logger;
        }


        /// <inheritdoc/>
        public async Task<bool> HandleMessage(IPlayerSession session, GodotMessage message)
        {
            if (session.PlayerAccount == null) return false;

            var text = message.ReadString();
            _logger.LogInformation("\"{0}\" ({1}) said: \"{2}\"", session.PlayerAccount.Accountname, session.SessionId, text);

            var reply = _messageObjectPool.Get();
            try
            {
                reply.WriteUInt16(3);
                reply.WriteString(session.PlayerAccount.Accountname);
                reply.WriteString(text);

                foreach (var client in _clientManager.Clients.Values)
                    await client.Connection.Send(reply);
            }
            finally
            {
                _messageObjectPool.Return(reply);
            }

            return true;
        }
    }
}
