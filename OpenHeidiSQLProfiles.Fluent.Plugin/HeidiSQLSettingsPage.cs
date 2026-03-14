using Blast.API.Settings;
using Blast.Core.Objects;

namespace OpenHeidiSQLProfiles.Fluent.Plugin
{
	public sealed class HeidiSQLSettingsPage : SearchApplicationSettingsPage
	{
		public HeidiSQLSettingsPage(SearchApplicationInfo info) : base(info) { }

		[Setting(
			Name = "HeidiSQL Location",
			Description = "Path to HeidiSQL executable or HeidiSQL folder. If empty, the plugin will auto-detect it.",
			IconGlyph = "\uF12B"
		)]
		public string HeidiSQLPath { get; set; } = string.Empty;
	}
}