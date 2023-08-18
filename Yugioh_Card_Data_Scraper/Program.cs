using HtmlAgilityPack;
using System.Configuration;
using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;

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
            string base_URL = "https://www.db.yugioh-card.com/";

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
            List<string> inputTags = GetBoosterPackInputTags(html);

            Console.WriteLine(string.Format("Booster Packs Found On Site: {0}", inputTags.Count.ToString()));
            Console.WriteLine("HTML Extraction Complete...");

            Console.WriteLine("");

            // Make sure we have data to work with
            if (inputTags != null)
            {
                if (inputTags.Count != 0)
                {
                    // Now that we have the input tags for all booster packs, we need to get the value attr text as this is the link
                    // we want to add to the workBaseURL variable to get a complete URL for said booster pack

                    Console.WriteLine("Fetching card data. This will take some time...");

                    // The name of the booster pack we are working with can be found in the element which has a class attr of "broad_title"
                    string packNameElementClass = "broad_title";

                    foreach (string line in inputTags)
                    {
                        // Build the URL for the booster pack page
                        string fullPackLink = base_URL;
                        fullPackLink += line.Substring(48, line.Length - 50);

                        // Get the HTML for the booster pack webpage
                        string packHtml = await CallUrl(fullPackLink);

                        // Create Html Document based on the booster pack url
                        HtmlDocument packHtmlDoc = new HtmlDocument();
                        packHtmlDoc.LoadHtml(packHtml);

                        // Get the title of the booster pack from the HTML <title> tag
                        var pageTitleContainer = packHtmlDoc.DocumentNode.SelectSingleNode("//title");
                        string completePageTitle = pageTitleContainer.InnerHtml;
                        string packName = completePageTitle.Split('|')[0].Replace(" ", "");

                        Console.WriteLine(string.Format("Processing booster pack: {0}", packName));                        

                        #region Class text values for the information we need to retrieve from HTML elements

                        // The class name or whatever that is used to located the mark up where the card information is
                        string cardIdentifier = "";

                        // Each card in the booster pack is set in it's own div (that contains multiple child nodes for each
                        // part of the displayed information. Below is the class name of the container (div element) that
                        // houses ALL of the cards details.
                        string cardContainerClassName = "t_row c_normal";

                        // The image displayed on the page is contained in the container detailed below
                        string cardImageContainerClassName = "box_card_img";

                        // flex_1 is a html list tag that contains multiple child nodes i.e. <dl> followed by multiple <dd> tags
                        // each of which contains card data
                        string cardInfoParentContainer = "flex_1";

                        // The above has 1+ child nodes (<dd> tags) that each contain a piece of the displayed information.
                        // The name of the card is contained in dd with class="box_card_name flex_1 top_set" which itself has
                        // two child <span> nodes. We want the <span> with class="card_name"
                        string cardNameContainer = "card_name";

                        #endregion

                        // Get all html nodes that have a classa that = "t_row c_normal". Each one of these elements contains all the data
                        // needed for the card related to it
                        // Each of the items in the collection "cardsInBoosterPack" is an individual card
                        var cardsInBoosterPack = packHtmlDoc.DocumentNode.Descendants("div")
                            .Where(node => node.GetAttributeValue("class", "").Contains(cardContainerClassName))
                            .ToList();
                        
                        ProcessBoosterPack(cardsInBoosterPack);
                    }
                }               

                Console.WriteLine("Done...");
                Console.WriteLine("");
            }

            // DEBUGGING - To Pause App
            Console.Read();
        }            

        /// <summary>
        /// Get data for the 
        /// </summary>        
        /// <param name="cardsInBoosterPack"></param>
        private static void ProcessBoosterPack(List<HtmlNode> cardsInBoosterPack)
        {
            // Iterate through all cards in current pack
            foreach (HtmlNode node in cardsInBoosterPack)
            {
                Card currentCard = new Card();

                // Get the complete HTML from current HtmlNode. This will contain all the html related to the current card
                string currentCardHtml = node.OuterHtml;
                //var cardNamePart = cardInfoParentContainer

                // Create HtmlDoc that ONLY contains the current card html
                HtmlDocument cardDoc = new();
                cardDoc.LoadHtml(currentCardHtml);

                #region Card properties

                // Isolate the <span> tag that holds the card name text
                var crdNameTag = cardDoc.DocumentNode.Descendants("span")
                   .Where(node => node.GetAttributeValue("class", "").Contains("card_name"))
                   .ToList();

                // Get the card name from the above HtmlNode
                string cardName = crdNameTag[0].InnerText.Trim();

                // Isolate the attribute value of current card
                // i.e. DARK, LIGHT, WATER, FIRE, EARTH etc.
                var attributeTag = cardDoc.DocumentNode.Descendants("span")
                    .Where(node => node.GetAttributeValue("class", "")
                    .Contains("box_card_attribute"))
                    .ToList();

                string crdAttribute = attributeTag[0].InnerText.Trim();

                #endregion

                currentCard.Attribute = crdAttribute;
                currentCard.Name = cardName;

                string addToDb = ConfigurationManager.AppSettings["AddToDatabase"].ToString().Trim();

                // Check to see if we are to add cards to database and if we are then  proceed to setup database connection
                if (addToDb == "Y")
                {
                    AddCardToDatabase(currentCard);
                }

                Console.WriteLine(string.Format("Card: {0} Processed", cardName));
            }
        }

        /// <summary>
        /// Insert Card into the Card_Data table
        /// </summary>
        /// <param name="cardToAdd"></param>
        private static void AddCardToDatabase(Card cardToAdd)
        {
            // Get database connection string from the config file
            string connString = ConfigurationManager.ConnectionStrings["SQL_DB_ConnectionString"].ConnectionString.ToString();

            // Open connection
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // Get name of the table we are too add data to
                string tableName = ConfigurationManager.AppSettings["CardTable"].ToString().Trim();

                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    //TODO: Insert current card data into the Card_Data table
                }
             }
        }

        /// <summary>
        /// This function returns a list of every <input> html tag on the page in given URL, that has a class tag value of "pack pack_en".
        /// These input tags contain the extra URL string we need for all booster packs on the website.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static List<string> GetBoosterPackInputTags(string html)
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
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
    }
}