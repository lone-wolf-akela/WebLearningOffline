using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Net.Http;

namespace WebLearningOffline
{
    public partial class Form2 : Form
    {
        Form1 loginform;
        public CookieCollection cookies;

        public List<Course> courses;
        int nextjob = 0;
        int haserror = 0;
        int finished = 0;
        int totaltask = 0;
        bool canceled = false;
        object varlock = new object();
        List<Dictionary<string, object>> mainlist = null;

        List<DownloadTask> downlist = null;
        long totalsize = 0;
        long receivedsize = 0;
        long succ = 0;
        int nextdownjob = 0;

        public Form2(Form1 form1)
        {
            InitializeComponent();
            loginform = form1;
            cookies = form1.cookies;
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (button2.Text != "开始！")
            {
                e.NewValue = e.CurrentValue;
                Console.Beep();
                return;
            }
            CheckedListBox clb = (CheckedListBox)sender;
            // Switch off event handler
            clb.ItemCheck -= checkedListBox1_ItemCheck;
            clb.SetItemCheckState(e.Index, e.NewValue);

            // Now you can go further
            if (e.Index == 0)
            {
                for (int i = 1; i < clb.Items.Count; i++)
                    clb.SetItemCheckState(i, e.NewValue);
            }
            else
            {
                clb.SetItemChecked(0, true);
                for (int i = 1; i < clb.Items.Count; i++)
                    if (clb.GetItemChecked(i) == false)
                        clb.SetItemChecked(0, false);
            }
            // Switch on event handler
            clb.ItemCheck += checkedListBox1_ItemCheck;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                textBox1.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            radioButton2.Checked = true;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                textBox1.Text = folderBrowserDialog1.SelectedPath;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            this.ClientSize = new Size(groupBox2.Left + groupBox2.Width + groupBox1.Left, groupBox1.Top + groupBox1.Height + groupBox1.Top);
            textBox1.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (loginform.savepath != null)
            {
                radioButton1.Checked = loginform.desktop;
                radioButton2.Checked = !loginform.desktop;
                textBox1.Text = loginform.savepath;
            }
            new Thread(new ThreadStart(LoadCourses)).Start();
        }

        void LoadCourses()
        {
            var courseset = new HashSet<Course>();
            var typepageset = new HashSet<string>();
            try
            {
                var ret = Http.Get("http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/public_global.jsp",
                    out cookies, cookiesin: cookies);
                var matches = Regex.Matches(ret, @"MyCourse.jsp\?typepage=(\d+)");
                foreach (Match match in matches)
                {
                    typepageset.Add(match.Groups[1].Value);
                }
                foreach (var typepage in typepageset)
                {
                    ret = Http.Get("http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/MyCourse.jsp?typepage=" + typepage,
                        out cookies, cookiesin: cookies);
                    matches = Regex.Matches(ret, @"<tr class=.info_tr\d?.>(.+?)<\/tr>", RegexOptions.Singleline);
                    foreach (Match match in matches)
                    {
                        var str = match.Groups[1].Value;
                        string id;
                        bool isnew;
                        if (str.Contains("course_id"))
                        {
                            id = Regex.Match(str, @"course_id=(\d+)").Groups[1].Value;
                            isnew = false;
                        }
                        else
                        {
                            id = Regex.Match(str, @"coursehome\/([\w-]+)").Groups[1].Value;
                            isnew = true;
                        }
                        var name = Regex.Match(str, @"<a.+?>(.+?)<\/a>", RegexOptions.Singleline).Groups[1].Value;
                        name = name.Trim(new char[] { '\n', '\r', '\t', ' ' });
                        var term = name.Substring(name.LastIndexOf('(') + 1);
                        term = term.Substring(0, term.Length - 1);
                        name = name.Substring(0, name.LastIndexOf('('));
                        name = name.Substring(0, name.LastIndexOf('('));
                        var course = new Course() { isnew = isnew, id = id, name = name, term = term };
                        courseset.Add(course);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("获取课程列表失败！请重新登录。原因：\r\n" + e.Message);
                loginform.Show();
                loginform.BringToFront();
                this.Dispose();
                return;
            }
            courses = courseset.ToList();
            checkedListBox2.Items.Clear();
            checkedListBox2.Items.Add("全选");
            courses.ForEach(c => checkedListBox2.Items.Add(c.name + "(" + c.term + (c.isnew ? ")(新版)" : ")")));

            button2.Enabled = true;
            if (loginform.coursechecked == null || loginform.coursechecked.Length != checkedListBox2.Items.Count)
                for (int i = 0; i < checkedListBox2.Items.Count; i++)
                    checkedListBox2.SetItemChecked(i, true);
            else
                for (int i = 0; i < checkedListBox2.Items.Count; i++)
                    checkedListBox2.SetItemChecked(i, loginform.coursechecked[i]);
            checkedListBox2.Enabled = true;
            if (loginform.itemchecked == null || loginform.itemchecked.Length != checkedListBox1.Items.Count)
                for (int i = 0; i < checkedListBox1.Items.Count; i++)
                    checkedListBox1.SetItemChecked(i, true);
            else
                for (int i = 0; i < checkedListBox1.Items.Count; i++)
                    checkedListBox1.SetItemChecked(i, loginform.itemchecked[i]);
            groupBox1.Text += "(" + courses.Count + ")";
        }

        private void checkedListBox2_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (button2.Text != "开始！")
            {
                e.NewValue = e.CurrentValue;
                var freq = (int)(440 * Math.Pow(2.0, (checkedListBox2.Items.Count / 2 - e.Index) / 12.0));
                if (freq < 37) freq = 37;
                if (freq > 32767) freq = 32767;
                Console.Beep(freq, 400);
                return;
            }
            CheckedListBox clb = (CheckedListBox)sender;
            // Switch off event handler
            clb.ItemCheck -= checkedListBox2_ItemCheck;
            clb.SetItemCheckState(e.Index, e.NewValue);

            // Now you can go further
            if (e.Index == 0)
            {
                for (int i = 1; i < clb.Items.Count; i++)
                    clb.SetItemCheckState(i, e.NewValue);
            }
            else
            {
                clb.SetItemChecked(0, true);
                for (int i = 1; i < clb.Items.Count; i++)
                    if (clb.GetItemChecked(i) == false)
                        clb.SetItemChecked(0, false);
            }
            // Switch on event handler
            clb.ItemCheck += checkedListBox2_ItemCheck;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text == "取消")
            {
                if (MessageBox.Show("现在取消，下次会尽量从相同的位置继续，确认吗？", "取消下载", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    canceltask();
                return;
            }
            button2.Text = "取消";
            checkedListBox2.BeginUpdate();
            for (int i = 1; i < checkedListBox2.Items.Count; i++)
            {
                courses[i - 1].selected = checkedListBox2.GetItemChecked(i);
                if (courses[i - 1].selected) totaltask++;
            }
            checkedListBox2.EndUpdate();
            button1.Enabled = false;
            radioButton1.Enabled = radioButton2.Enabled = false;
            progressBar1.Maximum = totaltask;
            groupBox5.Visible = true;
            groupBox4.Visible = true;
            this.ClientSize = new Size(this.ClientSize.Width, groupBox5.Top + groupBox5.Height + groupBox1.Top);
            label1.Text = "完成" + finished + "/" + totaltask + " 成功" + (finished - haserror) + " 失败" + haserror;
            new Thread(new ThreadStart(run)).Start();
        }

        void run()
        {
            SystemSleepManagement.PreventSleep(false);
            Util.PostLog("window start run");
            var threads = new Thread[6];
            var main = new Dictionary<string, object>();
            var info = Http.Get("http://learn.tsinghua.edu.cn/MultiLanguage/vspace/vspace_userinfo1.jsp", out cookies, cookiesin: cookies);
            var match = Regex.Match(info, @"学生姓名<.*?tr_l2.>(.*?)<", RegexOptions.Singleline);
            var name = match.Success ? match.Groups[1].Value : "";
            match = Regex.Match(info, @"学生编号<.*?tr_l.>(.*?)<", RegexOptions.Singleline);
            var id = match.Success ? match.Groups[1].Value : "";
            main.Add("StudentName", name);
            main.Add("StudentID", id);
            mainlist = new List<Dictionary<string, object>>();
            lock (varlock)
            {
                for (int i = 0; i < threads.Length; i++)
                {
                    var tcookies = new CookieCollection();
                    tcookies.Add(cookies);
                    threads[i] = new Thread(new ParameterizedThreadStart(work));
                    threads[i].Start(tcookies);
                }
            }
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }
            main.Add("StudentCourses", mainlist);
            var bp = textBox1.Text;
            if (!bp.EndsWith(Path.DirectorySeparatorChar + "")) bp += Path.DirectorySeparatorChar;
            Util.WriteHTML("res" + Path.DirectorySeparatorChar + "网络学堂.html", bp + "网络学堂.html", main);


            var page1 = Http.Get("http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/MyCourse.jsp", out cookies, cookies);
            var link1 = Regex.Match(page1, @"iframe src=(.+?) ").Groups[1].Value;
            var mycookies = new CookieCollection();
            Http.Get(link1, out mycookies, mycookies, false);
            downlist = new List<DownloadTask>();
            courses.ForEach(course =>
            {
                if (course.selected)
                    downlist.AddRange(Util.LoadTaskList(bp + Util.GetSafePathName(course.term) + Path.DirectorySeparatorChar + Util.GetSafePathName(course.name) + Path.DirectorySeparatorChar + "downloadlist.dat"));
            });
            var tdl = new List<DownloadTask>();
            downlist.ForEach(t => {
                if (t.size > 0)
                {
                    var same = false;
                    foreach (var item in tdl)
                    {
                        if (item.local == t.local) same = true;
                    }
                    if (!same) tdl.Add(t);
                }
            });
            downlist = tdl;
            downlist.ForEach(t => totalsize += t.size);
            while (true)
            {
                if (nextdownjob >= downlist.Count)
                {
                    break;
                }
                var retry = 0;
                startdown:
                if (canceled) { nextdownjob = downlist.Count; goto fin; }
                var local = downlist[nextdownjob].local;
                if (!File.Exists(downlist[nextdownjob].local) || new FileInfo(local).Length != downlist[nextdownjob].size)
                {
                    // start download
                    long tsize = downlist[nextdownjob].size;
                    long nsize = 0;
                    bool okay = false;
                    var buf = new byte[100 * 1024];
                    try
                    {
                        using (var client = new TcpClient())
                        {
                            progressBar3.Value = 0;
                            label2.Text = "完成" + nextdownjob + "/" + downlist.Count + "个 " + Util.BytesToString(receivedsize + nsize) + "/" + Util.BytesToString(totalsize) + " 成功" + succ + " 失败" + (nextdownjob - succ);
                            label3.Text = "当前文件" + Util.BytesToString(nsize) + "/" + Util.BytesToString(tsize) + " " + downlist[nextdownjob].name;
                            var req = "GET " + Util.FindPathInURL(downlist[nextdownjob].url) + " HTTP/1.1\r\n";
                            req += "Host: " + Util.FindHostInURL(downlist[nextdownjob].url) + "\r\n";
                            req += "User-Agent: Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.82 Safari/537.36\r\n";
                            req += "Cookie: " + Util.CookieToHeaderString(downlist[nextdownjob].url.Contains("n.cic.t")?mycookies: cookies) + "\r\n\r\n";
                            var array = Encoding.UTF8.GetBytes(req);
                            client.Connect(Util.FindHostInURL(downlist[nextdownjob].url), 80);
                            using (var stream = client.GetStream())
                            {
                                stream.Write(array, 0, array.Length);
                                stream.Flush();
                                var rc = stream.Read(buf, 0, buf.Length);
                                var i = 0;
                                for (; i < rc; i++)
                                {
                                    if (buf[i] == '\r' && buf[i + 1] == '\n' && buf[i + 2] == '\r' && buf[i + 3] == '\n')
                                    {
                                        i = i + 4;
                                        break;
                                    }
                                }
                                using (var fs = new FileStream(local, FileMode.Create))
                                {
                                    nsize = rc - i;
                                    fs.Write(buf, i, (int)nsize);
                                    while (nsize < tsize)
                                    {
                                        if (canceled) { nextdownjob = downlist.Count; goto fin; }
                                        rc = stream.Read(buf, 0, buf.Length);
                                        if (rc <= 0) break;
                                        fs.Write(buf, 0, rc);
                                        nsize += rc;
                                        Util.SetProgressSafe(progressBar3, nsize, tsize);
                                        Util.SetProgressSafe(progressBar2, receivedsize + nsize, totalsize);
                                        label2.Text = "完成" + nextdownjob + "/" + downlist.Count + "个 " + Util.BytesToString(receivedsize + nsize) + "/" + Util.BytesToString(totalsize) + " 成功" + succ + " 失败" + (nextdownjob - succ);
                                        label3.Text = "当前文件" + Util.BytesToString(nsize) + "/" + Util.BytesToString(tsize) + " " + downlist[nextdownjob].name;
                                    }
                                }
                            }
                        }
                        okay = true;
                    }
                    catch (Exception e)
                    {
                        Console.Write(e);
                        try { File.Delete(local); } catch (Exception) { }
                        retry++;
                        if (retry < 5)
                        {
                            Console.WriteLine(" 重试" + retry);
                            goto startdown;
                        }
                        else Console.WriteLine();
                    }
                    if (okay) succ++;
                }
                else succ++;
                receivedsize += downlist[nextdownjob].size;
                nextdownjob++;
            fin:;
            }

            loginform.savepath = textBox1.Text;
            loginform.desktop = radioButton1.Checked;
            loginform.itemchecked = new bool[checkedListBox1.Items.Count];
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
                loginform.itemchecked[i] = checkedListBox1.GetItemChecked(i);
            loginform.coursechecked = new bool[checkedListBox2.Items.Count];
            for (int i = 0; i < checkedListBox2.Items.Count; i++)
                loginform.coursechecked[i] = checkedListBox2.GetItemChecked(i);

            SystemSleepManagement.ResotreSleep();
            Util.PostLog("window fin " + haserror + " " + courses.Count + " " + succ + " " + downlist.Count);
            if (haserror > 0 || succ < downlist.Count) MessageBox.Show("有部分内容下载失败，可以再次运行，下载余下的内容");
            else MessageBox.Show("下载全部成功！");
            try
            {
                if (haserror == 0 && succ == downlist.Count)
                {
                    courses.ForEach(course =>
                    {
                        var home = bp + Util.GetSafePathName(course.term);
                        home += Path.DirectorySeparatorChar + Util.GetSafePathName(course.name);
                        home += Path.DirectorySeparatorChar;
                        try
                        {
                            File.Delete(home + "downloadlist.dat");
                        }
                        catch (Exception) { }
                    });
                    System.Diagnostics.Process.Start(bp + "网络学堂.html");
                }
            }
            catch (Exception) { }


            nextjob = 0;
            haserror = 0;
            finished = 0;
            totaltask = 0;
            canceled = false;
            mainlist = null;

            downlist = null;
            totalsize = 0;
            receivedsize = 0;
            succ = 0;
            nextdownjob = 0;
            checkedListBox2.Items.Clear();
            checkedListBox2.Items.Add("正在加载课程列表……");
            this.label1.Text = "完成0/0 成功0 失败0";
            this.label3.Text = "当前文件0MB/0MB";
            this.label2.Text = "完成0/0个 0MB/0MB 成功0 失败0";
            progressBar1.Value = progressBar2.Value = progressBar3.Value = 0;
            groupBox4.Visible = groupBox5.Visible = false;
            radioButton1.Enabled = radioButton2.Enabled = button1.Enabled = true;
            groupBox2.Text = "选择要下载的课程";

            button2.Enabled = false;
            button2.Text = "开始！";
            Form2_Load(null, null);
            return;
        }



        void work(object incookies)
        {
            lock (varlock) { }
            int i;
            var bp = textBox1.Text;
            var mycookies = (CookieCollection)incookies;
            if (!bp.EndsWith(Path.DirectorySeparatorChar + "")) bp += Path.DirectorySeparatorChar;
            while (true)
            {
                lock (varlock)
                {
                    i = nextjob;
                    if (i == courses.Count) return;
                    nextjob++;
                }
                if (!courses[i].selected) continue;
                Dictionary<string, object> titem = null;
                try
                {
                    checkcancelled();
                    var course = courses[i];
                    var home = bp + Util.GetSafePathName(course.term);
                    Directory.CreateDirectory(home);
                    home += Path.DirectorySeparatorChar + Util.GetSafePathName(course.name);
                    Directory.CreateDirectory(home);
                    home += Path.DirectorySeparatorChar;
                    var downfiles = new HashSet<DownloadTask>(Util.LoadTaskList(home + "downloadlist.dat"));

                    if (!course.isnew)
                    {
                        var course_locate = Http.Get("http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/course_locate.jsp?course_id=" + course.id, out mycookies, cookiesin: mycookies);
                        if (course_locate.Contains("getnoteid_student") && checkedListBox1.GetItemChecked(1) && !File.Exists(home + "课程公告.html"))
                        {
                            var array = Util.InitDictionary(course);
                            var note = Http.Get("http://learn.tsinghua.edu.cn/MultiLanguage/public/bbs/getnoteid_student.jsp?course_id=" + course.id, out mycookies, cookiesin: mycookies);
                            var trs = Regex.Matches(note, @"<tr class=.tr\d?.+?>(.+?)<\/tr>", RegexOptions.Singleline);
                            var list = new List<Dictionary<string, object>>();
                            for (int j = 0; j < trs.Count; j++)
                            {
                                checkcancelled();
                                var tr = trs[j];
                                var tnote = new Dictionary<string, object>();
                                var tds = Regex.Matches(tr.Groups[1].Value, @"<td.*?>(.*?)<\/td>", RegexOptions.Singleline);
                                tnote.Add("NoteNumber", tds[0].Groups[1].Value);
                                tnote.Add("NoteCaption", Regex.Match(tds[1].Groups[1].Value, @"<a.*?>(.*?)<\/a>", RegexOptions.Singleline).Groups[1].Value);
                                tnote.Add("NoteAuthor", tds[2].Groups[1].Value);
                                tnote.Add("NoteDate", tds[3].Groups[1].Value);
                                var noteid = Regex.Match(tr.Groups[1].Value, @"href='(.+?)'").Groups[1].Value;
                                var notecontent = Http.Get("http://learn.tsinghua.edu.cn/MultiLanguage/public/bbs/" + noteid, out mycookies, cookiesin: mycookies);
                                var doc = new HtmlAgilityPack.HtmlDocument();
                                doc.LoadHtml(notecontent);
                                var bodynode = doc.DocumentNode.SelectNodes("//table")[1].SelectSingleNode("tr[2]/td[2]");
                                tnote.Add("NoteBody", bodynode.InnerHtml);
                                list.Add(tnote);
                            }
                            array.Add("Notes", list);
                            Util.WriteHTML("res" + Path.DirectorySeparatorChar + "课程公告.html", home + "课程公告.html", array);
                        }
                        checkcancelled();
                        if (course_locate.Contains("course_info") && checkedListBox1.GetItemChecked(2) && !File.Exists(home + "课程信息.html"))
                        {
                            var array = Util.InitDictionary(course);
                            var info = Http.Get("http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/course_info.jsp?course_id=" + course.id, out mycookies, cookiesin: mycookies);
                            info = Regex.Match(info, @"(<table id.+?\/table>)", RegexOptions.Singleline).Groups[1].Value;
                            info = Regex.Replace(info, @"<img.+?>", "");
                            array.Add("InfoBody", info);
                            Util.WriteHTML("res" + Path.DirectorySeparatorChar + "课程信息.html", home + "课程信息.html", array);
                        }
                        checkcancelled();
                        if (course_locate.Contains("download") && checkedListBox1.GetItemChecked(3) && !File.Exists(home + "课程文件.html"))
                        {
                            var array = Util.InitDictionary(course);
                            var page = Http.Get("http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/download.jsp?course_id=" + course.id, out mycookies, cookiesin: mycookies);
                            var classes = Regex.Matches(page, @"<td class=.textTD.+?>(.*?)<\/td>", RegexOptions.Singleline);
                            var layers = Regex.Matches(page, @"<div class=.layerbox.+?>(.*?)<\/div>", RegexOptions.Singleline);
                            var list = new List<Dictionary<string, object>>();
                            Directory.CreateDirectory(home + "课程文件");
                            for (int j = 0; j < layers.Count; j++)
                            {
                                var classname = classes[j].Groups[1].Value;
                                var trs = Regex.Matches(layers[j].Groups[1].Value, @"<tr class=.tr\d?.>(.*?)<\/tr>", RegexOptions.Singleline);
                                foreach (Match tr in trs)
                                {
                                    checkcancelled();
                                    var tfile = new Dictionary<string, object>();
                                    var tds = Regex.Matches(tr.Groups[1].Value, @"<td.*?>(.*?)<\/td>", RegexOptions.Singleline);
                                    tfile.Add("FileClass", classname);
                                    tfile.Add("FileNumber", tds[0].Groups[1].Value);
                                    var filetitle = Regex.Match(tds[2].Groups[1].Value, @"<a.+?>(.*?)<\/a>", RegexOptions.Singleline).Groups[1].Value;
                                    tfile.Add("FileTitle", filetitle);
                                    var filename = Regex.Match(tds[1].Groups[1].Value, @"getfilelink=(.+?)&").Groups[1].Value;
                                    tfile.Add("FileName", filename);
                                    tfile.Add("FileComment", tds[3].Groups[1].Value);
                                    tfile.Add("FileSize", tds[4].Groups[1].Value);
                                    tfile.Add("FileDate", tds[5].Groups[1].Value);
                                    var url = "http://learn.tsinghua.edu.cn" + Regex.Match(tds[2].Groups[1].Value, "href=\"(.*?)\"").Groups[1].Value;
                                    tfile.Add("FileUrl", url);
                                    var local = "课程文件" + Path.DirectorySeparatorChar + Util.GetSafePathName(filename);
                                    tfile.Add("FileLocal", local.Replace('\\','/'));
                                    //Util.downfile(url, home+local, mycookies);
                                    downfiles.Add(new DownloadTask() { url = url, local = home + local, size = Util.GetRemoteFileSize(url, mycookies), name = filename });
                                    list.Add(tfile);
                                }
                            }
                            array.Add("Files", list);
                            Util.WriteHTML("res" + Path.DirectorySeparatorChar + "课程文件.html", home + "课程文件.html", array);
                        }
                        checkcancelled();
                        if (course_locate.Contains("hom_wk_brw") && checkedListBox1.GetItemChecked(4) && !File.Exists(home + "课程作业.html"))
                        {
                            var array = Util.InitDictionary(course);
                            var page = Http.Get("http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/hom_wk_brw.jsp?course_id=" + course.id, out mycookies, cookiesin: mycookies);
                            var items = Regex.Matches(page, @"<tr class=.tr\d?.>(.+?)<\/tr>", RegexOptions.Singleline);
                            var list = new List<Dictionary<string, object>>();
                            Directory.CreateDirectory(home + "课程作业");
                            for (int j = 0; j < items.Count; j++)
                            {
                                checkcancelled();
                                var thwk = new Dictionary<string, object>();
                                var tds = Regex.Matches(items[j].Groups[1].Value, @"<td.*?>(.*?)<\/td>", RegexOptions.Singleline);
                                var name = Regex.Match(tds[0].Groups[1].Value, @"<a.*?>(.*?)<\/a>", RegexOptions.Singleline).Groups[1].Value;
                                thwk.Add("HomeworkName", name);
                                Directory.CreateDirectory(home + "课程作业" + Path.DirectorySeparatorChar + Util.GetSafePathName(name));
                                thwk.Add("HomeworkStart", tds[1].Groups[1].Value);
                                thwk.Add("HomeworkEnd", tds[2].Groups[1].Value);
                                thwk.Add("HomeworkSubmitted", tds[3].Groups[1].Value.Contains("已经") ? "Yes" : "No");
                                var scored = Regex.Match(tds[5].Groups[1].Value, "查看批阅\" (.)").Groups[1].Value != "d";
                                thwk.Add("HomeworkScored", scored ? "Yes" : "No");
                                var detailurl = "http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/" + Regex.Match(tds[0].Groups[1].Value, "href=\"(.*?)\"").Groups[1].Value;
                                var detailpage = Http.Get(detailurl, out mycookies, cookiesin: mycookies);
                                var content = Regex.Matches(detailpage, @"<textarea.*?>(.*?)<\/textarea>", RegexOptions.Singleline)[0].Groups[1].Value;
                                thwk.Add("HomeworkHandout", content);
                                thwk.Add("HomeworkHasHandout", content.Trim() != "" ? "Yes" : "No");
                                var handin = Regex.Matches(detailpage, @"<textarea.*?>(.*?)<\/textarea>", RegexOptions.Singleline)[1].Groups[1].Value;
                                thwk.Add("HomeworkHandin", handin);
                                thwk.Add("HomeworkHasHandin", handin.Trim() != "" ? "Yes" : "No");
                                var trs = Regex.Matches(detailpage, @"<tr>(.*?)<\/tr>", RegexOptions.Singleline);
                                var attn = "";
                                var attnname = "";
                                var upattn = "";
                                var upattnname = "";
                                foreach (Match tr in trs)
                                {
                                    var inner = tr.Groups[1].Value;
                                    if (inner.Contains("> 作业附件<"))
                                    {
                                        var tmatch = Regex.Match(inner, "href=\"(.*?)\"");
                                        if (tmatch.Success) attn = "http://learn.tsinghua.edu.cn" + tmatch.Groups[1].Value;
                                        tmatch = Regex.Match(inner, @"<a.*?>(.*?)<\/a>", RegexOptions.Singleline);
                                        if (tmatch.Success) attnname = tmatch.Groups[1].Value;
                                    }
                                    if (inner.Contains(">上交作业附件<"))
                                    {
                                        var tmatch = Regex.Match(inner, "href=\"(.*?)\"");
                                        if (tmatch.Success) upattn = "http://learn.tsinghua.edu.cn" + tmatch.Groups[1].Value;
                                        tmatch = Regex.Match(inner, @"<a.*?>(.*?)<\/a>", RegexOptions.Singleline);
                                        if (tmatch.Success) upattnname = tmatch.Groups[1].Value;
                                    }
                                }
                                thwk.Add("HomeworkHasAttnOut", attn != "" ? "Yes" : "No");
                                thwk.Add("HomeworkAttnOut", attn);
                                thwk.Add("HomeworkAttnOutName", attnname);
                                if (attn != "")
                                {
                                    var aolocal = home + "课程作业" + Path.DirectorySeparatorChar + Util.GetSafePathName(name) + Path.DirectorySeparatorChar + Util.GetSafePathName(attnname);
                                    thwk.Add("HomeworkAttnOutLocal", aolocal);
                                    //Util.downfile(attn, aolocal, mycookies);
                                    downfiles.Add(new DownloadTask() { url = attn, local = aolocal, size = Util.GetRemoteFileSize(attn, mycookies), name = attnname });
                                }
                                else thwk.Add("HomeworkAttnOutLocal", "");
                                thwk.Add("HomeworkHasAttnIn", upattn != "" ? "Yes" : "No");
                                thwk.Add("HomeworkAttnIn", upattn);
                                thwk.Add("HomeworkAttnInName", upattnname);
                                if (upattn != "")
                                {
                                    var ailocal = home + "课程作业" + Path.DirectorySeparatorChar + Util.GetSafePathName(name) + Path.DirectorySeparatorChar + Util.GetSafePathName(upattnname);
                                    thwk.Add("HomeworkAttnInLocal", ailocal);
                                    //Util.downfile(upattn, ailocal, mycookies);
                                    downfiles.Add(new DownloadTask() { url = upattn, local = ailocal, size = Util.GetRemoteFileSize(upattn, mycookies), name = upattnname });
                                }
                                else thwk.Add("HomeworkAttnInLocal", "");
                                if (scored)
                                {
                                    var url = "http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/hom_wk_view.jsp?" + Regex.Match(items[j].Groups[1].Value, @"hom_wk_view.jsp\?(.+?)'").Groups[1].Value;
                                    var scorepage = Http.Get(url, out mycookies, cookiesin: mycookies);
                                    trs = Regex.Matches(scorepage, @"<tr.*?>(.*?)<\/tr>", RegexOptions.Singleline);
                                    string teacher = "", date = "", score = "", comment = "", file = "", filename = "";
                                    foreach (Match tr in trs)
                                    {
                                        var inner = tr.Groups[1].Value;
                                        if (inner.Contains("批阅老师"))
                                        {
                                            var tmatch = Regex.Matches(inner, @"<td.*?>(.*?)<\/td>", RegexOptions.Singleline);
                                            teacher = tmatch[1].Groups[1].Value;
                                            date = tmatch[3].Groups[1].Value;
                                        }
                                        else if (inner.Contains("得分"))
                                        {
                                            var tmatch = Regex.Matches(inner, @"<td.*?>(.*?)<\/td>", RegexOptions.Singleline);
                                            score = tmatch[1].Groups[1].Value;
                                        }
                                        else if (inner.Contains(">评语<"))
                                        {
                                            var tmatch = Regex.Match(inner, @"<textarea.*?>(.*?)<\/textarea>", RegexOptions.Singleline);
                                            comment = tmatch.Groups[1].Value;
                                        }
                                        else if (inner.Contains("评语附件"))
                                        {
                                            var tmatch = Regex.Match(inner, "href=\"(.*?)\"");
                                            if (tmatch.Success) file = "http://learn.tsinghua.edu.cn" + tmatch.Groups[1].Value;
                                            tmatch = Regex.Match(inner, @"<a.*?>(.*?)<\/a>", RegexOptions.Singleline);
                                            if (tmatch.Success) filename = tmatch.Groups[1].Value;
                                        }
                                    }
                                    thwk.Add("HomeworkScorer", teacher);
                                    thwk.Add("HomeworkScoreDate", date);
                                    thwk.Add("HomeworkScore", score);
                                    thwk.Add("HomeworkScoreComment", comment);
                                    thwk.Add("HomeworkScoreHasAttn", file != "" ? "Yes" : "No");
                                    thwk.Add("HomeworkScoreAttn", file);
                                    thwk.Add("HomeworkScoreAttnName", filename);
                                    if (file != "")
                                    {
                                        var aslocal = home + "课程作业" + Path.DirectorySeparatorChar + Util.GetSafePathName(name) + Path.DirectorySeparatorChar + Util.GetSafePathName(filename);
                                        thwk.Add("HomeworkScoreAttnLocal", aslocal);
                                        //Util.downfile(file, aslocal, mycookies);
                                        downfiles.Add(new DownloadTask() { url = file, local = aslocal, size = Util.GetRemoteFileSize(file, mycookies), name = filename });
                                    }
                                    else thwk.Add("HomeworkScoreAttnLocal", "");
                                }
                                list.Add(thwk);
                            }
                            array.Add("Homeworks", list);
                            Util.WriteHTML("res" + Path.DirectorySeparatorChar + "课程作业.html", home + "课程作业.html", array);
                        }
                    }
                    else
                    {
                        var page1 = Http.Get("http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/MyCourse.jsp", out mycookies, mycookies);
                        var link1 = Regex.Match(page1, @"iframe src=(.+?) ").Groups[1].Value;
                        //why here create new coockie container?
                        //mycookies = new CookieCollection();
                        Http.Get(link1, out mycookies, mycookies, false);
                        var course_config = Http.Get("http://learn.cic.tsinghua.edu.cn/b/myCourse/course_config/list/" + course.id + "/S", out mycookies, mycookies);
                        if (course_config.Contains("coursenotice") && checkedListBox1.GetItemChecked(1) && !File.Exists(home + "课程公告.html"))
                        {
                            var noticepage = Http.Get("http://learn.cic.tsinghua.edu.cn/b/myCourse/notice/listForStudent/" + course.id + "?currentPage=1&pageSize=5000", out mycookies, mycookies);
                            var ser = new DataContractJsonSerializer(typeof(NoticeObject));
                            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(noticepage)))
                            {
                                var obj = (NoticeObject)ser.ReadObject(ms);
                                var recs = obj.paginationList.recordList;
                                var array = Util.InitDictionary(course);
                                var list = new List<Dictionary<string, object>>();
                                for (int j = 0; j < recs.Length; j++)
                                {
                                    var notice = recs[j].courseNotice;
                                    var tnote = new Dictionary<string, object>();
                                    tnote.Add("NoteNumber", recs.Length - j + "");
                                    tnote.Add("NoteCaption", notice.title);
                                    tnote.Add("NoteAuthor", notice.owner);
                                    tnote.Add("NoteDate", notice.regDate);
                                    tnote.Add("NoteBody", notice.detail);
                                    list.Add(tnote);
                                }
                                array.Add("Notes", list);
                                Util.WriteHTML("res" + Path.DirectorySeparatorChar + "课程公告.html", home + "课程公告.html", array);
                            }
                        }
                        checkcancelled();
                        if (course_config.Contains("courseinfo") && checkedListBox1.GetItemChecked(2) && !File.Exists(home + "课程信息.html"))
                        {
                            var infopage= Http.Get("http://learn.cic.tsinghua.edu.cn/b/mycourse/courseExtension/getCourseExtensionByCourseId/" + course.id, out mycookies, mycookies);
                            var ser = new DataContractJsonSerializer(typeof(InfoObject));
                            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(infopage)))
                            {
                                var obj = (InfoObject)ser.ReadObject(ms);
                                var array = Util.InitDictionary(course);
                                array.Add("CourseNumber", obj.allInfo.course_no);
                                array.Add("CourseSeq", obj.allInfo.course_seq);
                                array.Add("CourseCredit", obj.allInfo.credit);
                                array.Add("CourseTime", obj.allInfo.course_time);
                                array.Add("TeacherName", obj.allInfo.teacherInfo.name);
                                array.Add("TeacherEmail", obj.allInfo.teacherInfo.email);
                                array.Add("TeacherPhone", obj.allInfo.teacherInfo.phone);
                                array.Add("TeacherNote", obj.allInfo.teacherInfo.note);
                                array.Add("ReferenceBook", obj.allInfo.ref_book_c);
                                array.Add("CourseGuide", obj.allInfo.guide);
                                array.Add("ExamType", obj.allInfo.exam_type);
                                array.Add("CoursePrereq", obj.allInfo.requirement);
                                array.Add("CourseDetail", obj.allInfo.detail_c);
                                array.Add("CourseSchedule", obj.schedule);
                                Util.WriteHTML("res" + Path.DirectorySeparatorChar + "课程信息(新版).html", home + "课程信息.html", array);
                            }
                        }
                        checkcancelled();
                        if (course_config.Contains("courseware") && checkedListBox1.GetItemChecked(3) && !File.Exists(home + "课程文件.html"))
                        {
                            var filepage= Http.Get("http://learn.cic.tsinghua.edu.cn/b/myCourse/tree/getCoursewareTreeData/" + course.id+"/0", out mycookies, mycookies);
                            var cats = Util.DivideJson(filepage, "childMapData");
                            var ser = new DataContractJsonSerializer(typeof(FileType));
                            var objs = new List<FileType>();
                            foreach (var cat in cats)
                            {
                                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(cat)))
                                {
                                    var obj = (FileType)ser.ReadObject(ms);
                                    Array.Sort(obj.courseCoursewareList);
                                    objs.Add(obj);
                                }
                            }
                            objs.Sort();
                            var array = Util.InitDictionary(course);
                            var list = new List<Dictionary<string, object>>();
                            Directory.CreateDirectory(home + "课程文件");
                            foreach (var obj in objs)
                            {
                                foreach (var file in obj.courseCoursewareList)
                                {
                                    checkcancelled();
                                    var tfile = new Dictionary<string, object>();
                                    tfile.Add("FileClass", obj.courseOutlines.title);
                                    tfile.Add("FileNumber", file.position.ToString());
                                    tfile.Add("FileTitle", file.title);
                                    tfile.Add("FileName", file.resourcesMappingByFileId.fileName);
                                    tfile.Add("FileComment", file.detail);
                                    long fsize = 0;
                                    string sizetext = "";
                                    if (long.TryParse(file.resourcesMappingByFileId.fileSize, out fsize)) sizetext = Util.BytesToString(fsize);
                                    else sizetext = file.resourcesMappingByFileId.fileSize;
                                    tfile.Add("FileSize", sizetext);
                                    tfile.Add("FileDate", Util.TimestampToDate(file.resourcesMappingByFileId.regDate/1000));
                                    var url = "http://learn.cic.tsinghua.edu.cn/b/resource/downloadFile/" + file.resourcesMappingByFileId.fileId;
                                    var json = Http.Get(url, out mycookies, mycookies);
                                    url = "http://learn.cic.tsinghua.edu.cn" + Regex.Match(json, "result\":\"(.*?)\"").Groups[1].Value;
                                    tfile.Add("FileUrl", url);
                                    var local = "课程文件" + Path.DirectorySeparatorChar + Util.GetSafePathName(file.resourcesMappingByFileId.fileName);
                                    tfile.Add("FileLocal", local.Replace('\\', '/'));
                                    downfiles.Add(new DownloadTask() { url = url, local = home + local, size = fsize, name = file.resourcesMappingByFileId.fileName });
                                    list.Add(tfile);
                                }
                            }
                            array.Add("Files", list);
                            Util.WriteHTML("res" + Path.DirectorySeparatorChar + "课程文件.html", home + "课程文件.html", array);
                        }
                        checkcancelled();
                        if (course_config.Contains("homework") && checkedListBox1.GetItemChecked(4) && !File.Exists(home + "课程作业.html"))
                        {
                            var hwpage= Http.Get("http://learn.cic.tsinghua.edu.cn/b/myCourse/homework/list4Student/" + course.id+"/0", out mycookies, mycookies);
                            var hwstrs = Util.DivideJson(hwpage, "resultList",false);
                            var ser = new DataContractJsonSerializer(typeof(HomeworkObject));
                            var hws = new List<HomeworkObject>();
                            foreach (var hwstr in hwstrs)
                            {
                                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(hwstr)))
                                {
                                    var obj = (HomeworkObject)ser.ReadObject(ms);
                                    hws.Add(obj);
                                }
                            }
                            var array = Util.InitDictionary(course);
                            var list = new List<Dictionary<string, object>>();
                            Directory.CreateDirectory(home + "课程作业");
                            foreach (var hw in hws)
                            {
                                checkcancelled();
                                var thwk = new Dictionary<string, object>();
                                thwk.Add("HomeworkName", hw.courseHomeworkInfo.title);
                                Directory.CreateDirectory(home + "课程作业" + Path.DirectorySeparatorChar + Util.GetSafePathName(hw.courseHomeworkInfo.title));
                                thwk.Add("HomeworkStart", Util.TimestampToDate(hw.courseHomeworkInfo.beginDate / 1000));
                                thwk.Add("HomeworkEnd", Util.TimestampToDate(hw.courseHomeworkInfo.endDate / 1000));
                                thwk.Add("HomeworkSubmitted", int.Parse(hw.courseHomeworkRecord.status) != 0 ? "Yes" : "No");
                                thwk.Add("HomeworkScored", int.Parse(hw.courseHomeworkRecord.status) >= 2 ? "Yes" : "No");
                                thwk.Add("HomeworkHandout", hw.courseHomeworkInfo.detail);
                                thwk.Add("HomeworkHasHandout", hw.courseHomeworkInfo.detail != null ? "Yes" : "No");
                                thwk.Add("HomeworkHandin", hw.courseHomeworkRecord.homewkDetail);
                                thwk.Add("HomeworkHasHandin", hw.courseHomeworkRecord.homewkDetail!=null ? "Yes" : "No");

                                thwk.Add("HomeworkHasAttnOut", hw.courseHomeworkInfo.homewkAffix != null ? "Yes" : "No");
                                thwk.Add("HomeworkAttnOut", hw.courseHomeworkInfo.homewkAffix);
                                thwk.Add("HomeworkAttnOutName", hw.courseHomeworkInfo.homewkAffixFilename);
                                if (hw.courseHomeworkInfo.homewkAffix != null)
                                {
                                    var aolocal = home + "课程作业" + Path.DirectorySeparatorChar + Util.GetSafePathName(hw.courseHomeworkInfo.title) + Path.DirectorySeparatorChar + Util.GetSafePathName(hw.courseHomeworkInfo.homewkAffixFilename);
                                    thwk.Add("HomeworkAttnOutLocal", aolocal);
                                    var url = "http://learn.cic.tsinghua.edu.cn/b/resource/downloadFile/" + hw.courseHomeworkInfo.homewkAffix;
                                    var json = Http.Get(url, out mycookies, mycookies);
                                    url = "http://learn.cic.tsinghua.edu.cn" + Regex.Match(json, "result\":\"(.*?)\"").Groups[1].Value;
                                    downfiles.Add(new DownloadTask() { url = url, local = aolocal, size = Util.GetRemoteFileSize(url, mycookies), name = hw.courseHomeworkInfo.homewkAffixFilename });
                                }
                                else thwk.Add("HomeworkAttnOutLocal", "");
                                if (hw.courseHomeworkRecord.resourcesMappingByHomewkAffix != null)
                                {
                                    thwk.Add("HomeworkHasAttnIn", "Yes");
                                    thwk.Add("HomeworkAttnIn", hw.courseHomeworkRecord.resourcesMappingByHomewkAffix.fileId);
                                    thwk.Add("HomeworkAttnInName", hw.courseHomeworkRecord.resourcesMappingByHomewkAffix.fileName);
                                    var ailocal = home + "课程作业" + Path.DirectorySeparatorChar + Util.GetSafePathName(hw.courseHomeworkInfo.title) + Path.DirectorySeparatorChar + Util.GetSafePathName(hw.courseHomeworkRecord.resourcesMappingByHomewkAffix.fileName);
                                    thwk.Add("HomeworkAttnInLocal", ailocal);
                                    var url = "http://learn.cic.tsinghua.edu.cn/b/resource/downloadFile/" + hw.courseHomeworkRecord.resourcesMappingByHomewkAffix.fileId;
                                    var json = Http.Get(url, out mycookies, mycookies);
                                    url = "http://learn.cic.tsinghua.edu.cn" + Regex.Match(json, "result\":\"(.*?)\"").Groups[1].Value;
                                    downfiles.Add(new DownloadTask() { url = url, local = ailocal, size = long.Parse(hw.courseHomeworkRecord.resourcesMappingByHomewkAffix.fileSize), name = hw.courseHomeworkRecord.resourcesMappingByHomewkAffix.fileName });
                                }
                                else
                                {
                                    thwk.Add("HomeworkHasAttnIn", "No");
                                    thwk.Add("HomeworkAttnIn", "");
                                    thwk.Add("HomeworkAttnInName", "");
                                    thwk.Add("HomeworkAttnInLocal", "");
                                }
                                if (int.Parse(hw.courseHomeworkRecord.status) >= 2)
                                {
                                    thwk.Add("HomeworkScorer", hw.courseHomeworkRecord.gradeUser);
                                    thwk.Add("HomeworkScoreDate", Util.TimestampToDate(long.Parse(hw.courseHomeworkRecord.replyDate) / 1000));
                                    thwk.Add("HomeworkScore", hw.courseHomeworkRecord.mark);
                                    thwk.Add("HomeworkScoreComment", hw.courseHomeworkRecord.replyDetail);
                                    if (hw.courseHomeworkRecord.resourcesMappingByReplyAffix != null)
                                    {
                                        thwk.Add("HomeworkScoreHasAttn", "Yes");
                                        thwk.Add("HomeworkScoreAttn", hw.courseHomeworkRecord.resourcesMappingByReplyAffix.fileId);
                                        thwk.Add("HomeworkScoreAttnName", hw.courseHomeworkRecord.resourcesMappingByReplyAffix.fileName);
                                        var aslocal = home + "课程作业" + Path.DirectorySeparatorChar + Util.GetSafePathName(hw.courseHomeworkInfo.title) + Path.DirectorySeparatorChar + Util.GetSafePathName(hw.courseHomeworkRecord.resourcesMappingByReplyAffix.fileName);
                                        thwk.Add("HomeworkScoreAttnLocal", aslocal);
                                        var url = "http://learn.cic.tsinghua.edu.cn/b/resource/downloadFile/" + hw.courseHomeworkRecord.resourcesMappingByReplyAffix.fileId;
                                        var json = Http.Get(url, out mycookies, mycookies);
                                        url = "http://learn.cic.tsinghua.edu.cn" + Regex.Match(json, "result\":\"(.*?)\"").Groups[1].Value;
                                        downfiles.Add(new DownloadTask() { url = url, local = aslocal, size = long.Parse(hw.courseHomeworkRecord.resourcesMappingByReplyAffix.fileSize), name = hw.courseHomeworkRecord.resourcesMappingByReplyAffix.fileName });
                                    }
                                    else
                                    {
                                        thwk.Add("HomeworkScoreHasAttn", "No");
                                        thwk.Add("HomeworkScoreAttn", "");
                                        thwk.Add("HomeworkScoreAttnName", "");
                                        thwk.Add("HomeworkScoreAttnLocal", "");
                                    }
                                }
                                list.Add(thwk);
                            }
                            array.Add("Homeworks", list);
                            Util.WriteHTML("res" + Path.DirectorySeparatorChar + "课程作业.html", home + "课程作业.html", array);
                        }
                    }

                    checkcancelled();
                    Util.SaveTaskList(downfiles.ToList(), home + "downloadlist.dat");
                    listitem(i, "成功");
                    titem = Util.InitDictionary(course);
                    titem.Add("HasNotes", File.Exists(home + "课程公告.html") ? "Yes" : "No");
                    titem.Add("HasInfo", File.Exists(home + "课程信息.html") ? "Yes" : "No");
                    titem.Add("HasDownload", File.Exists(home + "课程文件.html") ? "Yes" : "No");
                    titem.Add("HasHomework", File.Exists(home + "课程作业.html") ? "Yes" : "No");
                    titem.Add("BasePath", Util.GetSafePathName(course.term) + '/' + Util.GetSafePathName(course.name));
                }
                catch (Exception ex)
                {
                    listitem(i, "失败：" + ex.Message);
                    lock (varlock)
                    {
                        haserror++;
                    }
                    continue;
                }
                lock (varlock)
                {
                    if (titem != null) mainlist.Add(titem);
                    finished++;
                    progressBar1.Value = finished;
                    label1.Text = "完成" + finished + "/" + totaltask + " 成功" + (finished - haserror) + " 失败" + haserror;
                }
            }
        }

        void checkcancelled()
        {
            lock (varlock)
            {
                if (canceled) throw new Exception("用户取消了操作");
            }
        }

        void listitem(int i, string s)
        {
            var c = courses[i];
            lock (varlock)
            {
                checkedListBox2.Items[i + 1] = c.name + "(" + c.term + (c.isnew ? ")(新版)" : ")") + "  " + s;
            }
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }

        public void canceltask()
        {
            lock (varlock)
            {
                canceled = true;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://student.tsinghua.edu.cn/");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://student.tsinghua.edu.cn/");
            linkLabel1.LinkVisited = true;
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }
    }
}
