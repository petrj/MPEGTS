﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class MPEGTransportStreamPacket
    {
        public const byte MPEGTSSyncByte = 71;

        public byte SyncByte { get; set; }

        public bool TransportErrorIndicator { get; set; }
        public bool PayloadUnitStartIndicator { get; set; }
        public bool TransportPriority { get; set; }

        public ScramblingControlEnum ScramblingControl { get; set; }
        public AdaptationFieldControlEnum AdaptationFieldControl { get; set; }

        public int PID { get; set; }
        public byte ContinuityCounter { get; set; }

        public List<byte> Payload { get; set; } = new List<byte>();

        public static void WriteByteArrayToConsole(byte[] bytes)
        {
            var sb = new StringBuilder();
            var sbc = new StringBuilder();
            var sbb = new StringBuilder();
            var sbh = new StringBuilder();
            int c = 0;
            int row = 0;

            for (var i = 0; i < bytes.Length; i++)
            {
                sbb.Append($"{Convert.ToString(bytes[i], 2).PadLeft(8, '0'),9} ");
                sbh.Append($"{("0x"+Convert.ToString(bytes[i], 16)).PadLeft(8, ' ').ToUpper(),9} ");
                sb.Append($"{bytes[i].ToString(),9} ");


                if (bytes[i] >= 32 && bytes[i] <= 128)
                {
                    sbc.Append($"{Convert.ToChar(bytes[i]),9} ");
                }
                else
                {
                    sbc.Append($"{"",9} ");
                }
                c++;

                if (c >= 10)
                {
                    Console.WriteLine(sbb.ToString()+"  "+((row+1)*10).ToString().PadLeft(3));
                    Console.WriteLine(sbh.ToString());
                    Console.WriteLine(sb.ToString());
                    Console.WriteLine(sbc.ToString());
                    Console.WriteLine();
                    sb.Clear();
                    sbb.Clear();
                    sbc.Clear();
                    sbh.Clear();

                    c = 0;
                    row++;
                }
            }
            Console.WriteLine(sbb.ToString());
            Console.WriteLine(sbh.ToString());
            Console.WriteLine(sb.ToString());
            Console.WriteLine(sbc.ToString());
            Console.WriteLine();
        }

        public void WriteToConsole()
        {
            Console.WriteLine($"Sync Byte: {Convert.ToChar(SyncByte)} ({SyncByte.ToString()})");
            Console.WriteLine($"PID      : {PID}");
            Console.WriteLine($"TransportErrorIndicator  : {TransportErrorIndicator}");
            Console.WriteLine($"PayloadUnitStartIndicator: {PayloadUnitStartIndicator}");
            Console.WriteLine($"TransportPriority        : {TransportPriority}");

            Console.WriteLine($"ScramblingControl        : {ScramblingControl}");
            Console.WriteLine($"AdaptationFieldControl   : {AdaptationFieldControl}");

            Console.WriteLine($"ContinuityCounter        : {ContinuityCounter}");

            WriteByteArrayToConsole(Payload.ToArray());
        }
        public static string WriteBytesToString(List<byte> bytes)
        {
            var res = new StringBuilder();

            var sb = new StringBuilder();
            var sbc = new StringBuilder();
            var sbb = new StringBuilder();
            var sbp = new StringBuilder();
            var sbh = new StringBuilder();
            int c = 0;
            int row = 0;

            for (var i = 0; i < bytes.Count; i++)
            {
                sbp.Append($"{("["+Convert.ToString(i)+"]").PadLeft(8, ' '),9} ");
                sbb.Append($"{Convert.ToString(bytes[i], 2).PadLeft(8, '0'),9} ");
                sbh.Append($"{("0x" + Convert.ToString(bytes[i], 16)).PadLeft(8, ' '),9} ");
                sb.Append($"{bytes[i].ToString(),9} ");


                if (bytes[i] >= 32 && bytes[i] <= 128)
                {
                    sbc.Append($"{Convert.ToChar(bytes[i]),9} ");
                }
                else
                {
                    sbc.Append($"{"",9} ");
                }
                c++;

                if (c >= 10)
                {
                    res.AppendLine(sbp.ToString());
                    res.AppendLine(sbb.ToString());
                    res.AppendLine(sb.ToString());
                    res.AppendLine(sbh.ToString());
                    res.AppendLine(sbc.ToString());
                    res.AppendLine();
                    sb.Clear();
                    sbb.Clear();
                    sbc.Clear();
                    sbp.Clear();
                    sbh.Clear();

                    c = 0;
                    row++;
                }
            }
            res.AppendLine(sbp.ToString());
            res.AppendLine(sbb.ToString());
            res.AppendLine(sb.ToString());
            res.AppendLine(sbc.ToString());
            res.AppendLine();

            return res.ToString();
        }

        public static List<MPEGTransportStreamPacket> FindPacketsByPID(List<MPEGTransportStreamPacket> packets, int PID)
        {
            var res = new List<MPEGTransportStreamPacket>();
            bool firstPacketFound = false;

            foreach (var packet in packets)
            {
                if (packet.PID == PID)
                {
                    if (!firstPacketFound)
                    {
                        if (packet.PayloadUnitStartIndicator)
                        {
                            firstPacketFound = true;
                            res.Add(packet);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (packet.PayloadUnitStartIndicator)
                        {
                            break;
                        } else
                        {
                            res.Add(packet);
                        }
                    }
                }
            }

            return res;
        }

        public static Dictionary<int, List<byte>> GetAllPacketsPayloadBytesByPID(List<MPEGTransportStreamPacket> packets, long PID)
        {
            var res = new Dictionary<int, List<byte>>();
            var firstPacketFound = false;
            var currentKey = 0;

            foreach (var packet in packets)
            {
                if (packet.PID == PID)
                {
                    if (!firstPacketFound)
                    {
                        if (packet.PayloadUnitStartIndicator)
                        {
                            firstPacketFound = true;

                            res.Add(currentKey, new List<byte>());

                            res[currentKey].AddRange(packet.Payload);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (packet.PayloadUnitStartIndicator)
                        {
                            currentKey++;
                            res.Add(currentKey, new List<byte>());
                        }

                        res[currentKey].AddRange(packet.Payload);
                    }
                }
            }

            return res;
        }

        public static List<byte> GetPacketPayloadBytes(List<MPEGTransportStreamPacket> packets)
        {
            var result = new List<byte>();

            foreach (var packet in packets)
            {
                result.AddRange(packet.Payload);
            }

            return result;
        }

        public static List<byte> GetPacketPayloadBytesByPID(List<byte> bytes, int PID)
        {
            var packets = Parse(bytes);

            var filteredPackets = FindPacketsByPID(packets, PID);

            var result = new List<byte>();

            foreach (var packet in filteredPackets)
            {
                result.AddRange(packet.Payload);
            }

            return result;
        }

        public void ParseBytes(IEnumerable<byte> bytes)
        {
            Payload.Clear();
            int bytePos = 0;
            byte pidFirstByte = 0;
            foreach (var b in bytes)
            {
                switch (bytePos)
                {
                    case 0:
                        SyncByte = b;
                        break;
                    case 1:
                        TransportErrorIndicator = (b & 128) == 128;
                        PayloadUnitStartIndicator = (b & 64) == 64;
                        TransportPriority = (b & 32) == 32;
                        pidFirstByte = b;
                        break;
                    case 2:

                        var pidFirst5Bits = (pidFirstByte & 31) << 8;
                        PID = pidFirst5Bits + b;

                        break;
                    case 3:
                        var enumByte = (b & 192) >> 6;
                        ScramblingControl = (ScramblingControlEnum)enumByte;

                        enumByte = (b & 48) >> 4;
                        AdaptationFieldControl = (AdaptationFieldControlEnum) enumByte;

                        ContinuityCounter = Convert.ToByte(b & 15);

                        break;
                    default:
                        Payload.Add(b);
                        break;
                }
                bytePos++;
            }
        }

        /// <summary>
        /// Finding sync byte position
        /// </summary>
        /// <param name="bytes">byte buffer</param>
        /// <param name="startPos">first position</param>
        /// <param name="endPos">last position, -1 if end position corresponds to bytes size</param>
        /// <returns></returns>
        public static int FindSyncBytePosition(byte[] bytes, int startPos = 0, int endPos = -1)
        {
            var pos = startPos;
            if (endPos == -1)
            {
                endPos = bytes.Length;
            }
            var buff = new byte[188];
            while (pos + 188 < endPos)
            {
                if (bytes[pos] != MPEGTSSyncByte)
                {
                    // bad position
                    //Console.WriteLine("Looking for sync byte .....");

                    pos++;
                    continue;
                }

                // is next byte sync byte?
                if (bytes[pos + 188] != MPEGTSSyncByte)
                {
                    pos++;
                    continue;
                }

                return pos;
            }

            return -1;
        }

        /// <summary>
        /// Finding sync byte position
        /// </summary>
        /// <param name="bytes">byte buffer</param>
        /// <param name="startPos">first position</param>
        /// <param name="endPos">last position, -1 if end position corresponds to bytes size</param>
        /// <returns></returns>
        public static int FindSyncBytePosition(List<byte> bytes, int startPos = 0, int endPos = -1)
        {
            return FindSyncBytePosition(bytes.ToArray(), startPos, endPos);
        }

        public static List<MPEGTransportStreamPacket> Parse(byte[] bytes, int PIDFilter = -1)
        {
            return Parse(new List<byte>(bytes), PIDFilter);
        }

        public static List<MPEGTransportStreamPacket> Parse(List<byte> bytes, int PIDFilter = -1)
        {
            return Parse(bytes.ToArray(), 0, bytes.Count, PIDFilter);
        }

        public static List<MPEGTransportStreamPacket> Parse(byte[] bytes, int startPos, int endPos = -1, int PIDFilter = -1)
        {
            var pos = FindSyncBytePosition(bytes, startPos, endPos);
            if (endPos == -1)
            {
                endPos = bytes.Length;
            }

            var res = new List<MPEGTransportStreamPacket>();

            if (pos == -1)
                return res;

            while (pos + 188 < endPos)
            {
                var buff = new byte[188];
                for (var i=0;i<188;i++)
                {
                    buff[i] = bytes[pos + i];
                }

                var packet = new MPEGTransportStreamPacket();
                packet.ParseBytes(buff);

                if (
                        (PIDFilter == -1)  // add all packets
                        ||
                        ((PIDFilter != -1) && (packet.PID == PIDFilter))
                   )
                {
                    res.Add(packet);
                }

                pos += 188;
            }

            return res;
        }

        public static SortedDictionary<long, List<MPEGTransportStreamPacket>> SortPacketsByPID(IEnumerable<MPEGTransportStreamPacket> packets)
        {
            var packetsByPID = new SortedDictionary<long, List<MPEGTransportStreamPacket>>();

            foreach (var packet in packets)
            {
                if (!packetsByPID.ContainsKey(packet.PID))
                {
                    packetsByPID.Add(packet.PID, new List<MPEGTransportStreamPacket>());
                }

                packetsByPID[packet.PID].Add(packet);
            }

            return packetsByPID;
        }

        public static Dictionary<ServiceDescriptor, long> GetAvailableServicesMapPIDs(SDTTable sDTTable, PSITable pSITable)
        {
            var res = new Dictionary<ServiceDescriptor, long>();

            foreach (var sdi in sDTTable.ServiceDescriptors)
            {
                foreach (var pr in pSITable.ProgramAssociations)
                {
                    if (pr.ProgramNumber == sdi.ProgramNumber)
                    {
                        res.Add(sdi, pr.ProgramMapPID);
                        break;
                    }
                }
            }

            return res;
        }
    }
}

