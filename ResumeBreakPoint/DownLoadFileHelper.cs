using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Xly.LBC.ResumeBreakPoint;

namespace ResumeBreakPoint
{
    public class DownLoadFileHelper
    {
        /// <summary>
        /// //单次文件下载量:单位B
        /// </summary>
        private long perRange = 1024 * 1024;

        private string fileName;

        private long historyFileLength = 0;

        private long contentFileLength = 0;

        private string url;

        /// <summary>
        /// 设置单次文件下载量:单位B
        /// </summary>
        /// <param name="perRange"></param>
        public void SetPerRange(long perRange)
        {
            this.perRange = perRange;
        }

        public void DownLoadFile(string url, string savePath)
        {
            Action<long, long> action = (p, q) =>
             {
                 Console.WriteLine($"下载文件大小：{Math.Round((decimal)q / 1024 / 1024, 2)}MB;已下载：{Math.Round((decimal)p / 1024 / 1024, 2)}MB;下载进度：{Math.Round((decimal)p * 100 / q, 2)}%");
             };


            this.url = url;
            WebHeaderCollection webHeaderCollection = GetDataHeader(url);
            bool ifRange = webHeaderCollection?.Get("Accept-Ranges") == "bytes";//是否支持range查询

            /*断点续传*/
            if (ifRange)
            {
                SetDownloadFileName(savePath, webHeaderCollection);
                SetDownloadFileLength(webHeaderCollection);
                /*分批次下载文件*/
                while (historyFileLength < contentFileLength)
                {
                    byte[] result = DownloadFile(url);
                    SaveDataToFile(fileName, result);
                    historyFileLength += result?.LongLength ?? 0;
                    if (historyFileLength == contentFileLength)
                        break;
                    action(historyFileLength, contentFileLength);
                }
                action(historyFileLength, contentFileLength);
            }
            /*普通下载*/
            else
            {
                string fileName = DateTime.Now.ToString("yyyyMMddHHmmss");//ETag标识作为文件名称
                fileName = savePath + @"\" + fileName;

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


        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private byte[] DownloadFile(string url)
        {
            long? startRange = historyFileLength;
            long? endRangeShouldBe = historyFileLength + perRange;
            long? endRange = endRangeShouldBe > contentFileLength ? null : endRangeShouldBe;
            //WebHeaderCollection webHeaderCollection = new WebHeaderCollection();
            //webHeaderCollection.Add("Range", "bytes=" + startRange + "-" + endRange);

            var item2 = new HttpItem() { URL = url, Method = "GET", ResultType = ResultType.Byte/*, Header = webHeaderCollection*/, Range = Tuple.Create(startRange, endRange) };
            var result = GetHttpResult(item2)?.ResultByte;
            return result;
        }

        /// <summary>
        /// 设置待下载文件的已下载大小和总大小
        /// </summary>
        /// <param name="webHeaderCollection"></param>
        private void SetDownloadFileLength(WebHeaderCollection webHeaderCollection)
        {
            historyFileLength = GetHistoryDataLength(fileName);//获取当前下载文件的已下载部分Length
            long.TryParse(webHeaderCollection.Get("Content-Length"), out contentFileLength);//获取待下载文件的总文件Length
        }

        /// <summary>
        /// 设置下载文件名称
        /// </summary>
        /// <param name="savePath"></param>
        /// <param name="webHeaderCollection"></param>
        private void SetDownloadFileName(string savePath, WebHeaderCollection webHeaderCollection)
        {
            fileName = webHeaderCollection?.Get("ETag")?.Replace("\"", "");//ETag标识作为文件名称
            string fileType = url.Substring(url.LastIndexOf('.'), url.Length - url.LastIndexOf('.'));
            fileName = savePath + @"\" + fileName + fileType;
        }

        private static WebHeaderCollection GetDataHeader(string url)
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
        private static long GetHistoryDataLength(string fileName)
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
        private static void SaveDataToFile(string path, byte[] data)
        {
            if (data == null || data.Length == 0) return;
            using (FileStream stream = new FileStream(path, FileMode.Append))
            {
                stream.Write(data, 0, data.Length);
            }
        }
        private static HttpResult GetHttpResult(HttpItem item)
        {
            HttpHelper httpHelper = new HttpHelper();
            HttpResult httpResult = httpHelper.GetHtml(item);
            return httpResult;
        }
    }
}
