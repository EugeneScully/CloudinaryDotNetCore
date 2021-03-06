﻿using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;

namespace CloudinaryDotNet.Actions
{
    [DataContract]
    public class DelDerivedResResult : BaseResult
    {
        [DataMember(Name = "deleted")]
        public Dictionary<string, string> Deleted { get; protected set; }

        /// <summary>
        /// Parses HTTP response and creates new instance of this class
        /// </summary>
        /// <param name="response">HTTP response</param>
        /// <returns>New instance of this class</returns>
        internal static DelDerivedResResult Parse(HttpWebResponse response)
        {
            return Parse<DelDerivedResResult>(response);
        }
    }
}
