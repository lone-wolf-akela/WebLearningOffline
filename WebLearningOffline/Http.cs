using System;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Http;
namespace WebLearningOffline
{
    public static class Http
    {
        public static string GetResponse(ref HttpWebRequest req, out CookieCollection cookies)
        {
            HttpWebResponse res = null;
            StreamReader reader = null;
            try
            {
                cookies = null;
                res = (HttpWebResponse)req.GetResponse();
                cookies = res.Cookies;
                reader = new StreamReader(res.GetResponseStream(), Encoding.UTF8);
                string respHTML = reader.ReadToEnd();
                reader.Close();
                res.Close();
                return respHTML;
            }
            catch (Exception e)
            {
                try { reader.Close();}catch (Exception) { }
                try { res.Close();}catch (Exception) { }
                throw e;
            }
        }
        public static string GetResponse(ref HttpWebRequest req)
        {
            CookieCollection c;
            return GetResponse(ref req, out c);
        }
        public static HttpWebRequest GenerateRequest(string URL, string Method, string Referer = null, CookieCollection cookies = null)
        {
            Uri httpUrl = new Uri(URL);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(httpUrl);
            req.ProtocolVersion = new Version("1.0");
            req.Method = Method;
            req.KeepAlive = false;
            if (Referer != null) req.Referer = Referer;
            if (cookies != null)
            {
                req.CookieContainer = new CookieContainer();
                req.CookieContainer.Add(cookies);
            }
            return req;
        }
        public static string Get(string URL, out CookieCollection cookies, CookieCollection cookiesin = null, bool redirect=true)
        {
            HttpWebRequest req = GenerateRequest(URL, "GET", cookies: cookiesin);
            if (redirect == false) req.AllowAutoRedirect = false;
            var newcookies = new CookieCollection();
            string ret = null;
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    ret = GetResponse(ref req, out newcookies);
                    if (ret != null) break;
                }
                catch (Exception e)
                {
                    if (i == 9) throw e;
                    try
                    {
                        Thread.Sleep(1000);
                    }
                    catch (Exception) { }
                }
            }
            if (cookiesin == null) cookies = newcookies;
            else
            {
                cookiesin.Add(newcookies);
                cookies = cookiesin;
            }
            return ret;
        }
    }
}
