using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Classes
{
	public static class AccentNormalizer
	{
		// ripped out of lucene...

		/// <summary>
		/// The plain letter equivalent of the accented letters.
		/// </summary>
		private const string PLAIN_ASCII = "AaEeIiOoUu" + // grave
			"AaEeIiOoUuYy" + // acute
			"AaEeIiOoUuYy" + // circumflex
			"AaOoNn" + // tilde
			"AaEeIiOoUuYy" + // umlaut
			"Aa" + // ring
			"Cc" + // cedilla
			"OoUu"; // double acute

		/// <summary>
		/// Unicode characters corresponding to various accented letters. For example: \u00DA is U acute etc...
		/// </summary>
		private const string UNICODE = "\u00C0\u00E0\u00C8\u00E8\u00CC\u00EC\u00D2\u00F2\u00D9\u00F9" +
				"\u00C1\u00E1\u00C9\u00E9\u00CD\u00ED\u00D3\u00F3\u00DA\u00FA\u00DD\u00FD" +
				"\u00C2\u00E2\u00CA\u00EA\u00CE\u00EE\u00D4\u00F4\u00DB\u00FB\u0176\u0177" +
				"\u00C3\u00E3\u00D5\u00F5\u00D1\u00F1" +
				"\u00C4\u00E4\u00CB\u00EB\u00CF\u00EF\u00D6\u00F6\u00DC\u00FC\u0178\u00FF" +
				"\u00C5\u00E5" + "\u00C7\u00E7" + "\u0150\u0151\u0170\u0171";


		private static Dictionary<char, char> _accent2ascii = new Dictionary<char, char>()
		{
			{'À', 'A'},
			{'à', 'a'},
			{'È', 'E'},
			{'è', 'e'},
			{'Ì', 'I'},
			{'ì', 'i'},
			{'Ò', 'O'},
			{'ò', 'o'},
			{'Ù', 'U'},
			{'ù', 'u'},
			{'Á', 'A'},
			{'á', 'a'},
			{'É', 'E'},
			{'é', 'e'},
			{'Í', 'I'},
			{'í', 'i'},
			{'Ó', 'O'},
			{'ó', 'o'},
			{'Ú', 'U'},
			{'ú', 'u'},
			{'Ý', 'Y'},
			{'ý', 'y'},
			{'Â', 'A'},
			{'â', 'a'},
			{'Ê', 'E'},
			{'ê', 'e'},
			{'Î', 'I'},
			{'î', 'i'},
			{'Ô', 'O'},
			{'ô', 'o'},
			{'Û', 'U'},
			{'û', 'u'},
			{'Ŷ', 'Y'},
			{'ŷ', 'y'},
			{'Ã', 'A'},
			{'ã', 'a'},
			{'Õ', 'O'},
			{'õ', 'o'},
			{'Ñ', 'N'},
			{'ñ', 'n'},
			{'Ä', 'A'},
			{'ä', 'a'},
			{'Ë', 'E'},
			{'ë', 'e'},
			{'Ï', 'I'},
			{'ï', 'i'},
			{'Ö', 'O'},
			{'ö', 'o'},
			{'Ü', 'U'},
			{'ü', 'u'},
			{'Ÿ', 'Y'},
			{'ÿ', 'y'},
			{'Å', 'A'},
			{'å', 'a'},
			{'Ç', 'C'},
			{'ç', 'c'},
			{'Ő', 'O'},
			{'ő', 'o'},
			{'Ű', 'U'},
			{'ű', 'u'},
		};


		public static void make_dict()
		{
			int ind = 0;
			StringBuilder buf = new StringBuilder();
			foreach (var uc in UNICODE)
			{
				buf.AppendLine($"{{'{uc}', '{PLAIN_ASCII[ind++]}'}},");
			}
			var x = buf.ToString();
		}

		// I timed it at about 20% faster than the old method
		public static string Normalize(string accentedWord)
		{
			var sb = new StringBuilder();
			char nc;
			foreach (var c in accentedWord.AsSpan())
			{
				if (_accent2ascii.TryGetValue(c, out nc))
				{
					sb.Append(nc);
				}
				else
				{
					sb.Append(c);
				}
			}
			return sb.ToString();
		}

		public static string Normalize_old(string accentedWord)
		{
			var sb = new StringBuilder();
			foreach (var c in accentedWord.AsSpan())
			{
				int pos = UNICODE.IndexOf(c);
				if (pos > -1)
				{
					sb.Append(PLAIN_ASCII[pos]);
				}
				else
				{
					sb.Append(c);
				}
			}
			return sb.ToString();
		}
	}
}
