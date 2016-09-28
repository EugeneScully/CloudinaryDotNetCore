﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CloudinaryDotNet.Actions;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.Threading;

namespace CloudinaryDotNet
{
    /// <summary>
    /// Technological layer to work with cloudinary API
    /// </summary>
    public class Api : ISignProvider
    {
        public const string ADDR_API = "api.cloudinary.com";
        public const string ADDR_RES = "res.cloudinary.com";
        public const string API_VERSION = "v1_1";
        public const string HTTP_BOUNDARY = "notrandomsequencetouseasboundary";
        public static readonly string USER_AGENT;

        string m_apiAddr = "https://" + ADDR_API;

        public bool CSubDomain;
        public bool ShortenUrl;
        public bool UseRootPath;
        public bool UsePrivateCdn;
        public string PrivateCdn;
        public string Suffix;
        public string UserPlatform;
        public Func<string, HttpWebRequest> RequestBuilder = (x) => HttpWebRequest.Create(x) as HttpWebRequest;

        public int Timeout = 0;

        ///// <summary>
        ///// Sets whether to use the use chunked encoding. See http://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.6.1 for further info.
        ///// Server must support HTTP/1.1 in order to use the chunked encoding.
        ///// </summary>
        //public bool UseChunkedEncoding = true;

        /// <summary>
        /// Maximum size of chunk when uploading a file.
        /// </summary>
        public int ChunkSize = 65000;

        private readonly IHttpContextAccessor m_contextAccessor;

        ///// <summary>
        ///// Initializes the <see cref="Api"/> class.
        ///// </summary>
        //static Api()
        //{
        //    var version = Assembly.GetExecutingAssembly().GetName().Version;
        //    USER_AGENT = String.Format("CloudinaryDotNet/{0}.{1}.{2}",
        //        version.Major, version.Minor, version.Build);
        //}

        /// <summary>
        /// Default parameterless constructor.
        /// Assumes that environment variable CLOUDINARY_URL is set.
        /// </summary>
        public Api(IHttpContextAccessor contextAccessor)
            : this(Environment.GetEnvironmentVariable("CLOUDINARY_URL"), contextAccessor) { }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="cloudinaryUrl">Cloudinary URL</param>
        public Api(string cloudinaryUrl, IHttpContextAccessor contextAccessor)
        {
            if (string.IsNullOrEmpty(cloudinaryUrl))
                throw new ArgumentException("Valid cloudinary init string must be provided!");

            Uri cloudinaryUri = new Uri(cloudinaryUrl);

            if (string.IsNullOrEmpty(cloudinaryUri.Host))
                throw new ArgumentException("Cloud name must be specified as host name in URL!");

            string[] creds = cloudinaryUri.UserInfo.Split(':');
            Account = new Account(cloudinaryUri.Host, creds[0], creds[1]);

            UsePrivateCdn = !string.IsNullOrEmpty(cloudinaryUri.AbsolutePath) &&
                cloudinaryUri.AbsolutePath != "/";

            PrivateCdn = string.Empty;

            if (UsePrivateCdn)
            {
                PrivateCdn = cloudinaryUri.AbsolutePath;
            }
            m_contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Parametrized constructor
        /// </summary>
        /// <param name="account">Cloudinary account</param>
        /// <param name="usePrivateCdn">Whether to use private Content Delivery Network</param>
        /// <param name="privateCdn">Private Content Delivery Network</param>
        /// <param name="shortenUrl">Whether to use shorten url when possible.</param>
        /// <param name="cSubDomain">if set to <c>true</c> [c sub domain].</param>
        public Api(Account account, bool usePrivateCdn, string privateCdn, bool shortenUrl, bool cSubDomain, IHttpContextAccessor contextAccessor)
            : this(account, contextAccessor)
        {
            UsePrivateCdn = usePrivateCdn;
            PrivateCdn = privateCdn;
            ShortenUrl = shortenUrl;
            CSubDomain = cSubDomain;
        }

        /// <summary>
        /// Parametrized constructor
        /// </summary>
        /// <param name="account">Cloudinary account</param>
        public Api(Account account, IHttpContextAccessor contextAccessor)
        {
            if (account == null)
                throw new ArgumentException("Account can't be null!");

            if (string.IsNullOrEmpty(account.Cloud))
                throw new ArgumentException("Cloud name must be specified in Account!");

            UsePrivateCdn = false;
            Account = account;
            m_contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Cloudinary account information.
        /// </summary>
        public Account Account { get; private set; }

        /// <summary>
        /// Gets or sets API base address (https://api.cloudinary.com by default) which is used to build ApiUrl*
        /// </summary>
        public string ApiBaseAddress
        {
            get { return m_apiAddr; }
            set { m_apiAddr = value; }
        }

        /// <summary>
        /// Default URL for working with resources
        /// </summary>
        public Url Url
        {
            get
            {
                return new Url(Account.Cloud, this)
                    .CSubDomain(CSubDomain)
                    .Shorten(ShortenUrl)
                    .PrivateCdn(UsePrivateCdn)
                    .Secure(UsePrivateCdn)
                    .SecureDistribution(PrivateCdn);
            }
        }

        /// <summary>
        /// Default URL for working with uploaded images
        /// </summary>
        public Url UrlImgUp
        {
            get
            {
                return Url
                    .ResourceType("image")
                    .Action("upload")
                    .UseRootPath(UseRootPath)
                    .Suffix(Suffix);
            }
        }

        /// <summary>
        /// Default URL for working with uploaded videos
        /// </summary>
        public Url UrlVideoUp
        {
            get
            {
                return Url
                    .ResourceType("video")
                    .Action("upload")
                    .UseRootPath(UseRootPath)
                    .Suffix(Suffix);
            }
        }

        /// <summary>
        /// Default cloudinary API URL
        /// </summary>
        public Url ApiUrl
        {
            get
            {
                return Url.
                    CloudinaryAddr(m_apiAddr);
            }
        }

        /// <summary>
        /// Default cloudinary API URL for uploading images
        /// </summary>
        public Url ApiUrlImgUp
        {
            get
            {
                return ApiUrl.
                    Action("upload").
                    ResourceType("image");
            }
        }

        /// <summary>
        /// Default cloudinary API URL with version
        /// </summary>
        public Url ApiUrlV
        {
            get
            {
                return ApiUrl.
                    ApiVersion(API_VERSION);
            }
        }

        /// <summary>
        /// Default cloudinary API URL for uploading images with version
        /// </summary>
        public Url ApiUrlImgUpV
        {
            get
            {
                return ApiUrlV.
                    Action("upload").
                    ResourceType("image");
            }
        }

        /// <summary>
        /// Default cloudinary API URL for uploading images with version
        /// </summary>
        public Url ApiUrlVideoUpV
        {
            get
            {
                return ApiUrlV.
                    Action("upload").
                    ResourceType("video");
            }
        }

        /// <summary>
        /// Gets cloudinary parameter from enumeration
        /// </summary>
        /// <typeparam name="T">Enum which fields are decorated with DescriptionAttribute</typeparam>
        /// <param name="e">Field of enum</param>
        /// <returns>Cloudinary-compatible parameter</returns>
        public static string GetCloudinaryParam<T>(T e)
        {
            Type eType = typeof(T);
            FieldInfo fi = eType.GetRuntimeField(e.ToString());
            DescriptionAttribute[] attrs = (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            if (attrs.Length == 0)
                throw new ArgumentException("Enum fields must be decorated with DescriptionAttribute!");

            return attrs[0].Description;
        }

        /// <summary>
        /// Parses cloudinary parameters and creates enumeration value
        /// </summary>
        /// <typeparam name="T">Enum which fields are decorated with DescriptionAttribute</typeparam>
        /// <param name="s">String to parse</param>
        /// <returns>API-compatible parameter</returns>
        public static T ParseCloudinaryParam<T>(string s)
        {
            Type eType = typeof(T);
            foreach (var fi in eType.GetRuntimeFields())
            {
                DescriptionAttribute[] attrs = (DescriptionAttribute[])fi.GetCustomAttributes(
                    typeof(DescriptionAttribute), false);

                if (attrs.Length == 0)
                    continue;

                if (s == attrs[0].Description)
                    return (T)fi.GetValue(null);
            }

            return default(T);
        }

        /// <summary>
        /// Custom call to cloudinary API
        /// </summary>
        /// <param name="method">HTTP method of call</param>
        /// <param name="url">URL to call</param>
        /// <param name="parameters">Dictionary of call parameters (can be null)</param>
        /// <param name="file">File to upload (must be null for non-uploading actions)</param>
        /// <returns>HTTP response on call</returns>
        public HttpWebResponse Call(HttpMethod method, string url, SortedDictionary<string, object> parameters, FileDescription file)
        {
#if DEBUG
            Console.WriteLine(String.Format("{0} REQUEST:", method));
            Console.WriteLine(url);
#endif

            HttpWebRequest request = RequestBuilder(url);
            request.Method = Enum.GetName(typeof(HttpMethod), method);

            //// Add platform information to the USER_AGENT header
            //// This is intended for platform information and not individual applications!
            //request.UserAgent = string.IsNullOrEmpty(UserPlatform)
            //    ? USER_AGENT
            //    : string.Format("{0} {1}", UserPlatform, USER_AGENT);

            //if (Timeout > 0)
            //{
            //    request.Timeout = Timeout;
            //}

            RequestState currentRequestState = new RequestState(request);

            try
            {
                byte[] authBytes = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", Account.ApiKey, Account.ApiSecret));
                request.Headers["Authorization"] = string.Format("Basic {0}", Convert.ToBase64String(authBytes));

                if ((method == HttpMethod.POST || method == HttpMethod.PUT) && parameters != null)
                {
                    // TODO: Property missing in .net core 1.0
                    //if (UseChunkedEncoding)
                    //    request.SendChunked = true;

                    request.ContentType = "multipart/form-data; boundary=" + HTTP_BOUNDARY;

                    if (!parameters.ContainsKey("unsigned") || parameters["unsigned"].ToString() == "false")
                        FinalizeUploadParameters(parameters);

                    request.BeginGetRequestStream(new AsyncCallback(RequestStreamCallback), currentRequestState);

                    using (Stream requestStream = currentRequestState.WaitForStream(Timeout))
                    {
                        using (StreamWriter writer = new StreamWriter(requestStream))
                        {
                            foreach (var param in parameters)
                            {
                                if (param.Value != null)
                                {
                                    if (param.Value is IEnumerable<string>)
                                    {
                                        foreach (var item in (IEnumerable<string>)param.Value)
                                        {
                                            WriteParam(writer, param.Key + "[]", item);
                                        }
                                    }
                                    else
                                    {
                                        WriteParam(writer, param.Key, param.Value.ToString());
                                    }
                                }
                            }

                            if (file != null)
                            {
                                WriteFile(writer, file);
                            }

                            writer.Write("--{0}--", HTTP_BOUNDARY);
                        }
                    }
                }

                try
                {
                    request.BeginGetResponse(new AsyncCallback(ResponseCallback), currentRequestState);
                    var response = currentRequestState.WaitForResponse(Timeout);
                    return response;
                }
                catch (WebException ex)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response == null) throw;
                    else return response;
                }
            }
            finally
            {
                currentRequestState.Dispose();
            }
        }

        private static void RequestStreamCallback(IAsyncResult asynchronousResult)
        {
            // State of request is asynchronous.
            RequestState requestState = (RequestState)asynchronousResult.AsyncState;

            try
            {
                requestState.SetRequestStream(requestState.Request.EndGetRequestStream(asynchronousResult));
            }
            catch (WebException e)
            {
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
                requestState.Abort();
            }
        }

        private static void ResponseCallback(IAsyncResult asynchronousResult)
        {
            // State of request is asynchronous.
            RequestState requestState = (RequestState)asynchronousResult.AsyncState;

            try
            {
                requestState.SetResponse((HttpWebResponse)requestState.Request.EndGetResponse(asynchronousResult));
            }
            catch (WebException e)
            {
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
                if (e.Response is HttpWebResponse)
                    requestState.SetResponse(e.Response as HttpWebResponse);
                else
                    requestState.Abort();
            }
        }

        /// <summary>
        /// Signs and serializes upload parameters.
        /// </summary>
        /// <param name="parameters">Dictionary of upload parameters.</param>
        /// <returns>JSON representation of upload parameters.</returns>
        public string PrepareUploadParams(IDictionary<string, object> parameters)
        {
            if (parameters == null)
                parameters = new SortedDictionary<string, object>();

            if (!(parameters is SortedDictionary<string, object>))
                parameters = new SortedDictionary<string, object>(parameters);

            string path = "";
            if (parameters.ContainsKey("callback") && parameters["callback"] != null)
                path = parameters["callback"].ToString();

            try
            {
                parameters["callback"] = BuildCallbackUrl(path);
            }
            catch (Exception) { }

            if (!parameters.ContainsKey("unsigned") || parameters["unsigned"].ToString() == "false")
                FinalizeUploadParameters(parameters);

            return JsonConvert.SerializeObject(parameters);
        }

        /// <summary>
        /// Gets the upload URL.
        /// </summary>
        /// <param name="resourceType">Type of the resource.</param>
        /// <returns>
        /// The upload URL.
        /// </returns>
        public string GetUploadUrl(string resourceType = "auto")
        {
            return ApiUrlV.Action("upload").ResourceType(resourceType).BuildUrl();
        }

        public string BuildCallbackUrl(string path = "")
        {
            if (String.IsNullOrEmpty(path))
                path = "/Content/cloudinary_cors.html";

            if (Regex.IsMatch(path.ToLower(), "^https?:/.*"))
            {
                return path;
            }
            else
            {
                if (m_contextAccessor.HttpContext != null)
                {
                    var url = new Uri(Microsoft.AspNetCore.Http.Extensions.UriHelper.GetEncodedUrl(m_contextAccessor.HttpContext.Request));
                    return new Uri(url, path).ToString();
                }
                else
                    throw new Exception("Http context is not set. Either use this method in the right context or provide an absolute path to file!");
            }
        }

        /// <summary>
        /// Builds HTML form
        /// </summary>
        /// <returns>HTML form</returns>
#if NET40
        public IHtmlString BuildUnsignedUploadForm(string field, string preset, IDictionary<string, object> parameters = null, IDictionary<string, string> htmlOptions = null)
#else
        public string BuildUnsignedUploadForm(string field, string preset, SortedDictionary<string, object> parameters = null, Dictionary<string, string> htmlOptions = null)
#endif
        {
            if (parameters == null)
                parameters = new SortedDictionary<string, object>();

            parameters.Add("upload_preset", preset);
            parameters.Add("unsigned", true);

            return BuildUploadForm(field, "image", parameters, htmlOptions);
        }

        /// <summary>
        /// Builds HTML form
        /// </summary>
        /// <returns>HTML form</returns>
#if NET40
        public IHtmlString BuildUploadForm(string field, string resourceType, IDictionary<string, object> parameters = null, IDictionary<string, string> htmlOptions = null)
#else
        public string BuildUploadForm(string field, string resourceType, SortedDictionary<string, object> parameters = null, Dictionary<string, string> htmlOptions = null)
#endif
        {
            if (htmlOptions == null)
                htmlOptions = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(resourceType))
                resourceType = "auto";

            StringBuilder builder = new StringBuilder();

            builder.
                Append("<input type='file' name='file' data-url='").
                Append(GetUploadUrl(resourceType)).
                Append("' data-form-data='").
                Append(PrepareUploadParams(parameters)).
                Append("' data-cloudinary-field='").
                Append(field).
                Append("' class='cloudinary-fileupload");

            if (htmlOptions.ContainsKey("class"))
            {
                builder.Append(" ").Append(htmlOptions["class"]);
            }

            System.Text.Encodings.Web.HtmlEncoder encoder = System.Text.Encodings.Web.HtmlEncoder.Create(new System.Text.Encodings.Web.TextEncoderSettings());

            foreach (var item in htmlOptions)
            {
                if (item.Key == "class") continue;

                builder.
                    Append("' ").
                    Append(item.Key).
                    Append("='").
                    Append(encoder.Encode(item.Value));// HttpUtility.HtmlEncode(item.Value));
            }

            builder.Append("'/>");

#if NET40
            return new HtmlString(builder.ToString());
#else
            return builder.ToString();
#endif
        }

        /// <summary>
        /// Calculates signature of parameters
        /// </summary>
        /// <param name="parameters">Parameters to sign</param>
        /// <returns>Signature of parameters</returns>
        public string SignParameters(IDictionary<string, object> parameters)
        {
            StringBuilder signBase = new StringBuilder(String.Join("&", parameters
                .Where(pair => pair.Value != null)
                .Select(pair => String.Format("{0}={1}", pair.Key,
                    pair.Value is IEnumerable<string>
                    ? String.Join(",", ((IEnumerable<string>)pair.Value).ToArray())
                    : pair.Value.ToString()))
                .ToArray()));

            signBase.Append(Account.ApiSecret);

            var hash = ComputeHash(signBase.ToString());
            StringBuilder sign = new StringBuilder();
            foreach (byte b in hash) sign.Append(b.ToString("x2"));

            return sign.ToString();
        }

        /// <summary>
        /// Signs the specified URI part.
        /// </summary>
        /// <param name="uriPart">The URI part.</param>
        /// <returns></returns>
        public string SignUriPart(string uriPart)
        {

            var hash = ComputeHash(uriPart + Account.ApiSecret);
            var sign = Convert.ToBase64String(hash);
            return "s--" + sign.Substring(0, 8).Replace("+", "-").Replace("/", "_") + "--/";
        }

        private byte[] ComputeHash(string s)
        {
            using (var sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(Encoding.UTF8.GetBytes(s));
            }
        }

        /// <summary>
        /// Calculates current UNIX time
        /// </summary>
        /// <returns>Amount of seconds from 1 january 1970</returns>
        private string GetTime()
        {
            return Convert.ToInt64(((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds)).ToString();
        }

        internal void FinalizeUploadParameters(IDictionary<string, object> parameters)
        {
            parameters.Add("timestamp", GetTime());
            parameters.Add("signature", SignParameters(parameters));
            parameters.Add("api_key", Account.ApiKey);
        }

        private void WriteParam(StreamWriter writer, string key, string value)
        {
#if DEBUG
            Console.WriteLine(string.Format("{0}: {1}", key, value));
#endif
            WriteLine(writer, "--{0}", HTTP_BOUNDARY);
            WriteLine(writer, "Content-Disposition: form-data; name=\"{0}\"", key);
            WriteLine(writer);
            WriteLine(writer, value);
        }

        private void WriteFile(StreamWriter writer, FileDescription file)
        {
            if (file.IsRemote)
            {
                WriteParam(writer, "file", file.FilePath);
            }
            else
            {
                int bytesSent = 0;
                if (file.Stream == null)
                {
                    using (FileStream stream = new FileStream(file.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        stream.Seek(file.BytesSent, SeekOrigin.Begin);
                        file.EOF = WriteFile(writer, stream, file.BufferLength, file.FileName, out bytesSent);
                        file.BytesSent += bytesSent;
                    }
                }
                else
                {
                    file.EOF = WriteFile(writer, file.Stream, file.BufferLength, file.FileName, out bytesSent);
                    file.BytesSent += bytesSent;
                }
            }
        }

        /// <summary>
        /// Writes one chunk of file to stream.
        /// </summary>
        /// <param name="writer">Output writer.</param>
        /// <param name="fileName">Name of file.</param>
        /// <param name="stream">Input stream.</param>
        /// <param name="length">Maximum amount of bytes to send.</param>
        /// <param name="bytesSent">Amount of sent bytes.</param>
        /// <returns>
        /// true for EOF.
        /// </returns>
        private bool WriteFile(StreamWriter writer, Stream stream, int length, string fileName, out int bytesSent)
        {
            WriteLine(writer, "--{0}", HTTP_BOUNDARY);
            WriteLine(writer, "Content-Disposition: form-data;  name=\"file\"; filename=\"{0}\"", fileName);
            WriteLine(writer, "Content-Type: application/octet-stream");
            WriteLine(writer);

            writer.Flush();

            bytesSent = 0;
            int toSend = 0;
            byte[] buf = new byte[ChunkSize];
            int cnt = 0;

            while ((toSend = length - bytesSent) > 0
                && (cnt = stream.Read(buf, 0, (toSend > buf.Length ? buf.Length : toSend))) > 0)
            {
                writer.BaseStream.Write(buf, 0, cnt);
                bytesSent += cnt;
            }

            return cnt == 0;
        }

        private void WriteLine(StreamWriter writer)
        {
            writer.Write("\r\n");
        }

        private void WriteLine(StreamWriter writer, string format)
        {
            writer.Write(format);
            writer.Write("\r\n");
        }

        private void WriteLine(StreamWriter writer, string format, Object val)
        {
            writer.Write(format, val);
            writer.Write("\r\n");
        }
    }

    public interface ISignProvider
    {
        string SignParameters(IDictionary<string, object> parameters);
        string SignUriPart(string uriPart);
    }

    /// <summary>
    /// HTTP method
    /// </summary>
    public enum HttpMethod
    {
        /// <summary>DELETE</summary>
        DELETE,
        /// <summary>GET</summary>
        GET,
        /// <summary>POST</summary>
        POST,
        /// <summary>PUT</summary>
        PUT
    }

    /// <summary>
    /// Class to track the status of a request
    /// </summary>
    internal class RequestState : IDisposable
    {
        // This class stores the State of the request.
        private readonly HttpWebRequest _request;
        private HttpWebResponse _response;
        public Stream _streamResponse;
        private readonly ManualResetEvent _streamDone = new ManualResetEvent(false);
        private readonly ManualResetEvent _responseDone = new ManualResetEvent(false);

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