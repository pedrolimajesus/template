using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Lok.AwareLive.Clients.Move.Model;

namespace Lok.AwareLive.Clients.Move
{
    public class XmlHelper
    {
        private static XNamespace NS = "http://www.w3.org/2000/svg";
        private static XNamespace MOVENS = "http://www.aware-live.com/namespaces/move";
        private static string AREA = "AREA";
        private static string LINE = "LINE";
        private static string UNKNOWN = "<unknown>";
        private static char[] DELIMETER_CHARS = { ',', '\t', '-' };

        public static LineDefinitionList XmlToLineDefinitionsList(string body)
        {
            var lineList = new LineDefinitionList();
            var doc = XDocument.Parse(body);

            // Handle all LINES
            var lines = from node in doc.Descendants(NS + "line")
                        select new
                        {
                            object_type = node.Attribute(MOVENS + "object-type").Value,
                            name = (node.Attribute("title") == null) ? null : node.Attribute("title").Value,
                            active = (node.Attribute(MOVENS + "active") == null) ? null : node.Attribute(MOVENS + "active").Value,
                            id = (node.Attribute(MOVENS + "id") == null) ? null : node.Attribute(MOVENS + "id").Value,
                            stroke = (node.Attribute("stroke") == null) ? null : node.Attribute("stroke").Value,
                            used = ((node.Attribute(MOVENS + "used") != null) ? (node.Attribute(MOVENS + "used").Value.ToLower() == "true") : false),
                            x1 = node.Attribute("x1").Value,
                            y1 = node.Attribute("y1").Value,
                            x2 = node.Attribute("x2").Value,
                            y2 = node.Attribute("y2").Value,
                            events = ((node.Attribute("name") == null) ? null : node.Attribute("name").Value)
                        };
            foreach (var line in lines)
            {
                if (line.object_type.ToUpper() != LINE) continue;

                var newLine = new LineDefinition();
                newLine.Id = int.Parse(line.id);
                newLine.Name = line.name;
                newLine.Active = (line.active.ToLower() == "true");
                newLine.Used = line.used;
                newLine.Color = line.stroke;
                newLine.LeftName = LeftName(line.events);
                newLine.RightName = RightName(line.events);
                newLine.Initial.X = (int) Convert.ToDouble(line.x1);
                newLine.Initial.Y = (int) Convert.ToDouble(line.y1);
                newLine.Terminal.X = (int) Convert.ToDouble(line.x2);
                newLine.Terminal.Y = (int) Convert.ToDouble(line.y2);
                lineList.LineDefinitions.Add(newLine);
            }

            return lineList;
        }

        private static string RightName(string text)
        {
            string DEFUALT = "[RIGHT]";

            try
            {
                if (string.IsNullOrEmpty(text)) return DEFUALT;
                string[] words = text.Split(DELIMETER_CHARS);
                if (words.Count() < 2) return DEFUALT;
                return words[1];
            }
            catch (Exception)
            {}

            return DEFUALT;
        }

        private static string LeftName(string text)
        {
            string DEFUALT = "[LEFT]";

            try
            {
                if (string.IsNullOrEmpty(text)) return DEFUALT;
                string[] words = text.Split(DELIMETER_CHARS);
                return words[0];
            }
            catch (Exception)
            {}

            return DEFUALT;
        }




        public static AreaDefinitionList XmlToAreaDefinitionsList(string body)
        {
            var areas = new AreaDefinitionList();
            var doc = XDocument.Parse(body);

            // Handle all ELLIPSES
            var ellipses = from node in doc.Descendants(NS + "ellipse")
                           select new
                           {
                               object_type = node.Attribute(MOVENS + "object-type").Value,
                               name = (node.Attribute("title") == null) ? null : node.Attribute("title").Value,
                               active = (node.Attribute(MOVENS + "active") == null) ? null : node.Attribute(MOVENS + "active").Value,
                               id = (node.Attribute(MOVENS + "id") == null) ? null : node.Attribute(MOVENS + "id").Value,
                               fill = node.Attribute("fill").Value,
                               used = ((node.Attribute(MOVENS + "used") != null) ? (node.Attribute(MOVENS + "used").Value.ToLower() == "true") : false),
                           };
            foreach (var ellipse in ellipses )
            {
                if (ellipse.object_type.ToUpper() != AREA) continue;

                var area = new AreaDefinition();
                area.Id = Convert.ToInt32(ellipse.id);
                area.Name = ellipse.name;
                area.Active = (ellipse.active.ToLower() == "true");
                area.Used = ellipse.used;
                area.Color = ellipse.fill;
                areas.AreaDefinitions.Add(area);
            }

            // Handle all CIRCLES
            var circles = from node in doc.Descendants(NS + "circle")
                           select new
                           {
                               object_type = node.Attribute(MOVENS + "object-type").Value,
                               name = (node.Attribute("title") == null) ? null : node.Attribute("title").Value,
                               active = (node.Attribute(MOVENS + "active") == null) ? null : node.Attribute(MOVENS + "active").Value,
                               id = (node.Attribute(MOVENS + "id") == null) ? null : node.Attribute(MOVENS + "id").Value,
                               fill = node.Attribute("fill").Value,
                               used = ((node.Attribute(MOVENS + "used") != null) ? (node.Attribute(MOVENS + "used").Value.ToLower() == "true") : false),
                           };
            foreach (var circle in circles)
            {
                if (circle.object_type.ToUpper() != AREA) continue;

                var area = new AreaDefinition();
                area.Id = Convert.ToInt32(circle.id);
                area.Name = circle.name;
                area.Active = (circle.active.ToLower() == "true");
                area.Used = circle.used;
                area.Color = circle.fill;
                areas.AreaDefinitions.Add(area);
            }

            // Handle all RECTANGLES
            var retangles = from node in doc.Descendants(NS + "rect")
                          select new
                          {
                              object_type = node.Attribute(MOVENS + "object-type").Value,
                              name = (node.Attribute("title") == null) ? null : node.Attribute("title").Value,
                              active = (node.Attribute(MOVENS + "active") == null) ? null : node.Attribute(MOVENS + "active").Value,
                              id = (node.Attribute(MOVENS + "id") == null) ? null : node.Attribute(MOVENS + "id").Value,
                              fill = node.Attribute("fill").Value,
                              used = ((node.Attribute(MOVENS + "used") != null) ? (node.Attribute(MOVENS + "used").Value.ToLower() == "true") : false),
                          };
            foreach (var retangle in retangles)
            {
                if (retangle.object_type.ToUpper() != AREA) continue;

                var area = new AreaDefinition();
                area.Id = Convert.ToInt32(retangle.id);
                area.Name = retangle.name;
                area.Active = (retangle.active.ToLower() == "true");
                area.Used = retangle.used;
                area.Color = retangle.fill;
                areas.AreaDefinitions.Add(area);
            }

            // Handle all PATHS
            var paths = from node in doc.Descendants(NS + "path")
                            select new
                            {
                                object_type = node.Attribute(MOVENS + "object-type").Value,
                                name = (node.Attribute("title") == null) ? null : node.Attribute("title").Value,
                                active = (node.Attribute(MOVENS + "active") == null) ? null : node.Attribute(MOVENS + "active").Value,
                                id = (node.Attribute(MOVENS + "id") == null) ? null : node.Attribute(MOVENS + "id").Value,
                                fill = node.Attribute("fill").Value,
                                used = ((node.Attribute(MOVENS + "used") != null) ? (node.Attribute(MOVENS + "used").Value.ToLower() == "true") : false),
                            };
            foreach (var path in paths)
            {
                if (path.object_type.ToUpper() != AREA) continue;

                var area = new AreaDefinition();
                area.Id = Convert.ToInt32(path.id);
                area.Name = path.name;
                area.Active = (path.active.ToLower() == "true");
                area.Used = path.used;
                area.Color = path.fill;
                areas.AreaDefinitions.Add(area);
            }

            return areas;

// <ellipse move:used="true" title="Desk Front" fill="#00FF19" move:active="true" move:id="2" cx="1113" 
// cy="842" rx="155" ry="80" move:object-type="area" id="2" transform="rotate(-29.9816 1113 842)"/>
        }
    }
}
