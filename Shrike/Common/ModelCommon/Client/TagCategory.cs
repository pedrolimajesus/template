using System.Drawing;

namespace Lok.Unik.ModelCommon.Client
{
    using AppComponents;

    using Raven.Imports.Newtonsoft.Json;
    using Raven.Imports.Newtonsoft.Json.Converters;

    public enum CategoryColor
    {
        Red,

        Blue,

        Green,

        Yellow,

        Cyan,

        Magenta,

        Orange,

        Lime,

        Default,

        Purple,

        Brown
    }

    public class TagCategory
    {
        [DocumentIdentifier]
        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public KnownColor Color { get; set; }
    }

}