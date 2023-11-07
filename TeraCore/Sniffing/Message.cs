﻿// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TeraCore.Sniffing
{
    public class Message
    {
        public DateTime Time { get; private set; }
        public MessageDirection Direction { get; private set; }
        public ArraySegment<byte> Data { get; private set; }
        public ushort OpCode => (ushort)(Data[Data.Offset] | Data[Data.Offset + 1] << 8);
        public ArraySegment<byte> Payload => Data.Slice(Data.Offset + 2, Data.Count - 2);

        public Message(DateTime time, MessageDirection direction, ArraySegment<byte> data)
        {
            Time = time;
            Direction = direction;
            Data = data;
        }
    }
}