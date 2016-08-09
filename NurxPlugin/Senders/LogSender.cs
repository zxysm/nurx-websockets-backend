using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.CLI;

namespace NurxPlugin.Senders
{
    class LogSender : INurxMessageSender
    {
        // Private vars.
        private NurxService _service;


        /// <summary>
        /// Hook the events for log writes to pipe them out to listeners.
        /// </summary>
        /// <param name="pogoSession">Necro pogo session.</param>
        /// <param name="service">Nurx service instance.</param>
        public void RegisterSender(Session pogoSession, NurxService service)
        {
            _service = service;
            _service.Logger.OnLogWrite += Logger_OnLogWrite;
        }

        /// <summary>
        /// Send the log data out to any clients.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Log write event args instance.</param>
        private void Logger_OnLogWrite(object sender, LogWriteEventArgs e)
        {
            _service.Broadcast("log_message", e);
        }
    }
}
