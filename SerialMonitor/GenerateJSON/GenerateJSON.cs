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
using System.Diagnostics;

namespace GenerateJSON
{
    class GenerateJSON
    {
        // TODO: Fetch DB connection from common storage
        static string connectionStringTemplate = "Server=192.168.1.32;Database=garden;User Id={0};Password={1}";
        static string past24HourQuery = "SELECT TOP 96 AVG(NUMVAL) AS VALUE, STEP FROM SENSOR_AGG WHERE DEVICE = @DEVICE AND SENSOR = @SENSOR GROUP BY STEP ORDER BY MAX(COLLECTED) DESC";
        static string past30DayQuery = "SELECT TOP 30 MAX(NUMVAL) AS MAXVAL, AVG(NUMVAL) AS AVGVAL, MIN(NUMVAL) AS MINVAL, CONVERT(date,COLLECTED) AS DATE FROM SENSORDATA WHERE DEVICE = @DEVICE AND SENSOR = @SENSOR GROUP BY CONVERT(date,COLLECTED) ORDER BY MAX(COLLECTED) DESC";

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

            string past24Temp = app.generatePast24Hours(connectionString, "GARDEN1", "DHT221", "temperatureData");
            Console.WriteLine(past24Temp);
            string past24Humidity = app.generatePast24Hours(connectionString, "GARDEN1", "DHT220",  "humidityData");
            Console.WriteLine(past24Humidity);
            string past24Light = app.generatePast24Hours(connectionString, "GARDEN1", "LIGHT0",  "lightData", -1024.0, -1.0);
            Console.WriteLine(past24Light);
            string monthTemp = app.generate30DaySummary(connectionString, "GARDEN1", "DHT221", "temp30dayData");
            Console.WriteLine(monthTemp);
            string monthHumidity = app.generate30DaySummary(connectionString, "GARDEN1", "DHT220", "humidity30dayData");
            Console.WriteLine(monthHumidity);
            string monthLight = app.generate30DaySummary(connectionString, "GARDEN1", "LIGHT0", "light30dayData", -1024.0, -1.0);
            Console.WriteLine(monthLight);
        }

        private string generatePast24Hours(string connectionString, string device, string sensor, string variableName, double offset = 0.0, double multiplier = 1.0)
        {
            SqlConnection conn = null;
            string[] labels = null;
            string[] data = null;
            string retVal = string.Empty;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

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
                                valueList.Add(string.Format("{0}", (Convert.ToDouble(results[0]) + offset) * multiplier));
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
                retVal = string.Format("var {0} = null;\nvar {0}Error = \"{1}\"", variableName, e.Message);
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
                stopWatch.Stop();
            }
            retVal += string.Format("\n var {0}Time = {1};", variableName, stopWatch.ElapsedMilliseconds);

            return retVal;
        }

        private string generate30DaySummary(string connectionString, string device, string sensor, string variableName, double offset = 0.0, double multiplier = 1.0)
        {
            SqlConnection conn = null;
            string[] labels = null;
            string[] mindata = null;
            string[] maxdata = null;
            string[] avgdata = null;
            string retVal = string.Empty;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                using (conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    List<string> labelList = new List<string>();
                    List<string> maxValueList = new List<string>();
                    List<string> avgValueList = new List<string>();
                    List<string> minValueList = new List<string>();
                    
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
                                maxValueList.Add(string.Format("{0}", (Convert.ToDouble(results[0]) + offset) * multiplier));
                                avgValueList.Add(string.Format("{0}", (Convert.ToDouble(results[1]) + offset) * multiplier));
                                minValueList.Add(string.Format("{0}", (Convert.ToDouble(results[2]) + offset) * multiplier));
                                labelList.Add(string.Format("{0:d}", results[3]));
                            }
                        }
                    }

                    while (labelList.Count < 30)
                    {
                        // pad with zeroes
                        maxValueList.Add("0");
                        avgValueList.Add("0");
                        minValueList.Add("0");
                        // pad with nothing
                        labelList.Add(string.Empty);
                    }

                    labels = labelList.Reverse<string>().ToArray();
                    maxdata = maxValueList.Reverse<string>().ToArray();
                    avgdata = avgValueList.Reverse<string>().ToArray();
                    mindata = minValueList.Reverse<string>().ToArray();
                }

                retVal = string.Format("var {0} = {{ labels : [\"{1}\"], datasets : [{{fillColor:\"rgba({2},0.33)\",strokeColor:\"rgba({2},1)\",data:[{3}]}}," +
                    "{{fillColor:\"rgba({4},0.33)\",strokeColor:\"rgba({4},1)\",data:[{5}]}}," +
                    "{{fillColor:\"rgba({6},0.33)\",strokeColor:\"rgba({6},1)\",data:[{7}]}}] }};",
                    variableName,
                    string.Join("\",\"", labels),
                    "205,50,50",
                    string.Join(",", maxdata),
                    "220,220,220",
                    string.Join(",", avgdata),
                    "28,22,125",
                    string.Join(",", mindata)
                    );
            }
            catch (Exception e)
            {
                retVal = string.Format("var {0} = null;\nvar {0}Error = \"{1}\"", variableName, e.Message);
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
                stopWatch.Stop();
            }
            retVal += string.Format("\n var {0}Time = {1};", variableName, stopWatch.ElapsedMilliseconds);

            return retVal;
        }

    }
}
