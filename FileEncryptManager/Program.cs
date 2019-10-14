using ExtensionTimer;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace FileEncryptManager
{
    class Program
    {
        static void Main(string[] args)
        {
            {
                //    Console.WriteLine("输入0为加密,输入1为解密.");

                //    int x = Convert.ToInt32(Console.ReadLine());
                //    if (x == 0)
                //    {
                //        Console.WriteLine("请输入文件路径");
                //        string filePath = Console.ReadLine();
                //        Console.WriteLine("请耐心等待程序完成");

                //        FileStream readFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                //        byte[] bytes = new byte[readFileStream.Length];
                //        readFileStream.Read(bytes, 0, bytes.Length);
                //        readFileStream.Close();
                //        for (int i = 0; i < bytes.Length; i++)
                //        {
                //            ++bytes[i];
                //        }
                //        string newPath = filePath.Insert(filePath.IndexOf('.'), "fales");
                //        FileStream writeFileStream = new FileStream(newPath, FileMode.Create, FileAccess.Write);
                //        writeFileStream.Write(bytes, 0, bytes.Length);
                //        writeFileStream.Close();

                //        Console.WriteLine("操作完成");
                //    }
                //    else if (x == 1)
                //    {
                //        Console.WriteLine("请输入文件路径");
                //        string filePath = Console.ReadLine();
                //        Console.WriteLine("请耐心等待程序完成");
                //        FileStream readFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                //        byte[] bytes = new byte[readFileStream.Length];
                //        readFileStream.Read(bytes, 0, bytes.Length);
                //        readFileStream.Close();
                //        for (int i = 0; i < bytes.Length; i++)
                //        {
                //            --bytes[i];
                //        }
                //        string newPath = filePath.Insert(filePath.IndexOf('.'), "true");
                //        FileStream writeFileStream = new FileStream(newPath, FileMode.Create, FileAccess.Write);
                //        writeFileStream.Write(bytes, 0, bytes.Length);
                //        writeFileStream.Close();
                //        Console.WriteLine("操作完成");
                //    }

            }




        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="array">要加密的 byte[] 数组</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] Encrypt(byte[] array, string key)
        {
            byte[] keyArray = Encoding.UTF8.GetBytes(key);

            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = rDel.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(array, 0, array.Length);

            return resultArray;
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="array">要解密的 byte[] 数组</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] Decrypt(byte[] array, string key)
        {
            byte[] keyArray = Encoding.UTF8.GetBytes(key);

            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = rDel.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(array, 0, array.Length);

            return resultArray;
        }
    }
}
