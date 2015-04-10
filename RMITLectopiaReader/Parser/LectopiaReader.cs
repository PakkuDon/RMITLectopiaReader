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
using System.IO;

namespace RMITLectopiaReader
{
    class LectopiaReader
    {
        // Constants
        private const String BASE_URL = "https://lectopia.rmit.edu.au/lectopia/";
        private const String RECORDINGS_URL = BASE_URL + "lectopia.lasso?ut=";
        private const String DOWNLOAD_URL = BASE_URL + "casterframe.lasso?fid=";
        private const int MAX_CONNECTIONS = 10;

        // Constructor
        public LectopiaReader()
        {
            FailedReads = new List<int>();
        }

        // Properties / Instance vars
        public List<int> FailedReads { get; set; }

        // -- Methods --
        /// <summary>
        /// Scrapes Lectopia lists whose ID lies between the values for startID and endID.
        /// Performs read operation across multiple pages in parallel.
        /// </summary>
        /// <param name="startID"></param>
        /// <param name="endID"></param>
        /// <param name="callback"></param>
        public List<CourseInstance> ReadListingsInRange(
            int startID = 1, int endID = Int16.MaxValue, IProgress<Double> callback = null)
        {
            var URLsRead = 0;
            object lockObj = new Object();
            var courses = new List<CourseInstance>();

            // Attempt to connect to constructed URLs in parallel
            Parallel.For(startID, endID,
                new ParallelOptions { MaxDegreeOfParallelism = MAX_CONNECTIONS },
                i =>
                {
                    var courseInstance = ReadCourseInformation(i);
                    lock (lockObj)
                    {
                        // If course instance data added successfully, add to list
                        if (courseInstance != null)
                        {
                            courses.Add(courseInstance);
                        }

                        // Update and report progress
                        URLsRead++;
                        if (callback != null)
                        {
                            callback.Report((double)URLsRead / (endID - startID) * 100);
                        }
                    }
                });
            return courses;
        }

        public List<CourseInstance> ReadFailedURLs(IProgress<Double> callback = null)
        {
            var courses = new List<CourseInstance>();
            var readIds = new List<int>();
            var lockObj = new object();
            var reads = 0;

            Parallel.ForEach(FailedReads, id =>
            {
                // Attempt to read course information with matching ID
                // If found, update course list and record read ID
                var course = ReadCourseInformation(id);
                if (course != null)
                {
                    lock (readIds)
                    {
                        readIds.Add(id);
                        courses.Add(course);
                    }
                }

                // Update and report progress
                lock (lockObj)
                {
                    reads++;
                    if (callback != null)
                    {
                        callback.Report((double)reads / FailedReads.Count() * 100);
                    }
                }
            });

            // Remove successfully read IDs from list of timed out IDs
            readIds.ForEach(r => FailedReads.Remove(r));
            return courses;
        }

        /// <summary>
        /// Attempts to load the Lectopia recording list with the given ID. On a successful load, 
        /// this method will parse the contents of the page and append the parsed data to the model
        /// in a form of a course instance object.
        /// </summary>
        /// <param name="id"></param>
        public CourseInstance ReadCourseInformation(int id)
        {
            String URL = RECORDINGS_URL + id;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.AllowAutoRedirect = false;
            request.Proxy = null;

            // Check if given URL points to a valid listing
            // If list loaded successfully, load document
            HtmlDocument document = null;
            try
            {
                document = LoadDocument(URL);
            }
            // In event of a connection timeout, add attempted URL to collection for a later retry
            catch (WebException)
            {
                // TODO: Log error
                lock (FailedReads)
                {
                    FailedReads.Add(id);
                }
            }

            // If document loaded successfully, parse contents
            if (document != null)
            {
                // Retrieve subject title
                var title = document.DocumentNode.SelectSingleNode(
                    "//table[@id='header']//h2").InnerText;

                // Construct course instance
                var course = new CourseInstance(id, title);
                course.PageURLs.Add(URL);

                // Extract page links
                var pageNodes = document.DocumentNode.SelectNodes(
                                "(//td[@class='noNesting'])[1]/a");
                if (pageNodes != null)
                {
                    var pageLinks = from a in pageNodes
                                    select BASE_URL + a.GetAttributeValue("href", "");
                    course.PageURLs.AddRange(pageLinks);
                }

                // Return constructed instance
                return course;
            }
            return null;
        }

        /// <summary>
        /// Returns a list of recordings scraped from the URLS provided
        /// by the course instance object.</summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public List<Recording> GetRecordings(CourseInstance course, IProgress<Double> callback = null)
        {
            var recordingList = new List<Recording>();

            for (var i = 0; i < course.PageURLs.Count(); i++)
            {
                var link = course.PageURLs[i];
                HtmlDocument document = LoadDocument(link);

                // TODO: Figure out what to do on document load failure
                if (document != null)
                {
                    // Retrieve nodes containing recording data
                    var recordingNodes = document.DocumentNode.SelectNodes(
                        "//table[@class='mainindex']");

                    if (recordingNodes != null)
                    {
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
                            var formatList = GetRecordingFormats(node);
                            formatList.ForEach(f => recording.Formats.Add(f));

                            // Add recording to course instance
                            recordingList.Add(recording);
                        }
                    }

                    // Report progress
                    if (callback != null)
                    {
                        lock (callback)
                        {
                            callback.Report((double)i / course.PageURLs.Count() * 100);
                        }
                    }
                }
            }
            return recordingList;
        }

        /// <summary>
        /// Parses a collection of option elements and retrieves recording information.
        /// </summary>
        /// <param name="formNode">HtmlNode containing form surrounded option list.</param>
        /// <returns></returns>
        private List<Format> GetRecordingFormats(HtmlNode formNode)
        {
            var formatList = new List<Format>();

            // Retrieve option elements
            var optionNodes = formNode.SelectNodes(
                            ".//form[contains(@name, 'Download')]//option[position() > 2]");

            // Retrieve ID and format name for each option
            if (optionNodes != null)
            {
                foreach (var option in optionNodes)
                {
                    var formatString = option.GetAttributeValue("value", "");
                    var formatID = Convert.ToInt32(formatString.Split(',')[1]);
                    var formatOption = new Format(formatID, option.InnerHtml);

                    formatList.Add(formatOption);
                }
            }
            return formatList;
        }

        private HtmlDocument LoadDocument(String URL)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.AllowAutoRedirect = false;
            request.Proxy = null;

            // Check if given URL points to a valid listing
            // If list loaded successfully, load document
            HtmlDocument document = null;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(responseStream))
                        {
                            HtmlNode.ElementsFlags.Remove("form");
                            HtmlNode.ElementsFlags.Remove("option");
                            document = new HtmlDocument();
                            document.Load(sr);
                        }
                    }
                }
            }
            return document;
        }
    }
}
