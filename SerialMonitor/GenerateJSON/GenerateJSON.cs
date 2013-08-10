// <copyright file="GenerateJSON.cs" company="Hermit Colony">
// Copyright (c) 2013 All Right Reserved, http://ipatch.ca/
//
// This source is subject to no license whatsoever.  If you hurt 
// yourself using this code, you shouldn't do that.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Malcolm Walker</author>
// <email>evil.overlord.esq@gmail.com</email>
// <date>2013-07-20</date>
// <summary>Contains an appliance application for extracting and  base, abstract class for an AuthorisationPolicyProvider</summary>

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
        static string connectionStringTemplate = "Server=192.168.1.32;Database=garden;User Id={0};Password={1}";
        static string past24HourQuery = "SELECT TOP 96 AVG(NUMVAL) AS VALUE, STEP FROM SENSOR_AGG WHERE DEVICE = @DEVICE AND SENSOR = @SENSOR GROUP BY STEP ORDER BY MAX(COLLECTED) DESC";
        static string past30DayQuery = "SELECT TOP 30 MIN(NUMVAL) AS MINVAL, MAX(NUMVAL) AS MAXVAL,	AVG(NUMVAL) AS AVGVAL, CONVERT(date,COLLECTED) AS DATE FROM SENSORDATA WHERE DEVICE = @DEVICE AND SENSOR = @SENSOR GROUP BY CONVERT(date,COLLECTED) ORDER BY MAX(COLLECTED) DESC";

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine(string.Format("Usage: {0} <user> <password>", System.AppDomain.CurrentDomain.FriendlyName));
                return;
            }
            // past 24 hours: 96 data points, 24 labels
            GenerateJSON app = new GenerateJSON();
            string connectionString = string.Format(connectionStringTemplate, args[0], args[1]);

            string past24Temp = app.generatePast24Hours("GARDEN1", "DHT221", connectionString, "temperatureData");
            Console.WriteLine(past24Temp);
            string past24Humidity = app.generatePast24Hours("GARDEN1", "DHT220", connectionString, "humidityData");
            Console.WriteLine(past24Humidity);
            string monthTemp = app.generate30DaySummary("GARDEN1", "DHT221", connectionString, "temp30dayData");
            Console.WriteLine(monthTemp);
            string monthHumidity = app.generate30DaySummary("GARDEN1", "DHT220", connectionString, "humidity30dayData");
            Console.WriteLine(monthHumidity);
        }

        private string generatePast24Hours(string device, string sensor, string connectionString, string variableName)
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

                    labels = labelList.Reverse<string>().ToArray();
                    data = valueList.Reverse<string>().ToArray();
                }

                retVal = string.Format("var {2} = {{ labels : [\"{1}\"], datasets : [{{data : [{0}]}}] }};", string.Join(",", data), string.Join("\",\"", labels), variableName);
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

        private string generate30DaySummary(string device, string sensor, string connectionString, string variableName)
        {
            SqlConnection conn = null;
            string[] labels = null;
            string[] mindata = null;
            string[] maxdata = null;
            string[] avgdata = null;
            string retVal = string.Empty;

            try
            {
                using (conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    List<string> labelList = new List<string>();
                    List<string> minValueList = new List<string>();
                    List<string> maxValueList = new List<string>();
                    List<string> avgValueList = new List<string>();

                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = past30DayQuery;

                        cmd.Parameters.Add(new SqlParameter("@DEVICE", device));
                        cmd.Parameters.Add(new SqlParameter("@SENSOR", sensor));

                        using (SqlDataReader results = cmd.ExecuteReader())
                        {
                            while (results.Read())
                            {
                                minValueList.Add(string.Format("{0}", results[0]));
                                maxValueList.Add(string.Format("{0}", results[1]));
                                avgValueList.Add(string.Format("{0}", results[2]));
                                labelList.Add(string.Format("{0}", results[3]));
                            }
                        }
                    }

                    while (labelList.Count < 30)
                    {
                        // pad
                        minValueList.Add(string.Empty);
                        maxValueList.Add(string.Empty);
                        avgValueList.Add(string.Empty);
                        labelList.Add(string.Empty);
                    }

                    labels = labelList.Reverse<string>().ToArray();
                    mindata = minValueList.Reverse<string>().ToArray();
                    maxdata = maxValueList.Reverse<string>().ToArray();
                    avgdata = avgValueList.Reverse<string>().ToArray();
                }

                retVal = string.Format("var {0} = {{ labels : [\"{1}\"], datasets : [{{fillColor:\"rgba({2},0.33)\",strokeColor:\"rgba({2},1)\",data:[{3}]}}," +
                    "{{fillColor:\"rgba({4},0.5)\",strokeColor:\"rgba({4},1)\",data:[{5}]}}," +
                    "{{fillColor:\"rgba({6},0.5)\",strokeColor:\"rgba({6},1)\",data:[{7}]}}] }};",
                    variableName,
                    string.Join("\",\"", labels),
                    "151,187,205",
                    string.Join(",", mindata),
                    "220,220,220",
                    string.Join(",", avgdata),
                    "205,187,151",
                    string.Join(",", maxdata)
                    );
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
