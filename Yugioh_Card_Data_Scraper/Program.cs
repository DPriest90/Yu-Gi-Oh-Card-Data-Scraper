using System;
using HtmlAgilityPack;
using ScrapySharp;
using ScrapySharp.Extensions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net;
using System.Text;

namespace Yugioh_Card_Data_Scraper // Note: actual namespace depends on the project name.
{
    public class Program
    {
        /// <summary>
        /// Application entry point and Main function
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            // We will create a pipe delimited CSV file (character seperated value) so setup some vars for that
            string delimiter = "|";
            string outputFileName = "";

            // We will at some point use the fandom wiki to get extra card information where needed
            string wiki_URL = "https://yugioh.fandom.com/wiki/";

            // Website base url
            string base_URL = "https://www.db.yugioh-card.com";

            // This is the URL we will use to append the link info to
            string workBaseURL = "https://www.db.yugioh-card.com/yugiohdb/";

            // This URL will get us a complete list of available booster packs.
            // Each pack has it's own list of cards available to it.
            string URL = "https://www.db.yugioh-card.com/yugiohdb/card_list.action";

            Console.WriteLine("Fetching HTML...");

            // Get the <body> tag contents of the web page
            string html = await CallUrl(URL);

            Console.WriteLine("Done...");

            Console.WriteLine("");


            Console.WriteLine("Extracting data from HTML...");

            // Get a list of all input html tags as this is what contains the link to each packs web page
            List<string> inputTags = GetPackDivElements(html);

            Console.WriteLine("Done...");

            Console.WriteLine("");

            // Make sure we have data to work with
            if (inputTags.Count != 0 || inputTags != null)
            {
                // Now that we have the input tags for all booster packs, we need to get the value attr text as this is the link
                // we want to add to the workBaseURL variable to get a complete URL for said booster pack
                using (StreamWriter writer = new StreamWriter(outputFileName))
                {
                    Console.WriteLine("Creating txt file...");

                    foreach (string line in inputTags)
                    {
                        writer.WriteLine(line);
                    }

                    Console.WriteLine("Done...");
                    Console.WriteLine("");
                }
            }

            // DEBUGGING - To Pause App
            Console.Read();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static List<string> GetPackDivElements(string html)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                string divName = "pack pack_en";

                var packDivs = htmlDoc.DocumentNode.Descendants("div")
                    .Where(node => node.GetAttributeValue("class", "").Contains(divName))
                    .ToList();

                List<string> links = new List<string>();

                // Iterate through our div collection
                foreach (HtmlNode node in packDivs)
                {
                    string packHtml = node.OuterHtml;

                    // Create a new temporary HtmlDoc that contains ONLY the div we collected before
                    HtmlDocument packHtmlDoc = new HtmlDocument();
                    packHtmlDoc.LoadHtml(packHtml);

                    // Query the new HtmlDoc to get ONLY the input HTML tag as this is what contains the link
                    // to every booster packs web page
                    var theInputTag = packHtmlDoc.DocumentNode.Descendants("input")
                        .Where(node => node.GetAttributeValue("class", "").Contains("link_value"))
                        .ToList();

                    // Add input HTML tag to list
                    links.Add(theInputTag[0].OuterHtml);
                }

                return links;
            }
            catch (Exception ex)
            {
                return null;
            }
            
        }

        /// <summary>
        /// Retrieve HTML from given URL and return as string
        /// </summary>
        /// <param name="fullUrl">The page to retrieve</param>
        /// <returns>string</returns>
        private static async Task<string> CallUrl(string fullUrl)
        {
            try
            {
                // Create an HttpClient object to retrieve the HTML of given URL
                using (HttpClient client = new HttpClient())
                {
                    //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
                    client.DefaultRequestHeaders.Accept.Clear();

                    // Get the contents of our pages <body> tag
                    var response = client.GetStringAsync(fullUrl);

                    // return the data
                    return await response;
                }
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
            
        }
    }
}