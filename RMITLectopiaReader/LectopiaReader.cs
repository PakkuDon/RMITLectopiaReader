using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RMITLectopiaReader
{
    class LectopiaReader
    {
        // Constants
        private const String BASE_URL = "https://lectopia.rmit.edu.au/lectopia/lectopia.lasso?ut=";

        // Constructor
        public LectopiaReader()
        {
            RecordingListURLs = new List<String>();
            UnsuccessfulURLs = new List<String>();
            URLsRead = 0;
        }

        // Properties / Instance vars
        public List<String> RecordingListURLs { get; private set; }
        public List<String> UnsuccessfulURLs { get; private set; }
        public int URLsRead { get; private set; }

        // -- Methods --
        public void ReadCourseData(int start = 1, int end = Int16.MaxValue, IProgress<Double> callback = null)
        {
            URLsRead = 0;
            Parallel.For(start, end,
                new ParallelOptions { MaxDegreeOfParallelism = 10 },
                i =>
                {
                    ReadRecordingList(i);
                    URLsRead++;
                    if (callback != null)
                    {
                        callback.Report((double)URLsRead / end * 100);
                    }
                });
        }

        public void ReadRecordingList(int id)
        {
            String URL = BASE_URL + id;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "HEAD";
            request.AllowAutoRedirect = false;
            request.Proxy = null;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        RecordingListURLs.Add(URL);

                        // TODO: Retrieve recording details
                    }
                }
            }
            catch (WebException)
            {
                // TODO: Log error
                UnsuccessfulURLs.Add(URL);
            }
        }
    }

}
