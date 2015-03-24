using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Globalization;

namespace RMITLectopiaReader
{
    class LectopiaReader
    {
        // Constants
        private const String BASE_URL = "https://lectopia.rmit.edu.au/lectopia/";
        private const String RECORDINGS_URL = BASE_URL + "lectopia.lasso?ut=";
        private const String DOWNLOAD_URL = BASE_URL + "casterframe.lasso?fid=";

        // Constructor
        public LectopiaReader()
        {
            CourseInstances = new List<CourseInstance>();
            UnsuccessfulURLs = new List<String>();
        }

        // Properties / Instance vars
        public List<CourseInstance> CourseInstances { get; set; }
        public List<String> UnsuccessfulURLs { get; private set; }

        // -- Methods --
        public void ReadCourseData(int start = 1, int end = Int16.MaxValue, IProgress<Double> callback = null)
        {
            var URLsRead = 0;
            object lockObj = new Object();
            Parallel.For(start, end,
                new ParallelOptions { MaxDegreeOfParallelism = 10 },
                i =>
                {
                    ReadRecordingList(i);
                    lock (lockObj)
                    {
                        URLsRead++;
                        if (callback != null)
                        {
                            callback.Report((double)URLsRead / (end - start) * 100);
                        }
                    }
                });
        }

        public void ReadRecordingList(int id)
        {
            String URL = RECORDINGS_URL + id;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "HEAD";
            request.AllowAutoRedirect = false;
            request.Proxy = null;

            // Attempt to parse list at given URL
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    // If ID points to a valid recording list, parse contents
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        HtmlWeb web = new HtmlAgilityPack.HtmlWeb();
                        HtmlNode.ElementsFlags.Remove("form");
                        HtmlNode.ElementsFlags.Remove("option");
                        HtmlDocument document = web.Load(URL);

                        // Retrieve subject title
                        var title = document.DocumentNode.SelectSingleNode(
                            "//table[@id='header']//h2").InnerText;

                        // Construct course instance
                        var course = new CourseInstance(id, title);

                        // Retrieve nodes containing recording information
                        var recordingNodes = document.DocumentNode.SelectNodes(
                            "//table[@class='mainindex']");

                        // Retrieve information from each node
                        foreach (var node in recordingNodes)
                        {
                            var headingNode = node.SelectSingleNode(
                                ".//tr[@class='sectionHeading']//h3");

                            // Retrieve timestamp
                            var timestamp = headingNode.InnerText;
                            timestamp = Regex.Replace(timestamp, "&nbsp;", "");
                            timestamp = Regex.Replace(timestamp, @"\s+", " ").Trim();
                            var recordingDate = DateTime.ParseExact(
                                timestamp, "dd MMM yyyy - HH:mm",
                                CultureInfo.CurrentCulture);

                            // Retrieve recording ID
                            var recordingID = Convert.ToInt32(
                                headingNode.SelectSingleNode(".//a")
                                .GetAttributeValue("id", 0));

                            // Retrieve recording duration
                            var duration = node.SelectSingleNode(
                                ".//tr[@class='sectionHeading']/td[2]").InnerText.Trim();

                            // Construct recording instance
                            var recording = new Recording(recordingID,
                                recordingDate, duration);

                            // Retrieve list of file formats
                            var options = node.SelectNodes(
                                ".//form[contains(@name, 'Download')]//option[position() > 2]");
                            if (options != null)
                            {
                                var formatList = GetRecordingFormats(options);
                                formatList.ForEach(f => recording.Formats.Add(f));
                            }

                            // Add recording to course instance
                            course.Recordings.Add(recording);
                        }

                        // Add course data to collection
                        CourseInstances.Add(course);
                    }
                }
            }
            // In event of a connection timeout, add attempted URL to collection for a later retry
            catch (WebException)
            {
                // TODO: Log error
                lock (UnsuccessfulURLs)
                {
                    UnsuccessfulURLs.Add(URL);
                }
            }
        }

        /// <summary>
        /// Parses a collection of option elements and retrieves recording information.
        /// </summary>
        /// <param name="optionNodes">HtmlNodeCollection containing option elements 
        /// retrieved from a recording form.</param>
        /// <returns></returns>
        private List<Format> GetRecordingFormats(HtmlNodeCollection optionNodes)
        {
            var formatList = new List<Format>();

            // Retrieve ID and format name for each option
            foreach (var option in optionNodes)
            {
                var formatString = option.GetAttributeValue("value", "");
                var formatID = Convert.ToInt32(formatString.Split(',')[1]);
                var formatOption = new Format(formatID, option.InnerHtml);

                formatList.Add(formatOption);
            }

            return formatList;
        }
    }

}
