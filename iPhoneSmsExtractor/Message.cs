using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iPhoneSmsExtractor
{
    public class Message
    {
        public string iMessageFlags { get; set; }
        public string smsFlags { get; set; }
        public bool Sent
        {
            get
            {
                // 32773 - send error
                if (iMessageFlags == "36869" || iMessageFlags == "102405" || iMessageFlags == "32773" || smsFlags.Contains("3"))
                {
                    return true;
                }
                else if (iMessageFlags == "12289" || iMessageFlags == "77825" || smsFlags == "2")
                {
                    return false;
                }
                else
                {
                    throw new Exception("Found unknown flags: " + iMessageFlags + " (Madrid), " + smsFlags + " (SMS)");
                }
            }
        }

        public DateTime Timestamp { get; set; }
        public string Text { get; set; }
    }
}
