using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Runtime.Serialization;
using System.Threading;

namespace WebLearningOffline
{
    public class Course
    {
        public bool selected;
        public bool isnew;
        public string id;
        public string name;
        public string term;
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
        public bool Equals(Course c)
        {
            return c.id == id;
        }
        public override bool Equals(object obj)
        {
            return obj is Course && Equals((Course)obj);
        }
    }

    [DataContract] public class NoticeBody
    {
        [DataMember] public string title;
        [DataMember] public string owner;
        [DataMember] public string regDate;
        [DataMember] public string detail;
    }
    [DataContract] public class NoticeObject
    {
        [DataMember] public NoticePage paginationList;
    }
    [DataContract] public class NoticePage
    {
        [DataMember] public NoticeList[] recordList;
    }
    [DataContract] public class NoticeList
    {
        [DataMember] public NoticeBody courseNotice;
    }

    [DataContract] public class CourseInfo
    {
        [DataMember] public string course_no;
        [DataMember] public string course_seq;
        [DataMember] public string credit;
        [DataMember] public string course_time;
        [DataMember] public TeacherInfo teacherInfo;
        [DataMember] public string ref_book_c;
        [DataMember] public string guide;
        [DataMember] public string exam_type;
        [DataMember] public string requirement;
        [DataMember] public string detail_c;
    }
    [DataContract] public class TeacherInfo
    {
        [DataMember] public string name;
        [DataMember] public string email;
        [DataMember] public string phone;
        [DataMember] public string note;
    }
    [DataContract] public class InfoObject
    {
        [DataMember] public CourseInfo allInfo;
        [DataMember] public string schedule;
    }

    [DataContract] public class FileType: IComparable<FileType>
    {
		[DataMember] public FileTypeInfo courseOutlines;
		[DataMember] public int position;
		[DataMember] public FileEntry[] courseCoursewareList;
        public int CompareTo(FileType other)
        {
            return position.CompareTo(other.position);
        }
    }
    [DataContract] public class FileTypeInfo
    {
		[DataMember] public string title;
    }
	[DataContract] public class FileEntry: IComparable<FileEntry>
    {
		[DataMember] public FileDetail resourcesMappingByFileId;
		[DataMember] public int position;
		[DataMember] public string title;
		[DataMember] public string detail;
        public int CompareTo(FileEntry other)
        {
            return position.CompareTo(other.position);
        }
    }
	[DataContract] public class FileDetail
    {
		[DataMember] public string fileName;
		[DataMember] public string fileSize;
		[DataMember] public string fileId;
		[DataMember] public long regDate;
    }

    [DataContract] public class HomeworkObject
    {
        [DataMember] public HomeworkInfo courseHomeworkInfo;
        [DataMember] public HomeworkRecord courseHomeworkRecord;
    }
    [DataContract] public class HomeworkInfo
    {
        [DataMember] public long beginDate;
        [DataMember] public long endDate;
        [DataMember] public string title;
        [DataMember] public string detail;
        [DataMember] public string homewkAffix;
        [DataMember] public string homewkAffixFilename;

    }
    [DataContract] public class HomeworkRecord
    {
        [DataMember] public string status;
        [DataMember] public HomeworkFile resourcesMappingByHomewkAffix;
        [DataMember] public string homewkDetail;
        [DataMember] public string gradeUser;
        [DataMember] public string mark;
        [DataMember] public string replyDate;
        [DataMember] public string replyDetail;
        [DataMember] public HomeworkFile resourcesMappingByReplyAffix;
    }
    [DataContract] public class HomeworkFile
    {
        [DataMember] public string fileId;
        [DataMember] public string fileName;
        [DataMember] public string fileSize;
    }

    [Serializable] public class DownloadTask
    {
        public string url;
        public string local;
        public long size;
        public string name;
        public override int GetHashCode()
        {
            return url.GetHashCode();
        }
        public bool Equals(DownloadTask c)
        {
            return c.url == url;
        }
        public override bool Equals(object obj)
        {
            return obj is DownloadTask && Equals((DownloadTask)obj);
        }
    }

    public static class Util
    {
        public static void SetProgressSafe(System.Windows.Forms.ProgressBar progress, long current, long total)
        {
            var value = (int)Math.Round((double)current * (progress.Maximum - progress.Minimum) / (total == 0 ? 1 : total));
            SetProgressSafe(progress, value);
        }
        public static void SetProgressSafe(System.Windows.Forms.ProgressBar progress, int value)
        {
            if (value < progress.Minimum) value = progress.Minimum;
            if (value > progress.Maximum) value = progress.Maximum;
            progress.Value = value;
        }
        public static string FindHostInURL(string url)
        {
            var match = Regex.Match(url, @"http:\/\/(.+?)\/").Groups[1].Value;
            return match;
        }
        public static string FindPathInURL(string url)
        {
            var match = Regex.Match(url, @"http:\/\/.+?(\/.*)").Groups[1].Value;
            return match;
        }
        public static string CookieToHeaderString(CookieCollection cookies)
        {
            var tmp = "";
            foreach (Cookie cookie in cookies)
            {
                tmp += cookie.Name + "=" + cookie.Value + "; ";
            }
            if (tmp.EndsWith("; ")) tmp = tmp.Substring(0, tmp.Length - 2);
            return tmp;
        }
        public static string TimestampToDate(long unixTimeStamp)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).AddHours(8);
            return dtDateTime.ToString("yyyy-MM-dd");
        }
        public static long GetRemoteFileSize(string url, CookieCollection cookies)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.CookieContainer = new CookieContainer();
            req.CookieContainer.Add(cookies);
            req.Method = "HEAD";
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.82 Safari/537.36";
            using (var resp = req.GetResponse())
            {
                int ContentLength;
                if (int.TryParse(resp.Headers.Get("Content-Length"), out ContentLength))
                {
                    return ContentLength;
                }
            }
            return -1;
        }
        public static void SaveTaskList(List<DownloadTask> list, string file)
        {
            var bf = new BinaryFormatter();
            using(var fs=new FileStream(file,FileMode.Create))
                bf.Serialize(fs, list);
        }
        public static List<DownloadTask> LoadTaskList(string file)
        {
            var bf = new BinaryFormatter();
            try
            {
                using (var fs = new FileStream(file, FileMode.Open))
                {
                    var list = bf.Deserialize(fs) as List<DownloadTask>;
                    if (list != null) return list;
                    else return new List<DownloadTask>();
                }
            }
            catch (Exception)
            {
                return new List<DownloadTask>();
            }
        }
        public static string GetSafePathName(string s)
        {
            s = HttpUtility.HtmlDecode(s);
            foreach (var c in Path.GetInvalidPathChars().Union(Path.GetInvalidFileNameChars()))
            {
                s = s.Replace(c, '_');
            }
            return s.Trim().Replace(' ','_');
        }
        public static void DownloadFile(string url, string file, CookieCollection cookies)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    using (var wc = new CookieAwareWebClient(cookies))
                        wc.DownloadFile(url, file);
                    break;
                }
                catch (Exception e)
                {
                    if (i == 4) throw e;
                }
            }
        }
        public static Dictionary<string, object> InitDictionary(Course c)
        {
            var dict = new Dictionary<string, object>();
            dict.Add("CourseId", c.id);
            dict.Add("IsNew", c.isnew ? "Yes" : "No");
            dict.Add("CourseName", c.name);
            dict.Add("CourseTerm", c.term);
            return dict;
        }
        public static void WriteHTML(string infile, string outfile, Dictionary<string, object> array)
        {
            using (var sr = new StreamReader(infile))
            using (var sw = new StreamWriter(outfile))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    var match = Regex.Match(line, "<!-- replace (.+?) -->");
                    if (match.Success)
                    {
                        sw.WriteLine(ReplaceVariable(match.Groups[1].Value, array));
                    }
                    else
                    {
                        match = Regex.Match(line, "<!-- foreach (\\w+) (.+?) -->");
                        if (match.Success)
                        {
                            var aname = match.Groups[1].Value;
                            var templ = match.Groups[2].Value;
                            if (!array.ContainsKey(aname)|| array[aname].GetType() != typeof(List<Dictionary<string, object>>)) { sw.WriteLine(line); continue; }
                            var list = (List<Dictionary<string, object>>)array[aname];
                            list.ForEach(e => sw.WriteLine(ReplaceVariable(templ, e)));
                        }
                        else sw.WriteLine(line);
                    }
                }
            }
        }

        static string ReplaceVariable(string str, Dictionary<string, object> array)
        {
            var ret = new StringBuilder();
            var varname = new StringBuilder();
            var building = false;
            var iifstage = 0;
            var iifresult = false;
            str += " ";
            foreach (var c in str)
            {
                if (building)
                {
                    if (char.IsLetterOrDigit(c)) varname.Append(c);
                    else
                    {
                        var aname = varname.ToString();
                        building = false;
                        if (!array.ContainsKey(aname))
                        {
                            ret.Append('$');
                            ret.Append(aname);
                            ret.Append(c);
                        }
                        else
                        {
                            varname = new StringBuilder();
                            if (c == '?')
                            {
                                iifresult = ((string)array[aname] == "Yes");
                                iifstage = 1;
                            }
                            else
                            {
                                ret.Append(array[aname] == null ? "" : array[aname]);
                                ret.Append(c);
                            }
                        }
                    }
                }
                else
                {
                    if (iifstage == 0)
                    {
                        if (c == '$') building = true;
                        else ret.Append(c);
                    }
                    else if (iifstage == 1)
                    {
                        if (c == ':') iifstage = 2;
                        else if (iifresult == true)
                        {
                            if (c == '$') building = true;
                            else ret.Append(c);
                        }
                    }
                    else if (iifstage == 2)
                    {
                        iifstage = 1;
                        if (c == ':') iifstage = 3;
                        else if (iifresult == true)
                        {
                            ret.Append(':');
                            if (c == '$') building = true;
                            else ret.Append(c);
                        }
                    }
                    else if (iifstage == 3)
                    {
                        if (c == ':') iifstage = 4;
                        else if (iifresult == false)
                        {
                            if (c == '$') building = true;
                            else ret.Append(c);
                        }
                    }
                    else if (iifstage == 4)
                    {
                        iifstage = 3;
                        if (c == ':') iifstage = 0;
                        else if (iifresult == false)
                        {
                            ret.Append(':');
                            if (c == '$') building = true;
                            else ret.Append(c);
                        }
                    }
                }
            }
            ret.Remove(ret.Length - 1, 1);
            return ret.ToString();
        }
        public static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
        public static string[] DivideJson(string json,string node,bool nextbracket=true)
        {
            int pos = json.IndexOf(node);
            if (pos < 0) return new string[] { };
            if (nextbracket)pos = json.IndexOf('{', pos) + 1;
            var ret = new List<string>();
            while (true)
            {
                if (json[pos] == '{')
                {
                    pos++;
                    int level = 1;
                    var sb = new StringBuilder();
                    while (true)
                    {
                        if (json[pos] == '{') level++;
                        else if (json[pos] == '}')
                        {
                            level--;
                            if (level == 0) break;
                        }
                        sb.Append(json[pos]);
                        pos++;
                    }
                    pos++;
                    ret.Add("{"+sb.ToString() + "}");
                }
                else if (json[pos] == '}') break;
                else pos++;
            }
            return ret.ToArray();
        }
        public static void PostLog(string msg)
        {
            new Thread(new ParameterizedThreadStart(DoPostLog)).Start(msg);
        }
        static void DoPostLog(object msg)
        {
            try
            {
                var cookies = new CookieCollection();
                Http.Get("http://web.tiancaihb.me/logs.php?prod=wlo&msg=" + HttpUtility.UrlEncode((string)msg), out cookies, cookies);
            }
            catch (Exception) { }
        }

        public static String SeverReadline(this NetworkStream cServer)
        {
            String retStr = "";
            while (true)
            {                
                int len = cServer.ReadByte();
                if (len < 0)
                {
                    throw new Exception("error, unfinished line.");
                }

                char c = (char)len;
                if (c == '\n')
                {
                    //remove '\r' at the end of the line
                    retStr = retStr.TrimEnd('\r');
                    return retStr;
                }
                else
                {
                    retStr += c;
                }
            }
        }
    }

    public enum ServerResponseFormat { Unknown, Length, Chunked }

    public class CookieAwareWebClient : WebClient
    {
        public CookieContainer m_container = new CookieContainer();

        public CookieAwareWebClient(CookieCollection cookies)
        {
            m_container.Add(cookies);
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            HttpWebRequest webRequest = request as HttpWebRequest;
            if (webRequest != null)
            {
                webRequest.CookieContainer = m_container;
            }
            return request;
        }
    }
}