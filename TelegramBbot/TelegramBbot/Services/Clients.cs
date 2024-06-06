using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TelegramBbot.Models;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using TelegramBbot.Models;
using TelegramBbot;

namespace TelegramBbot.ClientsAndMethods
{
    public class BrawlApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseAddress;

        public BrawlApiClient(string baseAddress)
        {
            _httpClient = new HttpClient();
            _baseAddress = baseAddress;
        }

        public async Task<Player> GetPlayerInfo(string playerId)
        {
            var response = await _httpClient.GetAsync($"{_baseAddress}/Brawl/player/{playerId}");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var player = JsonConvert.DeserializeObject<Player>(jsonResponse);

            return player;
        }

        public async Task<Club> GetClubInfo(string clubId)
        {
            var response = await _httpClient.GetAsync($"{_baseAddress}/Brawl/club/{clubId}");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var club = JsonConvert.DeserializeObject<Club>(jsonResponse);

            return club;
        }

        public async Task<List<EventItem>> GetEvents()
        {
            var response = await _httpClient.GetAsync($"{_baseAddress}/Brawl/events");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var events = JsonConvert.DeserializeObject<List<EventItem>>(jsonResponse);

            return events;
        }

        public async Task<ScrambledBrawler> GetScrambledBrawler()
        {
            var response = await _httpClient.GetAsync($"{_baseAddress}/Brawl/scrambled");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var scrambledBrawler = JsonConvert.DeserializeObject<ScrambledBrawler>(jsonResponse);

            return scrambledBrawler;
        }

        public async Task<string> AddPlayer(string playerId)
        {
            var response = await _httpClient.PostAsync($"{_baseAddress}/Player?playerTag={playerId}", null);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> UpdatePlayer(string playerId)
        {
            var response = await _httpClient.PutAsync($"{_baseAddress}/Player?playerTag={playerId}", null);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> DeletePlayer(string playerId)
        {
            var response = await _httpClient.DeleteAsync($"{_baseAddress}/Player?playerTag={playerId}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<Player>> GetAllPlayers()
        {
            var response = await _httpClient.GetAsync($"{_baseAddress}/Player");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var players = JsonConvert.DeserializeObject<List<Player>>(jsonResponse);

            return players;
        }
    }

    public static class BotMethods
    {
        private static Dictionary<long, ScrambledBrawler> gameStates = new Dictionary<long, ScrambledBrawler>();

        public static async Task GetPlayerInfo(TelegramBotClient botClient, BrawlApiClient brawlClient, long chatId, string playerId)
        {
            try
            {
                var player = await brawlClient.GetPlayerInfo(playerId);

                var playerInfo = $@"
Player: {player.Name}
Tag: {player.Tag}
Trophies: {player.Trophies}
Highest Trophies: {player.HighestTrophies}
Experience Level: {player.ExpLevel}
Experience Points: {player.ExpPoints}
Solo Victories: {player.SoloVictories}
Duo Victories: {player.DuoVictories}
Highest Power Play Points: {player.HighestPowerPlayPoints}
Best Robo Rumble Time: {player.BestRoboRumbleTime}
Best Time As Big Brawler: {player.BestTimeAsBigBrawler}
";

                await botClient.SendTextMessageAsync(chatId, playerInfo);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, $"Error: {ex.Message}");
            }
        }

        public static async Task ComparePlayers(TelegramBotClient botClient, BrawlApiClient brawlClient, long chatId, string playerId1, string playerId2)
        {
            try
            {
                var player1 = await brawlClient.GetPlayerInfo(playerId1);
                var player2 = await brawlClient.GetPlayerInfo(playerId2);

                string comparisonResult;
                if (player1.Trophies > player2.Trophies)
                {
                    comparisonResult = $"{player1.Name} has more trophies then {player2.Name}";
                }
                else
                {
                    comparisonResult = $"{player2.Name} has more trophies then {player1.Name}";
                }
                await botClient.SendTextMessageAsync(chatId, comparisonResult);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, $"Error: {ex.Message}");
            }
        }

        public static async Task GetClubInfo(TelegramBotClient botClient, BrawlApiClient brawlClient, long chatId, string clubId)
        {
            try
            {
                var club = await brawlClient.GetClubInfo(clubId);

                var clubInfo = $@"
Club Name: {club.Name}
Club Tag: {club.Tag}
Description: {club.Description}
Trophies: {club.Trophies}
Required Trophies: {club.RequiredTrophies}
Members:
{string.Join("\n", club.Members.Select(m => $"{m.Name} ({m.Tag}) - {m.Trophies} Trophies"))}";

                await botClient.SendTextMessageAsync(chatId, clubInfo);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, $"Error: {ex.Message}");
            }
        }

        public static async Task GetEvents(TelegramBotClient botClient, BrawlApiClient brawlClient, long chatId)
        {
            try
            {
                var scheduledEvents = await brawlClient.GetEvents();
                var eventsInfo = string.Join("\n\n", scheduledEvents.Select(e =>
                    $"Slot ID: {e.SlotId}\n" +
                    $"Event ID: {e.Event.Id}\n" +
                    $"Mode: {e.Event.Mode}\n" +
                    $"Map: {e.Event.Map}\n" +
                    $"Start Time: {e.StartTime}\n" +
                    $"End Time: {e.EndTime}"));

                await botClient.SendTextMessageAsync(chatId, eventsInfo);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, $"Error: {ex.Message}");
            }
        }

        public static async Task StartGuessingGame(TelegramBotClient botClient, BrawlApiClient brawlClient, long chatId)
        {
            var scrambledBrawler = await brawlClient.GetScrambledBrawler();
            gameStates[chatId] = scrambledBrawler;

            await botClient.SendTextMessageAsync(chatId, $"Guess Brawler: {scrambledBrawler.scrambledName}");
        }

        public static async Task HandleGuess(TelegramBotClient botClient, long chatId, string guess)
        {
            if (!gameStates.ContainsKey(chatId))
            {
                await botClient.SendTextMessageAsync(chatId, "No active game. Start a new game with /guessbrawler.");
                return;
            }

            var scrambledBrawler = gameStates[chatId];
            if (guess.Equals(scrambledBrawler.originalName, StringComparison.OrdinalIgnoreCase))
            {
                await botClient.SendTextMessageAsync(chatId, $"Congratulations! You guessed correctly. The brawler is {scrambledBrawler.originalName}. Do you want to play again? Type /guessbrawler to start a new game or /exit to stop playing.");
                gameStates.Remove(chatId);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, $"Wrong guess. Try again.");
            }
        }

        public static async Task ExitGame(TelegramBotClient botClient, long chatId)
        {
            if (gameStates.ContainsKey(chatId))
            {
                gameStates.Remove(chatId);
                await botClient.SendTextMessageAsync(chatId, "Game exited.");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "No active game to exit.");
            }
        }

        public static async Task AddPlayer(TelegramBotClient botClient, BrawlApiClient brawlClient, long chatId, string playerId)
        {
            try
            {
                var response = await brawlClient.AddPlayer(playerId);
                await botClient.SendTextMessageAsync(chatId, response);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, $"Error: {ex.Message}");
            }
        }

        public static async Task UpdatePlayer(TelegramBotClient botClient, BrawlApiClient brawlClient, long chatId, string playerId)
        {
            try
            {
                var response = await brawlClient.UpdatePlayer(playerId);
                await botClient.SendTextMessageAsync(chatId, response);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, $"Error: {ex.Message}");
            }
        }

        public static async Task DeletePlayer(TelegramBotClient botClient, BrawlApiClient brawlClient, long chatId, string playerId)
        {
            try
            {
                var response = await brawlClient.DeletePlayer(playerId);
                await botClient.SendTextMessageAsync(chatId, response);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, $"Error: {ex.Message}");
            }
        }

        public static async Task GetAllPlayers(TelegramBotClient botClient, BrawlApiClient brawlClient, long chatId)
        {
            try
            {
                var players = await brawlClient.GetAllPlayers();
                var playersInfo = string.Join("\n\n", players.Select(p =>
                    $"Player: {p.Name}\n" +
                    $"Tag: {p.Tag}\n" +
                    $"Trophies: {p.Trophies}\n" +
                    $"Highest Trophies: {p.HighestTrophies}\n" +
                    $"Experience Level: {p.ExpLevel}\n" +
                    $"Experience Points: {p.ExpPoints}\n" +
                    $"Solo Victories: {p.SoloVictories}\n" +
                    $"Duo Victories: {p.DuoVictories}\n" +
                    $"Highest Power Play Points: {p.HighestPowerPlayPoints}\n" +
                    $"Best Robo Rumble Time: {p.BestRoboRumbleTime}\n" +
                    $"Best Time As Big Brawler: {p.BestTimeAsBigBrawler}"
                ));

                await botClient.SendTextMessageAsync(chatId, playersInfo);
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, $"Error: {ex.Message}");
            }
        }
    }
}
