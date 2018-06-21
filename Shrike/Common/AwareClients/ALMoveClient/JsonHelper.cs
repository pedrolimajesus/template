using System;
using Lok.AwareLive.Clients.Move.Model;
using Newtonsoft.Json.Linq;

namespace Lok.AwareLive.Clients.Move
{
    internal static class JsonHelper
    {
        public static StateRec JsonToStateRec(string txt)
        {
            try
            {
                var jStateRec = JObject.Parse(txt);
                var rec = new StateRec();
                rec.DataService = StringToState((string) jStateRec["data_service"]);
                rec.Engine = StringToState((string) jStateRec["engine"]);
                return rec;
            }
            catch (Exception ex)
            {
                var newEx = new Exception("Invalid JSON data format returned for 'State'.", ex);
                throw newEx;
            }
        }


        public static HotspotEventsRec JsonToHotspotEventsRec(string txt)
        {
            try
            {
                var retval = new HotspotEventsRec();
                var jHotspotEvents = JObject.Parse(txt);

                foreach (var jRec in jHotspotEvents["area_events"])
                {
                    var rec = new HotspotEvent();
                    rec.Hotspot = IntToId((int) jRec["area"]["id"]);
                    rec.Object = IntToId((int) jRec["object"]["id"]);
                    rec.Time = DoubleToTime((double) jRec["time"]);
                    rec.Type = IntToType((int) jRec["type"]);
                    retval.HotspotEvents.Add(rec);
                }

                return retval;
            }
            catch (Exception ex)
            {
                var newEx = new Exception("Invalid JSON data format returned for 'State'.", ex);
                throw newEx;
            }
        }

        public static BorderEventsRec JsonToBorderEventsRec(string txt)
        {
            try
            {
                var retval = new BorderEventsRec();
                var jHotspotEvents = JObject.Parse(txt);

                // Parse out BORDER Events
                foreach (var jRec in jHotspotEvents["line_events"])
                {
                    var rec = new BorderEvent();
                    rec.Border = IntToId((int)jRec["line"]["id"]);
                    rec.Direction = IntToDirection((int)jRec["direction"]);
                    rec.Object = IntToId((int)jRec["object"]["id"]);
                    rec.Time = DoubleToTime((double)jRec["time"]);
                    retval.BorderEvents.Add(rec);
                }

                return retval;
            }
            catch (Exception ex)
            {
                var newEx = new Exception("Invalid JSON data format returned for 'State'.", ex);
                throw newEx;
            }
        }

        public static EventsRec JsonToEventsRec(string txt)
        {
            try
            {
                var retval = new EventsRec();
                var jHotspotEvents = JObject.Parse(txt);

                // Parse out HOTSPOT Events
                foreach (var jRec in jHotspotEvents["area_events"])
                {
                    var rec = new HotspotEvent();
                    rec.Hotspot = IntToId((int)jRec["area"]["id"]);
                    rec.Object = IntToId((int)jRec["object"]["id"]);
                    rec.Time = DoubleToTime((double)jRec["time"]);
                    rec.Type = IntToType((int)jRec["type"]);
                    retval.HotspotEvents.Add(rec);
                }

                // Parse out BORDER Events
                foreach (var jRec in jHotspotEvents["line_events"])
                {
                    var rec = new BorderEvent();
                    rec.Border = IntToId((int)jRec["line"]["id"]);
                    rec.Direction = IntToDirection((int)jRec["direction"]);
                    rec.Object = IntToId((int)jRec["object"]["id"]);
                    rec.Time = DoubleToTime((double)jRec["time"]);
                    retval.BorderEvents.Add(rec);
                }

                return retval;
            }
            catch (Exception ex)
            {
                var newEx = new Exception("Invalid JSON data format returned for 'State'.", ex);
                throw newEx;
            }
        }

        public static BorderDefinitionList JsonToBorderList(string txt)
        {
            try
            {
                var retval = new BorderDefinitionList();
                var jBorderList = JArray.Parse(txt);

                // Parse out BORDER Definitions
                foreach (var jRec in jBorderList)
                {
                    var rec = new BorderDefinition();
                    rec.Active = StringToBool((string) jRec["active"]);
                    rec.Id = (int) jRec["id"];
                    rec.Initial = JsonToCordinate(jRec["initial"]);
                    rec.LeftName = (string) jRec["left_name"];
                    rec.Name = (string) jRec["right_name"];
                    rec.Terminal = JsonToCordinate(jRec["terminal"]);
                    retval.BorderDefinitions.Add(rec);
                }

                return retval;
            }
            catch (Exception ex)
            {
                var newEx = new Exception("Invalid JSON data format returned for 'State'.", ex);
                throw newEx;
            }
        }

        public static HotspotDefinitionList JsonToHotspotList(string txt)
        {
            try
            {
                var retval = new HotspotDefinitionList();
                var jBorderList = JArray.Parse(txt);

                // Parse out HOTSPOT Definitions
                foreach (var jRec in jBorderList)
                {
                    var rec = new HotspotDefinition();
                    rec.Active = StringToBool((string)jRec["active"]);
                    rec.Id = (int)jRec["id"];
                    rec.MaskColor = JsonToMaskColor(jRec["mask_color"]);
                    rec.Name = (string)jRec["name"];
                    retval.HotspotDefinitions.Add(rec);
                }

                return retval;
            }
            catch (Exception ex)
            {
                var newEx = new Exception("Invalid JSON data format returned for 'State'.", ex);
                throw newEx;
            }
        }

        private static MaskColor JsonToMaskColor(JToken token)
        {
            var retval = new MaskColor();
            retval.Blue = (int) token["B"];
            retval.Green = (int) token["G"];
            retval.Red = (int) token["R"];
            return retval;
        }

        private static CordinateType JsonToCordinate(JToken token)
        {
            var retval = new CordinateType();
            retval.X = (int) token["x"];
            retval.Y = (int) token["y"];
            return retval;
        }

        private static bool StringToBool(string p)
        {
            if (p.ToLower() == "true") return true;
            if (p.ToLower() == "false") return false;

            // Bad input parameter provided
            throw new Exception("Invalid 'bool' value provided to JSON parser => " + p);
        }

        private static DirectionType IntToDirection(int directionCode)
        {
            switch (directionCode)
            {
                case (0): return DirectionType.left_to_right;
                case (1): return DirectionType.right_to_left;
                default: return DirectionType.unknown_direction;
            }
        }

        private static IdType IntToId(int p)
        {
            var id = new IdType();
            id.Id = p;
            return id;
        }

        private static DateTime DoubleToTime(double p)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddMilliseconds(p).ToLocalTime();
            return dtDateTime;
        }

        private static HotspotEventType IntToType(int eventCode)
        {
            switch (eventCode)
            {
                case (0) : return HotspotEventType.Enter;
                case (1) : return HotspotEventType.Exit;
                default  : return HotspotEventType.Unknown;
            }
        }

        private static State StringToState(string txt)
        {
            switch (txt)
            {
                case ("stopped"):   return State.stopped;
                case ("starting"):  return State.starting;
                case ("running"):   return State.running;
                case ("stopping"):  return State.stopping;
                default:            return State.unknown;
            }
        }
    }
}
