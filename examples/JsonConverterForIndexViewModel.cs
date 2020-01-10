//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:3.0.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonConverterGenerator
{
    public sealed class JsonConverterForIndexViewModel : JsonConverter<IndexViewModel>
    {
        private JsonConverterForIndexViewModel() {}
        
        public static readonly JsonConverterForIndexViewModel Instance = new JsonConverterForIndexViewModel();
        
        private static ReadOnlySpan<byte> ActiveOrUpcomingEventsBytes => new byte[22] { (byte)'A', (byte)'c', (byte)'t', (byte)'i', (byte)'v', (byte)'e', (byte)'O', (byte)'r', (byte)'U', (byte)'p', (byte)'c', (byte)'o', (byte)'m', (byte)'i', (byte)'n', (byte)'g', (byte)'E', (byte)'v', (byte)'e', (byte)'n', (byte)'t', (byte)'s' };
        private static ReadOnlySpan<byte> FeaturedCampaignBytes => new byte[16] { (byte)'F', (byte)'e', (byte)'a', (byte)'t', (byte)'u', (byte)'r', (byte)'e', (byte)'d', (byte)'C', (byte)'a', (byte)'m', (byte)'p', (byte)'a', (byte)'i', (byte)'g', (byte)'n' };
        private static ReadOnlySpan<byte> IsNewAccountBytes => new byte[12] { (byte)'I', (byte)'s', (byte)'N', (byte)'e', (byte)'w', (byte)'A', (byte)'c', (byte)'c', (byte)'o', (byte)'u', (byte)'n', (byte)'t' };
        private static ReadOnlySpan<byte> HasFeaturedCampaignBytes => new byte[19] { (byte)'H', (byte)'a', (byte)'s', (byte)'F', (byte)'e', (byte)'a', (byte)'t', (byte)'u', (byte)'r', (byte)'e', (byte)'d', (byte)'C', (byte)'a', (byte)'m', (byte)'p', (byte)'a', (byte)'i', (byte)'g', (byte)'n' };
        
        public override IndexViewModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Validate that the reader's cursor is at a start object token.
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }
            
            // Create returned object. This assumes type has public parameterless ctor.
            IndexViewModel value = new IndexViewModel();
            
            // Read all properties.
            while (true)
            {
                reader.Read();
                
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                
                // Only unescaped property names are allowed.
                ReadOnlySpan<byte> propertyName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                
                // Move reader cursor to property value.
                reader.Read();
                
                // Try to match property name with object properties (case sensitive).
                
                // Determine if JSON property matches 'ActiveOrUpcomingEvents'.
                if (ActiveOrUpcomingEventsBytes.SequenceEqual(propertyName))
                {
                    value.ActiveOrUpcomingEvents = JsonConverterForListActiveOrUpcomingEvent.Instance.Read(ref reader, typeToConvert, options);
                }
                // Determine if JSON property matches 'FeaturedCampaign'.
                else if (FeaturedCampaignBytes.SequenceEqual(propertyName))
                {
                    value.FeaturedCampaign = JsonConverterForCampaignSummaryViewModel.Instance.Read(ref reader, typeToConvert, options);
                }
                // Determine if JSON property matches 'IsNewAccount'.
                else if (IsNewAccountBytes.SequenceEqual(propertyName))
                {
                    value.IsNewAccount = reader.GetBoolean();
                }
            }
            
            return value;
        }
        
        public override void Write(Utf8JsonWriter writer, IndexViewModel value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            
            writer.WriteStartObject();
            
            writer.WritePropertyName(ActiveOrUpcomingEventsBytes);
            JsonConverterForListActiveOrUpcomingEvent.Instance.Write(writer, value.ActiveOrUpcomingEvents, options);
            
            writer.WritePropertyName(FeaturedCampaignBytes);
            JsonConverterForCampaignSummaryViewModel.Instance.Write(writer, value.FeaturedCampaign, options);
            
            writer.WriteBoolean(IsNewAccountBytes, value.IsNewAccount);
            
            writer.WriteBoolean(HasFeaturedCampaignBytes, value.HasFeaturedCampaign);
            
            writer.WriteEndObject();
        }
    }
}