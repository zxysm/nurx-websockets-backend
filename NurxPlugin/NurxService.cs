using Newtonsoft.Json;
using PoGo.NecroBot.CLI;
using NurxPlugin.SenderResponders;
using NurxPlugin.Senders;
using PoGo.NecroBot.CLI.Plugin;
using PoGo.NecroBot.Logic;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NurxPlugin
{

    class NurxService : INecroPlugin
    {
        // Private vars.
        private Session _pogoSession;
        private ConsoleLogger _logger;
        private GlobalSettings _settings;
        private WebSocketServer _websocket;
        private List<WebSocketSession> _authSessions;
        private Dictionary<string, INurxMessageResponder> _responders;

    
        // Public properties.
        public ConsoleLogger Logger { get { return _logger; } }


        /// <summary>
        /// Create the Nurx Service and kick off the websockets server.
        /// </summary>
        /// <param name="pogoSession"></param>
        public void Initialize(PluginInitializerInfo startInfo)
        {
            if (startInfo.Session == null) throw new ArgumentException("NecroBot Session not given.");
            if (startInfo.Settings == null) throw new ArgumentException("NecroBot Settings not given.");
            if (startInfo.Statistics == null) throw new ArgumentException("NecroBot Statistics object not given.");
            if (startInfo.Logger == null) throw new ArgumentException("NecroBot Logger object not given.");

            _pogoSession = startInfo.Session;
            _logger = startInfo.Logger;
            _settings = startInfo.Settings;         
            _responders = new Dictionary<string, INurxMessageResponder>();
            
            // TODO: Use reflection to discover and register all senders and responders.
            INurxMessageSender Log = new LogSender();
            Log.RegisterSender(_pogoSession, this);

            LocationSenderResponder Location = new LocationSenderResponder();
            Location.RegisterSender(_pogoSession, this);
            Location.RegisterResponder(_pogoSession, this);

            PokeStopSenderResponder PokeStop = new PokeStopSenderResponder();
            PokeStop.RegisterSender(_pogoSession, this);
            PokeStop.RegisterResponder(_pogoSession, this);

            PokemonListSenderResponder PokemonList = new PokemonListSenderResponder();
            PokemonList.RegisterSender(_pogoSession, this);
            PokemonList.RegisterResponder(_pogoSession, this);

            ProfileSenderResponder Profile = new ProfileSenderResponder();
            Profile.RegisterSender(_pogoSession, this);
            Profile.RegisterResponder(_pogoSession, this);

            // Setup the websocket and check for success.
            if (SetupWebSocket())
            {
                // Broadcast statistics.
                startInfo.Statistics.DirtyEvent += () =>
                {
                    var currentStats = startInfo.Statistics.GetCurrentInfo(_pogoSession.Inventory);
                    Broadcast("stats", currentStats);
                };
            }
        }


        /// <summary>
        /// Add a responder to the responders dictionary.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="responder"></param>
        public void RegisterResponder(string command, INurxMessageResponder responder)
        {
            if (!_responders.ContainsKey(command))
                _responders.Add(command, responder);
        }


        /// <summary>
        /// Create the websocket server.
        /// </summary>
        private bool SetupWebSocket()
        {
            _authSessions = new List<WebSocketSession>();
            _websocket = new WebSocketServer();

            // Create websockets server setup.
            /*var config = new ServerConfig
            {
                Name = "NurxWebSocket",
                Certificate = new CertificateConfig
                {
                    FilePath = @"cert.pfx",
                    Password = "necro"
                },
                Port = _settings.NurxWebSocketPort,
                Security = "tls",
                Ip = "Any"
            };*/

            // Setup the appServer
            if (!_websocket.Setup(14151))
            {
                Logger.Write("Failed to setup Nurx Websockets server.", PoGo.NecroBot.Logic.Logging.LogLevel.Error);                
                return false;
            }

            // Set hooks.
            _websocket.NewMessageReceived += new SessionHandler<WebSocketSession, string>(WebSocket_NewMessageReceived); 
            _websocket.NewSessionConnected += new SessionHandler<WebSocketSession>(WebSocket_NewSessionConnected);
            _websocket.SessionClosed += new SessionHandler<WebSocketSession, CloseReason>(WebSocket_SessionClosed);

            // Try to start the appServer
            if (!_websocket.Start())
            {
                Logger.Write("Failed to start Nurx Websockets server.", PoGo.NecroBot.Logic.Logging.LogLevel.Error);                
                return false;
            }

            return true;
        }
        

        /// <summary>
        /// Handle websockets session close.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        private void WebSocket_SessionClosed(WebSocketSession session, CloseReason reason)
        {
            // Sometimes this doesn't work. ¯\_(ツ)_/¯
            try
            {
                if (_authSessions.Contains(session))
                    _authSessions.Remove(session);
            }
            catch { }
        }


        /// <summary>
        /// Handle websockets new session.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        private void WebSocket_NewSessionConnected(WebSocketSession session)
        {
            // TODO: Remove this and wait for actual auth.
            _authSessions.Add(session);
        }


        /// <summary>
        /// Handle websockets command received.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        private void WebSocket_NewMessageReceived(WebSocketSession session, string message)
        {
            // Make sure the session is authenticated.
            if (_authSessions.Contains(session))
            {
                // Don't try to interact with NecroBot if the profile hasn't
                // even loaded yet.
                if (_pogoSession.Profile == null)
                    return;

                try
                {
                    NurxCommand cmd = JsonConvert.DeserializeObject<NurxCommand>(message);
                    // Find the appropriate responder and pass it the message.
                    if (_responders.ContainsKey(cmd.Command))
                            _responders[cmd.Command].MessageReceived(cmd, session);

                }
                catch (Exception ex)
                {
                    Logger.Write("Error processing nurx websockets command: " + ex.Message, LogLevel.Debug);
                    Logger.Write(ex.StackTrace, LogLevel.Debug);
                }
            }
        }


        /// <summary>
        /// Broadcast a message to all sessions.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="data"></param>
        public void Broadcast(string messageType, object data)
        {
            var response = new
            {
                MessageType = messageType,
                Data = data
            };

            _websocket.Broadcast(_authSessions.ToArray(), JsonConvert.SerializeObject(response), (s, b) => { });
        }


        /// <summary>
        /// Send a message to a single session.
        /// </summary>
        /// <param name="wsSession"></param>
        /// <param name="messageType"></param>
        /// <param name="data"></param>
        public void Send(WebSocketSession wsSession, string messageType, Object data)
        {
            var response = new
            {
                MessageType = messageType,
                Data = data
            };

            wsSession.Send(JsonConvert.SerializeObject(response));
        }
    }
}
