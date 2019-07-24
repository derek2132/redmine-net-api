﻿/*
   Copyright 2011 - 2019 Adrian Popescu.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using RedmineClient.Exceptions;
using RedmineClient.Extensions;

namespace RedmineClient
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class RedmineJsonSerializer : IRedmineSerializer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        public T Deserialize<T>(string response) where T : new()
        {
            using (var sr = new StringReader(response))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    reader.DateTimeZoneHandling = DateTimeZoneHandling.Local;

                    var obj = Activator.CreateInstance<T>();

                    if (!(obj is IJsonSerializable ser))
                    {
                        throw new RedmineException($"object '{typeof(T)}' should implement IJsonSerializable.");
                    }

                    if (reader.Read())
                    {
                        reader.Read();
                        ser.ReadJson(reader);
                    }

                    return (T)ser;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        public PaginatedResult<T> DeserializeList<T>(string response) where T : class
        {
            using (var sr = new StringReader(response))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    reader.DateTimeZoneHandling = DateTimeZoneHandling.Local;

                    var total = 0;
                    var offset = 0;
                    var limit = 0;
                    List<T> list = null;

                    while (reader.Read())
                    {
                        if (reader.TokenType != JsonToken.PropertyName)
                        {
                            continue;
                        }

                        switch (reader.Value)
                        {
                            case RedmineKeys.TOTAL_COUNT:
                                total = reader.ReadAsInt32().GetValueOrDefault();
                                break;
                            case RedmineKeys.OFFSET:
                                offset = reader.ReadAsInt32().GetValueOrDefault();
                                break;
                            case RedmineKeys.LIMIT:
                                limit = reader.ReadAsInt32().GetValueOrDefault();
                                break;
                            default:
                                list = reader.ReadAsCollection<T>();
                                break;
                        }
                    }

                    return new PaginatedResult<T>(list, total, offset, limit);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public RedmineSerializerType Type { get; } = RedmineSerializerType.Json;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        public int Count<T>(string response) where T : new()
        {
            using (var sr = new StringReader(response))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    var total = 0;

                    while (reader.Read())
                    {
                        if (reader.TokenType != JsonToken.PropertyName)
                        {
                            continue;
                        }

                        if (!(reader.Value is RedmineKeys.TOTAL_COUNT))
                        {
                            continue;
                        }

                        total = reader.ReadAsInt32().GetValueOrDefault();
                        return total;
                    }

                    return total;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string Serialize<T>(T obj) where T : class
        {
            var sb = new StringBuilder();

            using (var sw = new StringWriter(sb))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.DateFormatHandling = DateFormatHandling.IsoDateFormat;

                    if (!(obj is IJsonSerializable ser))
                    {
                        throw new RedmineException($"object '{typeof(T)}' should implement IJsonSerializable.");
                    }

                    ser.WriteJson(writer);

                    return sb.ToString();
                }
            }
        }
    }
}