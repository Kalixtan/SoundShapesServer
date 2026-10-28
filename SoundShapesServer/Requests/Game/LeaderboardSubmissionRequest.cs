namespace SoundShapesServer.Requests.Game;

public class LeaderboardSubmissionRequest
{
    public static bool TryDeserializeSubmission(string str, out LeaderboardSubmissionRequest response)
    {
        response = new();
        
        string[] queries = str.Split("&");

        foreach (string? query in queries)
        {
            string[] nameAndValue = query.Split("=");
            if (nameAndValue.Length < 2) return false;
            string name = nameAndValue[0];
            string value = nameAndValue[1];

            switch (name)
            {
                case "deaths":
                    if (!int.TryParse(value, out int deaths)) return false;
                    response.Deaths = deaths;
                    break;
                case "playTime":
                    if (!long.TryParse(value, out long playTime)) return false;
                    response.PlayTime = playTime;
                    break;
                case "golded":
                    if (!int.TryParse(value, out int golded)) return false;
                    response.Golded = golded;
                    break;
                case "tokenCount":
                    if (!int.TryParse(value, out int notes)) return false;
                    response.Notes = notes;
                    break;
                case "score":
                    if (!long.TryParse(value, out long score)) return false;
                    response.Score = score;
                    break;
                case "completed":
                    if (!int.TryParse(value, out int completed)) return false;
                    response.Completed = completed == 1;
                    break;
            }
            
        }

        return true;
    }
    public int Deaths { get; set; }
    public long PlayTime { get; set; }
    public int Golded { get; set; }
    public int Notes { get; set; }
    public long Score { get; set; }
    
    // is true by default (completed) because campaign score submissions don't use the completion parameter
    public bool Completed { get; set; } = true; 
}
