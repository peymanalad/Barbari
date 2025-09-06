using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BarcopoloWebApi.Helper
{
    public class TehranDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetDateTime();
            return TehranDateTime.Convert(value);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var tehranTime = TehranDateTime.Convert(value);
            writer.WriteStringValue(tehranTime);
        }
    }
}