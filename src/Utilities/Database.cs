using System;
using System.Data.OleDb;
using System.Data.SqlClient;

namespace Utilities
{
	public class Database
	{
		public static OleDbCommand OpenDatabase( string dbPath, out OleDbConnection accessConn)
		{ 
			OleDbCommand		accessCmd = null;
			accessConn = new OleDbConnection();

            accessConn.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data source=" + dbPath;
            accessConn.Open();

			accessCmd = accessConn.CreateCommand();

			return accessCmd;
		}

        public static OleDbCommand OpenDatabase2(string dbPath, out OleDbConnection accessConn)
        {
            OleDbCommand accessCmd = null;
            accessConn = new OleDbConnection();

            accessConn.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data source=" + dbPath;
            accessConn.Open();

            accessCmd = accessConn.CreateCommand();

            return accessCmd;
        }

        public static SqlCommand OpenSqlDatabase( string connectionString, out SqlConnection conn )
		{ 
			SqlCommand cmd = null;
			conn = new SqlConnection();

			conn.ConnectionString = connectionString;
			
			try
			{
				conn.Open();
			}
			catch ( Exception e )
			{
				Console.Write(e);
			}
			cmd = conn.CreateCommand();

			return cmd;
		}

		public static string GetRange ( string section )
		{
			string range = string.Empty;

			switch ( section )
			{
				case "nt":
					range = " BETWEEN 40 AND 66 ";
					break;
				case "g":
					range = " BETWEEN 40 AND 43 ";
					break;
				case "ga":
					range = " BETWEEN 40 AND 44 ";
					break;
				case "paul":
					range = " BETWEEN 45 AND 57 ";
					break;
				case "ge":
					range = " BETWEEN 58 AND 65 ";
					break;
				case "john":
					range = " IN ( 43, 62, 63, 64, 66 ) ";
					break;
				case "james":
					range = " = 59 ";
					break;
				case "peter":
					range = " BETWEEN 60 AND 61 ";
					break;
				case "luke":
					range = " IN ( 42, 44 ) ";
					break;
				default:
					range = "";
					break;
			}

			return range;
		}
	}
}
