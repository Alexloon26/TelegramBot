using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using TelegramBbot.ClientsAndMethods;
using TelegramBbot.Models;

namespace TelegramBbot
{
    class Program
    {
        private static string _token = "6607240329:AAFUeBpBnWNX7H6qqqrcaYF4PFoGYyo5TZQ";

        static async Task Main(string[] args)
        {
            var botClient = new TelegramBotClient(_token);
            var brawlClient = new BrawlApiClient("https://localhost:7257");

            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Bot {me.Username} is running.");

            int offset = 0;

            while (true)
            {
                var updates = await botClient.GetUpdatesAsync(offset);

                foreach (var update in updates)
                {
                    if (update.Message != null)
                    {
                        var message = update.Message;
                        var chatId = message.Chat.Id;

                        Console.WriteLine($"Received a message from {chatId}: {message.Text}");

                        try
                        {
                            if (message.Text.StartsWith("/guessbrawler"))
                            {
                                await BotMethods.StartGuessingGame(botClient, brawlClient, chatId);
                            }
                            else if (message.Text.StartsWith("/exit"))
                            {
                                await BotMethods.ExitGame(botClient, chatId);
                            }
                            else if (message.Text.StartsWith("/getinfo"))
                            {
                                var playerId = message.Text.Split(' ').ElementAtOrDefault(1);
                                if (!string.IsNullOrEmpty(playerId))
                                {
                                    await BotMethods.GetPlayerInfo(botClient, brawlClient, chatId, playerId);
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chatId, "Please provide a player ID: /getinfo {playerId}");
                                }
                            }
                            else if (message.Text.StartsWith("/compare"))
                            {
                                var playerIds = message.Text.Split(' ').Skip(1).ToArray();
                                if (playerIds.Length == 2)
                                {
                                    await BotMethods.ComparePlayers(botClient, brawlClient, chatId, playerIds[0], playerIds[1]);
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chatId, "Please provide two player IDs: /compare {playerId1} {playerId2}");
                                }
                            }
                            else if (message.Text.StartsWith("/getclubinfo"))
                            {
                                var clubId = message.Text.Split(' ').ElementAtOrDefault(1);
                                if (!string.IsNullOrEmpty(clubId))
                                {
                                    await BotMethods.GetClubInfo(botClient, brawlClient, chatId, clubId);
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chatId, "Please provide a club ID: /getclubinfo {clubId}");
                                }
                            }
                            else if (message.Text == "/getevent")
                            {
                                await BotMethods.GetEvents(botClient, brawlClient, chatId);
                            }
                            else if (message.Text.StartsWith("/postplayer"))
                            {
                                var playerId = message.Text.Split(' ').ElementAtOrDefault(1);
                                if (!string.IsNullOrEmpty(playerId))
                                {
                                    await BotMethods.AddPlayer(botClient, brawlClient, chatId, playerId);
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chatId, "Please provide a player ID: /postplayer {playerId}");
                                }
                            }
                            else if (message.Text.StartsWith("/putplayer"))
                            {
                                var playerId = message.Text.Split(' ').ElementAtOrDefault(1);
                                if (!string.IsNullOrEmpty(playerId))
                                {
                                    await BotMethods.UpdatePlayer(botClient, brawlClient, chatId, playerId);
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chatId, "Please provide a player ID: /putplayer {playerId}");
                                }
                            }
                            else if (message.Text.StartsWith("/deleteplayer"))
                            {
                                var playerId = message.Text.Split(' ').ElementAtOrDefault(1);
                                if (!string.IsNullOrEmpty(playerId))
                                {
                                    await BotMethods.DeletePlayer(botClient, brawlClient, chatId, playerId);
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chatId, "Please provide a player ID: /deleteplayer {playerId}");
                                }
                            }
                            else if (message.Text == "/getallplayers")
                            {
                                await BotMethods.GetAllPlayers(botClient, brawlClient, chatId);
                            }
                            else if (message.Text.StartsWith("/"))
                            {
                                await botClient.SendTextMessageAsync(chatId, "Unknown command. Please type /guessbrawler to start the guessing game.");
                            }
                            else
                            {
                                await BotMethods.HandleGuess(botClient, chatId, message.Text);
                            }
                        }
                        catch (ApiRequestException ex) when (ex.ErrorCode == 403)
                        {
                            Console.WriteLine($"Bot was blocked by the user {chatId}: {ex.Message}");
                        }
                        offset = update.Id + 1;
                    }
                }
            }
        }
    }
}