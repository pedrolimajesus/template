using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Remoting.Proxies;
using System.Text;

namespace Lok.AwareLive.Clients.Look.Model
{
    public class FacesRec
    {
        public Gender majority_gender { get; set; }
        public Gender primary_gender { get; set; }
        public List<FaceRec> faces { get; set; }
    }

    public class FaceRec
    {
        public int id { get; set; }
        public int age { get; set; }
        public Gender gender { get; set; }
    }

    public class FacesHistoricRec
    {
        public List<FaceHistoricRec> faces { get; set; }
    }

    public class FaceHistoricRec
    {
        public int id { get; set; }
        public int sessionId { get; set; }
        public int age { get; set; }
        public double ageConfidence { get; set; }
        public Gender gender { get; set; }
        public double genderConfidence { get; set; }
        public DateTime enterTime { get; set; }
        public DateTime exitTime { get; set; }
        public bool cleanExit { get; set; }
        public List<TimePeriod> TimePeriods { get; set; }
    }

    public class TimePeriod
    {
        public DateTime enterTime { get; set; }
        public DateTime exitTime { get; set; }
    }

    public enum Gender { Male, Female, Unknown }
}



/*
* 
{
"majority_gender": -1,
"objects": [
{
"age": "39.5744680851064",
"age_confidence": "0.480845452551689",
"gender": "-1",
"gender_confidence": "0.936170212765957",
"id": "0"
}
],
"primary_gender": -1
}
* */



