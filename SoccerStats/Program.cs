using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Net;

namespace SoccerStats
{
    class Program
    {
        static void Main(string[] args)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            DirectoryInfo directory = new DirectoryInfo(currentDirectory);
            var fileName = Path.Combine(directory.FullName, "SoccerGameResults.csv");
            var fileContents = ReadSoccerResults(fileName);
            fileName = Path.Combine(directory.FullName, "players.json");
            var players = DeserializePlayers(fileName);
            var topTenPlayers = GetTopTenPlayers(players);
            foreach (var player in topTenPlayers)
            {
                List<NewsResult> newsResults = GetNewsForPlayer(string.Format("{0} {1}", player.FirstName, player.LastName));
                SentimentResponse sentimentResponse = GetSentimentResponse(newsResults);
                foreach (var sentiment in sentimentResponse.Sentiments)
                {
                    foreach (var newsResult in newsResults)
                    {
                        if (newsResult.HeadLine == sentiment.Id)
                        {
                            double score;
                            if (double.TryParse(sentiment.Score, out score))
                            {
                                newsResult.SentimentScore = score;
                            }
                            break;
                        }
                    }
                }
                foreach (var result in newsResults)
                {
                    Console.WriteLine(string.Format("Sentiment Score: {0:P} Date: {1:f}, Headline: {2}, Summary: {3} \r\n", result.SentimentScore, result.DatePublished, result.HeadLine, result.Summary));
                    Console.ReadKey();
                }
            }
            fileName = Path.Combine(directory.FullName, "topten.json");
            SerializePlayerToFile(topTenPlayers, fileName);
            

        }


        public static string ReadFile(string fileName)
        {
            using (var reader = new StreamReader(fileName))
            {
                return reader.ReadToEnd();
            }
        }

        public static List<GameResult> ReadSoccerResults(string fileName)
        {
            var soccerResults = new List<GameResult>();
            using (var reader = new StreamReader(fileName))
            {
                string line = "";
                reader.ReadLine();
                while ((line = reader.ReadLine()) != null)
                {
                    var gameResult = new GameResult();
                    string[] values = line.Split(',');

                    DateTime gameDate;
                    if (DateTime.TryParse(values[0], out gameDate))
                    {
                        gameResult.GameDate = gameDate;
                    }

                    gameResult.TeamName = values[1];

                    HomeOrAway homeOrAway;
                    if (Enum.TryParse(values[2], out homeOrAway))
                    {
                        gameResult.HomeOrAway = homeOrAway;
                    }

                    int parseInt;
                    if (int.TryParse(values[3], out parseInt))
                    {
                        gameResult.Goals = parseInt;
                    }
                    if (int.TryParse(values[4], out parseInt))
                    {
                        gameResult.GoalAttempts = parseInt;
                    }
                    if (int.TryParse(values[5], out parseInt))
                    {
                        gameResult.ShotsOnGoal = parseInt;
                    }
                    if (int.TryParse(values[6], out parseInt))
                    {
                        gameResult.ShotsOffGoal = parseInt;
                    }

                    double posessionPercent;
                    if (double.TryParse(values[7], out posessionPercent))
                    {
                        gameResult.PosessionPercent = posessionPercent;
                    }

                    

                    soccerResults.Add(gameResult);
                    
                }
            }
            return soccerResults;
        }

        public static List<Player> DeserializePlayers(string fileName)
        {
            var players = new List<Player>();
            var serializer = new JsonSerializer();
            using (var reader = new StreamReader(fileName))
            using (var jsonReader = new JsonTextReader(reader))
            {
                players = serializer.Deserialize<List<Player>>(jsonReader);
            }
            return players;

        }

        public static List<Player> GetTopTenPlayers(List<Player> players)
        {
            var topTenPlayers = new List<Player>();
            players.Sort(new PlayerComparer());
            int counter = 0;
            foreach(var player in players)
            {
                topTenPlayers.Add(player);
                counter++;
                if (counter == 10)
                    break;
            }
            return topTenPlayers;
        }

        public static void SerializePlayerToFile(List<Player> players, string fileName)
        {

            var serializer = new JsonSerializer();
            using (var writer = new StreamWriter(fileName))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                serializer.Serialize(jsonWriter, players);
            }

        }

        public static string GetGoogleHomePage()
        {
            var webClient = new WebClient();
            byte[] googleHome = webClient.DownloadData("https://www.google.com");

            using (var stream = new MemoryStream(googleHome))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static List<NewsResult> GetNewsForPlayer(string playerName)
        {
            var results = new List<NewsResult>();
            var webClient = new WebClient();
            webClient.Headers.Add("Ocp-Apim-Subscription-Key", "b0dd71fdda434eb38340a8afe5b32d44");
            byte[] searchResults = webClient.DownloadData(string.Format("https://api.cognitive.microsoft.com/bing/v5.0/news/search?q={0}&mkt=en-us", playerName));
            var serializer = new JsonSerializer();
            using (var stream = new MemoryStream(searchResults))
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                results = serializer.Deserialize<NewsSearch>(jsonReader).NewsResults;
            }
            return results;
        }

        public static SentimentResponse GetSentimentResponse(List<NewsResult> newsResults)
        {
            var sentimentResponse = new SentimentResponse();
            var sentimentRequest = new SentimentRequest();
            sentimentRequest.Documents = new List<Document>();
            foreach (var result in newsResults)
            {
                sentimentRequest.Documents.Add(new Document { Id = result.HeadLine, Text = result.Summary });
            }

            var results = new List<NewsResult>();
            var webClient = new WebClient();
            webClient.Headers.Add("Ocp-Apim-Subscription-Key", "ca75435e155d4c799da31104e3450f87");
            webClient.Headers.Add(HttpRequestHeader.Accept, "application/json");
            webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            string requestJson = JsonConvert.SerializeObject(sentimentRequest);
            byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);
            byte[] response = webClient.UploadData("https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment", requestBytes);
            string sentiments = Encoding.UTF8.GetString(response);
            sentimentResponse = JsonConvert.DeserializeObject<SentimentResponse>(sentiments);
            return sentimentResponse;
        }
    }
}

