﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class SubtitlingDescriptor : EventDescriptor
    {
        public List<SubtitlingInfo> SubtitleInfos = new List<SubtitlingInfo>();

        public void Parse(byte[] bytes)
        {
            var pos = 0;

            Tag = bytes[pos + 0];
            Length = bytes[pos + 1];

            if (Tag != 89)
                return;

            pos += 2;

            // 8 bytes for each SubtitlingInfo

            while (pos + 8 <= bytes.Length)
            {
                var info = new SubtitlingInfo();

                info.LanguageCode = MPEGTSCharReader.ReadString(bytes, pos, 3);
                info.SubtitlingType = bytes[pos + 3];
                info.CompositionPageId = Convert.ToInt32(((bytes[pos + 4]) << 8) + bytes[pos + 5]);
                info.AncillaryPageId = Convert.ToInt32(((bytes[pos + 6]) << 8) + bytes[pos + 7]);

                SubtitleInfos.Add(info);

                pos += 8;
            }
        }
    }
}
