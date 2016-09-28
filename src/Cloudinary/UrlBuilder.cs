using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace CloudinaryDotNet
{
    public class UrlBuilder : UriBuilder
    {
        StringDictionary m_queryString = null;
        private readonly IHttpContextAccessor m_contextAccessor;

        public StringDictionary QueryString
        {
            get
            {
                if (m_queryString == null)
                {
                    m_queryString = new StringDictionary();
                }

                return m_queryString;
            }
        }

        public string PageName
        {
            get
            {
                string path = base.Path;
                return path.Substring(path.LastIndexOf("/") + 1);
            }
            set
            {
                string path = base.Path;
                path = path.Substring(0, path.LastIndexOf("/"));
                base.Path = string.Concat(path, "/", value);
            }
        }

        public UrlBuilder(string uri, IHttpContextAccessor contextAccessor)
            : base(uri)
        {
            PopulateQueryString();
            m_contextAccessor = contextAccessor;
        }

        public UrlBuilder(string uri, IDictionary<string, object> @params, IHttpContextAccessor contextAccessor)
            : base(uri)
        {
            PopulateQueryString();
            SetParameters(@params);
            m_contextAccessor = contextAccessor;
        }

        public void SetParameters(IDictionary<string, object> @params)
        {
            foreach (var param in @params)
            {
                if (param.Value is IEnumerable<string>)
                {
                    foreach (var s in (IEnumerable<string>)param.Value)
                    {
                        QueryString.Add(param.Key + "[]", s);
                    }
                }
                else
                {
                    QueryString[param.Key] = param.Value.ToString();
                }
            }
        }

        public new string ToString()
        {
            BuildQueryString();

            return base.Uri.AbsoluteUri;
        }

        public void Navigate()
        {
            _Navigate(true);
        }

        public void Navigate(bool endResponse)
        {
            _Navigate(endResponse);
        }

        private void _Navigate(bool endResponse)
        {
            string uri = this.ToString();
            m_contextAccessor.HttpContext.Response.Redirect(uri, endResponse);
        }

        private void PopulateQueryString()
        {
            string query = base.Query;

            if (query == string.Empty || query == null)
            {
                return;
            }

            if (m_queryString == null)
            {
                m_queryString = new StringDictionary();
            }

            m_queryString.Clear();

            query = query.Substring(1); //remove the ?

            string[] pairs = query.Split(new char[] { '&' });
            foreach (string s in pairs)
            {
                string[] pair = s.Split(new char[] { '=' });

                m_queryString[pair[0]] = (pair.Length > 1) ? pair[1] : string.Empty;
            }
        }

        private void BuildQueryString()
        {
            if (m_queryString == null) return;

            int count = m_queryString.Count;

            if (count == 0)
            {
                base.Query = string.Empty;
                return;
            }

            string[] keys = new string[count];
            string[] values = new string[count];
            string[] pairs = new string[count];

            m_queryString.Keys.CopyTo(keys, 0);
            m_queryString.Values.CopyTo(values, 0);

            for (int i = 0; i < count; i++)
            {
                pairs[i] = string.Concat(keys[i], "=", values[i]);
            }

            base.Query = string.Join("&", pairs);
        }
    }
}
