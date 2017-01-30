﻿namespace WireMock.Logging
{
    /// <summary>
    /// LogEntry
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets or sets the request message.
        /// </summary>
        /// <value>
        /// The request message.
        /// </value>
        public RequestMessage RequestMessage { get; set; }

        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        /// <value>
        /// The response message.
        /// </value>
        public ResponseMessage ResponseMessage { get; set; }
    }
}