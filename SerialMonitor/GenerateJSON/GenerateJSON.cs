using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace GenerateJSON
{
    class GenerateJSON
    {
        // TODO: Fetch DB connection from common storage
        static string connectionString = "Server=192.168.1.32;Database=garden;User Id=garden_collector;Password=gnome123!@#;";
        static string past24HourQuery = "SELECT TOP 96 AVG(NUMVAL) AS TEMPERATURE, STEP FROM SENSOR_AGG WHERE DEVICE = @DEVICE AND SENSOR = @SENSOR GROUP BY STEP ORDER BY MAX(COLLECTED) DESC";

        static void Main(string[] args)
        {
            // past 24 hours: 96 data points, 24 labels
            GenerateJSON app = new GenerateJSON();

            string past24Temp = app.generatePast24Hours("GARDEN1", "DHT221");
        }

        private string generatePast24Hours(string device, string sensor)
        {
            SqlConnection conn = null;
            string[] labels = null;
            string[] data = null;
            string retVal = string.Empty;

            try
            {
                using (conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    List<string> labelList = new List<string>();
                    List<string> valueList = new List<string>();

                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = past24HourQuery;

                        cmd.Parameters.Add(new SqlParameter("@DEVICE", device));
                        cmd.Parameters.Add(new SqlParameter("@SENSOR", sensor));

                        using (SqlDataReader results = cmd.ExecuteReader())
                        {
                            while (results.Read())
                            {
                                valueList.Add(string.Format("{0}", results[0]));
                                labelList.Add(((int)results[1] % 4 == 0 ? string.Format("{0,2}:00", (int)results[1] / 4) : string.Empty));
                            }
                        }
                     }

                    labels = labelList.ToArray();
                    data = valueList.ToArray();
                }

                retVal = string.Format("{{ labels : [\"{1}\"], datasets : [{{data : [{0}]}}] }}", string.Join(",", data), string.Join("\",\"", labels));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                try
                {
                    if (conn != null)
                    {
                        conn.Close();
                    }
                }
                catch (Exception)
                {
                    // It's over, all over
                }
            }
            return retVal;
        }

    }
}
