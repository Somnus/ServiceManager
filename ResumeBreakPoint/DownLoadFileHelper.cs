using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xly.LBC.ResumeBreakPoint;

namespace ResumeBreakPoint
{
    public class DownLoadFileHelper
    {
        /// <summary>
        /// 单次文件下载量:单位:Byte，默认值：1MB
        /// </summary>
        private long perRange = 1024 * 1024;

        /// <summary>
        /// 单次文件最大下载量，单位：Byte,默认值：100MB
        /// </summary>
        private long maxPerRange = 1024 * 1024 * 100;

        /// <summary>
        /// ETag标记
        /// </summary>
        private string token;

        /// <summary>
        /// 本地文件名称
        /// </summary>
        private string fileName;

        /// <summary>
        /// 本地已下载文件大小，单位:Byte
        /// </summary>
        private long historyFileLength = 0;

        /// <summary>
        /// 服务端文件大小，单位：Byte
        /// </summary>
        private long contentFileLength = 0;

        /// <summary>
        /// 服务端文件路径
        /// </summary>
        private string url;

        /// <summary>
        /// 是否结束下载
        /// </summary>
        private bool finishDownload = false;

        /// <summary>
        /// 多线程数量
        /// </summary>
        private int threadNum = 5;

        #region public
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url"></param>
        /// <param name="savePath">保存基础路径</param>
        /// <param name="action">自定义操作，委托中，第一个参数为已下载文件大小，第二个参数为总文件大小。</param>
        public void DownLoadFile(string url, string savePath, Action<long, long> action)
        {
            this.url = url;
            WebHeaderCollection webHeaderCollection = GetDataHeader(url);
            bool ifRange = webHeaderCollection?.Get("Accept-Ranges") == "bytes";//是否支持range查询

            /*断点续传*/
            if (ifRange)
            {
                SetDownloadFileName(savePath, webHeaderCollection);
                SetDownloadFileLength(webHeaderCollection);

                /*分批次下载文件*/
                while (finishDownload == false)
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
        /// 多线程下载
        /// </summary>
        /// <param name="url"></param>
        /// <param name="savePath"></param>
        /// <param name="action"></param>
        public void MutleThreadDownLoadFile(string url, string savePath, Action<long, long> action)
        {
            this.url = url;
            WebHeaderCollection webHeaderCollection = GetDataHeader(url);
            bool ifRange = webHeaderCollection?.Get("Accept-Ranges") == "bytes";//是否支持range查询

            /*断点续传*/
            if (!ifRange) return;


            SetDownloadFileName(savePath, webHeaderCollection);
            SetDownloadFileLength(webHeaderCollection);

            List<Tuple<long, long>> list = GetThreadGroup(contentFileLength, threadNum);

            Action<Tuple<long, long>> action1 = p =>
            {
                int index = list.IndexOf(p) + 1;
                long avg = contentFileLength / threadNum * (index - 1);

                string fileThreadName = token + "_" + index;
                long? contentThreadFileLength = p.Item2 - p.Item1 + 1;
                contentThreadFileLength += avg;

                long? historyThreadFileLength = GetLocalFileLength(fileThreadName);
                historyThreadFileLength += avg;

                while (finishDownload == false && historyThreadFileLength != contentThreadFileLength)
                {
                    long? endRange = historyThreadFileLength + perRange - 1;
                    if (endRange >= contentThreadFileLength)
                    {
                        if (index == threadNum)
                            endRange = null;
                        if (index < threadNum)
                            endRange = contentThreadFileLength - 1;
                    }

                    var item2 = new HttpItem()
                    {
                        URL = url,
                        Method = "GET",
                        ResultType = ResultType.Byte,
                        Range = Tuple.Create(historyThreadFileLength, endRange)
                    };
                    var result = GetHttpResult(item2)?.ResultByte;

                    SaveDataToFile(savePath + fileThreadName, result);

                    historyThreadFileLength += result?.LongLength ?? 0;

                    Console.WriteLine($"线程{list.IndexOf(p) + 1};" +
                        $"文件大小：{(contentThreadFileLength - avg) / 1024} kB;" +
                        $"已下载：{(historyThreadFileLength - avg) / 1024} kB");
                }
                Console.WriteLine($"线程{list.IndexOf(p) + 1};" +
                    $"文件大小：{(contentThreadFileLength - avg) / 1024} kB;" +
                    $"已下载：{(historyThreadFileLength - avg) / 1024} kB");
            };

            Parallel.ForEach(list, action1);

            list.ForEach(p =>
            {
                int index = list.IndexOf(p) + 1;
                string fileThreadName = token + "_" + index;
                byte[] result = GetDataFromFile(savePath + fileThreadName);
                SaveDataToFile(fileName, result);
            });

        }

        /// <summary>
        /// 设置单次文件下载量:单位：Byte
        /// </summary>
        /// <param name="perRange"></param>
        public void SetPerRange(long perRange)
        {
            if (perRange > maxPerRange)
                throw new Exception($"单次HTTP请求下载量不能超过{maxPerRange} Byte");
            this.perRange = perRange;
        }

        /// <summary>
        /// 设置最大单次文件下载量限制，单位：Byte
        /// </summary>
        /// <param name="maxPerRange"></param>
        public void SetMaxPerRange(long maxPerRange) => this.maxPerRange = maxPerRange;

        /// <summary>
        /// 获取服务端文件大小，单位：Byte
        /// </summary>
        /// <returns></returns>
        public long GetServerFileLength() => contentFileLength;

        /// <summary>
        /// 获取本地已下载文件大小，单位Byte
        /// </summary>
        /// <returns></returns>
        public long GetLocalFileLength() => historyFileLength;

        /// <summary>
        /// 立即结束下载
        /// </summary>
        public void FinishDownload() => this.finishDownload = true;

        /// <summary>
        /// 设置下载线程数，默认值：5
        /// </summary>
        /// <param name="num"></param>
        public void SetThreadNum(int num) => this.threadNum = num;

        /// <summary>
        /// 获取请求头信息
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public WebHeaderCollection GetDataHeader(string url)
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
        #endregion

        #region private
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private byte[] DownloadFile(string url, Tuple<long?, long?> Range = null)
        {
            long? startRange = historyFileLength;
            long? endRangeShouldBe = historyFileLength + perRange;
            long? endRange = endRangeShouldBe > contentFileLength ? null : endRangeShouldBe;

            var item2 = new HttpItem() { URL = url, Method = "GET", ResultType = ResultType.Byte, Range = Range ?? Tuple.Create(startRange, endRange) };
            var result = GetHttpResult(item2)?.ResultByte;
            return result;
        }

        /// <summary>
        /// 设置待下载文件的已下载大小和总大小
        /// </summary>
        /// <param name="webHeaderCollection"></param>
        private void SetDownloadFileLength(WebHeaderCollection webHeaderCollection)
        {
            historyFileLength = GetLocalFileLength(fileName);//获取当前下载文件的已下载部分Length
            long.TryParse(webHeaderCollection.Get("Content-Length"), out contentFileLength);//获取待下载文件的总文件Length
        }

        /// <summary>
        /// 设置下载文件名称
        /// </summary>
        /// <param name="savePath"></param>
        /// <param name="webHeaderCollection"></param>
        private void SetDownloadFileName(string savePath, WebHeaderCollection webHeaderCollection)
        {
            token = webHeaderCollection?.Get("ETag")?.Replace("\"", "");//ETag标识作为文件名称
            string fileType = url.Substring(url.LastIndexOf('.'), url.Length - url.LastIndexOf('.'));
            fileName = savePath + @"\" + token + fileType;
        }

        /// <summary>
        /// 获取本地已下载文件大小
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private long GetLocalFileLength(string fileName)
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

        /// <summary>
        /// 保存二进制数据到文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        private void SaveDataToFile(string path, byte[] data)
        {
            if (data == null || data.Length == 0) return;
            using (FileStream stream = new FileStream(path, FileMode.Append))
            {
                stream.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// 将文件读取为byte格式
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private byte[] GetDataFromFile(string path)
        {
            if (File.Exists(path))
            {
                using (FileStream stream = new FileStream(path, FileMode.Open))
                {
                    byte[] result = new byte[stream.Length];
                    stream.Read(result, 0, (int)stream.Length);
                    return result;
                }
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// http请求方法封装
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private HttpResult GetHttpResult(HttpItem item)
        {
            HttpHelper httpHelper = new HttpHelper();
            HttpResult httpResult = httpHelper.GetHtml(item);
            return httpResult;
        }

        /// <summary>
        /// 检查服务端文件是否已修改
        /// </summary>
        /// <returns></returns>
        private bool CheckFileNotModified()
        {
            HttpItem httpItem = new HttpItem()
            {
                URL = url,
                Method = "HEAD",
                Header = new WebHeaderCollection()
                {
                    { "If-None-Match", fileName.Substring(0,fileName.LastIndexOf('.'))}
                }
            };
            HttpResult result = GetHttpResult(httpItem);
            bool check = result.StatusCode == HttpStatusCode.NotModified;
            return check;
        }

        /// <summary>
        /// 多线程下载分组
        /// </summary>
        /// <param name="length"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        private List<Tuple<long, long>> GetThreadGroup(long length, int num)
        {
            List<Tuple<long, long>> list = new List<Tuple<long, long>>();
            for (int i = 0; i < num; i++)
            {
                long avg = length / num;
                long leave = length % num;

                long start = i * avg;
                long end = (i + 1) * avg - 1;

                if (i == num - 1) end += leave;

                list.Add(Tuple.Create(start, end));
            }

            return list;
        }
        #endregion
    }
}
