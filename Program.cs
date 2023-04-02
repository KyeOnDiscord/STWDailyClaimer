using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace STWDailyClaimer;

internal static class Program
{
    const string fortniteIOSGameClient_ClientID = "3446cd72694c4a4485d81b77adbb2141";
    const string fortniteIOSGameClient_Secret = "9209d4a5e25a457fb9b07489d313b41a";
    static async Task Main()
    {
        Config.InitConfig();
        string LoginURL = $"https://www.epicgames.com/id/api/redirect?clientId={fortniteIOSGameClient_ClientID}&responseType=code";
        var rewardDict = DailyRewardTable.CreateRewardDictionary();
        int RewardCount = DailyRewardTable.GetRewardCount();
        int TotalVbucksCount = 0;
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Select an option:");
            Console.WriteLine("1. List saved accounts");
            Console.WriteLine("2. Add account");
            Console.WriteLine("3. Claim daily rewards for all accounts");

            bool ValidNum = int.TryParse(Console.ReadLine(), out int option);
            if (!ValidNum)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid option");
                continue;
            }
            Console.Clear();
            switch (option)
            {
                case 1:
                    for (int i = 0; i < Config.CurrentConfig.Accounts.Count; i++)
                    {
                        Console.WriteLine($"{i + 1} | {Config.CurrentConfig.Accounts[i]["displayName"]} | ID: {Config.CurrentConfig.Accounts[i]["accountId"]}");
                    }
                    break;
                case 2:

                    Console.WriteLine("Enter Auth code:");
                    Process.Start(new ProcessStartInfo() { FileName = LoginURL, UseShellExecute = true });
                    string authCode = Console.ReadLine()!;

                    if (!string.IsNullOrEmpty(authCode) && authCode.Length == 32)
                    {
                        var accessToken = await GetAuthWithAuthCode(authCode);
                        string bearerToken = accessToken.GetProperty("access_token").GetString()!;
                        string accountID = accessToken.GetProperty("account_id").GetString()!;
                        string displayName = accessToken.GetProperty("displayName").GetString()!;
                        Console.WriteLine("Access token: " + bearerToken);
                        Console.WriteLine("Logged in as " + displayName + " | ID: " + accountID);
                        var deviceAuth = await GetDeviceAuth(bearerToken, accountID);
                        string deviceId = deviceAuth.GetProperty("deviceId").ToString();
                        string secret = deviceAuth.GetProperty("secret").ToString();

                        Dictionary<string, string> deviceAuthDict = new()
            {
                { "deviceId", deviceId },
                { "secret", secret },
                { "accountId", accountID },
                {"displayName", displayName}
            };
                        if (Config.CurrentConfig.Accounts.Exists(x => x.ContainsValue(accountID)))
                        {
                            Console.WriteLine("Updated account " + displayName);
                            Config.CurrentConfig.Accounts = Config.CurrentConfig.Accounts.Where(x => !x.ContainsValue(accountID)).ToList();
                        }
                        Config.CurrentConfig.Accounts.Add(deviceAuthDict);
                        Config.SaveConfig();
                    }
                    break;

                case 3:

                    for (int i = 0; i < Config.CurrentConfig.Accounts.Count; i++)
                    {
                        var curAccount = Config.CurrentConfig.Accounts[i];

                        var curAuth = await GetAuthWithDeviceAuth(curAccount["accountId"], curAccount["deviceId"], curAccount["secret"]);

                        if (curAuth.TryGetProperty("error", out JsonElement error))
                        {
                            Console.WriteLine(error.ToString());
                            continue;
                        }

                        var loginReward = await ClaimLoginReward(curAuth.GetProperty("access_token").GetString()!, curAccount["accountId"]);

                        bool SuccessfullyClaimed = false;

                        if (!loginReward.TryGetProperty("OwnsSTW", out _))
                        {
                            SuccessfullyClaimed = loginReward.GetProperty("notifications").EnumerateArray().Where(x => x.GetProperty("type").ToString() == "daily_rewards").Count() > 0;

                            if (SuccessfullyClaimed)
                            {
                                var queryProfile = await QueryProfile(curAuth.GetProperty("access_token").GetString()!, curAccount["accountId"]);
                                int vbuckCount = GetVbucks(queryProfile);
                                TotalVbucksCount += vbuckCount;
                                int nextDefaultReward = loginReward.GetProperty("profileChanges")[0].GetProperty("profile").GetProperty("stats").GetProperty("attributes").GetProperty("daily_rewards").GetProperty("nextDefaultReward").GetInt32();

                                string RewardItem = "";
                                int ItemCount = 0;
                                DailyRewardTable.GetRewardByNum(nextDefaultReward - 1, ref RewardItem, ref ItemCount);
                                RewardItem = rewardDict[RewardItem];
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"[{i + 1}/{Config.CurrentConfig.Accounts.Count}] Claimed Daily Reward for {curAccount["displayName"]} | Day {nextDefaultReward}/{DailyRewardTable.GetRewardCount()} : {RewardItem} x{ItemCount} | Total V-Bucks: {vbuckCount}");
                                Console.ResetColor();
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[{i + 1}/{Config.CurrentConfig.Accounts.Count}] {curAccount["displayName"]} does not own save the world");
                        }

                        Thread.Sleep(500); // 429 prevention
                    }
                    break;
            }
            Console.WriteLine("=========");
            Console.WriteLine($"Total V-Bucks: {TotalVbucksCount} across {Config.CurrentConfig.Accounts.Count} accounts");
            Console.WriteLine("\n\n\n==============");
        }
    }


    /// <summary>
    /// This method will open a browser window and wait for the user to login to their Epic Games account.
    /// </summary>
    /// <param name="authCode"></param>
    /// <returns></returns>
    static async Task<JsonElement> GetAuthWithAuthCode(string authCode)
    {

        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/token");
        //Make string into base64
        string basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{fortniteIOSGameClient_ClientID}:{fortniteIOSGameClient_Secret}"));


        request.Headers.Add("Authorization", $"Basic {basicAuth}");
        var collection = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("code", authCode)
        };
        var content = new FormUrlEncodedContent(collection);
        request.Content = content;
        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            string jsonResponse = await response.Content.ReadAsStringAsync();
            //Convert to json object
            return JsonDocument.Parse(jsonResponse).RootElement;
        }
        else
        {
            Console.WriteLine("Error: " + response.StatusCode + "," + response.Content);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
            return default;
        }
    }

    private static async Task<JsonElement> GetAuthWithDeviceAuth(string accountID, string deviceId, string secret)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/token");
        //Make string into base64
        string basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{fortniteIOSGameClient_ClientID}:{fortniteIOSGameClient_Secret}"));


        request.Headers.Add("Authorization", $"Basic {basicAuth}");
        var collection = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "device_auth"),
            new("account_id", accountID),
            new("device_id", deviceId),
            new("secret", secret)
        };
        var content = new FormUrlEncodedContent(collection);
        request.Content = content;
        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            string jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(jsonResponse).RootElement; //Convert to json object
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            return JsonDocument.Parse("{\"error\": \"Login expired for " + accountID + "\"}").RootElement;
        else
            return JsonDocument.Parse("{\"error\": \"Error: " + response.StatusCode + "\"}").RootElement;
    }

    private static async Task<JsonElement> GetDeviceAuth(string bearerToken, string accountID)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://account-public-service-prod.ol.epicgames.com/account/api/public/account/{accountID}/deviceAuth");
        request.Headers.Add("Authorization", $"Bearer {bearerToken}");
        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            string jsonResponse = await response.Content.ReadAsStringAsync();
            //Convert to json object
            return JsonDocument.Parse(jsonResponse).RootElement;
        }
        else
        {
            Console.WriteLine("Error: " + response.StatusCode + "," + response.Content);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
            return default;
        }
    }

    private static async Task<JsonElement> ClaimLoginReward(string bearerToken, string accountID)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://fortnite-public-service-prod11.ol.epicgames.com/fortnite/api/game/v2/profile/{accountID}/client/ClaimLoginReward?profileId=campaign&rvn=-1");
        request.Headers.Add("Authorization", $"Bearer {bearerToken}");
        var content = new StringContent("{}", null, "application/json");
        request.Content = content;
        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            string jsonResponse = await response.Content.ReadAsStringAsync();
            //Convert to json object
            return JsonDocument.Parse(jsonResponse).RootElement;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return JsonDocument.Parse("{\"OwnsSTW\": false}").RootElement;
        }
        else
        {
            Console.WriteLine("Error: " + response.StatusCode + "," + response.Content);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
            return default;
        }
    }
    private static async Task<Modal.QueryProfileRoot> QueryProfile(string bearerToken, string accountID)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://fortnite-public-service-prod11.ol.epicgames.com/fortnite/api/game/v2/profile/{accountID}/client/QueryProfile?profileId=common_core&rvn=-1");
        request.Headers.Add("Authorization", $"Bearer {bearerToken}");
        var content = new StringContent("{}", null, "application/json");
        request.Content = content;
        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            string jsonResponse = await response.Content.ReadAsStringAsync();
            //Convert to json object
            return JsonSerializer.Deserialize<Modal.QueryProfileRoot>(jsonResponse);
        }
        else
        {
            Console.WriteLine("Error: " + response.StatusCode + "," + response.Content);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
            return default;
        }
    }

    private static int GetVbucks(Modal.QueryProfileRoot common_core)
    {
        int VBucks = 0;
        foreach (var item in common_core.profileChanges[0].profile.items)
        {
            //MtxComplimentary
            //MtxGiveaway
            //MtxPurchaseBonus
            //MtxPurchased

            if (item.Value.templateId.StartsWith("Currency:Mtx"))
            {
                if (item.Value != null && item.Value.quantity != null)
                {
                    VBucks += (int)item.Value.quantity;
                }
            }
        }
        return VBucks;
    }
}