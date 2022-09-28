using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Mail;
using System.Reflection.Metadata;
using Microsoft.VisualBasic;
using System.Net.Http;

namespace PingReso
{
    public class Config
    {
        public string Domains { get; set; }
        public string AdminEmail { get; set; }
        public static int MaxErrorCount { get; set; } = 2;
        

    }


    public class Program
    {

        static string ReadJSON()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory
                + @"appsettings.json");
            return path;
        }

       

        static void SendEmail(string email, string body)
        {
            if (String.IsNullOrEmpty(email))
                return;
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(email);
                //mail.From = new MailAddress("resopingsend@reso.vn");
                mail.From = new MailAddress("trieuhchse161563@fpt.edu.vn");
                mail.Subject = $"SERVER DOWN!";

                mail.Body = body;

                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com"; //Or Your SMTP Server Address
                smtp.UseDefaultCredentials = false;
                //smtp.Credentials = new System.Net.NetworkCredential("resopingsend@reso.vn", "Beanoi1234"); // use valid credentials
                smtp.Credentials = new System.Net.NetworkCredential("trieuhchse161563@fpt.edu.vn", "0775711152haitrieu");
                smtp.Port = 587;

                //Or your Smtp Email ID and Password
                smtp.EnableSsl = true;
                smtp.Send(mail);
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Error message will be sent to your email");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void CreateLog(List<string> ErrorList)
        {
            string logFolderName = "logs/";
            if (!Directory.Exists(logFolderName))
            {
                Directory.CreateDirectory(logFolderName);
            }
            string logFileName = "";
            DateTime now = DateTime.Now;
            logFileName = String.Format("{0}_{1}_{2}_log.txt", now.Day, now.Month, now.Year);
            string fullFileLog = Path.Combine(logFolderName, logFileName);

            using (StreamWriter sw = new StreamWriter(fullFileLog))
            {
                foreach (var err in ErrorList)//save error to 1 file
                {
                    sw.WriteLine(String.Format("Error occurs at: {0}", now));
                    sw.WriteLine(String.Format("Error: An exception occurred during a Ping request."));
                    sw.WriteLine(String.Format("Error URL: {0}", err));
                    sw.WriteLine();
                }
            }


        }

        static void Main(string[] args)
        {

            Ping p = new Ping();
            bool check = true;
            string URL = "";


            //Read Json file
            string path = ReadJSON();
            using (StreamReader sr = new StreamReader(path))
            {

                var json = sr.ReadToEnd();
                var config = JsonConvert.DeserializeObject<Config>(json);
                string[] convertArrayToSplit = config.Domains.Split(",");//Convert to array to split
                List<string> domain = new List<string>(convertArrayToSplit);//Convert to list     
                Dictionary<string, int> DomainHashmap = domain.Distinct().ToDictionary(x => x, x => 0);//Convert to hashmap

                List<string> ErrorList = new List<string>();//list of error server
                for (; ; )
                {
                    foreach (var domains in DomainHashmap)
                    {                       
                        while (check)
                        {
                            if (DomainHashmap[domains.Key] < Config.MaxErrorCount)
                            {
                                try
                                {
                                    PingReply rep = p.Send(domains.Key, 1000);
                                    if (rep.Status.ToString() == "Success")
                                    {
                                        Console.ForegroundColor = ConsoleColor.Cyan;

                                        Console.WriteLine("Reply from: " + rep.Address + "Bytes=" + rep.Buffer.Length + " Time=" +
                                            rep.RoundtripTime + " TTL=" + rep.Options.Ttl + " Routers=" + (128 - rep.Options.Ttl) + " Status=" +
                                            rep.Status + " Server" + domains);//Print normal server
                                        URL = domains.ToString();
                                        Thread.Sleep(1000);
                                    }
                                    check = false;
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;

                                    DomainHashmap[domains.Key] = domains.Value + 1;//Error Server Count + 1
                                    Console.WriteLine("Error:{0}, {1}", domains.Key, DomainHashmap[domains.Key]);
                                    if (DomainHashmap[domains.Key] == Config.MaxErrorCount)
                                    {
                                        Console.WriteLine("Error at server: {0}", domains.Key);
                                        ErrorList.Add(domains.Key);//add error server to list for send mail                                                                               
                                        CreateLog(ErrorList);
                                        
                                        //string email = "resopingreceive@reso.vn";//receive email
                                        string email = "minhthse161598@fpt.edu.vn";
                                        string body = $"Error at link {domains}" + DateAndTime.Now;//error at link                                        
                                        SendEmail(email, body);
                                    }
                                    Thread.Sleep(1500);
                                    check = false;
                                }
                            }
                            else
                            {
                                check = false;
                            }
                        }

                        check = true;
                    }

                }

            }

        }
    }
}





