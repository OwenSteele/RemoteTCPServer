using System;
using System.Collections.Generic;

namespace RemoteTCPServer
{
    public class Security
    {
        /// <summary>
        /// Client request policy: 0 = Private, 1 = Prompt, 2 = auto accept
        /// </summary>
        private int _messaging = 2;
        private int _fileTransfer = 2;
        public int Messaging
        {
            get { return _messaging; }
            set { _messaging = Range(value); }
        }
        public int FileTransfer
        {
            get { return _fileTransfer; }
            set { _fileTransfer = Range(value); }
        }
        public string GetState(int property)
        {
            int secValue = -1;
            if (property == 0) secValue = _messaging;
            else if (property == 1) secValue = _fileTransfer;
            else return "Invalid request.";

            Dictionary<int, string> states = new()
            {
                { 0, "Private" },
                { 1, "Prompt" },
                { 2, "Auto Accept" }
            };
            return states.GetValueOrDefault(Range(secValue));
        }

        private int Range(int x, int upper = 2)
        {
            if (x < 0 || x > upper) throw new ArgumentOutOfRangeException(
                message: (upper == 2) ? "Client request policy: 0 = Private, 1 = Prompt, 2 = auto accept" : "No property", null);
            return x;
        }
    }
}
