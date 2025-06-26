using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BarcopoloWebApi.Helper 
{
    public class CurrencyDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                ReadOnlySpan<byte> span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                if (Utf8Parser.TryParse(span, out decimal number, out int bytesConsumed) && span.Length == bytesConsumed)
                    return number;

                if (decimal.TryParse(reader.GetString(), out number))
                    return number;
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDecimal();
            }

            throw new JsonException($"Unable to parse '{reader.TokenType}' as decimal.");
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            if (value == Math.Truncate(value)) 
            {
                try
                {
                    writer.WriteNumberValue(Convert.ToInt64(value));
                }
                catch (OverflowException) 
                {
                    writer.WriteNumberValue(value);
                }
            }
            else
            {
                writer.WriteNumberValue(value);
            }
        }
    }
}