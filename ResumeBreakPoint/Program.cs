using ResumeBreakPoint;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Xly.LBC.ResumeBreakPoint
{
    class Program
    {
        static void Main(string[] args)
        {

            string path = @"C:\Users\fuxuyang\Desktop\Download\";
            string[] urlList =
            {
                "http://xlyuntech-graysystem-test.oss-cn-beijing.aliyuncs.com/software/LBC3.5.3_Gray-1569390321231.zip"
            };

            DownLoadFileHelper downLoadFileHelper = new DownLoadFileHelper();
            downLoadFileHelper.SetMaxPerRange(1024 * 1024);
            downLoadFileHelper.SetPerRange(1024 * 1024);
            downLoadFileHelper.SetThreadNum(2);

            Action<long, long> action = (p, q) =>
            {
                Console.WriteLine($"下载文件大小：{Math.Round((decimal)q / 1024 / 1024, 2)}MB;已下载：{Math.Round((decimal)p / 1024 / 1024, 2)}MB;下载进度：{Math.Round((decimal)p * 100 / q, 2)}%");
            };
            foreach (var item in urlList)
            {

                downLoadFileHelper.MutleThreadDownLoadFile(item, path, action);
                Console.WriteLine("下载完成");
            }
        }
        public static void DownLoadData(string url, string savePath)
        {
            WebHeaderCollection webHeaderCollection = GetDataHeader(url);

            bool ifRange = webHeaderCollection?.Get("Accept-Ranges") == "bytes";//是否支持range查询

            /*断点续传*/
            if (ifRange)
            {
                string fileName = webHeaderCollection?.Get("ETag")?.Replace("\"", "");//ETag标识作为文件名称
                fileName = savePath + @"\" + fileName + ".zip";
                long historyFileLength = GetHistoryDataLength(fileName);//获取当前下载文件的已下载部分Length
                long.TryParse(webHeaderCollection.Get("Content-Length"), out long contentFileLength);//获取待下载文件的总文件Length

                /*分批次下载文件*/
                long perRange = 1024 * 10;//单次文件下载量
                while (historyFileLength < contentFileLength)
                {
                    string startRange = historyFileLength.ToString();
                    long endRangeShouldBe = historyFileLength + perRange;
                    string endRange = endRangeShouldBe > contentFileLength ? string.Empty : endRangeShouldBe.ToString();
                    WebHeaderCollection header2 = new WebHeaderCollection()
                    {
                        { "Range", "bytes=" + startRange + "-" + endRange }
                    };

                    var item2 = new HttpItem() { URL = url, Method = "GET", ResultType = ResultType.Byte, Header = header2 };
                    var result = GetHttpResult(item2)?.ResultByte;
                    historyFileLength += result?.LongLength ?? 0;

                    SaveDataToFile(fileName, result);

                    Console.WriteLine($"下载进度：{Math.Round((decimal)historyFileLength * 100 / contentFileLength, 2, MidpointRounding.AwayFromZero)}%");
                }
                Console.WriteLine($"下载进度：{Math.Round((decimal)historyFileLength * 100 / contentFileLength, 2, MidpointRounding.AwayFromZero)}%");
            }
            /*普通下载*/
            else
            {
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");//ETag标识作为文件名称
                fileName = savePath + @"\" + fileName + ".zip";

                HttpItem httpItem = new HttpItem()
                {
                    URL = url,
                    Method = "GET",
                    ResultType = ResultType.Byte
                };
                var result = GetHttpResult(httpItem)?.ResultByte;
                SaveDataToFile(fileName, result);
            }
        }

        public static WebHeaderCollection GetDataHeader(string url)
        {
            HttpItem httpItem = new HttpItem()
            {
                URL = url,
                Method = "HEAD"
            };
            HttpResult httpResult = GetHttpResult(httpItem);
            WebHeaderCollection webHeaderCollection = httpResult.Header;
            return webHeaderCollection;
        }
        public static long GetHistoryDataLength(string fileName)
        {
            long downloadCount = 0;
            if (File.Exists(fileName))
            {
                using (FileStream stream = new FileStream(fileName, FileMode.Open))
                {
                    downloadCount = stream.Length;
                };
            }
            return downloadCount;
        }
        public static void SaveDataToFile(string path, byte[] data)
        {
            using (FileStream stream = new FileStream(path, FileMode.Append))
            {
                stream.Write(data, 0, data.Length);
            }
        }
        public static HttpResult GetHttpResult(HttpItem item)
        {
            HttpHelper httpHelper = new HttpHelper();
            HttpResult httpResult = httpHelper.GetHtml(item);
            return httpResult;
        }
    }




}
