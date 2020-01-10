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
    public sealed class JsonConverterForLocation : JsonConverter<Location>
    {
        private JsonConverterForLocation() {}
        
        public static readonly JsonConverterForLocation Instance = new JsonConverterForLocation();
        
        private static ReadOnlySpan<byte> IdBytes => new byte[2] { (byte)'I', (byte)'d' };
        private static ReadOnlySpan<byte> Address1Bytes => new byte[8] { (byte)'A', (byte)'d', (byte)'d', (byte)'r', (byte)'e', (byte)'s', (byte)'s', (byte)'1' };
        private static ReadOnlySpan<byte> Address2Bytes => new byte[8] { (byte)'A', (byte)'d', (byte)'d', (byte)'r', (byte)'e', (byte)'s', (byte)'s', (byte)'2' };
        private static ReadOnlySpan<byte> CityBytes => new byte[4] { (byte)'C', (byte)'i', (byte)'t', (byte)'y' };
        private static ReadOnlySpan<byte> StateBytes => new byte[5] { (byte)'S', (byte)'t', (byte)'a', (byte)'t', (byte)'e' };
        private static ReadOnlySpan<byte> PostalCodeBytes => new byte[10] { (byte)'P', (byte)'o', (byte)'s', (byte)'t', (byte)'a', (byte)'l', (byte)'C', (byte)'o', (byte)'d', (byte)'e' };
        private static ReadOnlySpan<byte> NameBytes => new byte[4] { (byte)'N', (byte)'a', (byte)'m', (byte)'e' };
        private static ReadOnlySpan<byte> PhoneNumberBytes => new byte[11] { (byte)'P', (byte)'h', (byte)'o', (byte)'n', (byte)'e', (byte)'N', (byte)'u', (byte)'m', (byte)'b', (byte)'e', (byte)'r' };
        private static ReadOnlySpan<byte> CountryBytes => new byte[7] { (byte)'C', (byte)'o', (byte)'u', (byte)'n', (byte)'t', (byte)'r', (byte)'y' };
        
        public override Location Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Validate that the reader's cursor is at a start object token.
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }
            
            // Create returned object. This assumes type has public parameterless ctor.
            Location value = new Location();
            
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
                
                // Determine if JSON property matches 'Id'.
                if (IdBytes.SequenceEqual(propertyName))
                {
                    value.Id = reader.GetInt32();
                }
                // Determine if JSON property matches 'Address1'.
                else if (Address1Bytes.SequenceEqual(propertyName))
                {
                    value.Address1 = reader.GetString();
                }
                // Determine if JSON property matches 'Address2'.
                else if (Address2Bytes.SequenceEqual(propertyName))
                {
                    value.Address2 = reader.GetString();
                }
                // Determine if JSON property matches 'City'.
                else if (CityBytes.SequenceEqual(propertyName))
                {
                    value.City = reader.GetString();
                }
                // Determine if JSON property matches 'State'.
                else if (StateBytes.SequenceEqual(propertyName))
                {
                    value.State = reader.GetString();
                }
                // Determine if JSON property matches 'PostalCode'.
                else if (PostalCodeBytes.SequenceEqual(propertyName))
                {
                    value.PostalCode = reader.GetString();
                }
                // Determine if JSON property matches 'Name'.
                else if (NameBytes.SequenceEqual(propertyName))
                {
                    value.Name = reader.GetString();
                }
                // Determine if JSON property matches 'PhoneNumber'.
                else if (PhoneNumberBytes.SequenceEqual(propertyName))
                {
                    value.PhoneNumber = reader.GetString();
                }
                // Determine if JSON property matches 'Country'.
                else if (CountryBytes.SequenceEqual(propertyName))
                {
                    value.Country = reader.GetString();
                }
            }
            
            return value;
        }
        
        public override void Write(Utf8JsonWriter writer, Location value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            
            writer.WriteStartObject();
            
            writer.WriteNumber(IdBytes, value.Id);
            
            writer.WriteString(Address1Bytes, value.Address1);
            
            writer.WriteString(Address2Bytes, value.Address2);
            
            writer.WriteString(CityBytes, value.City);
            
            writer.WriteString(StateBytes, value.State);
            
            writer.WriteString(PostalCodeBytes, value.PostalCode);
            
            writer.WriteString(NameBytes, value.Name);
            
            writer.WriteString(PhoneNumberBytes, value.PhoneNumber);
            
            writer.WriteString(CountryBytes, value.Country);
            
            writer.WriteEndObject();
        }
    }
}