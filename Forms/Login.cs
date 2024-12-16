﻿using PrenburtisBot.Attributes;
using Telegram.Bot.Types;
using TelegramBotBase.Base;
using TelegramBotBase.Form;

namespace PrenburtisBot.Forms
{
	[BotCommandChat("Авторизация пользователя", null)]
	internal class Login : FormBase
	{
		private static WTelegram.Client? _staticClient = null;
		private WTelegram.Client? _client = null;
		private string? _loginInfo = null;

		private async Task<string?> RenderAsync(MessageResult message)
		{
			if (_staticClient is not null)
				return "Вы уже были авторизованы";

			if (_client is null)
			{
				const string LOGIN_API_ID = "LOGIN_API_ID";
				if (!int.TryParse(Environment.GetEnvironmentVariable(LOGIN_API_ID), out int apiId))
					return $"Значение переменной окружения {LOGIN_API_ID} не является валидным App api_id";

				const string LOGIN_API_HASH = "LOGIN_API_HASH";
				if (Environment.GetEnvironmentVariable(LOGIN_API_HASH) is not string apiHash)
					return $"Значение переменной окружения {LOGIN_API_HASH} не является валидным App api_hash";

				const string LOGIN_SESSION_PATH = "LOGIN_SESSION_PATH";
				if (Environment.GetEnvironmentVariable(LOGIN_SESSION_PATH) is not string sessionPathname)
					return $"Значение переменной окружения {LOGIN_SESSION_PATH} не является валидным путём";

				WTelegram.Helpers.Log = (int logLevel, string log) => { if (logLevel > (int)Microsoft.Extensions.Logging.LogLevel.Debug) Console.WriteLine(log); };
				_client ??= new(apiId, apiHash, sessionPathname);
			}
			
			if (string.IsNullOrEmpty(_loginInfo))
			{
				const string LOGIN_PHONE_NUMBER = "LOGIN_PHONE_NUMBER";
				if (Environment.GetEnvironmentVariable(LOGIN_PHONE_NUMBER) is not string phoneNumber)
					return $"Значение переменной окружения {LOGIN_PHONE_NUMBER} не является валидным номером телефона";

				_loginInfo = phoneNumber;
			}

			if ((await _client.Login(_loginInfo)) is string need)
			{
				PromptDialog promptDialog = new($"Введите {need}".Replace("_", "\\_"));
				promptDialog.Completed += (sender, args) =>
				{
					const string VERIFICATION_CODE_NEED = "verification_code";
					_loginInfo = need == VERIFICATION_CODE_NEED ? promptDialog.Value.Replace(" ", "") : promptDialog.Value;
				};
				await this.OpenModal(promptDialog);

				return null;
			}

			_staticClient = _client;
			LoginEvent?.Invoke(this.GetType(), _staticClient);
			return $"Вы успешно авторизовались как {_staticClient.User.first_name}";
		}

		public delegate void LoginEventHandler(Type? type, WTelegram.Client client);
		public static event LoginEventHandler? LoginEvent;

		public override async Task Render(MessageResult message)
		{
			string? text;
			try
			{
				text = await this.RenderAsync(message);
			}
			catch (Exception e)
			{
				text = e.Message;
			}

			if (!string.IsNullOrEmpty(text))
				await this.Device.Send(text.Replace("_", "\\_"));
		}
	}
}