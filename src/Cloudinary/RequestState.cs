using System;
using System.IO;
using System.Net;
using System.Threading;

namespace CloudinaryDotNet
{
    /// <summary>
    /// Class to track the status of a request
    /// </summary>
    internal class RequestState : IDisposable
    {
        #region Properties
        // This class stores the State of the request.
        private readonly HttpWebRequest _request;
        private HttpWebResponse _response;
        private Stream _streamResponse;
        private readonly ManualResetEvent _streamDone = new ManualResetEvent(false);
        private readonly ManualResetEvent _responseDone = new ManualResetEvent(false);
        #endregion Properties

        public RequestState(HttpWebRequest request)
        {
            _request = request;
            _streamResponse = null;
        }

        public HttpWebRequest Request { get { return _request; } }

        public void SetRequestStream(Stream stream)
        {
            _streamResponse = stream;
            _streamDone.Set();
        }

        public void SetResponse(HttpWebResponse response)
        {
            _response = response;
            _responseDone.Set();
        }

        public void Abort()
        {
            _responseDone.Set();
            _streamDone.Set();
        }

        public Stream WaitForStream(int timeout)
        {
            if (timeout > 0)
            {
                if (!_streamDone.WaitOne(timeout))
                    throw new WebException("Timeout waiting for request stream", WebExceptionStatus.Timeout);
            }
            else
                _streamDone.WaitOne();
            return _streamResponse;
        }

        public HttpWebResponse WaitForResponse(int timeout)
        {
            if (timeout > 0)
            {
                if (!_responseDone.WaitOne(timeout))
                    throw new WebException("Timeout waiting for response", WebExceptionStatus.Timeout);
            }
            else
                _responseDone.WaitOne();
            return _response;
        }

        public void Dispose()
        {
            _streamDone.Dispose();
            _responseDone.Dispose();
        }
    }
}
