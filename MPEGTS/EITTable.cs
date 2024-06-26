﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class EITTable : DVBTTable
    {
        // ID:
        // 78     0x4E event_information_section - actual_transport_stream, present/following
        // 79     0x4F event_information_section - other_transport_stream, present/following
        // 80-95  0x50 to 0x5F event_information_section - actual_transport_stream, schedule
        // 96-111 0x60 to 0x6F event_information_section - other_transport_stream, schedule
        // https://www.etsi.org/deliver/etsi_en/300400_300499/300468/01.11.01_60/en_300468v011101p.pdf

        public int ServiceId { get; set; }
        public int TransportStreamID { get; set; }
        public int OriginalNetworkID { get; set; }
        public byte SegmentLastSectionNumber { get; set; }
        public byte LastTableID { get; set; }

        public List<EventItem> EventItems { get; set; } = new List<EventItem>();

        public override void Parse(List<byte> bytes)
        {
            if (bytes == null || bytes.Count < 5)
                return;

            var pointerField = bytes[0];
            var pos = 1;

            if (pointerField != 0)
            {
                pos = pos + pointerField;
            }

            if (bytes.Count < pos + 2)
                return;

            ID = bytes[pos]; // 4E ~ 78

            SectionSyntaxIndicator = ((bytes[pos + 1] & 128) == 128);
            Private = ((bytes[pos + 1] & 64) == 64);
            Reserved = Convert.ToByte((bytes[pos + 1] & 48) >> 4);
            SectionLength = Convert.ToInt32(((bytes[pos + 1] & 15) << 8) + bytes[pos + 2]);

            Data = new byte[SectionLength];
            CRC = new byte[4];

            Data[0] = 0;
            bytes.CopyTo(pointerField+1, Data, 1, SectionLength-1);
            bytes.CopyTo(pointerField + SectionLength, CRC, 0, 4);

            pos = pos + 3;

            ServiceId = (bytes[pos + 0] << 8) + bytes[pos + 1];

            Version = Convert.ToByte((bytes[pos + 2] & 62) >> 1);
            CurrentIndicator = (bytes[pos + 2] & 1) == 1;
            SectionNumber = bytes[pos + 3];
            LastSectionNumber = bytes[pos + 4];

            pos = pos + 5;

            TransportStreamID = (bytes[pos + 0] << 8) + bytes[pos + 1];
            OriginalNetworkID = (bytes[pos + 2] << 8) + bytes[pos + 3];

            pos = pos + 4;

            SegmentLastSectionNumber = bytes[pos + 0];
            LastTableID = bytes[pos + 1];

            pos = pos + 2;

            // pointer + table id + sect.length + descriptors - crc
            var posAfterDescriptors = 4 + SectionLength - 4;

            // reading descriptors
            while (pos < posAfterDescriptors)
            {
                var eventId = (bytes[pos + 0] << 8) + bytes[pos + 1];

                pos = pos + 2;

                var startTime = ParseTime(bytes, pos);

                pos = pos + 5;

                var duration = ParseDuration(bytes, pos);

                var finishTime = startTime.AddSeconds(duration);

                pos = pos + 3;

                var running_status = (bytes[pos + 0] & 224) >> 5;
                var freeCAMode = (bytes[pos + 0] & 16) >> 4;

                var allDescriptorsLength = ((bytes[pos + 0] & 15) << 8) + bytes[pos + 1];

                pos = pos + 2;

                if (allDescriptorsLength+pos>bytes.Count)
                {
                    allDescriptorsLength = bytes.Count - pos;
                }

                var allDescriptorsData = new byte[allDescriptorsLength];
                bytes.CopyTo(pos, allDescriptorsData, 0, allDescriptorsLength);

                var allDescriptorsPos = 0;

                ShortEventDescriptor shortEventDescriptor = null;
                ContentDescriptor contentDescriptor = null;
                var extendedEventDescriptors = new SortedDictionary<int, ExtendedEventDescriptor> ();

                while (allDescriptorsPos+1 <= allDescriptorsLength)
                {
                    var descriptorTag = allDescriptorsData[allDescriptorsPos];
                    if (descriptorTag == 0x4D)
                    {
                        shortEventDescriptor = ShortEventDescriptor.Parse(allDescriptorsData, allDescriptorsPos);
                        allDescriptorsPos += shortEventDescriptor.Length + 2;
                    }
                    else if (descriptorTag == 0x4E)
                    {
                        var extendedEventDescriptor = ExtendedEventDescriptor.Parse(allDescriptorsData, allDescriptorsPos);
                        extendedEventDescriptors[extendedEventDescriptor.DescriptorNumber] = extendedEventDescriptor;
                        allDescriptorsPos += extendedEventDescriptor.Length + 2;
                    }
                    else if (descriptorTag == 0x54)
                    {
                        contentDescriptor = ContentDescriptor.Parse(allDescriptorsData, allDescriptorsPos);
                        allDescriptorsPos += contentDescriptor.Length + 2;
                    }
                    else
                    {
                        if (descriptorTag == 0x50)
                        {
                            // Component descriptor
                        }
                        else
                        if (descriptorTag == 0x69)
                        {
                            // PDC descriptor
                        } else
                        if (descriptorTag == 0x55)
                        {
                            // Parental rating descriptor
                        }
                        else
                        {
                            Console.WriteLine($"EIT: unknown tag descriptor: {descriptorTag:X} hex ({descriptorTag} dec)");
                        }

                        var unknownDescriptorLength = allDescriptorsData[allDescriptorsPos + 1];
                        allDescriptorsPos += unknownDescriptorLength + 2;
                    }
                }

                if (shortEventDescriptor != null)
                {
                    var eventItem = EventItem.Create(eventId, ServiceId, startTime, finishTime, shortEventDescriptor);

                    if (extendedEventDescriptors.Count > 0)
                    {
                        foreach (var kvp in extendedEventDescriptors)
                        {
                            eventItem.AppendExtendedDescriptor(kvp.Value);
                        }
                    }

                    EventItems.Add(eventItem);
                }

                pos += allDescriptorsPos;
            }
        }

        public void WriteToConsole(bool detailed = true)
        {
            Console.WriteLine(WriteToString(detailed));
        }

        public string WriteToString(bool detailed = true)
        {
            var sb = new StringBuilder();

            if (detailed)
            {
                sb.AppendLine($"ID                    : {ID}");
                sb.AppendLine($"SectionSyntaxIndicator: {SectionSyntaxIndicator}");
                sb.AppendLine($"Private               : {Private}");
                sb.AppendLine($"Reserved              : {Reserved}");
                sb.AppendLine($"SectionLength         : {SectionLength}");
                sb.AppendLine($"CRC OK                : {CRCIsValid()}");

                sb.AppendLine($"__________________");
            }

            sb.AppendLine($"NetworkID              : {NetworkID}");
            sb.AppendLine($"ServiceId              : {ServiceId}");

            foreach (var desc in EventItems)
            {
                if (desc is EventItem ev)
                {
                    sb.AppendLine(ev.WriteToString());
                }
            }

            return sb.ToString();
        }
    }
}
