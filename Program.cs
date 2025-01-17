﻿using TelegramBotBase.Builder;
using PrenburtisBot.Forms;
using TelegramBotBase.Args;
using TelegramBotBase.Form;
using PrenburtisBot.Types;
using Microsoft.Data.Sqlite;
using Telegram.Bot;
using TelegramBotBase;
using PrenburtisBot.Attributes;
using System.Reflection;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PrenburtisBot
{
    internal class Program
	{
		private static string? GetFilePath(string variable)
		{
			if (Environment.GetEnvironmentVariable(variable) is string path)
			{
				if (System.IO.File.Exists(path))
					return path;
				else
					Console.WriteLine($"Не существует файл {path}");
			}
			else
				Console.WriteLine($"Отсутствует переменная окружения {variable}");

			return null;
		}

		private static async Task Main(string[] args)
		{
			Session.Path = Environment.GetEnvironmentVariable("SESSION_PATH");
			try
			{
				if (Session.Read())
					Console.WriteLine($"Добавлены данные сессии из файла {Session.Path}");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}

			if (GetFilePath("TEAMS_NAMES") is string path)
			{
				using StreamReader streamReader = new(path);
				Console.WriteLine($"Добавлены имена команд ({Team.ReadNames(streamReader)}) из файла {path}");
			}

			if (GetFilePath("PRENBURTIS_DATA_BASE") is string dataSource) {
				SqliteConnectionStringBuilder connectionStringBuilder = new() { Mode = SqliteOpenMode.ReadOnly, DataSource = dataSource };
				using SqliteConnection connection = new(connectionStringBuilder.ConnectionString);
				try
				{
					connection.Open();
					Console.WriteLine($"Установлено соединение с {connection.DataSource}");

					SqliteCommand command = new("SELECT telegram_id, first_name, rank, rating FROM users", connection);
					SqliteDataReader reader = command.ExecuteReader();
					Console.WriteLine($"Добавлено ранговых игроков: {Users.Read(reader)}");

				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}

			const string API_KEY = "API_KEY";
			string? apiKey = Environment.GetEnvironmentVariable(API_KEY);
			if (string.IsNullOrEmpty(apiKey))
			{
				Console.WriteLine("Невозможно запустить бота, т.к. в переменных окружения отсутствует " + API_KEY);
				Console.ReadLine();
				return;
			}

			BotBase bot = BotBaseBuilder.Create()
				.WithAPIKey(apiKey)
				.DefaultMessageLoop()
				.WithStartForm<Start>()
				.NoProxy()
				.CustomCommands(action =>
				{
					foreach (var command in Commands.GetCommands())
						action.Add(command.Key, command.Value);
				})
				.NoSerialization()
				.UseRussian()
				.UseSingleThread()
				.Build();

			bot.Exception += (object? sender, SystemExceptionEventArgs args) =>
			{
				Console.WriteLine($"На форме {args.Device.ActiveForm.GetType().Name} при обработке \"{args.Command}\" на устройстве {args.DeviceId} возникла ошибка:"
					+ Environment.NewLine + args.Error.ToString() + Environment.NewLine);
			};

			bot.BotCommand += async (object sender, BotCommandEventArgs args) =>
			{
				FormBase newForm = Commands.GetNewForm(args.Command.StartsWith('/') ? args.Command[1..] : args.Command);
				Type type = newForm.GetType();
				if (type.GetCustomAttribute<BotCommandAttribute>() is BotCommandAttribute commandAttribute)
					switch (commandAttribute.Scope)
					{
						case BotCommandScopeType.Chat when commandAttribute is BotCommandChatAttribute attribute 
							&& Commands.GetChatId(attribute, type) is ChatId chatId && chatId != args.DeviceId:

						case BotCommandScopeType.AllChatAdministrators when args.Device.IsGroup && args.OriginalMessage.From?.Id is long userId
							&& ! new List<ChatMember>(await args.Device.Api(async (botClient) => await botClient.GetChatAdministratorsAsync(args.DeviceId))).Any((ChatMember member) => member.User.Id == userId):

							Console.WriteLine($"Предотвращён вызов формы {type.Name} пользователем {args.OriginalMessage.From?.FirstName} в чате \"{args.Device.GetChatTitle()}\"");
							return;

					};

				await args.Device.ActiveForm.NavigateTo(newForm);
			};

			TextMessage.GetStartForm = () => new Start();
			Login.LoginEvent += (Type? type, WTelegram.Client client) => {
				AddPlayers.TelegramClient = client;
				CreateCourt.TelegramClient = client;
			};
			await bot.UploadBotCommands();

			await bot.Start();
			Console.WriteLine($"Бот @{(await bot.Client.TelegramClient.GetMeAsync()).Username} запущен и работает");
			while (true) { }
		}
	}
}
