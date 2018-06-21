using System;
using System.Collections.Generic;
using Lok.AwareLive.Clients.Look.Model;
using Newtonsoft.Json.Linq;

namespace Lok.AwareLive.Clients.Look
{
    internal static class JsonHelper
    {

        public static StateRec JsonToStateRec(string txt)
        {
            try
            {
                var jStateRec = JObject.Parse(txt);
                var rec = new StateRec();
                rec.DataService = StringToState((string)jStateRec["data_service"]);
                rec.Engine = StringToState((string)jStateRec["engine"]);
                return rec;
            }
            catch (Exception ex)
            {
                var newEx = new Exception("Invalid JSON data format returned for 'State'.", ex);
                throw newEx;
            }
        }

        public static SnapshotRec JsonToSnapshotRec(string txt)
        {
            try
            {
                var jSnapshotRec = JObject.Parse(txt);
                var rec = new SnapshotRec();
                rec.Path = (string) jSnapshotRec["path"];
                return rec;
            }
            catch (Exception ex)
            {
                var newEx = new Exception("Invalid JSON data format returned for 'SnapshotRec'.", ex);
                throw newEx;
            }
        }

        public static FacesRec JsonToFacesRec(string txt)
        {
            try
            {
                var jFacesRec = JObject.Parse(txt);
                var rec = new FacesRec();
                rec.primary_gender = StringToGender((string)jFacesRec["primary_gender"]);
                rec.majority_gender = StringToGender((string)jFacesRec["majority_gender"]);

                rec.faces = new List<FaceRec>();
                foreach (var jfaces in jFacesRec["objects"])
                {
                    var face = new FaceRec();
                    face.id = StringToInt((string)jfaces["id"]);
                    face.age =(int) StringToFloat((string)jfaces["age"]);
                    face.gender = StringToGender((string)jfaces["gender"]);
                    rec.faces.Add(face);
                }

                return rec;
            }
            catch (Exception ex)
            {
                var newEx = new Exception("Invalid JSON data format returned for 'State'.", ex);
                throw newEx;
            }
        }

        /*
         *     {
      "age": "25.5344",
      "clean_exit": "false",
      "enter_time": "2014-03-20T21:52:23",
      "exit_time": "2014-03-20T21:59:09",
      "gender": "1",
      "id": "688",
      "session_id": "5",
      "time_periods": [
        {
          "enter_time": "2014-03-20T21:52:23",
          "exit_time": "2014-03-20T21:52:43"
        },
        {
          "enter_time": "2014-03-20T21:52:45",
          "exit_time": "2014-03-20T21:52:55"
        },
        {
          "enter_time": "2014-03-20T21:53:18",
          "exit_time": "2014-03-20T21:53:39"
        },
        {
          "enter_time": "2014-03-20T21:53:40",
          "exit_time": "2014-03-20T21:54:01"
        },
        {
          "enter_time": "2014-03-20T21:54:13",
          "exit_time": "2014-03-20T21:54:13"
        },
        {
          "enter_time": "2014-03-20T21:55:42",
          "exit_time": "2014-03-20T21:56:14"
        },
        {
          "enter_time": "2014-03-20T21:59:02",
          "exit_time": "2014-03-20T21:59:09"
        }
      ]
    },
         * 
         *  "age": "39.5744680851064",
            "age_confidence": "0.480845452551689",
            "gender": "-1",
            "gender_confidence": "0.936170212765957", 
         * */

        public static FacesHistoricRec JsonToFacesHistoricRec(string txt)
        {
            try
            {
                var jFacesRec = JObject.Parse(txt);
                var rec = new FacesHistoricRec();

                rec.faces = new List<FaceHistoricRec>();
                foreach (var jfaces in jFacesRec["objects"])
                {
                    // load a specific person's record
                    var face = new FaceHistoricRec();
                    face.id = StringToInt((string)jfaces["id"]);
                    face.sessionId = StringToInt((string)jfaces["session_id"]);
                    face.cleanExit = StringToBool((string)jfaces["clean_exit"]);
                    face.age = (int)StringToFloat((string)jfaces["age"]);
                    face.ageConfidence = (double)StringToFloat((string)jfaces["age_confidence"]);
                    face.gender = StringToGender((string)jfaces["gender"]);
                    face.genderConfidence = (double)StringToFloat((string)jfaces["gender_confidence"]);
                    face.enterTime = StringToUtc((string) jfaces["enter_time"]).ToLocalTime();
                    face.exitTime = StringToUtc((string)jfaces["exit_time"]).ToLocalTime();

                    // load a person's time periods
                    face.TimePeriods = new List<TimePeriod>();
                    foreach (var jperiod in jfaces["time_periods"])
                    {
                        if (jperiod != null)
                        {
                            var period = new TimePeriod();
                            period.enterTime = StringToUtc((string) jperiod["enter_time"]).ToLocalTime();
                            period.exitTime = StringToUtc((string)jperiod["exit_time"]).ToLocalTime();
                            face.TimePeriods.Add(period);
                        }
                    }
                    rec.faces.Add(face);
                }

                return rec;
            }
            catch (Exception ex)
            {
                var newEx = new Exception("Invalid JSON data format returned for 'State'.", ex);
                throw newEx;
            }           
        }

        private static DateTime StringToUtc(string str)
        {
            var value = DateTime.Parse(str);
            return value;
        }

        private static bool StringToBool(string str)
        {
            var value = str.ToLower();
            return (value == "true");
        }

        private static int StringToInt(string str)
        {
            return int.Parse(str);
        }

        private static float StringToFloat(string str)
        {
            return float.Parse(str);
        }

        private static Gender StringToGender(string gender)
        {
            var genderInt = int.Parse(gender);
            if (genderInt == 1)
            {
                return Gender.Female;
            }
            else if (genderInt == -1)
            {
                return Gender.Male;
            }
            else
            {
                return Gender.Unknown;
            }
        }

        private static State StringToState(string txt)
        {
            switch (txt)
            {
                case ("stopped"): return State.stopped;
                case ("starting"): return State.starting;
                case ("running"): return State.running;
                case ("stopping"): return State.stopping;
                default: return State.unknown;
            }
        }

    }
}
