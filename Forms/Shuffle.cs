﻿using PrenburtisBot.Attributes;
using PrenburtisBot.Types;

namespace PrenburtisBot.Forms
{
	[BotCommand("Перемешать игроков в командах")]
	internal class Shuffle : BotCommandFormBase
	{
		protected override async Task<string?> RenderAsync(string[] args)
		{
			string? courtId = args.Length == 1 ? args[0] : null;
			Court court = Courts.GetById(courtId);
			if (court.UserId != this.Device.DeviceId)
				return "Только создатель площадки может перемешивать игроков в командах";

			court.Shuffle();
			await this.NavigateTo(new Players(), courtId);
			return null;
		}
	}
}