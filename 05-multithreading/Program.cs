﻿/*
    1. Подготовка
    Найти десяток новостных порталов с RSS лентой, записать их в файл.

    2. Задание
    Раз в N минут нужно читать файл со списком адресов RSS лент (rss_list.txt), а так же файл со списоком уже обработанных ссылок (processed_articles.txt).
    Каждую RSS-ленту нужно скачать, распарсить, достать из неё список ссылок на новостные статьи. - done
    По тем ссылкам, которые ещё не было ранее обработаны, нужно скачать html содержимое.
    Содежимое нужно сохранить на диск, а так же записать в файл processed_articles.txt информацию о том что ссылка была обработана.

    3. Логи
    В процессе работы нужно писать логи в консоль: лента обработана, статья скачена и т.д. 
    После каждой итерации нужно выводить статистику: сколько лент обработано, сколько новых новостей, сколько старых.


    10 баллов
    Мягкий дедлайн: 31.03.2022 23:59
    Жесткий дедлайн: 12.05.2022 23:59
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Net;
using System.Globalization;
using System.Threading;

namespace news_portal {
    class RSS_Reader {
        public List<string> Read(string url) {
            List<string> links = new List<string>();
            WebRequest request = WebRequest.Create(url);
 
            WebResponse response = request.GetResponse();
            XmlDocument doc = new XmlDocument();
            try {
                doc.Load(response.GetResponseStream());
                XmlElement rssElem = doc["rss"];
                if (rssElem == null) {
                    return links;
                }
                XmlElement chanElem = rssElem["channel"];
                XmlNodeList itemElems = rssElem["channel"].GetElementsByTagName("item");
                if (chanElem != null) {
                    foreach (XmlElement itemElem in itemElems) {
                        links.Add(itemElem["link"].InnerText);                   
                    }
                }
            } catch (XmlException) {}
            
            return links;
        }
    }

    class Observer {
        private object _lock = new object();
        private int sleep_time;
        private FileLogger processed_article_writer = new FileLogger("processed_articles.txt");
        private FileLogger worker_logger = new FileLogger("logs.txt");
        private RSS_Reader rss_reader;
        public Observer(int sleep_time = 1 * 60 * 1000) {
            this.sleep_time = sleep_time;
            this.rss_reader = new RSS_Reader();
        }
        public void Observe() {
            while (True) {
                UpdateNews();
                Thread.Sleep(this.sleep_time);
            }
        }
        public void UpdateNews() {
            List<Task<int>> tasks = new List<Task<int>>();
            using (StreamReader reader = new StreamReader("rss_list.txt")) {
                while (reader.Peek() >= 0) {
                    string str = reader.ReadLine();
                    if (str == null || str.Length == 0) {
                        break;
                    }
                    // Console.WriteLine(str);
                    Task<int> task = new Task<int>(() => MethodForThread(str));
                    task.Start();
                    tasks.Add(task);
                }
            }
            Task.WaitAll(tasks.ToArray());
        }
        int MethodForThread(string url) {
            List<string> links = rss_reader.Read(url);
            foreach (string link in links) {
                // save info about article
                WebRequest request = WebRequest.Create(link);
                WebResponse response = request.GetResponse();
                // update article info
                Monitor.Enter(_lock);
                try {
                    using (StreamReader reader = new StreamReader("processed_articles.txt")) {
                        bool is_processed = false;
                        while (reader.Peek() >= 0) {
                            if (reader.ReadLine() == link) {
                                // Console.WriteLine("This article already processed, skipped it!");
                                is_processed = true;
                                break;
                            }
                        }
                        if (!is_processed) {
                            processed_article_writer.Logging(link);
                            // TODO: process unprocessed_article
                            
                        }
                    }
                } finally {
                    Monitor.Exit(_lock);
                }
            }
            // Console.WriteLine(url + " potok ended!");
            return 0;
        }
        void ProcessUnprocessedArticle(string link) {
                
        } 
   }
   class Program {

        public static void Main (string[] args) {
            Observer observer = new Observer();
            int count = 0;
            while (count < 10) {
                observer.UpdateNews();
                Thread.Sleep(1000);
            }
        }
    }
}