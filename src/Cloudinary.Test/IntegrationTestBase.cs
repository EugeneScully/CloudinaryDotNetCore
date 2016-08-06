//using Cloudinary.Test.Properties;
using CloudinaryDotNet.Actions;
using Moq;
//using NUnit.Framework;
using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CloudinaryDotNet.Test
{
    public class HttpContextAccessor : IHttpContextAccessor
    {
        public HttpContextAccessor()
        {
            HttpContext = new DefaultHttpContext();
        }

        public HttpContext HttpContext { get; set; }
    }

    public class IntegrationTestBase
    {
        public IntegrationTestBase()
        {
            Initialize();
        }

        protected string m_testImagePath;
        protected string m_testVideoPath;
        protected string m_testPdfPath;
        protected string m_testIconPath;

        protected const string TEST_TAG = "cloudinarydotnet_test";

        protected Account m_account;
        protected Cloudinary m_cloudinary;

        protected IConfigurationRoot m_config;

        public virtual void Initialize()
        {
            m_config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Properties\\launchSettings.json")
                .Build();

            m_account = GetAccountInstance();
            m_cloudinary = GetCloudinaryInstance(m_account);

            var basepath = System.AppContext.BaseDirectory;

            m_testVideoPath = Path.Combine(basepath, "movie.mp4");
            m_testImagePath = Path.Combine(basepath, "TestImage.jpg");
            m_testPdfPath = Path.Combine(basepath, "multipage.pdf");
            m_testIconPath = Path.Combine(basepath, "favicon.ico");

            var assembly = Assembly.Load(new AssemblyName("Cloudinary.Tests"));
            var resources = string.Join(Environment.NewLine, assembly.GetManifestResourceNames());
            Console.WriteLine("List of Manifest Resource Names");
            Console.WriteLine("======================");
            Console.WriteLine(resources);

            using (var fs = new FileStream(m_testPdfPath, FileMode.Create))
            {
                var inStream = assembly.GetManifestResourceStream("Cloudinary.Tests.Resources.multipage.pdf");
                inStream.CopyTo(fs);
            }

            using (var fs = new FileStream(m_testVideoPath, FileMode.Create))
            {
                var inStream = assembly.GetManifestResourceStream("Cloudinary.Tests.Resources.movie.mp4");
                inStream.CopyTo(fs);
            }

            using (var fs = new FileStream(m_testImagePath, FileMode.Create))
            {
                var inStream = assembly.GetManifestResourceStream("Cloudinary.Tests.Resources.TestImage.jpg");
                inStream.CopyTo(fs);
            }

            using (var fs = new FileStream(m_testIconPath, FileMode.Create))
            {
                var inStream = assembly.GetManifestResourceStream("Cloudinary.Tests.Resources.favicon.ico");
                inStream.CopyTo(fs);
            }
        }

        /// <summary>
        /// A convenience method for uploading an image before testing
        /// </summary>
        /// <param name="id">The ID of the resource</param>
        /// <returns>The upload results</returns>
        protected ImageUploadResult UploadTestResource(String id)
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(m_testImagePath),
                PublicId = id,
                Tags = "test"
            };
            return m_cloudinary.Upload(uploadParams);
        }

        /// <summary>
        /// A convenience method for deleting an image in the test
        /// </summary>
        /// <param name="id">The ID of the image to delete</param>
        /// <returns>The results of the deletion</returns>
        protected DelResResult DeleteTestResource(String id)
        {
            return m_cloudinary.DeleteResources(id);
        }

        /// <summary>
        /// A convenient method for initialization of new Account instance with necessary checks
        /// </summary>
        /// <returns>New Account instance</returns>
        private Account GetAccountInstance()
        {
            string cloudName = m_config["profiles:Development:environmentVariables:CloudName"];
            string apiSecret = m_config["profiles:Development:environmentVariables:ApiSecret"];
            string apiKey = m_config["profiles:Development:environmentVariables:ApiKey"];

            Account account = new Account(cloudName, apiKey, apiSecret);

            if (string.IsNullOrEmpty(account.Cloud))
                Console.WriteLine("Cloud name must be specified in test configuration (app.config)!");

            if (string.IsNullOrEmpty(account.ApiKey))
                Console.WriteLine("Cloudinary API key must be specified in test configuration (app.config)!");

            if (string.IsNullOrEmpty(account.ApiSecret))
                Console.WriteLine("Cloudinary API secret must be specified in test configuration (app.config)!");

            Assert.False(string.IsNullOrEmpty(account.Cloud));
            Assert.False(string.IsNullOrEmpty(account.ApiKey));
            Assert.False(string.IsNullOrEmpty(account.ApiSecret));
            return account;
        }

        /// <summary>
        /// A convenient method for initialization of new Coudinary instance with necessary checks
        /// </summary>
        /// <param name="account">Instance of Account</param>
        /// <returns>New Cloudinary instance</returns>
        protected Cloudinary GetCloudinaryInstance(Account account)
        {
            Cloudinary cloudinary = new Cloudinary(account, new HttpContextAccessor());
            var apiBaseAddress = m_config["profiles:Development:environmentVariables:ApiBaseAddress"];
            if (!string.IsNullOrWhiteSpace(apiBaseAddress) && apiBaseAddress != "None")
                cloudinary.Api.ApiBaseAddress = apiBaseAddress;
            return cloudinary;
        }

        ///// <summary>
        ///// Get stream from mock request to Cloudinary API represented as String
        ///// </summary>
        ///// <param name="requestParams">Parameters for Cloudinary call</param>
        ///// <param name="cloudinaryCall">Cloudinary call, e.g. "(cloudinaryInstance, params) => {return cloudinaryInstance.Text(params); }"</param>
        ///// <returns></returns>
        //protected string GetMockBodyOfCoudinaryRequest<TParams, TResult>(TParams requestParams, Func<Cloudinary, TParams, TResult> cloudinaryCall) 
        //    where TParams: BaseParams 
        //    where TResult : BaseResult
        //{
        //    #region Mock infrastructure
        //    var mock = new Mock<HttpWebRequest>();

        //    mock.Setup(x => x.GetRequestStream()).Returns(new MemoryStream());
        //    mock.Setup(x => x.GetResponse()).Returns((WebResponse)null);
        //    mock.CallBase = true;

        //    HttpWebRequest request = null;
        //    Func<string, HttpWebRequest> requestBuilder = (x) =>
        //    {
        //        request = mock.Object;
        //        request.Headers = new WebHeaderCollection();
        //        return request;
        //    };
        //    #endregion

        //    Cloudinary fakeCloudinary = GetCloudinaryInstance(m_account);
        //    fakeCloudinary.Api.RequestBuilder = requestBuilder;

        //    try
        //    {
        //        cloudinaryCall(fakeCloudinary, requestParams);
        //    }
        //    // consciously return null in GetResponse() and extinguish the ArgumentNullException while parsing response, 'cause it's not in focus of current test
        //    catch (ArgumentNullException) { }

        //    MemoryStream stream = request.GetRequestStream() as MemoryStream;
        //    return System.Text.Encoding.Default.GetString(stream.ToArray());
        //}

        protected long UnixTimeNow()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }
    }
}
