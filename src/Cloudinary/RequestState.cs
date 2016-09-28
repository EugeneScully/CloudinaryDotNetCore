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
        #region Internal Properties
        private readonly HttpWebRequest _request;
        private HttpWebResponse _response;
        private Stream _streamResponse;
        private readonly ManualResetEvent _streamDone = new ManualResetEvent(false);
        private readonly ManualResetEvent _responseDone = new ManualResetEvent(false);
        #endregion Internal Properties

        public RequestState(HttpWebRequest request)
        {
            _request = request;
            _streamResponse = null;
        }

        public void SetRequestResponseFromResult(IAsyncResult asynchronousResult)
        {
            _streamResponse = _request.EndGetRequestStream(asynchronousResult);
            _streamDone.Set();
        }

        public void SetResponseFromResult(IAsyncResult asynchronousResult)
        {
            var response = (HttpWebResponse)_request.EndGetResponse(asynchronousResult);
            SetResponse(response);
        }

        public void SetResponse(HttpWebResponse response)
        {
            _response = response;
            _responseDone.Set();
        }

        /// <summary>
        /// Abort the request
        /// </summary>
        public void Abort()
        {
            _responseDone.Set();
            _streamDone.Set();
        }

        /// <summary>
        /// Wait for the request stream
        /// </summary>
        /// <param name="timeout">The time to wait.</param>
        /// <returns></returns>
        public Stream WaitForRequestStream(int timeout)
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
