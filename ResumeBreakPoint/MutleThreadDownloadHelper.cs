using System;
using System.Collections.Generic;
using System.Text;

namespace ResumeBreakPoint
{
    public class MutleThreadDownloadHelper
    {
        /// <summary>
        /// 总文件大小
        /// </summary>
        private long serverFileLength = 0;

        DownLoadFileHelper DownLoadFileHelper = new DownLoadFileHelper();

        public MutleThreadDownloadHelper()
        {

        }


        public void DownloadFile(string url, string savePath)
        {
            var headers = DownLoadFileHelper.GetDataHeader(url);
            long.TryParse(headers.Get("Content-Length"), out serverFileLength);//获取待下载文件的总文件Length


        }


        /// <summary>
        /// 集合分区
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Lists"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        private List<List<T>> SpiltList<T>(List<T> Lists, int num) //where T:class
        {
            List<List<T>> fz = new List<List<T>>();
            //元素数量大于等于 分组数量
            if (Lists.Count >= num)
            {
                int avg = Lists.Count / num; //每组数量
                int vga = Lists.Count % num; //余数
                for (int i = 0; i < num; i++)
                {
                    List<T> cList = new List<T>();
                    if (i + 1 == num)
                    {
                        cList = Lists.Skip(avg * i).ToList<T>();
                    }
                    else
                    {
                        cList = Lists.Skip(avg * i).Take(avg).ToList<T>();
                    }
                    fz.Add(cList);
                }
            }
            else
            {
                fz.Add(Lists);//元素数量小于分组数量
            }
            return fz;
        }
    }
}
