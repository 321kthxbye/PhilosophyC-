using System;
using System.Threading;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Collections.ObjectModel;

namespace Philosophy
{
    public class Wikipage
    {
        public IWebDriver driver;
        public IWebElement heading;
        private string url;
        private ReadOnlyCollection<IWebElement> mainParagraphs;
        private List<IWebElement> usableLinks;
        public Wikipage(string url, IWebDriver driver)
        {
            this.driver = driver;
            this.url = url;
            this.usableLinks = new List<IWebElement>();
            this.load();
        }

        public bool isInParenthesis(string paragraph, int index, string left, string right)
        {
            var unclosedBrackets = 0;

            for (var i = 0; i < index; ++i)
            {
                if (paragraph[i].ToString() == left)
                    unclosedBrackets += 1;
                else if (paragraph[i].ToString() == right)
                    unclosedBrackets -= 1;
            }

            return unclosedBrackets != 0;
        }


        public void loadAllUsableLinks()
        {
            foreach (IWebElement p in this.mainParagraphs)
            {
                var links = p.FindElements(By.TagName("a"));

                foreach (var l in links)
                {
                    var parent = l.FindElement(By.XPath(".."));
                    if (parent.TagName == "i")
                        continue;
                    else if (l.GetAttribute("class") == "new")
                        continue;
                    else if (l.Text.StartsWith('[') && l.Text.EndsWith(']'))
                        continue;
                    else if (this.isInParenthesis(p.Text, p.Text.IndexOf(l.Text), "(", ")"))
                        continue;
                    else if (l.Text == "")
                        continue;
                    else if (l.GetAttribute("href") == null)
                        continue;
                    else if (!l.GetAttribute("href").StartsWith("https://en.wikipedia.org"))
                        continue;
                    else
                        this.usableLinks.Add(l);
                }
            }
        }

        public void load()
        {
            this.driver.Url = this.url;
            this.heading = this.driver.FindElement(By.Id("firstHeading"));
            this.mainParagraphs = this.driver.FindElement(By.Id("mw-content-text")).FindElements(By.XPath("//div[@class='mw-parser-output']/p"));
            this.loadAllUsableLinks();
        }

        public IWebElement getNthUsableLink(int index)
        {
            if (index >= 0 && index < this.usableLinks.Count)
            {
                return this.usableLinks[index];
            }
            else
                return null;
        }

        public Wikipage clickLink(int index)
        {
            var url = this.usableLinks[0].GetProperty("href");
            this.usableLinks[index].Click();
            return new Wikipage(url, this.driver);

        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var url = "https://en.wikipedia.org/wiki/Special:Random";
            //var url = "https://en.wikipedia.org/wiki/Pre-Hilalian_Urban_Arabic_dialects";
            //var url = "https://en.wikipedia.org/wiki/Wikipedia:Red_link";
            var driver = new ChromeDriver();
            var page = new Wikipage(url, driver);

            var transitions = 0;
            var visitedLinks = new List<string>();
            visitedLinks.Add(page.driver.Url);

            while (page.heading.Text != "Philosophy")
            {
                var link = page.getNthUsableLink(0);
                if (link == null)
                {
                    Console.WriteLine("Whoops, no link to click!");
                    break;
                }
                else if (visitedLinks.Contains(link.GetAttribute("href")))
                {
                    Console.WriteLine("Whoops, loop! Ending application!");
                    break;
                }
                else
                {
                    visitedLinks.Add(link.GetAttribute("href"));
                    page = page.clickLink(0);
                    Thread.Sleep(1000);
                    transitions += 1;
                }
            }

            Console.WriteLine(string.Format("Application made {0} transitions and visited folowing links", transitions));
            foreach (string visitedLink in visitedLinks)
                Console.WriteLine(visitedLink);
        }
    }
}
