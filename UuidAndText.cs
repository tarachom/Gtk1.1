
using NpgsqlTypes;

namespace GtkTest
{
	/// <summary>
	/// Композитний тип даних
	/// </summary>
	public class UuidAndText
	{
		public UuidAndText()
		{
			Text = "";
		}

		public UuidAndText(Guid uuid)
		{
			Uuid = uuid;
			Text = "";
		}

		public UuidAndText(Guid uuid, string text)
		{
			Uuid = uuid;
			Text = text;
		}

		/// <summary>
		/// Вказівник
		/// </summary>
		[PgName("uuid")]
		public Guid Uuid { get; set; }

		/// <summary>
		/// Додаткова інформація
		/// </summary>
		[PgName("text")]
		public string Text { get; set; }

		/// <summary>
		/// Дані у XML форматі
		/// </summary>
		/// <returns></returns>
		public string ToXml()
		{
			return $"<uuid>{Uuid}</uuid><text>{Text}</text>";
		}

		public override string ToString()
		{
			return $"('{Uuid}', '{Text}')";
		}
	}
}