﻿using System;

namespace WireMock.Admin.Mappings
{
    /// <summary>
    /// MappingModel
    /// </summary>
    public class MappingModel
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid? Guid { get; set; }

        /// <summary>
        /// Gets or sets the boolean that controls if this mapping will be logged when it fails to match with a query
        /// </summary>
        public Boolean LogOnMatchFail { get; set; }

        /// <summary>
        /// The unique title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The priority.
        /// </summary>
        public int? Priority { get; set; }

        /// <summary>
        /// The Scenario.
        /// </summary>
        public string Scenario { get; set; }

        /// <summary>
        /// Execution state condition for the current mapping.
        /// </summary>
        public object WhenStateIs { get; set; }

        /// <summary>
        /// The next state which will be signaled after the current mapping execution.
        /// In case the value is null state will not be changed.
        /// </summary>
        public object SetStateTo { get; set; }

        /// <summary>
        /// The request.
        /// </summary>
        public RequestModel Request { get; set; }

        /// <summary>
        /// The response.
        /// </summary>
        public ResponseModel Response { get; set; }
    }
}