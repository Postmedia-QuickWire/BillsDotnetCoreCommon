using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace Common.Classes
{
	/// <summary>
	/// This class can parse a CSV or any type of delimited file
	/// It can also parse a fixed width column text data file
	/// </summary>
	public class ColumnConfig
	{
		public string Name { get; set; }

		// used by fixed width columns
		public int Offset { get; set; }
		public int Width { get; set; }

		// used for delimited columns
		public int Ordinal { get; set; }

	}
	public class ParserConfig
	{

		public string Delimiter { get; set; } = ",";
		public bool isFixedWidth { get; set; } = false;
		public Encoding encoding_in { get; set; } = Encoding.UTF8;
		public Encoding encoding_out { get; set; } = Encoding.UTF8;
		public bool CheckForByteMarks { get; set; } = false; // good only for UTF encodings

		/// <summary>
		/// one's based start row
		/// </summary>
		public int StartRow { get; set; } = 0;
		public int MaxRows { get; set; } = 0;

		// if isFixedWidth then this MUST be set
		public void AddColumn(string name, int? offset, int? width, int? display_width)
        {
			if (isFixedWidth)
			{
				if (offset != null && width != null && width != 0)
					AddFixedColumn(name, (int)offset, (int)width);
			}
			else if (offset != null)
				AddDelimitedColumn(name, (int)offset, (int)display_width);
		}
		protected void AddFixedColumn(string name, int offset, int width)
		{
			Columns.Add(new ColumnConfig() { Name = name, Offset = offset, Width = width });
		}
		protected void AddDelimitedColumn(string name, int ordinal, int display_width)
		{
			Columns.Add(new ColumnConfig() { Name = name, Ordinal = ordinal, Width= display_width });
		}
		public List<ColumnConfig> Columns { get; set; } = new List<ColumnConfig>();
	}



	public class TextDataParser<TRowObj>
	{

		public ParserConfig Config { get; set; }

		public List<TRowObj> Rows { get; set; } = new List<TRowObj>();

		public List<string> LineBuf { get; set; } = new List<string>();

		// keyed on column name - case sensitive
		public Dictionary<string, ColumnConfig> Columns { get; } = new Dictionary<string, ColumnConfig>();
		public TextDataParser(ParserConfig config = null)
		{
			Config = config;
			if (Config == null)
				Config = new ParserConfig();

			int ord = 0;
			foreach (var cc in Config.Columns)
			{
				var new_cc = new ColumnConfig();
				new_cc.Name = cc.Name;
				new_cc.Width = cc.Width; // updated at load time (if delimited)
										 //new_cc.Offset = cc.Offset; no need
				new_cc.Ordinal = ord++;
				Columns[cc.Name] = new_cc;
			}
		}


		//helper - get column row data by name
		public string GetColumn(string name, string[] row, string def_val = null)
		{
			ColumnConfig cc;
			if (Columns?.TryGetValue(name, out cc) ?? false)
			{
				return row[cc.Ordinal];
			}
			return def_val;
		}


		public void CheckMaxColumnWidths(Func<TRowObj, string[]> getRow)
		{
			if (!Config.isFixedWidth)
			{
				int[] widths = new int[Columns.Count];
				foreach (var trow in Rows)
				{
					int ind = 0;
					foreach (var col in getRow(trow))
					{
						if (col.Length > widths[ind])
							widths[ind] = col.Length;
						ind++;
					}
					
				}
				foreach (var cc in Columns.Values)
				{
					cc.Width = widths[cc.Ordinal];
				}
			}
		}

		public void Parse(string filename, Func<int, string[], TRowObj> OnNewRow)
		{
			try
			{
				LineBuf.Clear();
				using (StreamReader reader = new StreamReader(filename, Config.encoding_in, Config.CheckForByteMarks))
				{
					int row_cnt = 1; // orig file cnt for error msgs
					int cnt = 0; // added rows cnt
					try
					{
						string lineBuf = reader.ReadLine();
						while (lineBuf != null)
						{
							LineBuf.Add(lineBuf); // save the orig

							if (row_cnt >= Config.StartRow)
							{
								string[] fields = null;
								if (Config.isFixedWidth)
								{
									fields = ParseFixedLine(lineBuf);
								}
								else
								{
									fields = ParseDelimitedLine(lineBuf);

								}

								var rec = OnNewRow(cnt, fields);
								if (rec != null)
								{
									Rows.Add(rec);
									cnt++;
									if (Config.MaxRows > 0 && cnt >= Config.MaxRows)
										break;
								}
							}
							row_cnt++;
							lineBuf = reader.ReadLine();
						}
					}
					catch (Exception e)
					{
						throw new Exception($"Error on line {row_cnt}, err:{e.Message}, file:{filename}");
					}
				}
			}
			catch (Exception e)
			{
				throw new Exception($"Error opening text data file, err:{e.Message}, file:{filename}");
			}

		}

		private string[] ParseFixedLine(string lineBuf)
		{
			List<string> cols = new List<string>();

			foreach (var cc in Config.Columns)
			{
				cols.Add(lineBuf.Substring(cc.Offset, cc.Width).Trim());
			}

			return cols.ToArray<string>();
		}


		private string[] ParseDelimitedLine(string lineBuf)
		{
			List<string> cols = new List<string>();
			// ah I thought there'd be more to it...
			// maybe handle more than one delimiter; lineBuf.Split(string[])  or mutli chars lineBuf.Split(char[])
			var all_cols = lineBuf.Split(Config.Delimiter, StringSplitOptions.TrimEntries);
			foreach (var cc in Config.Columns)
			{
				cols.Add(all_cols[cc.Ordinal]);
			}

			return cols.ToArray<string>();
		}

	}
}
